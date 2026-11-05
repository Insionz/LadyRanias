// Parametere for test-miljøet. Bare id-er og navn, ingen hemmeligheter.
// Hemmelighetene og varslingsadressen leses fra miljøvariabler (nederst i fila).
using './main.bicep'

param environmentName = 'test'
param minReplicas = 0

param acrName = 'crladyranias'

// az ad sp show --id <AZURE_CLIENT_ID fra GitHub-variablene> --query id -o tsv
param githubPrincipalId = '<object id til github-ladyranias>'

// Fra app-registreringen «LadyRanias» i LadyRanias-tenanten (de samme verdiene som i user secrets)
param entraInstance = 'https://<underdomene>.ciamlogin.com/'
param entraTenantId = '<tenant id>'
param entraClientId = '<client id>'

param emailSenderAddress = '<DoNotReply@….azurecomm.net>'

// Hemmeligheter og varslingsadressen leses fra miljøvariabler i terminalen der du deployer.
// Da havner de verken i git, i fila eller i historikken til terminalen. Se README, steg 11 del 2.
// '' er standardverdien, så editoren ikke klager. Glemmer du å sette dem, stopper appen ved oppstart
// og sier hva som mangler (sjekkene fra steg 8 og 9).
param entraClientSecret = readEnvironmentVariable('ENTRA_CLIENT_SECRET', '')
param emailConnectionString = readEnvironmentVariable('EMAIL_CONNECTION_STRING', '')
param emailNotifyAddress = readEnvironmentVariable('EMAIL_NOTIFY_ADDRESS', '')
