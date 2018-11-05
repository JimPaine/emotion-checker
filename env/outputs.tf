data "azurerm_client_config" "emotionfunc" {}

data "azurerm_storage_account" "emotionfunc" {
  name                = "${azurerm_storage_account.emotionfunc.name}"
  resource_group_name = "${azurerm_resource_group.emotionfunc.name}"
}

output "file_endpoint" {
  value = "${data.azurerm_storage_account.emotionfunc.primary_file_endpoint}"
}

output "function_name" {
  value = "${azurerm_function_app.emotionfunc.name}"
}

output "account_name" {
  value = "${data.azurerm_storage_account.emotionfunc.name}"
}

output "primary_connection_string" {
  value = "${data.azurerm_storage_account.emotionfunc.primary_connection_string}"
}

output "primary_blob_endpoint" {
  value = "${data.azurerm_storage_account.emotionfunc.primary_blob_endpoint}"
}

output "instrumentation_key" {
  value = "${azurerm_application_insights.emotionfunc.instrumentation_key}"
}

output "app_id" {
  value = "${azurerm_application_insights.emotionfunc.app_id}"
}

output "function_principal_id" {
  value = "${azurerm_function_app.emotionfunc.identity.0.principal_id}"
}

output "resource_group" {
  value = "${azurerm_resource_group.emotionfunc.name}"
}

output "test" {
  value = "${azurerm_template_deployment.emotionfunc.*.outputs.["face_key"]}"
}
