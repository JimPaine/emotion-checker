resource "azurerm_key_vault" "emotionfunc" {
  name                = "${var.resource_name}${random_id.emotionfunc.dec}vault"
  location            = "${azurerm_resource_group.emotionfunc.location}"
  resource_group_name = "${azurerm_resource_group.emotionfunc.name}"
  tenant_id           = "${data.azurerm_client_config.emotionfunc.tenant_id}"

  sku {
    name = "standard"
  }
}

resource "azurerm_key_vault_access_policy" "terraformclient" {
  vault_name          = "${azurerm_key_vault.emotionfunc.name}"
  resource_group_name = "${azurerm_key_vault.emotionfunc.resource_group_name}"

  tenant_id = "${data.azurerm_client_config.emotionfunc.tenant_id}"
  object_id = "${data.azurerm_client_config.emotionfunc.service_principal_object_id}"

  key_permissions = []

  secret_permissions = [
      "list",
      "set",
      "get",
    ]
}

resource "azurerm_key_vault_access_policy" "functionmsi" {
  vault_name          = "${azurerm_key_vault.emotionfunc.name}"
  resource_group_name = "${azurerm_key_vault.emotionfunc.resource_group_name}"

  tenant_id = "${data.azurerm_client_config.emotionfunc.tenant_id}"
  object_id = "${azurerm_function_app.emotionfunc.identity.0.principal_id}"

  key_permissions = []

  secret_permissions = [
    "get",
    "list",
  ]
}

resource "azurerm_key_vault_secret" "emotionfunc-face-key" {
  name      = "face-key"
  value     = "${azurerm_cognitive_account.emotionfunc.primary_access_key}"
  key_vault_id = "${azurerm_key_vault.emotionfunc.id}"
}

resource "azurerm_key_vault_secret" "emotionfunc-face-endpoint" {
  name      = "face-endpoint"
  value     = "http://${azurerm_container_group.emotionfunc.ip_address}:${azurerm_container_group.emotionfunc.container.0.port}/face/v1.0"
  key_vault_id = "${azurerm_key_vault.emotionfunc.id}"
}