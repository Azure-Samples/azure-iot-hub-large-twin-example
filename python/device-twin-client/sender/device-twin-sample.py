import random
import time
import sys
import os
import urllib.request
import json
import iothub_client
from iothub_client import IoTHubClient, IoTHubClientError, IoTHubTransportProvider, IoTHubClientResult
from iothub_client import IoTHubMessage, IoTHubMessageDispositionResult, IoTHubError, DeviceMethodReturnValue


# String containing Hostname, Device Id & Device Key in the format
CONNECTION_STRING = os.environ['CONNECTION_STRING']
DEVICE_ID = os.environ['DEVICE_ID']
# choose HTTP, AMQP or MQTT as transport protocol
PROTOCOL = IoTHubTransportProvider.MQTT
MESSAGE_TIMEOUT = 10000
AVG_WIND_SPEED = 6.0
SEND_CALLBACKS = 0
MSG_TXT = "{\"deviceId\": \"" + DEVICE_ID + "\",\"p-value\": %.2f}"

# Device Twin configuration:

TIMER_COUNT = 5
TWIN_CONTEXT = 0
SEND_REPORTED_STATE_CONTEXT = 0
def device_twin_callback(update_state, payload, user_context):
    print ( "" )
    print ( "Twin callback called with:" )
    print ( "    updateStatus: %s" % update_state )
    print ( "    payload: %s" % payload )
    print ( "Downloading new file ...")
    parsed_json = json.loads(payload)
    url = parsed_json["desired"]["configurationBlob"]["uri"]
    print ( "    url of the blob: %s" % url )
    response = urllib.request.urlopen(url)
    data = response.read()
    text = data.decode('utf-8')
    print(text)


def send_reported_state_callback(status_code, user_context):
    print ( "" )
    print ( "Confirmation for reported state called with:" )
    print ( "    status_code: %d" % status_code )

def environment_vars():
    resp = True
    error = ""
    if not CONNECTION_STRING.strip():
        print ("Error: CONNECTION_STRING env variable is empty.\n")
        if not DEVICE_ID.strip():
            print("Error: DEVICE_ID env variable is empty.\n")
        resp = False
    return resp

def send_confirmation_callback(message, result, user_context):
    global SEND_CALLBACKS
    print ( "Confirmation[%d] received for message with result = %s" % (user_context, result) )
    map_properties = message.properties()
    print ( "    message_id: %s" % message.message_id )
    print ( "    correlation_id: %s" % message.correlation_id )
    key_value_pair = map_properties.get_internals()
    print ( "    Properties: %s" % key_value_pair )
    SEND_CALLBACKS += 1
    print ( "    Total calls confirmed: %d \n" % SEND_CALLBACKS )

def iothub_client_init():
    # prepare iothub client
    client = IoTHubClient(CONNECTION_STRING, PROTOCOL)
    # set the time until a message times out
    client.set_option("messageTimeout", MESSAGE_TIMEOUT)
    client.set_option("logtrace", 0)

    if client.protocol == IoTHubTransportProvider.MQTT or client.protocol == IoTHubTransportProvider.MQTT_WS:
        client.set_device_twin_callback(device_twin_callback, TWIN_CONTEXT)
    return client

def iothub_client_telemetry_sample_run():
    try:
        client = iothub_client_init()
        print ( "IoT Hub device sending periodic messages, press Ctrl-C to exit \n" )
        message_counter = 0

        while True:
            msg_txt_formatted = MSG_TXT % (AVG_WIND_SPEED + (random.random() * 3 + 2))
            # messages can be encoded as string or bytearray
            if (message_counter & 1) == 1:
                message = IoTHubMessage(bytearray(msg_txt_formatted, 'utf8'))
            else:
                message = IoTHubMessage(msg_txt_formatted)
            # optional: assign ids
            message.message_id = "message_%d" % message_counter
            message.correlation_id = "correlation_%d" % message_counter
            # optional: assign properties
            prop_map = message.properties()
            prop_text = "PropMsg_%d" % message_counter
            prop_map.add("Property", prop_text)

            client.send_event_async(message, send_confirmation_callback, message_counter)
            print ( "IoTHubClient.send_event_async accepted message [%d] for transmission to IoT Hub." % message_counter )

            status = client.get_send_status()
            print ( "Send status: %s" % status )
            time.sleep(30)

            status = client.get_send_status()
            print ( "Send status: %s" % status )

            message_counter += 1

    except IoTHubError as iothub_error:
        print ( "Unexpected error %s from IoTHub" % iothub_error )
        return
    except KeyboardInterrupt:
        print ( "IoTHubClient sample stopped" )


if __name__ == '__main__':
    print ( "Simulating a device using the Azure IoT Hub Device SDK for Python" )
    if environment_vars():
        print ( "    Protocol %s" % PROTOCOL )
        print ( "    Device ID=%s" % DEVICE_ID )
        iothub_client_telemetry_sample_run()
    print ( "Simulated device says bye!" )
