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