// Benzene example Azure Function App - HTTP-triggered isolated-worker Function App
// (see HttpFunction.cs/StartUp.cs) plus the Storage Account the Functions runtime
// requires and a workspace-based Application Insights resource for telemetry.
//
// This template has been hand-checked against the Bicep/ARM resource schemas but not
// run through `az bicep build`/`az deployment group what-if` or actually deployed -
// review before using in production. See ../../../docs/azure-functions.md for the
// equivalent step-by-step `az cli` deployment walkthrough, and
// ../../../docs/kubernetes-health-checks.md if you're deploying to AKS instead of
// Functions.
//
// Deploy:
//   az group create --name my-function-rg --location eastus
//   az deployment group create --resource-group my-function-rg --template-file main.bicep \
//     --parameters functionAppName=my-benzene-function
//
// This only provisions the HTTP trigger path the example project actually uses. If you
// add Event Hub, Kafka, or Service Bus triggers (see docs/azure-functions.md's "Event
// Hub, Kafka, and Service Bus triggers" section), you'll need to add the corresponding
// namespace/queue/topic resources and connection-string app settings yourself - they're
// deliberately not included here since the shipped example doesn't use them.

@description('Name of the Function App. Must be globally unique.')
param functionAppName string

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Storage account name. Must be globally unique, lowercase, 3-24 chars, no hyphens.')
param storageAccountName string = toLower('${take(replace(functionAppName, '-', ''), 17)}stor')

@description('Consumption (Y1) is the default; switch to EP1 (Elastic Premium) or a Dedicated SKU for VNet integration or no cold starts.')
param hostingPlanSku string = 'Y1'

var applicationInsightsName = '${functionAppName}-ai'
var logAnalyticsWorkspaceName = '${functionAppName}-logs'
var hostingPlanName = '${functionAppName}-plan'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
  }
}

// Workspace-based Application Insights - the current recommended mode (classic,
// non-workspace App Insights is being retired). See docs/monitoring.md and
// docs/cookbooks/logging-application-insights.md for what Benzene logs into this.
resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
    IngestionMode: 'LogAnalytics'
  }
}

resource hostingPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: hostingPlanName
  location: location
  sku: {
    name: hostingPlanSku
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp,linux'
  // System-assigned managed identity: Azure creates and rotates a credential for this app so it
  // can authenticate to Service Bus/Event Hubs/Cosmos DB/Storage via Entra ID RBAC instead of
  // connection strings. Grant roles to the principalId output below - see
  // ../../../docs/cookbooks/managed-identity.md for the per-service roles and role-assignment
  // Bicep/CLI snippets (this template's HTTP-only example doesn't need any yet; AzureWebJobsStorage
  // below stays key-based because the Consumption plan's content share requires it - see the
  // cookbook's Consumption-plan caveat).
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    httpsOnly: true
    siteConfig: {
      // .NET 10 has no published Azure Functions isolated-worker runtime identifier at
      // the time of writing (see the equivalent dotnet8-for-.NET-10 caveat in
      // ../../Aws/Benzene.Examples.Aws/template.yaml) - verify the current supported
      // linuxFxVersion for your target framework before deploying; adjust if Azure has
      // published a DOTNET-ISOLATED|10.0 (or later) worker image by the time you read this.
      linuxFxVersion: 'DOTNET-ISOLATED|10.0'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.properties.ConnectionString
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
      ]
    }
  }
}

@description('Default hostname of the deployed Function App.')
output functionAppHostName string = functionApp.properties.defaultHostName

@description('Object id of the Function App\'s system-assigned managed identity - the principal to grant RBAC roles to (see docs/cookbooks/managed-identity.md).')
output functionAppPrincipalId string = functionApp.identity.principalId

@description('Application Insights connection string (also set as an app setting on the Function App).')
output applicationInsightsConnectionString string = applicationInsights.properties.ConnectionString
