resource "azurerm_storage_container" "emotionfunc" {
  name                  = "proxy"
  resource_group_name   = azurerm_resource_group.emotionfunc.name
  storage_account_name  = azurerm_storage_account.emotionfunc.name
  container_access_type = "container"
}
