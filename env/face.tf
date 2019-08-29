resource "azurerm_cognitive_account" "emotionfunc" {
  name                = "facearm"
  location            = azurerm_resource_group.emotionfunc.location
  resource_group_name = azurerm_resource_group.emotionfunc.name
  kind                = "Face"

  sku {
    name = "S0"
    tier = "Standard"
  }
}

resource "azurerm_container_group" "emotionfunc" {
  name                = "face_container"
  location            = azurerm_resource_group.emotionfunc.location
  resource_group_name = azurerm_resource_group.emotionfunc.name
  ip_address_type     = "public"
  os_type             = "Linux"

  image_registry_credential {
    username = var.registry_username
    password = var.registry_password
    server = "containerpreview.azurecr.io"
  }

  container {
    name   = "hw"
    image  = "containerpreview.azurecr.io/microsoft/cognitive-services-face"
    cpu    = "4"
    memory = "4"
    
    ports {
      port     = 5000
      protocol = "TCP"
    }

    environment_variables = {
      Eula = "accept"
      Billing = azurerm_cognitive_account.emotionfunc.endpoint
      ApiKey = azurerm_cognitive_account.emotionfunc.primary_access_key
    }
  }
}
