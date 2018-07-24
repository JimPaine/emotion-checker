#!/bin/bash
storage=$1
container=$2

# build and deploy function app
cd ../src/ImageProcessor
dotnet clean
dotnet publish -c "Release"

cd ../Proxy

mkdir bin
cp proxies.json bin/proxies.json

cd ../../scripts

blob_path=$(terraform output -json primary_blob_endpoint | jq '.value' | tr -d '"')
container="proxy"

sed -i "s|{bloblocation}|$blob_path$container|g" ../src/Proxy/bin/proxies.json