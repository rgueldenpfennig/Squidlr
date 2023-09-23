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
param customDomains array = []
param registryUsername string
@secure()
param registryPassword string

resource containerApp 'Microsoft.App/containerApps@2023-04-01-preview' ={
  name: name
  location: location
  properties:{
    managedEnvironmentId: containerAppEnvironmentId
    configuration: {
      activeRevisionsMode: 'single'
      secrets: [
        {
          name: 'container-registry-password'
          value: registryPassword
        }
      ]      
      registries: [
        {
          server: registry
          username: registryUsername
          passwordSecretRef: 'container-registry-password'
        }
      ]
      ingress: {
        external: externalIngress
        targetPort: port
        transport: transport
        allowInsecure: allowInsecure
        stickySessions: {
          affinity: stickySessionAffinity
        }
        customDomains: customDomains        
      }
    }
    template: {
      containers: [
        {
          image: repositoryImage
          name: name
          env: envVars
          resources: {
            cpu: json('.25')
            memory: '.5Gi'
          }
          probes: [
            {
              type: 'Readiness'
              httpGet: {
                port: probePort
                path: '/health'
                scheme: 'HTTP'
              }
            }            
            {
              type: 'Liveness'
              httpGet: {
                port: probePort
                path: '/health'
                scheme: 'HTTP'
              }
            }
          ]
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
      }
    }
  }
}

output fqdn string = !empty(containerApp.properties.configuration.ingress.customDomains) ? containerApp.properties.configuration.ingress.customDomains[0].name : containerApp.properties.configuration.ingress.fqdn
