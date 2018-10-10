---
services: azure-iot-hub-large-twin-example
platforms: dotnet-core
author: Kohei Kawata
---

# Azure IoT Hub Large Twin Example for .NET

This sample demonstrates how to extend Azure IoT Hub Twins via Azure Blob Storage. Concretely, a solution can benefit from this approach if the twin properties either exceed the current limit or refer to binary content that cannot be easily represented in the twin's JSON payload.

## Features
This project framework provides the following features:

* Enable Device Twin and Module Tiwn to pull properties from Azure Blob Storage
* Report and download properties when the Blob is updated

## Getting Started

### Prerequisites

To run samples in this repository, you need:

- [.NET Core 2.1 SDK](https://www.microsoft.com/net/download)
- [Azure IoT Hub](https://docs.microsoft.com/en-us/azure/iot-hub/)
- [Azure Blob Storage](https://docs.microsoft.com/en-us/azure/storage/blobs/storage-quickstart-blobs-dotnet?tabs=windows)

### Quickstart

1. git clone [https://github/]
2. 

