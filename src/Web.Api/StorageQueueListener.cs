
using System.Text.Json;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid.SystemEvents;
using CallAutomation.Contracts;
using JasonShave.AzureStorage.QueueService.Interfaces;
using JasonShave.AzureStorage.QueueService.Services;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Web.Api.Model;

namespace Web.Api;

public class StorageQueueListener : BackgroundService
{
    private readonly AzureStorageQueueClient _azureStorageQueueClient;
    private readonly CallAutomationClient _callAutomationClient;
    private readonly ILogger<StorageQueueListener> _logger;

    public StorageQueueListener(
        IQueueClientFactory queueClientFactory,
        CallAutomationClient callAutomationClient,
        ILogger<StorageQueueListener> logger)
    {
        _azureStorageQueueClient = queueClientFactory.GetQueueClient();
        _callAutomationClient = callAutomationClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Start processing storage queue messages...");

        while (!stoppingToken.IsCancellationRequested)
        {
            await _azureStorageQueueClient.ReceiveMessagesAsync<CloudEvent>(HandleMessage, HandleException);
            await Task.Delay(1000);
        }
    }

    private async ValueTask HandleMessage(CloudEvent? cloudEvent)
    {
        if (cloudEvent == null)
        {
            throw new ArgumentNullException(nameof(cloudEvent));
        }

        _logger.LogInformation($"Processing recording event triggered from Calling SDK.");

        // To Start a call recording, need to capture a ServerCallId which can then used to starte recording
        if (cloudEvent.Type.Equals("Microsoft.Communication.CallStarted") || cloudEvent.Type.Equals("Microsoft.Communication.CallEnded"))
        {
            await CaptureServerCallIdForRecording(cloudEvent).ConfigureAwait(false);
        }

        // To download and/or delete a recording, need to capture recording content location and delete location
        if (cloudEvent.Type.Equals("Microsoft.Communication.RecordingFileStatusUpdated"))
        {
            CaptureRecordingContentLocation(cloudEvent);
        }
    }

    private async Task CaptureServerCallIdForRecording(CloudEvent cloudEvent)
    {
        var callEvent = JsonSerializer.Deserialize<CallEvent>(cloudEvent?.Data);
        _logger.LogInformation($"Server Call Id to use in recording: {callEvent.serverCallId}");
    }

    private void CaptureRecordingContentLocation(CloudEvent? cloudEvent)
    {
        try
        {
            // Parse RecordingFileStatusUpdated and get recording content and delete location
            var recordingFileStatusUpdatedEventData = JsonSerializer.Deserialize<AcsRecordingFileStatusUpdatedEventData>(cloudEvent?.Data);
            string contentLocation = recordingFileStatusUpdatedEventData.RecordingStorageInfo.RecordingChunks[0].ContentLocation;
            string deleteLocation = recordingFileStatusUpdatedEventData.RecordingStorageInfo.RecordingChunks[0].DeleteLocation;
            _logger.LogInformation($"Recording content location: {contentLocation}");
            _logger.LogInformation($"Recording delete location: {deleteLocation}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error download and delete a recording {ex.ToString()}");
        }
    }

    private ValueTask HandleException(Exception exception)
    {
        return ValueTask.CompletedTask;
    }
}
