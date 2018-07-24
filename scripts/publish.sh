#!/bin/bash

# group publish artifacts
rm temp -fr
mkdir temp
cd temp
mv ../../src/ImageProcessor/bin/Release/netstandard2.0/publish/ app/

# include proxies
cp ../../src/Proxy/bin/proxies.json app/proxies.json

mkdir proxy/
cp ../../src/Proxy/index.html proxy/index.html

cd ../

connectionstring=$(terraform output -json primary_connection_string | jq '.value' | tr -d '"')
fileendpoint=$(terraform output -json file_endpoint | jq '.value' | tr -d '"')
functionname=$(terraform output -json function_name | jq '.value' | tr -d '"')
resourcegroup=$(terraform output -json resource_group | jq '.value' | tr -d '"')

echo "Dest: $filendpoint$functionname-content"

az functionapp stop \
    --name $functionname \
    --resource-group $resourcegroup


az storage file delete-batch \
    --source "$fileendpoint$functionname-content" \
    --connection-string $connectionstring

az storage file upload-batch \
    --destination "$fileendpoint$functionname-content" \
    --source ./temp/app/ \
    --destination-path "site/wwwroot" \
    --connection-string $connectionstring

az storage blob upload \
    --container-name "proxy" \
    --file temp/proxy/index.html \
    --name "index.html" \
    --connection-string $connectionstring

az functionapp start \
    --name $functionname \
    --resource-group $resourcegroup

cd ../