resource "azurerm_key_vault" "emotionfunc" {
  name                = "${var.resource_name}${random_id.emotionfunc.dec}vault"
  location            = "${azurerm_resource_group.emotionfunc.location}"
  resource_group_name = "${azurerm_resource_group.emotionfunc.name}"
  tenant_id           = "${data.azurerm_client_config.emotionfunc.tenant_id}"

  sku {
    name = "standard"
  }

  access_policy {
    tenant_id = "${data.azurerm_client_config.emotionfunc.tenant_id}"

    # known bug need to run as service principal
    # https://github.com/terraform-providers/terraform-provider-azurerm/issues/656
    object_id = "${data.azurerm_client_config.emotionfunc.service_principal_object_id}"

    key_permissions = []

    secret_permissions = [
      "list",
      "set",
      "get",
    ]
  }

  access_policy {
    tenant_id = "${data.azurerm_client_config.emotionfunc.tenant_id}"
    object_id = "${azurerm_function_app.emotionfunc.identity.0.principal_id}"

    key_permissions = []

    secret_permissions = [
      "get",
      "list",
    ]
  }
}

resource "azurerm_key_vault_secret" "emotionfunc-face-key" {
  name      = "face-key"
  value     = "${azurerm_template_deployment.emotionfunc.outputs["face_key"]}"
  vault_uri = "${azurerm_key_vault.emotionfunc.vault_uri}"
}

resource "azurerm_key_vault_secret" "emotionfunc-face-endpoint" {
  name      = "face-endpoint"
  value     = "${azurerm_template_deployment.emotionfunc.outputs["face_endpoint"]}"
  vault_uri = "${azurerm_key_vault.emotionfunc.vault_uri}"
}
