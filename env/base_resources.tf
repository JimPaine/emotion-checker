resource "azurerm_resource_group" "emotionfunc" {
  name     = "${var.resource_name}"
  location = "northeurope"
}

resource "random_id" "emotionfunc" {
  keepers = {
    # Generate a new ID only when a new resource group is defined
    resource_group = "${azurerm_resource_group.emotionfunc.name}"
  }

  byte_length = 2
}
