
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

public class AnswerCallWorker : BackgroundService
{
    private readonly AzureStorageQueueClient _azureStorageQueueClient;
    private readonly CallAutomationClient _callAutomationClient;
    private readonly ILogger<AnswerCallWorker> _logger;
    private static string _recordingId;
    private static string _contentLocation;
    private static string _deleteLocation;

    public AnswerCallWorker(
        IQueueClientFactory queueClientFactory,
        CallAutomationClient callAutomationClient,
        ILogger<AnswerCallWorker> logger)
    {
        _azureStorageQueueClient = queueClientFactory.GetQueueClient();
        _callAutomationClient = callAutomationClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting answer call worker...");
        
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

        // Start call recording 
        if (cloudEvent.Type.Equals("Microsoft.Communication.CallStarted") && string.IsNullOrWhiteSpace(_recordingId))
        {
            await StartRecording(cloudEvent).ConfigureAwait(false);
        }

        // Stop Recording
        if (cloudEvent.Type.Equals("Microsoft.Communication.CallEnded") && !string.IsNullOrWhiteSpace(_recordingId))
        {
            await StopRecording().ConfigureAwait(false);
        }

        // Download and delete recording
        if (cloudEvent.Type.Equals("Microsoft.Communication.RecordingFileStatusUpdated") && !string.IsNullOrWhiteSpace(_recordingId))
        {
            DownloadAndEndRecording(cloudEvent);
        }
    }

    private async Task StartRecording(CallAutomationClient callClient, string serverCallId, ILogger<AnswerCallWorker> logger)
    {
        try
        {
            StartRecordingOptions recordingOptions = new StartRecordingOptions(new ServerCallLocator(serverCallId));
            recordingOptions.RecordingChannel = RecordingChannel.Mixed;
            recordingOptions.RecordingContent = RecordingContent.AudioVideo;
            recordingOptions.RecordingFormat = RecordingFormat.Mp4;

            var startRecordingResponse = await callClient.GetCallRecording()
                .StartAsync(recordingOptions).ConfigureAwait(false);

            _recordingId = startRecordingResponse.Value.RecordingId;

            logger.LogInformation($"Recording started with recording id: {startRecordingResponse.Value.RecordingId} " +
                $"recording state: {startRecordingResponse.Value.RecordingState}");
        }
        catch (Exception ex)
        {
            logger.LogError($"Error start a recording {ex.ToString()}");
        }
    }

    private async Task StopRecording()
    {
        try
        {
            // Stop recording content 
            var stopResponse = await _callAutomationClient.GetCallRecording().StopAsync(_recordingId).ConfigureAwait(false);
            _logger.LogInformation($"Recording Stopped: {stopResponse.Status}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error stop a recording {ex.ToString()}");
        }
    }

    private async Task StartRecording(CloudEvent cloudEvent)
    {
        var callEvent = JsonSerializer.Deserialize<CallEvent>(cloudEvent?.Data);
        await StartRecording(_callAutomationClient, callEvent.serverCallId, _logger);
    }

    private void DownloadAndEndRecording(CloudEvent? cloudEvent)
    {
        try
        {
            // Parse RecordingFileStatusUpdated and get recording content and delete location
            var recordingFileStatusUpdatedEventData = JsonSerializer.Deserialize<AcsRecordingFileStatusUpdatedEventData>(cloudEvent?.Data);
            _contentLocation = recordingFileStatusUpdatedEventData.RecordingStorageInfo.RecordingChunks[0].ContentLocation;
            _deleteLocation = recordingFileStatusUpdatedEventData.RecordingStorageInfo.RecordingChunks[0].DeleteLocation;

            // Download recording content 
            var callRecording = _callAutomationClient.GetCallRecording();
            var downloadResponse = callRecording.DownloadTo(new Uri(_contentLocation), $"RecordingFile_{DateTime.Now.ToString("yyyymmddhhmmss")}.mp4");
            _logger.LogInformation($"Recording downloaded: {downloadResponse.Status}");

            // Delete the recording
            var deleteResponse = _callAutomationClient.GetCallRecording().DeleteAsync(new Uri(_deleteLocation));
            _logger.LogInformation($"Recording deleted: {deleteResponse.Status}");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error download and delete a recording {ex.ToString()}");
        }
        finally
        {
            _recordingId = string.Empty;
        }
    }

    private ValueTask HandleException(Exception exception)
    {
        return ValueTask.CompletedTask;
    }
}
