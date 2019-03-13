resource "azurerm_app_service_custom_hostname_binding" "emotionfunc" {
  hostname            = "${var.hostname}"
  app_service_name    = "${azurerm_function_app.emotionfunc.name}"
  resource_group_name = "${azurerm_resource_group.emotionfunc.name}"
}


resource "azurerm_template_deployment" "emotionfunc" {
  name                = "${var.resource_name}${random_id.emotionfunc.dec}cert"
  resource_group_name = "${azurerm_resource_group.emotionfunc.name}"

  template_body = <<DEPLOY
    {
        "$schema": "https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#",
        "contentVersion": "1.0.0.0",
        "parameters" : {
            "certificateName" : {
                "type": "string"
            },
            "existingAppLocation" : {
                "type": "string"
            },
            "existingKeyVaultId" : {
                "type": "string"
            },
            "existingKeyVaultSecretName" : {
                "type": "string"
            },
            "existingServerFarmId" : {
                "type": "string"
            },
            "existingWebAppName" : {
                "type": "string"
            },
            "hostname" : {
                "type": "string"
            }            
        },
        "resources": [
        {
            "type": "Microsoft.Web/certificates",
            "name": "[parameters('certificateName')]",
            "apiVersion": "2016-03-01",
            "location": "[parameters('existingAppLocation')]",
            "properties": {
                "keyVaultId": "[parameters('existingKeyVaultId')]",
                "keyVaultSecretName": "[parameters('existingKeyVaultSecretName')]",
                "serverFarmId": "[parameters('existingServerFarmId')]"
            }
        },
        {
            "type": "Microsoft.Web/sites",
            "name": "[parameters('existingWebAppName')]",
            "apiVersion": "2016-03-01",
            "location": "[parameters('existingAppLocation')]",
            "dependsOn": [
                "[resourceId('Microsoft.Web/certificates', parameters('certificateName'))]"
            ],
            "properties": {
                "name": "[parameters('existingWebAppName')]",
                "hostNameSslStates": [
                {
                    "name": "[parameters('hostname')]",
                    "sslState": "SniEnabled",
                    "thumbprint": "[reference(resourceId('Microsoft.Web/certificates', parameters('certificateName'))).Thumbprint]",
                    "toUpdate": true
                }
                ]
            }
        }
    ]
}
DEPLOY

  parameters {
      "certificateName" = "${var.hostname}"
      "existingAppLocation" = "${azurerm_resource_group.emotionfunc.location}"
      "existingKeyVaultId" = "${azurerm_key_vault.emotionfunc.id}"
      "existingKeyVaultSecretName" = "${azurerm_key_vault_secret.cert.name}"
      "existingServerFarmId" = "${azurerm_app_service_plan.emotionfunc.id}"
      "existingWebAppName" = "${azurerm_function_app.emotionfunc.name}"
      "hostname" = "${azurerm_app_service_custom_hostname_binding.emotionfunc.hostname}"
  }

  deployment_mode = "Incremental"
}