#!/bin/bash
set -e

export RESOURCE_GROUP=$1
export AZURE_STORAGE_ACCOUNT=$2
export AZURE_STORAGE_CONTAINER=$3
export SENSOR_ID=$4
export FILE=$5

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

# Create the patch object
TS=`date '+%Y-%m-%dT%H:%M:%S'`
TWIN_PATCH='{"uri":"'${NEW_URL}'","ts":"'${TS}'","contentType":"text/plain"}'
echo $TWIN_PATCH

# Update the twin
az iot hub device-twin update -n $IOT_HUB_NAME \
    -g $RESOURCE_GROUP -d $SENSOR_ID \
    --set properties.desired.configurationBlob=$TWIN_PATCH
