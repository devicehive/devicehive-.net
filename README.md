DeviceHive .NET framework
=========================

[DeviceHive]: http://devicehive.com "DeviceHive framework"
[DataArt]: http://dataart.com "DataArt"

DeviceHive turns any connected device into the part of Internet of Things.
It provides the communication layer, control software and multi-platform
libraries to bootstrap development of smart energy, home automation, remote
sensing, telemetry, remote control and monitoring software and much more.

Connect embedded Linux using Python or C++ libraries and JSON protocol or
connect AVR, Microchip devices using lightweight C libraries and BINARY protocol.
Develop client applications using HTML5/JavaScript, iOS and Android libraries.
For solutions involving gateways, there is also gateway middleware that allows
to interface with devices connected to it. Leave communications to DeviceHive
and focus on actual product and innovation.

Please refer to the [DeviceHive] website for more information.

Project overview
------------------

This project includes .NET implementation of the following DeviceHive componenets:
* [Server application](#server-application)
* [Client library](#client-library)
* [Device and Gateway libraries](#device-and-gateway-libraries)

### Server application
The Server is a web application which implements [DeviceHive RESTful API](http://devicehive.com/restful). It could be installed on a dedicated server in order to build a private or public DeviceHive network.

The server application provides core features of DeviceHive such as:
* Support of user accounts, access keys, authentication and access control
* Access to information about networks and devices
* Routing of messages between clients and devices
* Various business-oriented tasks (e.g. device status control, device equipment state tracking, etc.)

The .NET server supports Cross-Origin Resource Sharing (CORS) standard, so the corresponding DeviceHive resources could be accessed from websites hosted on different domains using AJAX requests.

#### Installation
The following OS and applications are required for the DeviceHive .NET server to operate:

1. Windows Server 2008 (or higher) or Windows 7
2. Microsoft IIS 7 (or higher)
3. Microsoft .NET Framework 4.5.2 (or higher)
4. MongoDB 2.6 (or higher) or Microsoft SQL Server 2008 (or higher)

The setup package could be built from the sources or downloaded from the DeviceHive [downloads](http://devicehive.com/download) page.

### Client library

.NET client library is a wrapper around [DeviceHive RESTful API](http://devicehive.com/restful) which includes set of methods to access corresponding DeviceHive resources. This library could be used in .NET applications to enable their interaction with the DeviceHive service to monitor and control all registered devices.

The library supports the following actions:
* Get information about DeviceHive networks, devices and device classes
* Get current state of equipment that devices have onboard
* Get real-time notifications from devices about various events
* Send a command to a particular device

Client library is available in two options: .NET Framework 4.5 library and .NET Portable Library which targets the following frameworks: .NET for Windows Store Apps (Windows 8.1+) and Windows Phone 8.1+.

#### Installation and Usage

The library is available via NuGet: http://www.nuget.org/packages/DeviceHive.Client/

Please refer to the [VirtualLed Client Tutorial](http://devicehive.com/virtualled-client-tutorial) to see step-by-step instructions on how to configure and launch sample VirtualLed client based on the .NET DeviceHive client libraries.

Client Portable Library usage in Windows 8.1 is described in the [DeviceHive Manager Windows 8 Application Tutorial](http://devicehive.com/devicehive-manager-windows-8-application-tutorial).

See [Library Reference](http://devicehive.com/reference-net-client) for more detailed information on library classes and methods.

### Device and Gateway libraries

These libraries were created in order to support device prototyping and development on the .NET platform. Despite the fact that .NET framework has limited usage on embedded devices, it still may be appropriate to run devices using it in some cases. For example, equipment may be connected directly to a computer’s IO ports, or a Windows PC may act as a proxy/gateway between DeviceHive and actual low-level devices utilizing a different protocol.

Another reason to use .NET framework as platform for devices is to quickly prototype a device simulator on a high-level language. This will allow developers to create and debug other components in the system without involving actual hardware.

The library consists of two primary parts:

1. DeviceHive domain objects and a wrapper around [DeviceHive RESTful API](http://devicehive.com/restful). The library implements all necessary API methods to support the following actions:
  * Register new or existing device in the DeviceHive
  * Send notifications about various events to clients
  * Get real-time commands from clients
  * Update clients about command execution result
2. Bases classes for devices and device hosting support.

Devices can be implements as simply as deriving from the DeviceBase class, overriding the Main function and declaring action methods to handle commands from clients. The library also provides DeviceHost class that connects device implementations with DeviceHive API interfaces to route all incoming and outgoing messages.

Please refer to the [VirtualLed Device Tutorial](http://devicehive.com/virtualled-device-tutorial) to see step-by-step instructions on how to configure and launch sample VirtualLed device based on the .NET DeviceHive device library.

See [Library Reference](http://devicehive.com/reference-net-device) for more detailed information on library classes and methods.

Build instructions
------------------

In order to build all components, run the `/src/build.cmd` script. The build output will be placed into the `/bin` folder.
The following tools are required for making the build:

1. Microsoft Visual Studio 2013 Update 2 or later
2. Microsoft .NET Framework 4.5.2 Developer Pack
3. [WiX Toolset](http://wixtoolset.org/)
4. [MSBuild Community Tasks](https://github.com/loresoft/msbuildtasks)

Alternatively, individual components can be built using the `build.cmd` scripts in the corresponding component folders.

Change history
------------------

#### 2.0.0
* API: Added ability for users to block devices (device.isBlocked field)
* API: The user entity now includes 'data' field for storing arbitrary data
* Server: Added server installer project
* Server: Do not return HTTP 400 in pollMany calls if specified deviceGuid is invalid
* Server: Performance optimizations for MongoDB database
* Server: Performance optimizations for inter-server communications in cluster environment
* Server: Support for reading client IP address from a HTTP header (for proxy server scenarios)
* Client: Added support for Reconnecting state
* Client: Added DeviceHiveClient.WaitCommandResultAsync method to get awaitable task for command result
* Client: Allow to set custom HttpMessageHandler in the RestClient contructor
* Client: Extracted IRestClient interface from the RestClient class for better portability
* Client: Added OldPassword field to the User entity for password changing functionality
* Client: Portable library now targets Windows 8.1 and Windows Phone 8.1 platforms

#### 1.3.1
* API: New access key permission actions to cover administrative API methods
* API: New authentication API for creating session access keys, it also supports user authentication via OAuth
* API: Added social provider details to the User resource; password is now optional
* API: Device.Key field is now optional
* API: Device deletion is now allowed to devices
* Client: Introduced IDeviceHiveClient interface to simplify unit testing
* Client: Get rid of Microsoft.AspNet.WebApi.Client dependency

#### 1.3.0
* API: Added support for access key authentication in REST and WebSocket APIs
* API: Added API resources for managing user API keys and their permissions
* API: WebSocket client API has been extended to support device actions
* API: Client credentials (or API keys) can now be used to perform device-specific actions
* API: Added ability to subscribe to notification or commands with specific name
* API: Added API methods to retrieve grouped historical notifications
* API: Added support for OAuth 2.0 protocol for access key retrieval (DeviceHive appears as authorization and resource server)
* API: Added API resources for managing OAuth clients and grants to support OAuth 2.0 key exchange
* API: Equipment object is now always returned as part of the DeviceClass object
* API: Device GUID may now be an arbitrary string
* Server: The server has been migrated to .NET 4.5 and Entity Framework 6.1
* Server: Added support for handling WebSocket ping/pong frames
* Server: The device is now kept online if it's polling for commands or sending WebSocket ping frames
* Server: Introduced mechanism for attaching custom plug-ins which can inspect all transmitted notifications and commands
* Server: Introduced configurable user password policy
* Client: The library has been migrated to .NET 4.5 with support for async methods
* Client: Internal implementation now relies on "channel" abstractions to support REST and WebSocket protocols

DeviceHive license
------------------

[DeviceHive] is developed by [DataArt] Apps and distributed under Open Source
[MIT license](http://en.wikipedia.org/wiki/MIT_License). This basically means
you can do whatever you want with the software as long as the copyright notice
is included. This also means you don't have to contribute the end product or
modified sources back to Open Source, but if you feel like sharing, you are
highly encouraged to do so!

© Copyright 2015 DataArt Apps · All Rights Reserved