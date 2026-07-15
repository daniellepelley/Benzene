# BenzeneStarter

A [Benzene](https://github.com/daniellepelley/Benzene) service on Azure Functions (isolated
worker), triggered by HTTP requests, generated from the `benzene.azure.http` template.

## Run it locally

Requires [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local).

```bash
func start
```

```bash
curl http://localhost:7071/hello/world
# {"message":"Hello world!"}
```

(`host.json` already clears the default `/api` route prefix, so no `/api/hello/world`.)

**`local.settings.json` holds secrets and machine-specific values - don't commit it if this
project becomes its own git repo** (add it to `.gitignore`).

## Deploy

Requires the [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli).

```bash
az group create --name my-function-rg --location eastus
az storage account create --name mystorageacct --location eastus --resource-group my-function-rg --sku Standard_LRS
az functionapp create --resource-group my-function-rg --consumption-plan-location eastus \
  --runtime dotnet-isolated --functions-version 4 --name my-function-app --storage-account mystorageacct

func azure functionapp publish my-function-app
```

## Where to go next

- **`HelloWorldMessageHandler.cs`** is where your logic goes - replace it, or add more handlers
  alongside it.
- **`StartUp.cs`**/**`Program.cs`** wire the trigger type(s) this Function App handles - add Event
  Hub, Kafka, or Service Bus triggers alongside HTTP if needed.
- Full guide, including Event Hub/Kafka/Service Bus triggers and Bicep deployment:
  [Azure Functions Setup](https://github.com/daniellepelley/Benzene/blob/main/docs/azure-functions.md)
