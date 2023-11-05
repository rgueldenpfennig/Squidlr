@minLength(1)
@description('Primary location for all resources')
param location string = resourceGroup().location

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention')
@allowed([
  'staging'
  'prod'
])
param environmentName string

@description('Name of the application')
param applicationName string = 'squidlr'

@minLength(1)
@description('The API key')
@secure()
param apiKey string

@description('The proxy address')
param proxyAddress string

@description('The proxy user name')
@secure()
param proxyUserName string

@description('The proxy password')
@secure()
param proxyPassword string

@description('Specifies if the API app exists')
param apiAppExists bool = false

@description('Specifies if the web app exists')
param webAppExists bool = false

// create the azure container registry
resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: 'cr${applicationName}${environmentName}'
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
  }
}

// create the Azure Container Apps environment
module env 'environment.bicep' = {
  name: 'cae-${applicationName}-${environmentName}'
  params: {
    location: location
    baseName: '${applicationName}-${environmentName}'
  }
}

// environment depending settings
var environmentSettings = {
  staging: {
    aspNetEnvironment: 'Staging'
    minReplicas: 1
    maxReplicas: 1
  }
  prod: {
    aspNetEnvironment: 'Production'
    minReplicas: 2
    maxReplicas: 2
  }
}

// create the various config pairs
var shared_config = [
  {
    name: 'ASPNETCORE_ENVIRONMENT'
    value: environmentSettings[environmentName].aspNetEnvironment
  }
  {
    name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
    value: env.outputs.appInsightsConnectionString
  }
  {
    name: 'APPLICATION__APIKEY'
    value: apiKey
  }
]

// create the api app config pairs
var api_app_config = [
  {
    name: 'ASPNETCORE_URLS'
    value: 'http://*:80;http://*:5001'
  }
  {
    name: 'SQUIDLR__PROXYOPTIONS__PROXYADDRESS'
    value: proxyAddress
  }
  {
    name: 'SQUIDLR__PROXYOPTIONS__USERNAME'
    value: proxyUserName
  }
  {
    name: 'SQUIDLR__PROXYOPTIONS__PASSWORD'
    value: proxyPassword
  }
]

// create the web app config pairs
var web_app_config = [
  {
    name: 'APPLICATION__APIHOSTURI'
    value: 'https://${api.outputs.fqdn}'
  }
  {
    name: 'ASPNETCORE_URLS'
    value: 'http://*:80;http://*:5002'
  }
]

// create the backend API container app
module api 'core/container-app-upsert.bicep' = {
  name: 'ca-${applicationName}-${environmentName}-api'
  params: {
    name: 'ca-${applicationName}-${environmentName}-api'
    location: location
    registryPassword: acr.listCredentials().passwords[0].value
    registryUsername: acr.listCredentials().username
    containerAppEnvironmentId: env.outputs.id
    registry: acr.properties.loginServer
    envVars: union(shared_config, api_app_config)
    externalIngress: true
    allowInsecure: false
    transport: 'auto'
    probePort: 5001
    minReplicas: environmentSettings[environmentName].minReplicas
    maxReplicas: environmentSettings[environmentName].maxReplicas
    exists: apiAppExists
  }
}

// create the frontend Blazor web container app
module web 'core/container-app-upsert.bicep' = {
  name: 'ca-${applicationName}-${environmentName}-web'
  params: {
    name: 'ca-${applicationName}-${environmentName}-web'
    location: location
    registryPassword: acr.listCredentials().passwords[0].value
    registryUsername: acr.listCredentials().username
    containerAppEnvironmentId: env.outputs.id
    registry: acr.properties.loginServer
    envVars: union(shared_config, web_app_config)
    externalIngress: true
    allowInsecure: false
    port: 80
    probePort: 5002
    minReplicas: environmentSettings[environmentName].minReplicas
    maxReplicas: environmentSettings[environmentName].maxReplicas
    transport: 'auto'
    stickySessionAffinity: 'sticky'
    exists: webAppExists
  }
}

output acrName string = acr.name
output acrLoginServer string = acr.properties.loginServer

output apiContainerAppName string = api.name
output webContainerAppName string = web.name
