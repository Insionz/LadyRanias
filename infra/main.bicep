// =====================================================================================
// LadyRanias: alt som trengs for å kjøre appen i ett miljø (test eller prod).
//
// Bicep beskriver hvordan ting SKAL være. Kjører du den på nytt, sammenligner Azure med det
// som finnes, og endrer bare det som er annerledes. Prod (steg 12) er samme fil med andre parametere.
//
// Deploy:  az deployment group create -g rg-ladyranias -f infra/main.bicep -p infra/test.bicepparam
// =====================================================================================

@description('test eller prod. Brukes i navnene, så miljøene ikke blander seg.')
@allowed(['test', 'prod'])
param environmentName string

param location string = resourceGroup().location

@description('Navnet på container registryet fra steg 10 (uten hash og uten .azurecr.io).')
param acrName string

@description('Hvilken tag som deployes. CI bytter til commit-SHA med az containerapp update.')
param imageTag string = 'main'

@description('0 = slås av når ingen bruker den (billig, men treg første gang). 1 = alltid på.')
@minValue(0)
param minReplicas int = 0

@description('Object id til service principal for github-ladyranias. Får lov til å deploye nye versjoner av appen.')
param githubPrincipalId string

@description('Hovedadressen, for eksempel www.ladyranias.no. Besøk på domenet uten www sendes hit. Tom = ingen omdirigering.')
param canonicalHost string = ''

@description('Egne domener med sertifikat: [{ name, certificateId }]. Fylles inn etter at domenet er koblet til første gang (README steg 12). Uten denne ville en ny deploy fjernet domenene.')
param customDomains array = []

// ---- Innlogging (Entra External ID, steg 8). Id-er, ikke hemmeligheter. ----
param entraInstance string
param entraTenantId string
param entraClientId string

// ---- E-post (steg 9) ----
param emailSenderAddress string

@description('Hvem som får e-postvarsel om nye kontaktmeldinger. Leses fra en miljøvariabel ved deploy, så adressen ikke ligger i git.')
param emailNotifyAddress string

// ---- Hemmeligheter: @secure() gjør at de aldri vises i logger eller i portalen ----
@secure()
param entraClientSecret string

@secure()
param emailConnectionString string

// =====================================================================================
// Felles for alle miljøer: logging og Container Apps-miljøet
// =====================================================================================

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'log-ladyranias'
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource containerEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: 'cae-ladyranias'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

// Registryet finnes allerede (steg 10). «existing» betyr: ikke lag det, bare slå det opp.
resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: acrName
}

// =====================================================================================
// Per miljø: storage, identitet, roller og selve appen
// =====================================================================================

// Navn må være unikt i hele Azure. uniqueString lager en fast «hash» av ressursgruppen.
var storageName = take('stlr${environmentName}${uniqueString(resourceGroup().id)}', 24)

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageName
  location: location
  sku: { name: 'Standard_LRS' }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: false // ingen nøkler: bare innlogging med identitet
    supportsHttpsTrafficOnly: true
  }
}

// Identiteten appen kjører som. Laget før appen, så rollene er på plass når appen starter.
resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-ladyranias-${environmentName}'
  location: location
}

// Innebygde roller i Azure har faste id-er.
var roles = {
  acrPull: '7f951dda-4ed3-4680-a7ca-43fe172d538d'
  storageTableDataContributor: '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3'
  storageBlobDataContributor: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
  storageQueueDataContributor: '974c5e8b-45b9-4653-ba55-5f855dd0fb88'
  contributor: 'b24988ac-6180-42a0-ab88-20f7382dd24c'
}

resource acrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(registry.id, identity.id, roles.acrPull)
  scope: registry
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.acrPull)
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// Én rolle per tjeneste i storage-kontoen. «for» lager tre tildelinger fra lista.
resource storageRoles 'Microsoft.Authorization/roleAssignments@2022-04-01' = [
  for role in [roles.storageTableDataContributor, roles.storageBlobDataContributor, roles.storageQueueDataContributor]: {
    name: guid(storage.id, identity.id, role)
    scope: storage
    properties: {
      roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', role)
      principalId: identity.properties.principalId
      principalType: 'ServicePrincipal'
    }
  }
]

// Application Insights (steg 13): forespørsler, feil, kall til Storage og loggene fra appen.
// Dataene lagres i det felles Log Analytics-workspacet, så alt kan søkes i samme sted.
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-ladyranias-${environmentName}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

resource app 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'ca-ladyranias-${environmentName}'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${identity.id}': {} }
  }
  properties: {
    managedEnvironmentId: containerEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
        allowInsecure: false // http omdirigeres til https av Azure
        customDomains: [
          for domain in customDomains: {
            name: domain.name
            certificateId: domain.certificateId
            bindingType: 'SniEnabled'
          }
        ]
      }
      registries: [
        {
          server: registry.properties.loginServer
          identity: identity.id // henter imaget med identiteten, uten passord
        }
      ]
      secrets: [
        { name: 'entra-client-secret', value: entraClientSecret }
        { name: 'email-connection-string', value: emailConnectionString }
        { name: 'appinsights-connection-string', value: appInsights.properties.ConnectionString }
      ]
    }
    template: {
      containers: [
        {
          name: 'ladyranias'
          image: '${registry.properties.loginServer}/ladyranias:${imageTag}'
          resources: { cpu: json('0.5'), memory: '1Gi' }
          // __ i navnet betyr : i konfigurasjonen. secretRef henter verdien fra secrets over.
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
            { name: 'AZURE_CLIENT_ID', value: identity.properties.clientId }
            { name: 'Storage__AccountName', value: storage.name }
            { name: 'ForwardedHeaders__TrustAllProxies', value: 'true' }
            { name: 'CanonicalHost', value: canonicalHost }
            { name: 'AzureAd__Instance', value: entraInstance }
            { name: 'AzureAd__TenantId', value: entraTenantId }
            { name: 'AzureAd__ClientId', value: entraClientId }
            { name: 'AzureAd__ClientSecret', secretRef: 'entra-client-secret' }
            { name: 'Email__ConnectionString', secretRef: 'email-connection-string' }
            { name: 'Email__SenderAddress', value: emailSenderAddress }
            { name: 'Email__NotifyAddress', value: emailNotifyAddress }
            // Finnes denne, sender appen telemetri til Application Insights (se TelemetryComponent).
            { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', secretRef: 'appinsights-connection-string' }
            // Navnet appen får i Application Insights (cloud_RoleName).
            { name: 'OTEL_SERVICE_NAME', value: 'ladyranias-${environmentName}' }
          ]
          // Azure spør appen jevnlig om den har det bra (steg 13).
          // Startup:   venter til appen har startet før de andre slår inn (opptil 10 × 3 s = 30 s).
          // Liveness:  svarer ikke /health/live 3 ganger på rad, startes containeren på nytt.
          // Readiness: svarer ikke /health/ready (Table Storage), får containeren ikke trafikk før den svarer igjen.
          probes: [
            {
              type: 'Startup'
              httpGet: { path: '/health/live', port: 8080 }
              periodSeconds: 3
              failureThreshold: 10
            }
            {
              type: 'Liveness'
              httpGet: { path: '/health/live', port: 8080 }
              periodSeconds: 30
              failureThreshold: 3
            }
            {
              type: 'Readiness'
              httpGet: { path: '/health/ready', port: 8080 }
              periodSeconds: 15
              timeoutSeconds: 5
              failureThreshold: 3
            }
          ]
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: 1 // grensen i kontaktskjemaet og bakgrunnsjobben ligger i minnet til én instans
      }
    }
  }
  dependsOn: [acrPull] // appen kan ikke hente imaget før rollen finnes
}

// GitHub får lov til å deploye nye versjoner av akkurat denne appen, og ikke noe annet.
resource githubDeploy 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(app.id, githubPrincipalId, roles.contributor)
  scope: app
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', roles.contributor)
    principalId: githubPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output url string = 'https://${app.properties.configuration.ingress.fqdn}'
output containerAppName string = app.name
output storageAccountName string = storage.name
