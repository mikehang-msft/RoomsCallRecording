[[_TOC_]]

# Overview
`RoomsCallRecording` project demonstrates the ability to perform a recording inside a Room call by integrating Azure Communication Service (ACS) Rooms, calling SDK, and Call Automation (CA). This document highlights major components, prerequisites, and instructions to execute a sample application to perform a room call recording. 
The following components are used in this demonstration.

1. _**ACS Rooms SDK**_: Rooms SDK is used to create a new and manage existing virtual meeting rooms. The Rooms APIs let us to define meeting start/end times and add/update/remove participants in a room. Please refer to [Rooms concept](https://learn.microsoft.com/en-us/azure/communication-services/concepts/rooms/room-concept) document for details. For simplicity, `CreateRoom` API is built into `RoomsCallRecording` Web API project to easily create a room.

1. **_RoomsCallRecording Web API_**: A server-side service listens for pre-registered events (call started, call ended, etc.) and triggers call automation client to start, stop, download or delete a recording. The Web API also exposes other recording operations like pause, resume, GetRecordingState and Rooms operations.

1. **_ACS Web Calling SDK sample application_**: this a sample client-side calling app allows participants to join a Room call and add other participants into the call. This app is available to public in this [Web Sample Calling App](https://learn.microsoft.com/en-us/samples/azure-samples/communication-services-web-calling-tutorial/acs-calling-tutorial/) tutorial.

**_Components Diagram_**

![image.png](/.attachments/image-524e8be2-ed85-4a84-a063-0492ea4498e1.png)


# Prerequisites
1. Visual Studio 2022 or Visual Source Code 1.86.0 or higher
1. .NET 6 or higher

# Set up RoomsCallRecording
## ACS Resource Configurations
### Even Grid Configuration
Event grid subscription needs to be created to notify Call Automation on interested events. 
1. On the Overview page of an ACS Resource
1. Select Events and click on + Event Subscription to create a new event subscription
1. Enter a descriptive Name
1. Select Cloud Event Schema v1.0 for Event Schema 
![image.png](/.attachments/image-b85375b6-582c-4ccf-b430-84c4b27373ec.png)
1. For Event Type, select the following: 
   - Call Started (Preview)
   - Call Ended (Preview)
   - Recording File Status Updated
![image.png](/.attachments/image-46c3cf53-d2ae-4371-955c-626e0844537f.png)

1. Select Storage Queue as Endpoint Type and configure storage queue. Please follow the UIs to create a storage queue to store the event message. Refer to the following [Azure Storage QuickStart](https://learn.microsoft.com/en-us/azure/storage/queues/storage-quickstart-queues-portal) for additional information on how to create a storage queue 

![image.png](/.attachments/image-7677a7e1-c7dc-4e51-a3ee-3520f20927fd.png)
 
## RoomsCallRecording Project
### Code Repos
1. git clone `https://github.com/mikehang-msft/RoomsCallRecording`
1. cd `RoomsCallRecording`
1. dotnet build

### Code Structure
- `src/Web.API/Program.cs`: An entry point to the start the Web.API. This exposes several CA recording operations like Start/Stop/Pause/Resume recording.

- `src/Web.API/WebApplicationExtensions.cs`: This class exposes additional endpoints to assist with rooms operations like create a new room with participants. 
- `src/Web.API/AnswerCallWorker.cs`: This is background service which continuously searches and processes messages by listening to Azure Storage Queue. This queue is set up to store incoming call event messages enqueued by an event grid, which is configured in the ACS resource.
- `src/Web.API/appsettings.json`: This is an `appsettings` storing essential config data like connection string and PSTN phone number, etc. See configuration data section below for details.

### Configuration Data

- `Acs:ConnectionString`: specify connection string to ACS Resource used in this project
- `Storage.ConnectionString`: specify connection string for Azure Storage Queue. This is storage account connection string selected when configuring an event grid subscription endpoint above.
- `Storage.QueueName`: specify name of Azure Storage Queue. This is name of the storage queue created in when configuring an event grid subscription endpoint above.

### Run projects
1. Fill appsettings.json with required configuration values
1. Hit F5 to run the project in a debug mode
 
![image.png](/.attachments/image-e5640f2a-4cd1-490f-8255-2fe152e825ee.png)

# Set up ACS Calling Web App:
Once specifying ACS connection string in serverConfig.json, execute npm run start-local in the terminal. Please refer to [tutorial](https://learn.microsoft.com/en-us/samples/azure-samples/communication-services-web-calling-tutorial/acs-calling-tutorial/) for steps by steps to run ACS Calling Web App sample.

![image.png](/.attachments/image-746dc4bd-6164-4d65-92e6-f9897afb60a4.png)

# Execute Room Call Recording End to End
With both `RoomsCallRecording` Web API and ACS Calling Web App are running. The following steps can be used to start and manage a recording. 
## Step 1 - Create a room
Create a room with 2 participants: participant 1 and participant 2
1. Go to `RoomsCallRecording` Swagger UI and select POST /api/room operation
1. Fill in the participants using Azure Communication Service (ACS) user ID. ACS user IDs can be retrieved using the ACS Calling Web App after hitting “Login ACS user and initial SDK” button on the landing screen.

![image.png](/.attachments/image-9986ea81-e7f3-4980-88cf-656a3c1760c1.png)
_Find the identity (ACS User ID) in ACS Calling Web App_

![image.png](/.attachments/image-95419b16-5089-4329-bb75-47e7a58ea8f3.png)
_Add Participants ACS User IDs to create a room_

![image.png](/.attachments/image-b570130e-24b6-457e-a30c-cde7695dfd47.png)
_A room is created with default values and specified participants_

## Step 2 – Participants join a room call

The first and second participants join a room call by entering the room ID. 
![image.png](/.attachments/image-f4e1640f-8a82-4dc1-aeb1-21c5824f15d5.png)
_Enter room Id to join a room call._

![image.png](/.attachments/image-e474d2c3-a09d-4d1f-9d95-d8eecb39fb2d.png)
_The 1st and 2nd participants join the room call._

## Step 3 – Capture `ServerCallId` in a room call
On the `RoomsCallRecording` project, as soon as the 1st participant joins the call, the `Microsoft.Communication.CallStarted` event it published and subscribed by `RoomsCallRecording` web API. For the purpose of this demonstration, the web API handles the event but only printing out `ServerCallId` which can then later used to start call recording. Please copy the `ServerCallId` in the output window for later usage. (Tips: clear output windows to remove unnecessary output data prior to reaching to this step).
 
![image.png](/.attachments/image-3c1ebbd5-e772-420b-ac87-3ba93bae0722.png)

## Step 4 – start a recording
Once `ServerCallId` is available, it can be used to start a call recording. Open the Swagger UI windows and select POST: api/recording/{`serviceCallId`}/start, click `Try it Out`, and fill in the `serverCallId` captured in the previous step

![image.png](/.attachments/image-3cd4411a-7e17-491c-9f55-933d4bd30b04.png)

Once recording has been started successfully, the `RecordingId` value is returned and printed in the output windows. Please copy the `RecordingId` for the other operations on the ongoing recording.

## Step 5 – Perform operations on ongoing recording
Once `RecordingId` is available, we can execute the following operations on the existing recording: Pause/Resume/GetRecordingState/Stop. 
 
![image.png](/.attachments/image-15b6a6c2-405c-4cea-a252-07022254949f.png)

## Step 6 – Capture recording content and delete locations
Once recording is stopped, a `Microsoft.Communication.RecordingFileStatusUpdated` is notified to server side web API. This event contains recording content location and delete location to facilitate users to download and delete the recording accordingly. The `RoomsCallRecording` handles the event and prints out the content and delete locations for references. 

![image.png](/.attachments/image-c7deb3de-b39b-434a-b0b0-af76ff752f51.png)
 
## Step 7 – Download and delete a recording
Once recording content location and delete locations are available, user can call the following operations to download and delete a recording.
 
![image.png](/.attachments/image-92b4913b-3277-4db2-98f6-5f74afe92eba.png)
_download recording file_

![image.png](/.attachments/image-5fdb9b33-41cc-4d99-8569-dc676388be66.png)
_delete recording file_

# References:
[Azure Communication Services Rooms overview - An Azure Communication Services concept document | Microsoft Learn](https://learn.microsoft.com/en-us/azure/communication-services/concepts/rooms/room-concept)

[Azure-Samples/communication-services-web-calling-tutorial: Onboarding sample for web calling capabilities for Azure Communication Services (github.com)](https://github.com/Azure-Samples/communication-services-web-calling-tutorial)

[Azure Communication Services Call Recording overview - An Azure Communication Services concept document | Microsoft Learn](https://learn.microsoft.com/en-us/azure/communication-services/concepts/voice-video-calling/call-recording)

[Azure Communication Services Call Recording API quickstart - An Azure Communication Services document | Microsoft Learn](https://learn.microsoft.com/en-us/azure/communication-services/quickstarts/voice-video-calling/get-started-call-recording?pivots=programming-language-csharp)
