# WatchfulEye
A security camera system that is designed to have a central server that communicates with multiple PiCamera clients on a local network.

The project uses [NetMQ](https://github.com/zeromq/netmq) to handle the local network communication and [Serilog](https://github.com/serilog/serilog) for logging. [VLC](https://code.videolan.org/videolan/LibVLCSharp) is used for the server GUI for live video playback.

The system is broken up into multiple projects:

- WatchfulEye.Client
- WatchfulEye.Server
- WatchfulEye.Server.App
- WatchfulEye.Shared

## WatchfulEye.Client

### Description
This project contains the client code that is meant to be deployed to a RaspberryPi that has a PiCamera. It contains C# code to handle receiving messages/requests over the local network to run the needed python scripts to interface with the PiCamera. The data captured on the PiCamera is then sent back over the local network to the central Server. 

### Features
The client is rather bare-bones and can only support the following features:
- Auto register with Server
- Capture Picture
- Live Stream


## WatchfulEye.Server

### Description
This project contains the server code that can be deployed anywhere. It contains C# code to serve two purposes, registering clients over the local network and interfacing with our client over the local network. More specifically, it contains a network discovery loop that is constantly waiting for client registration. Once it does, it creates a socket to send and receive messages from our client, and constantly monitors it for connection.

### Features
The server supports the following features:
- UDP Network Discovery for client registration
- Socket interface for client interaction
- Receiving data stream over the network for picture or live stream
- Multi-Threading to manage multiple cameras
    - Still testing what the limit is


## WatchfulEye.Server.App

### Description
This project contains a GUI app to run the `WatchfulEye.Server` project. It has a simple interface to handle the current display and live view of multiple cameras. All using a MVC design, taking advantage of DataBindings for easy view manipulation.

### Features
The GUI fully interfaces with the `WatchfulEye.Server` project to support the following features:
- Poll every 60 seconds for a thumbnail preview of a camera
- Live video playback of a camera
- Manager for up to 4 cameras

### Remarks
It currently uses [WPF](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/?view=netdesktop-7.0) for the GUI related things. This severly limits it to Windows only, so the plan is to move to [Xamarin](https://dotnet.microsoft.com/en-us/apps/xamarin) for full platform support. The GUI app only supports up to 4 camera for layout reasons, not any kind of technical restriction


## WatchfulEye.Shared

### Description
This project contains the shared messaging library between the `WatchfulEye.Client` and `WatchfulEye.Server`. It serializes message data into JSON and uses [NetMQ](https://github.com/zeromq/netmq) to handle sending the bytes over the local network via a Socket.

### Features
The shared code allows for projects to have the following functionality:
- Sending messages over a Socket
- Receiving messages over a Socket
- Register callbacks for received messages
- Heartbeat monitor for network health
- Serilog logging for general behavior and debugging