targetScope = 'resourceGroup'

var suffix = uniqueString(subscription().id, resourceGroup().id)
param location string = resourceGroup().location

param ui_image string
param api_image string

resource logs 'Microsoft.OperationalInsights/workspaces@2021-06-01' = {
  name: 'logs${suffix}'
  location: location

  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource environment 'Microsoft.App/managedEnvironments@2022-01-01-preview' = {
  name: 'environment'
  location: location

  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logs.properties.customerId
        sharedKey: logs.listKeys().primarySharedKey
      }
    }
  }
}

resource face 'Microsoft.CognitiveServices/accounts@2022-03-01' = {
  name: 'face${suffix}'
  location: location
  kind: 'Face'

  sku: {
    name: 'S0'
  }

  properties: {}
}

var sku = {
  cpu: json('0.25')
  memory: '.5Gi'
}

var http_scale = {
  minReplicas: 0
  maxReplicas: 5
  rules: [
    {
      name: 'http-requests'
      http: {
        metadata: {
          concurrentRequests: '10'
        }
      }
    }
  ]
}

resource ui 'Microsoft.App/containerApps@2022-01-01-preview' = {
  name: 'ui'
  location: location

  properties: {
    managedEnvironmentId: environment.id
    configuration: {
      registries: []
      ingress: {
        external: true
        targetPort: 80
      }
    }

    template: {
      containers: [
        {
          name: 'ui'
          image: api_image
          env: [
            {
              name: 'api-uri'
              value: api.properties.configuration.ingress.fqdn
            }
          ]
          resources: sku
        }
      ]
      scale: http_scale
    }
  }
}

resource api 'Microsoft.App/containerApps@2022-01-01-preview' = {
  name: 'api'
  location: location

  properties: {
    managedEnvironmentId: environment.id
    configuration: {
      registries: []
      ingress: {
        external: false
        targetPort: 80
      }
    }

    template: {
      containers: [
        {
          name: 'api'
          image: api_image
          env: [
            {
              name: 'face-key'
              value: face.listKeys().key1
            }
            {
              name: 'face-endpoint'
              value: face.properties.endpoint
            }
          ]
          resources: sku
        }
      ]
      scale: http_scale
    }
  }
}
