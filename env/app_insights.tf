resource "azurerm_application_insights" "emotionfunc" {
  name                = "${var.resource_name}${random_id.emotionfunc.dec}insights"
  location            = "${azurerm_resource_group.emotionfunc.location}"
  resource_group_name = "${azurerm_resource_group.emotionfunc.name}"
  application_type    = "Web"
}
