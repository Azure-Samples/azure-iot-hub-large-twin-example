import random
import time
import sys
import os
import datetime
import urllib.request
import dateutil.parser
import asyncio
from azure.iot.device.aio import IoTHubDeviceClient
from azure.iot.device import Message

# String containing Hostname, Device Id & Device Key in the format
CONNECTION_STRING = os.environ['CONNECTION_STRING']
DEVICE_ID = os.environ['DEVICE_ID']
# v2 SDK we are currently only supporting the MQTT
PROTOCOL = 'MQTT'
MESSAGE_TIMEOUT = 10000
SEND_CALLBACKS = 0
MSG_TXT = "{\"deviceId\": \"" + DEVICE_ID + "}"

# Device Twin configuration:
LAST_TS=datetime.datetime.min.replace(tzinfo=datetime.timezone.utc)
# define behavior for receiving a twin patch
def twin_patch_handler(patch):
    global LAST_TS
    print ("the data in the desired properties patch was: {}".format(patch))
    print ( "" )
    print ("Change triggered on device twin")
    url       = patch["configurationBlob"]["uri"]
    ts_string = patch["configurationBlob"]["ts"]
    ts        = dateutil.parser.parse(ts_string) 
    print ( "    url of the blob: %s" % url )
    print ( "    timestamp of the blob: %s" % ts )

    if ts > LAST_TS:
        
        print ( "Downloading new file ...")
        response = urllib.request.urlopen(url)
        data = response.read()
        
        text = data.decode('utf-8')
        print(text)

        LAST_TS = ts

    else:
        print ( "Skipping download due to earlier timestamp ... ")       

def environment_vars():
    resp = True
    error = ""
    if not CONNECTION_STRING.strip():
        print ("Error: CONNECTION_STRING env variable is empty.\n")
        if not DEVICE_ID.strip():
            print("Error: DEVICE_ID env variable is empty.\n")
        resp = False
    return resp

def print_send_request(message):
    global SEND_CALLBACKS
    key_value_pair = message.custom_properties
    print ( "    message_id: %s" % message.message_id )
    print ( "    correlation_id: %s" % message.correlation_id )
    print ( "    Properties: %s" % key_value_pair )
    SEND_CALLBACKS += 1
    print ( "    Total calls confirmed: %d \n" % SEND_CALLBACKS )

async def iothub_client_telemetry_sample_run():
    try:
        # prepare iothub client
        client = IoTHubDeviceClient.create_from_connection_string(CONNECTION_STRING)
        await client.connect()    
        print ( "IoT Hub device sending periodic messages, press Ctrl-C to exit \n" )
        # set the twin patch handler on the client
        client.on_twin_desired_properties_patch_received = twin_patch_handler  
            
        message_counter = 0
        while True:
            msg_txt_formatted = MSG_TXT 
            # messages can be encoded as string or bytearray
            if (message_counter & 1) == 1:
                message = Message(bytearray(msg_txt_formatted, 'utf8'))
            else:
                message = Message(msg_txt_formatted)
            # optional: assign ids
            message.message_id = "message_%d" % message_counter
            message.correlation_id = "correlation_%d" % message_counter
            # optional: assign properties
            prop_text = "PropMsg_%d" % message_counter
            message.custom_properties["Property"] = prop_text

            await client.send_message(message)
            print ( "IoTHubClient.send_event_async accepted message [%d] for transmission to IoT Hub." % message_counter )
            # print message details
            print_send_request(message)
            time.sleep(30)
            message_counter += 1

    except IoTHubDeviceClient.on_background_exception as iothub_error:
        print ( "Unexpected error %s from IoTHub" % iothub_error )
        return
    except KeyboardInterrupt:
        print ( "IoTHubClient sample stopped" )

#if __name__ == '__main__':
async def main():
    print ( "Simulating a device using the Azure IoT Hub Device SDK for Python" )
    if environment_vars():
        print ( "    Protocol %s" % PROTOCOL )
        print ( "    Device ID=%s" % DEVICE_ID )
        await iothub_client_telemetry_sample_run()
    print ( "Simulated device says bye!" )
if __name__ == "__main__":
    asyncio.run(main())    