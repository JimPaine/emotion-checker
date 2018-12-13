resource "azurerm_storage_account" "emotionfunc" {
  name                     = "${var.resource_name}${random_id.emotionfunc.dec}store"
  resource_group_name      = "${azurerm_resource_group.emotionfunc.name}"
  location                 = "${azurerm_resource_group.emotionfunc.location}"
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_app_service_plan" "emotionfunc" {
  name                = "azure-functions-${var.resource_name}-service-plan"
  location            = "${azurerm_resource_group.emotionfunc.location}"
  resource_group_name = "${azurerm_resource_group.emotionfunc.name}"
  kind                = "FunctionApp"

  sku {
    tier = "Dynamic"
    size = "Y1"
  }
}

resource "azurerm_function_app" "emotionfunc" {
  name                      = "${var.resource_name}${random_id.emotionfunc.dec}"
  location                  = "${azurerm_resource_group.emotionfunc.location}"
  resource_group_name       = "${azurerm_resource_group.emotionfunc.name}"
  app_service_plan_id       = "${azurerm_app_service_plan.emotionfunc.id}"
  storage_connection_string = "${azurerm_storage_account.emotionfunc.primary_connection_string}"
  
  version = "~2"

  app_settings {
    APPINSIGHTS_INSTRUMENTATIONKEY = "${azurerm_application_insights.emotionfunc.instrumentation_key}"
    face-key = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.emotionfunc-face-key.id})"
    face-endpoint = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.emotionfunc-face-endpoint.id})"
    proxyIndex = "https://jimpaine.github.io/index.html"
    proxyFiles = "https://jimpaine.github.io/{file}"
  }

  identity {
    type = "SystemAssigned"
  }
}