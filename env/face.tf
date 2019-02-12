resource "azurerm_cognitive_account" "emotionfunc" {
  name                = "facearm"
  location            = "${azurerm_resource_group.emotionfunc.location}"
  resource_group_name = "${azurerm_resource_group.emotionfunc.name}"
  kind                = "Face"

  sku {
    name = "S0"
    tier = "Standard"
  }
}
