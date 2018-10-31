terraform {
  backend "azurerm" {
    lock    = false
    #example properties
    #resource_group_name  = ""
    #storage_account_name = ""
    #container_name       = ""
    #key                  = ""
    #access_key           = ""
  }
}