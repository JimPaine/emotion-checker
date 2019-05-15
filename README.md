# Function & Cognitive demo

A static website served via an Azure Function Proxy which then streams your camera to the Face API capturing and returning your age and your emotion, while also showing some great capabilities in Azure Functions

Highlights being:
- [App Setting KeyVault Syntax](https://github.com/JimPaine/emotion-checker/blob/master/env/functions.tf#L32-L33)
- [App Settings for proxy](https://github.com/JimPaine/emotion-checker/blob/master/src/ImageProcessor/proxies.json#L21) 
- [and using apps settings in proxy.json](https://github.com/JimPaine/emotion-checker/blob/master/env/functions.tf#L34-L35)
- [Cognitive Service AI Model running in ACI](https://github.com/JimPaine/emotion-checker/blob/master/env/face.tf#L13-L43)


| Stage | Status |
| ----- | ------ |
| Provision Environment | [![Build Status](https://dev.azure.com/jimpaine-msft/github%20pipelines/_apis/build/status/JimPaine.emotion-checker?branchName=master&stageName=ProvisionEnvironment)](https://dev.azure.com/jimpaine-msft/github%20pipelines/_build/latest?definitionId=8?branchName=master) |
| Build | [![Build Status](https://dev.azure.com/jimpaine-msft/github%20pipelines/_apis/build/status/JimPaine.emotion-checker?branchName=master&stageName=Build)](https://dev.azure.com/jimpaine-msft/github%20pipelines/_build/latest?definitionId=8?branchName=master) |
| Publish | [![Build Status](https://dev.azure.com/jimpaine-msft/github%20pipelines/_apis/build/status/JimPaine.emotion-checker?branchName=master&stageName=Deploy)](https://dev.azure.com/jimpaine-msft/github%20pipelines/_build/latest?definitionId=8?branchName=master) |

## Deploy to Azure without ACI

[![Deploy to Azure](http://azuredeploy.net/deploybutton.png)](https://azuredeploy.net/)

## Deploy to Azure with ACI

- [Use Terraform](https://github.com/JimPaine/emotion-checker/tree/master/env)

## Architecture

- Consumer calls the function proxy which presents static html
- The static html allows the user to take a photo and calls the API
- The function app uses Managed Service Identity to call Key Vault
- Key vault used to stored the connection details for the Face API
- The Face API is called with the original image and obtains the emotion

![Architecture](/docs/images/architecture.png)
