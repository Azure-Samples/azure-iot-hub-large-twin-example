FROM brusmx/iot-hub-python-sdk:latest
# For this example our work directory will be /sender
WORKDIR /sender
# You must set these variables to the container
ENV CONNECTION_STRING='' \
    DEVICE_ID='' 
RUN apt-get install -y python-dateutil 
# Now we copy our script
COPY /sender/device-twin-sample.py /sender/device-twin-sample.py
# Remember that Entrypoint doesn't work on headless environment. It will only work in docker run
ENTRYPOINT ["python","/sender/device-twin-sample.py"]
