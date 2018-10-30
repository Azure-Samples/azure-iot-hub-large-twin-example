---
services: iot-hub 
platforms: nodejs,python,dotnet
author: druttka
---

> NOTE: The documentation and sample code in this repository is in active development.

# How To Extend Azure IoT Hub Twins via Azure Blob Storage  

This sample demonstrates how to extend Azure IoT Hub Twins via Azure Blob Storage. Concretely, a solution can benefit from this approach if the twin properties either [exceed the current limit](https://feedback.azure.com/forums/907045-azure-iot-edge/suggestions/33583492-iot-hub-device-and-module-twins-limit) or refer to binary content that cannot be easily represented in the twin's JSON payload.

## Problem Statement

Azure IoT Hub device twins and Azure IoT Edge module twins are limited in both size and content type. The twins are JSON payloads limited to 8kb. Members of the Azure IoT development community may wish to include twin data that exceeds the size limit. They may also wish to consume binary assets that are not easily represented in the JSON document.

## High Level Solution Overview

Conceptually, one can workaround the twin size limitation by placing the larger asset in an external store, and then delivering a reference to that store via the twin properties. Devices can still observe twin property changes, but they must retrieve the linked asset to fully process the update. 

![High level diagram](./images/HighLevelDiagram.png)

### Twin Contract 

The JSON payload for the twin represents a contract. In this example, we are using the following properties in the twin.

**desired**

We will refer to a blob asset in a rich object. The property name `configurationBlob` is arbitrary, but note the structure of the object value.

```
"properties": {
    "desired": {
        "configurationBlob": {
            "uri": "https://myaccount.blob.core.windows.net/sascontainer/sasblob.txt?sv=2015-04-05&st=2015-04-29T22%3A18%3A26Z&se=2015-04-30T02%3A23%3A26Z&sr=b&sp=rw&sip=168.1.5.60-168.1.5.70&spr=https&sig=Z%2FRHIX5Xcg0Mq2rqI3OlWTjEg2tYkboXr1P9ZUXDtkk%3D",
            "ts": "2018-10-09T19:40:18.7138092Z",
            "contentType": "json"
        }
    }
}
```

Through this structure, a device or module can observe
- the location of the linked asset
- the timestamp at which the asset was last modified
- the content type of the asset; while this sample does not currently use this value, one can imagine that the device client may know how to negotiate different content types, e.g., a connected display could present an image or play a video

**reported**

We will report an acknowledgment of the new blob asset in a rich object. The property name `configurationBlob` is arbitrary, but note the structure of the object value.

```
"properties": {
    "reported": {
        "configurationBlob": {
            "acknowledged": "https://foo.azurestorage.net/bar?abc",
            "current": "https://foo.azurestorage.net/old?xyz,
            "status": "received | failed"
        }
    }
}
```

Through this structure, a device or module can report
- which asset it is currently using
- which asset it has acknowledged, even if it has not been able to retrieve or apply it
- the status of applying the latest blob asset

### Device Client

When the values in the twin are changed, the device client will respond to the new desired properties by retrieving the linked asset. See platform specific examples in the [dotnet](./dotnet/), [python](./python/), and [nodejs](./nodejs/) folders for more detail.

### Back-end

Consider a use case where a large configuration object is required for a class of devices. Updating all of the twins could be a tedious and error-prone process. The example workaround includes a pattern for updating the twins at scale. 

After creating or updating the blob, an automated process (e.g., an Azure Function) can execute a device query and programatically update the twins for the targetted devices.

> TODO: Include example code for back-end solution

## Getting Started

### Prerequisites

- An Azure subscription. You can get a [free trial here](https://azure.microsoft.com/en-us/free/).
- See the README.md in each platform directory for any platform specific prerequisites.

### Quickstart
- See the README.md in each platform directory for further platform specific instructions

## Resources 

For details on contributing changes and information on this project's code of conduct, please refer to the [Contributing](CONTRIBUTING.md) file.