param name string
param location string = resourceGroup().location
param containerAppEnvironmentId string
param repositoryImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
param envVars array = []
param registry string
param minReplicas int = 1
param maxReplicas int = 1
param port int = 80
param probePort int = 5001
param externalIngress bool = false
param allowInsecure bool = true
param transport string = 'http'
param stickySessionAffinity string = 'none'
param registryUsername string
@secure()
param registryPassword string
param exists bool = false

resource existingApp 'Microsoft.App/containerApps@2023-05-01' existing = if (exists) {
  name: name
}

module app 'container-app.bicep' = {
  name: '${deployment().name}-update'
  params: {
    name: name
    location: location
    containerAppEnvironmentId: containerAppEnvironmentId
    repositoryImage: exists ? existingApp.properties.template.containers[0].image : repositoryImage
    envVars: envVars
    registry: registry
    minReplicas: minReplicas
    maxReplicas: maxReplicas
    port: port
    probePort: probePort
    externalIngress: externalIngress
    allowInsecure: allowInsecure
    transport: transport
    stickySessionAffinity: stickySessionAffinity
    customDomains: exists ? existingApp.properties.configuration.ingress.customDomains : []
    registryUsername: registryUsername
    registryPassword: registryPassword
  }
}

output fqdn string = app.outputs.fqdn
