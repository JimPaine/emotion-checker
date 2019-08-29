provider "azurerm" {
  subscription_id = var.subscriptionid
  version         = "~> 1.33"
  client_id       = var.clientid
  client_secret   = var.clientsecret
  tenant_id       = var.tenantid
}

provider "random" {
  version = "~> 2.1"
}

provider "acme" {
  server_url = "https://acme-v02.api.letsencrypt.org/directory"
  version = "~>1.2"
}

provider "dnsimple" {
  token = var.dnsimple_auth_token
  account = var.dnsimple_account
}
