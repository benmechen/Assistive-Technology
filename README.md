# Team 30 - Assistive Technology

[Documentation](https://team30.netlify.com/)

### Description

Windows server that handles the inputs sent from the client through the usage of TCP. The initial discovery of the server is done by registering a Zeroconf service.

### Usage

To use it just press the start service button. This will register a Zeroconf service and stablish a tcp connection to the client. You have a 20 seconds to stablish a connection. If this fails the TCP thread will exit. to Re-stablish a connection, you must stop and restart the services.


