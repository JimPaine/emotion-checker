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