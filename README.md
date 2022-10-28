
# EEBUS.Net

EEBUS protocol reference implementation leveraging the .Net Framework

## Getting Started

This reference implementation includes a client and a server built into a single web application based on .Net6.0. To run it, please press F5 from within Visual Studio to build and run the application locally on your PC. Your default browser window will open automatically and ask you to select a certificate for authenticating you as a user (which is part of the EEBus spec.). You can then select the EEBUS Browser tab to connect to discovered EEBUS devices in your network. By default, the "MICROSOFT-Azure-EEBUS-Gateway-100" will be discovered, representing the EEBUS server included in their reference implementation. Then press the Connect button to connect to your chosen EEBUS server. Once you accept the connection, you can send a simple test data item or send a SPINE message to the EEBUS server for testing purposes.


## Current Implementation Status

Smart Home Internet Protocol (SHIP): Fully implemented, self-tested.

Smart Premises Interoperable Neutralmessage Exchange (SPINE): Implementation started.

