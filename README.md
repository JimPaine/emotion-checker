# Function & Cognitive demo

A demo to show some great capabilities in Azure Functions. 
Highlights being:
- [App Setting KeyVault Syntax](https://github.com/JimPaine/emotion-checker/blob/master/env/functions.tf#L32-L33)
- [Proxy from App Settings](https://github.com/JimPaine/emotion-checker/blob/master/src/ImageProcessor/proxies.json#L21)[and](https://github.com/JimPaine/emotion-checker/blob/master/env/functions.tf#L34-L35)


[![Build Status](https://dev.azure.com/jimpaine-msft/github%20pipelines/_apis/build/status/JimPaine.emotion-checker)](https://dev.azure.com/jimpaine-msft/github%20pipelines/_build/latest?definitionId=8)

[![Deploy to Azure](http://azuredeploy.net/deploybutton.png)](https://azuredeploy.net/)

## Architecture

- Consumer calls the function proxy which presents static html
- The static html allows the user to take a photo and calls the API
- The function app uses Managed Service Identity to call Key Vault
- Key vault used to stored the connection details for the Face API
- The Face API is called with the original image and obtains the emotion

![Architecture](/docs/images/architecture.png)