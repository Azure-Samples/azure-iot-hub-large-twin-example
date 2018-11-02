#!/bin/bash
set -e
# Requires jquery
export RESOURCE_GROUP=$1
export AZURE_STORAGE_ACCOUNT=$2
export AZURE_STORAGE_CONTAINER=$3
export SENSOR_ID=$4
export FILE=$5

# export AZURE_STORAGE_CONTAINER="devtwinscontainer"
# export AZURE_STORAGE_ACCOUNT="iotdevtwins01"
# export AZURE_STORAGE_CONNECTION_STRING=""
# export SENSOR_ID=""tem
# export RESOURCE_GROUP=""
# export FILE=""

# Obtaining connection strings
export RAND_SUFFIX=$(cat /dev/urandom | env LC_CTYPE=C tr -dc 'a-z0-9' | fold -w 4 | head -n 1)
export AZURE_STORAGE_CONNECTION_STRING=`az storage account show-connection-string -g $RESOURCE_GROUP -n $AZURE_STORAGE_ACCOUNT -o tsv`

# Upload to blob
echo "Uploading file '$FILE' as 'payload-$RAND_SUFFIX.txt' to blob container '$AZURE_STORAGE_CONTAINER'"
az storage blob upload \
    --container-name $AZURE_STORAGE_CONTAINER \
    --account-name $AZURE_STORAGE_ACCOUNT \
    --connection-string $AZURE_STORAGE_CONNECTION_STRING \
    --name  payload-$RAND_SUFFIX.txt \
    --file $FILE

# Generate SAS token
echo "Generating SAS token"
export SAS_TOKEN=`az storage blob generate-sas \
    --name payload-$RAND_SUFFIX.txt \
    --container-name $AZURE_STORAGE_CONTAINER \
    --account-name $AZURE_STORAGE_ACCOUNT \
    --connection-string $AZURE_STORAGE_CONNECTION_STRING \
    --permissions r \
    --expiry 2042-04-02 -o tsv`
echo "Token generated:"
echo $SAS_TOKEN
export NEW_URL="https://${AZURE_STORAGE_ACCOUNT}.blob.core.windows.net/${AZURE_STORAGE_CONTAINER}/payload-$RAND_SUFFIX.txt?$SAS_TOKEN"
echo "The new blob is here:"
echo $NEW_URL 
# Get the device id twin
az iot hub device-twin show -n $IOT_HUB_NAME \
-g $RESOURCE_GROUP  \
-d $SENSOR_ID > $SENSOR_ID-device-twin.json

# Replace the blob url
jq '.properties.desired.configurationBlob.uri="'${NEW_URL}'"'  $SENSOR_ID-device-twin.json > $SENSOR_ID-device-twin-updated.json

# Waiting 1 second to allow the SAS token to be propagated
sleep 1
# Replace the device id twin with the updated version
az iot hub device-twin replace  -n $IOT_HUB_NAME \
    -g $RESOURCE_GROUP -d $SENSOR_ID \
    -j $SENSOR_ID-device-twin-updated.json
