// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
// PARTICULAR PURPOSE. 
// 
// Copyright (c) Microsoft Corporation. All rights reserved 

// includes
#include "SoftwareSerial.h"
// PINS
// constants
const int TX_BT = 10;
const int RX_BT = 11;
// Pushbutton pin numbers
const int redButtonPin = 3;			// the number of the pushbutton pin
const int greenButtonPin = 2;		// the number of the pushbutton pin
// LED pins
const int redLedRin = 6;			// the number of the red led pin
const int greenLedPin = 7;			// the number of the green led pin
// Messaging
const unsigned long periodicMessageFrequency = 1000; //Frequency for periodic messages [milliseconds]
unsigned long time = 0;

// Variable for reading the pushbutton status
boolean redPressed;
boolean greenPressed;

// Variable for color codes
int currentColorValueRed;
int currentColorValueGreen;
int currentColorValueBlue;

// Workforce
SoftwareSerial btSerial(TX_BT, RX_BT);

// Set everything up
void setup() 
{
	// initialize the RED LED pin as an output:
	pinMode(redLedRin, OUTPUT);
	// initialize the GREEN LED pin as an output:
	pinMode(greenLedPin, OUTPUT);

	Serial.begin(9600);
	Serial.println("Serial initialized");

	btSerial.begin(9600);
	Serial.println("Bluetooth initialized");

	// initialize the pushbutton pin as an input:
	pinMode(redButtonPin, INPUT);
	pinMode(greenButtonPin, INPUT);
}

// Loop for handling all events
void loop() 
{
	pushbuttonHandler();	// Handle the push of the buttons

	bluetoothHandler();		// Handle incoming bluetooth communication

	// Sending messages back to the application (if needed)
	/* 
		unsigned long currentTime = millis();
		if ((currentTime - time) > periodicMessageFrequency)
		{
			sendPeriodicMessages();
			time = currentTime;
		}
	*/
}

// Handler for incoming bluetooth communication
void bluetoothHandler() {

	// Make sure bluetooth is available
	if (btSerial.available())
	{
		int commandSize = 0;
		commandSize = (int) btSerial.read();
		char command[commandSize];
		int commandPos = 0;
		while (commandPos < commandSize)
		{
			if (btSerial.available())
			{
				command[commandPos] = (char) btSerial.read();
				commandPos++;
			}
		}
		command[commandPos] = 0;
		//Process command
		commandHandler(command);
	}
}

// Handler for push button events
void pushbuttonHandler() 
{
	// Read the state of the pushbutton value:
	redPressed = digitalRead(redButtonPin);
	greenPressed = digitalRead(greenButtonPin);

	currentColorValueRed = (255 - map(2, 0, 1024, 0, 255));	
	currentColorValueGreen = (255 - map(4, 0, 1024, 0, 255));
	//currentColorValueBlue = (255 - map(3, 0, 1024, 0, 255));	// Not using blue

	// check if the pushbutton is pressed.
	// if it is, the buttonState is HIGH:
	if (redPressed == true) {
		// Send message
		sendMessage("RED_PRESSED");
		delay(1000);
	}

	if (greenPressed == true) {
		// Send message
		sendMessage("GREEN_PRESSED");
		delay(1000);
	}

}

// Inbound
void commandHandler(char* message)
{
	// What command was sent
	if ((String) message == "TURN_ON_RED")
	{
		digitalWrite(redLedRin, currentColorValueRed);
		delay(1000);
		digitalWrite(redLedRin, LOW);
	}
	else if ((String) message == "TURN_OFF_RED")
	{
		digitalWrite(redLedRin, LOW);
	}
	else if ((String) message == "TURN_ON_GREEN")
	{
		digitalWrite(greenLedPin, currentColorValueGreen);
		delay(1000);
		digitalWrite(greenLedPin, LOW);
	}
	else if ((String) message == "TURN_OFF_GREEN")
	{
		digitalWrite(greenLedPin, LOW);
	}	
}

// Outbound communication
void sendMessage(char* message)
{
	int messageLen = strlen(message);
	if (messageLen < 256)
	{
		btSerial.write(messageLen);
		btSerial.print(message);
	}	
}

// Sending messages on a periodic basis (testing purposes)
void sendPeriodicMessages()
{
	int value1 = analogRead(A0);
	char value2[8] = "";
	char value3[16] = "";
	itoa(value1, value2, 10);
	strcat(value3, "A0=");
	strcat(value3, value2);
	sendMessage(value3);	
}