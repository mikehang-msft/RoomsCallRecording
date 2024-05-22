using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Communication.Rooms;
using Azure.Messaging;
using JasonShave.AzureStorage.QueueService.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Concurrent;
using Web.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHostedService<StorageQueueListener>();

builder.Services.AddSingleton(new CallAutomationClient(builder.Configuration["Acs:ConnectionString"]));
builder.Services.AddSingleton(new RoomsClient(builder.Configuration["Acs:ConnectionString"]));

var callbackHost = $"{builder.Configuration["Acs:CallbackUri"] ?? builder.Configuration["VS_TUNNEL_URL"]}" + "/api/callbacks";

builder.Services.AddAzureStorageQueueClient(x => x.AddDefaultClient(y =>
{
    y.ConnectionString = builder.Configuration["Storage:ConnectionString"];
    y.QueueName = builder.Configuration["Storage:QueueName"];
}));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("api/recording/{serverCallId}/start", async ([FromRoute] string serverCallId, CallAutomationClient client, ILogger<Program> logger) =>
{
    try
    {
        StartRecordingOptions recordingOptions = new StartRecordingOptions(new ServerCallLocator(serverCallId));
        recordingOptions.RecordingChannel = RecordingChannel.Mixed;
        recordingOptions.RecordingContent = RecordingContent.AudioVideo;
        recordingOptions.RecordingFormat = RecordingFormat.Mp4;

        var startRecordingResponse = await client.GetCallRecording()
            .StartAsync(recordingOptions).ConfigureAwait(false);

        var recordingId = startRecordingResponse.Value.RecordingId;

        logger.LogInformation($"Recording started with recording id: {startRecordingResponse.Value.RecordingId} " +
            $"recording state: {startRecordingResponse.Value.RecordingState}");
    }
    catch (Exception ex)
    {
        logger.LogError($"Error start a recording {ex.ToString()}");
    }
});

app.MapPost("api/recording/{recordingId}/stop", async ([FromRoute] string recordingId, CallAutomationClient client, ILogger<Program> logger) =>
{
    try
    {
        // Stop recording content 
        var stopResponse = await client.GetCallRecording().StopAsync(recordingId).ConfigureAwait(false);
        logger.LogInformation($"Recording Stopped: {stopResponse.Status}");
    }
    catch (Exception ex)
    {
        logger.LogError($"Error stop a recording {ex.ToString()}");
    }
});

app.MapPost("api/recording/{recordingId}/pause", async ([FromRoute] string recordingId, CallAutomationClient client, ILogger<Program> logger) =>
{
    try
    {
        // Pause recording content 
        var pauseResponse = await client.GetCallRecording().PauseAsync(recordingId).ConfigureAwait(false);
        logger.LogInformation($"Recording Paused: {pauseResponse.Status}");
    }
    catch (Exception ex)
    {
        logger.LogError($"Error stop a recording {ex.ToString()}");
    }
});

app.MapPost("api/recording/{recordingId}/resume", async ([FromRoute] string recordingId, CallAutomationClient client, ILogger<Program> logger) =>
{
    try
    {
        // Resume recording content 
        var resumeResponse = await client.GetCallRecording().ResumeAsync(recordingId).ConfigureAwait(false);
        logger.LogInformation($"Recording Resumed: {resumeResponse.Status}");
    }
    catch (Exception ex)
    {
        logger.LogError($"Error stop a recording {ex.ToString()}");
    }
});

app.MapPost("api/recording/{recordingId}/GetRecordingState", async ([FromRoute] string recordingId, CallAutomationClient client, ILogger<Program> logger) =>
{
    try
    {
        // Resume recording content 
        var stateResponse = await client.GetCallRecording().GetStateAsync(recordingId).ConfigureAwait(false);
        logger.LogInformation($"Recording state: {stateResponse}");

        return stateResponse;
    }
    catch (Exception ex)
    {
        logger.LogError($"Error stop a recording {ex.ToString()}");
        return null;
    }
});

app.MapPost("api/recording/{contentLocation}/download", ([FromRoute] string contentLocation, CallAutomationClient client, ILogger<Program> logger) =>
{
    try
    {
        // Download recording content 
        var callRecording = client.GetCallRecording();
        string locationUrl = Uri.UnescapeDataString(contentLocation);
        var downloadResponse = callRecording.DownloadTo(new Uri(locationUrl), $"RecordingFile_{DateTime.Now.ToString("yyyymmddhhmmss")}.mp4");
        logger.LogInformation($"Recording downloaded: {downloadResponse.Status}");
    }
    catch (Exception ex)
    {
        logger.LogError($"Error stop a recording {ex.ToString()}");
    }
});

app.MapPost("api/recording/{deleteLocation}/delete", async ([FromRoute] string deleteLocation, CallAutomationClient client, ILogger<Program> logger) =>
{
    try
    {
        // Delete the recording
        string locationUrl = Uri.UnescapeDataString(deleteLocation);
        var deleteResponse = await client.GetCallRecording().DeleteAsync(new Uri(locationUrl));
        logger.LogInformation($"Recording deleted: {deleteResponse.Status}");
    }
    catch (Exception ex)
    {
        logger.LogError($"Error stop a recording {ex.ToString()}");
    }
});

app.AddRoomsApiMappings();

app.Run();