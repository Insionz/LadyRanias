// Parametere for prod-miljøet. Bare id-er og navn, ingen hemmeligheter.
// Hemmelighetene og varslingsadressen leses fra miljøvariabler (nederst i fila).
using './main.bicep'

param environmentName = 'prod'

// Alltid på: besøkende skal ikke vente på at appen starter. Sett til 0 for å spare penger.
param minReplicas = 1

// Domenet uten www sendes hit
param canonicalHost = 'www.ladyranias.no'

// Fylles inn etter at domenene er koblet til første gang (README steg 12, del 3).
// Uten dette ville neste deploy fjernet domenene fra appen.
param customDomains = [
  // { name: 'www.ladyranias.no', certificateId: '<id til sertifikatet>' }
  // { name: 'ladyranias.no', certificateId: '<id til sertifikatet>' }
]

param acrName = 'crladyranias'

// az ad sp show --id <AZURE_CLIENT_ID fra GitHub-variablene> --query id -o tsv
param githubPrincipalId = '6e0a6ddf-4006-4667-b109-75d8437cb22c'

// Fra app-registreringen «LadyRanias» i LadyRanias-tenanten (de samme verdiene som i user secrets)
param entraInstance = 'https://ladyranias.ciamlogin.com/'
param entraTenantId = 'ec1cb7b0-a6c0-4ccd-ae62-f462228594dd'
param entraClientId = '19e03468-585f-40c5-a8f4-ad8bee817143'

param emailSenderAddress = 'DoNotReply@33e68950-6cce-46dc-9af3-35a8b7bb7f78.azurecomm.net'

// Hemmeligheter og varslingsadressen leses fra miljøvariabler i terminalen der du deployer.
// Da havner de verken i git, i fila eller i historikken til terminalen.
param entraClientSecret = readEnvironmentVariable('ENTRA_CLIENT_SECRET', '')
param emailConnectionString = readEnvironmentVariable('EMAIL_CONNECTION_STRING', '')
param emailNotifyAddress = readEnvironmentVariable('EMAIL_NOTIFY_ADDRESS', '')
