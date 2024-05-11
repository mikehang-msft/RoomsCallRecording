namespace Web.Api;

public class CallingConfiguration
{
    public Uri? CallbackUri { get; set; } = null;

    public Uri? CognitiveServicesUri {get; set;} = null;

    public string? Target {get; set;} = string.Empty;

    public string? CallerId {get; set;} = string.Empty;
}
