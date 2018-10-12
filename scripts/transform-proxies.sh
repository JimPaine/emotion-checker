sudo blob_path=$(terraform output -json primary_blob_endpoint | jq '.value' | tr -d '"')   
sudo container="proxy"   
sudo sed -i "s|{bloblocation}|$blob_path$container|g" ../src/ImageProcessor/proxies.json   