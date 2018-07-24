provider "azurerm" {
  subscription_id = "${var.subscription_id}"
  version         = "~> 1.8"
  client_id       = "${var.client_id}"
  client_secret   = "${var.client_secret}"
  tenant_id       = "${var.tenant_id}"
}

provider "random" {
  version = "~> 1.3"
}
