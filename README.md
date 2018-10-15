# emotion-checker

[![Build Status](https://dev.azure.com/jimpaine-msft/github%20pipelines/_apis/build/status/JimPaine.emotion-checker)](https://dev.azure.com/jimpaine-msft/github%20pipelines/_build/latest?definitionId=8)

## Architecture

- Consumer calls the function proxy which presents static html
- The static html allows the user to take a photo and calls the API
- The function app uses Managed Service Identity to call Key Vault
- Key vault used to stored the connection details for the Face API
- The Face API is called with the original image and obtains the emotion

![Architecture](/docs/images/architecture.png)

## Giving it a go

The azure-pipelines.yml represents an Azure DevOps Pipeline which will build out the environment and build and release the application. Follow the steps below to build it out and run the demo.

## Build Azure Pipeline

Set location to where you have forked the code base.

![pipeline location](/docs/images/pipeline-location.png)

Select the repository emotion-checker

![pipeline repo](/docs/images/pipeline-repo.png)

This will automatically pick up the azure-pipelines.yml

![pipeline template](/docs/images/pipeline-template.png)

At the moment you have to run it to save it, which is not great as we still need to setup the variables.

So click Run and then cancel the build right away.

Edit the pipeline

![click pipeline edit](/docs/images/pipeline-edit.png)

Click the variables tab and add the variables below.

![pipeline variables](/docs/images/pipeline-variables.png)

## Variables

Make sure in the variables section of the pipeline the following have been added.

| Variable                | Comment                                                                     |
| ----------------------- | ---------------------------------------------------------------------------:|
| subscription_id         | The guid that represents the subscription you would like run against        |
| tfstate_access_key      | The key for the storage account where the Terraform backend state is stored |
| tfstate_resource_group  | The resource group terraform state lives                                    |
| tfstate_storage_account | The storage account terraform state lives                                   |
| tfstate_container       | Storage container for terraform                                             |
| client_id               | The guid that represents the service prinicpal to run under                 |
| client_secret           | Secret of the service principal                                             |
| tenant_id               | The tenant where the service prinicpal belongs                              |
| app_url                 | The app ID URL of the service prinicpal / application                       |

Click Save and queue

This will:

* Build the environment in Azure
* Build the application
* Update application with things such as uris based on new environment
* Deploy application to Azure

### Start playing

* Browse to the root of the Function App
* Make some faces
* See the results
* Repeat

## Next steps

I still need to refine a few things

* Fix the staitc HTML site so work on more devices, look nice and work both in portrait and landscape mode.
* Allow for multiple faces
* Highlight face with specific emotion

## What was the point?

Firstly, off the back of a few discussions I thought it would be great to get some real end to end demos, including the environment and source being built and deployed.

I also wanted to be able to be run it from any platform. So to cover that there is no PowerShell, ARM templates [(may be a little ARM but ssshhh)](https://www.terraform.io/docs/providers/azurerm/r/template_deployment.html) or .NET Framework

Then I wanted to show / learn how an Azure Function App could have a [managed service identity](https://docs.microsoft.com/en-us/azure/app-service/app-service-managed-service-identity) generated at runtime / build time and have the relevant policies setup in Azure Key Vault, which for this example is storing the keys needed to access the [Face API](https://azure.microsoft.com/en-us/services/cognitive-services/face/) and then to wrap it all up inject the relevant settings into the app.

## Things to keep an eye out for

Azure Function v2 kept blowing up when using dependencies of anything lower than .net standard 2.0 so Key Vault, Managed Service Identity and Face API are all consumed using the HTTP Client

You will first need to setup a service principle you can use to run the scripts, while this is a little fustrating for a quick test it is a good thing get your head round. The reason I have gone this path for this demo was due to a [bug](https://github.com/terraform-providers/terraform-provider-azurerm/issues/656) in the terraform provider when I wrote the script, which has now been resolved in the latest version of the provider.

The Azure Function App and the Key Vault have a circular dependency. The Key Vault needs to know the identity of the Function App and the Function App needs to know the uri of the Key Vault, to work around this in Terraform I have manually built up the uri again in the Function App appsettings section, rather than use the output of Key Vault resource.

Terraform currently doesn't support the Face API so this is built using the template resource, allowing for ARM to be used within the environment build [link](https://www.terraform.io/docs/providers/azurerm/r/template_deployment.html)