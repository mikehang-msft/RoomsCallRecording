using Azure;
using Azure.Communication;
using Azure.Communication.Rooms;
using Microsoft.AspNetCore.Mvc;

namespace Web.Api;

public static class WebApplicationExtensions
{
    public static WebApplication AddRoomsApiMappings(this WebApplication app)
    {
        app.MapPost("api/room", async ([FromBody] IEnumerable<string>? participants, RoomsClient roomsClient) =>
        {
            var roomsParticipants = new List<RoomParticipant>();
            foreach(var participant in participants)
            {
                roomsParticipants.Add(new RoomParticipant(new CommunicationUserIdentifier(participant))
                {
                    Role = ParticipantRole.Presenter
                });
            }

            CreateRoomOptions createRoomOptions = new CreateRoomOptions()
            {
                ValidFrom = DateTimeOffset.UtcNow,
                ValidUntil = DateTimeOffset.UtcNow.AddDays(30),
                PstnDialOutEnabled = true,
                Participants = roomsParticipants,
            };

            CommunicationRoom room = await roomsClient.CreateRoomAsync(createRoomOptions);
            return Results.Ok(room);
        });

        app.MapGet("/api/room/{roomId}", (string roomId, RoomsClient roomsClient) =>
        {
            var response = roomsClient.GetRoomAsync(roomId);
            return Results.Ok(response.Result);
        });

        app.MapGet("/api/room/{roomId}/participants", async (string roomId, RoomsClient roomsClient) =>
        {
            List<RoomParticipant> roomParticipants = new List<RoomParticipant>();
            // Get list of participants in room
            AsyncPageable<RoomParticipant> existingParticipants = roomsClient.GetParticipantsAsync(roomId);
            await foreach (RoomParticipant participant in existingParticipants)
            {
                roomParticipants.Add(participant);
            }

            return Results.Ok(roomParticipants);
        });

        return app;
    }

    // need this because async streams can't be used in a lambda function
    private static async IAsyncEnumerable<T> ProcessData<T>(AsyncPageable<T> data)
    where T: notnull
    {
        await foreach(var item in data)
        {
            yield return item;
        }
    }
}
