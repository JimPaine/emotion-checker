#!/bin/bash

resource_name="emotionfunc"
subscription_id=""

# build environment
./build_env.sh $resource_name $subscription_id

./build_src.sh

./publish.sh