resource "azurerm_template_deployment" "emotionfunc" {
  name                = "facearm"
  resource_group_name = "${azurerm_resource_group.emotionfunc.name}"

  //count = "${var.face_api_instances}"

  template_body = <<DEPLOY
{
    "$schema": "http://schema.management.azure.com/schemas/2014-04-01-preview/deploymentTemplate.json#",
    "contentVersion": "1.0.0.0",
    "parameters": {
        "name": {
            "type": "String"
        },
        "location": {
            "type": "String"
        },
        "apiType": {
            "type": "String"
        },
        "sku": {
            "type": "String"
        }
    },
    "resources": [
        {
            "type": "Microsoft.CognitiveServices/accounts",
            "sku": {
                "name": "[parameters('sku')]"
            },
            "kind": "[parameters('apiType')]",
            "name": "[parameters('name')]",
            "apiVersion": "2016-02-01-preview",
            "location": "[parameters('location')]",
            "properties": {}
        }
    ],
    "outputs": {
        "face_endpoint": {
            "type": "string",
            "value": "[reference(parameters('name')).Endpoint]"
        },
        "face_key": {            
            "type": "string",
            "value": "[listKeys(resourceId('Microsoft.CognitiveServices/accounts', parameters('name')), providers('Microsoft.CognitiveServices', 'accounts').apiVersions[0]).key1]"
        }
    }
}
  DEPLOY

  parameters {
    "name"     = "face${count.index}${random_id.emotionfunc.dec}"
    "location" = "${azurerm_resource_group.emotionfunc.location}"
    "apiType"  = "Face"
    "sku"      = "S0"
  }

  deployment_mode = "Incremental"
}
