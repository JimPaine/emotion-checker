# emotion-checker

## What was the point?

Firstly, off the back of a few discussions I thought it would be great to get some real end to end demos, including the environment and source being built and deployed.

I also wanted to be able to be run it from any platform. So to cover that there is no PowerShell, ARM templates [(may be a little ARM but ssshhh)](https://www.terraform.io/docs/providers/azurerm/r/template_deployment.html) or .Net Framework

Then I wanted to show / learn how an Azure Function App could have a [managed service identity](https://docs.microsoft.com/en-us/azure/app-service/app-service-managed-service-identity) generated at runtime / build time and have the relevant policies setup in Azure Key Vault, which for this example is storing the keys needed to access the [Face API](https://azure.microsoft.com/en-us/services/cognitive-services/face/) and then to wrap it all up inject the relevant settings into the app.

## Things to keep an eye out for

Azure Function v2 kept blowing up when using dependencies of anything lower than .net standard 2.0 so Key Vault, Managed Service Identity and Face API are all consumed using the HTTP Client

You will first need to setup a service principle you can use to run the scripts, while this is a little fustrating for a quick test it is a good thing get your head round. The reason I have gone this path for this demo was due to a [bug](https://github.com/terraform-providers/terraform-provider-azurerm/issues/656) in the terraform provider when I wrote the script, which has now been resolved in the latest version of the provider.

The Azure Function App and the Key Vault have a circular dependency. The Key Vault needs to know the identity of the Function App and the Function App needs to know the uri of the Key Vault, to work around this in Terraform I have manually built up the uri again in the Function App appsettings section, rather than use the output of Key Vault resource.

Terraform currently doesn't support the Face API so this is built using the template resource, allowing for ARM to be used within the environment build [link](https://www.terraform.io/docs/providers/azurerm/r/template_deployment.html)

## The end game

Some nice picture of whats what!

## Giving it a go

### (Create service prinicpal)

Soon to be optional, but due to a bug a time of writing this is currently required.

[Show code for bash here]

### Update configuration

Modify /scripts/build_and_deploy.sh with the relevant parameters

* resource_name -  the root name you would like to use for the resource group and the created resources
* subscription_id - the guid that represents the subscription for deployment
* client_id - the id of the service prinicpal to run the scripts under. (Required due to bug at time of creation, see below)
* client_secret - the secret of the service prinicpal
* tenant_id - the tenant id that contains the service prinicpal

```
#!/bin/bash

resource_name="emotionfunc"
subscription_id=""
client_id=""
client_secret=""
tenant_id=""

# build environment
./build_env.sh $resource_name $subscription_id $client_id $client_secret $tenant_id

./build_src.sh

./publish.sh
```

This will then flow through to Terraform and while using the service prinicpal will mean it will run silently.

```
#!/bin/bash

resource_name=$1
subscription_id=$2
client_id=$3
client_secret=$4
tenant_id=$5

az account set --subscription $subscription_id

terraform init ../env
terraform apply -auto-approve \
    -var "subscription_id=$subscription_id" \
    -var "resource_name=$resource_name" \
    -var "client_id=$client_id" \
    -var "client_secret=$client_secret" \
    -var "tenant_id=$tenant_id"
    ../env
```

```
provider "azurerm" {
  subscription_id = "${var.subscription_id}"
  version         = "~> 1.8"
  client_id       = "${var.client_id}"
  client_secret   = "${var.client_secret}"
  tenant_id       = "${var.tenant_id}"
}
```

### Run the script

In a shell execute the script /scripts/build_and_deploy.sh

This will:

* Build the environment in Azure
* Build the application locally
* Update application with things such as uris based on new environment
* Deploy application to Azure

### Start playing

* Browse to the root of the Function App
* Make some faces
* See the results
* Repeat

## Next steps

I still need to refine a few things

* Update to AzureRM Provider 1.10, allowing for both Azure authentication methods to be used.
* Update entry script to take all variables to hand the scenario above
* Fix the staitc HTML site so work on more devices, look nice and work both in portrait and landscape mode.
* Allow for multiple faces
* Highlight face with specific emotion
* Allow the option to store terraform state in a backend
* Output the Azure Function App URL at the end of the script