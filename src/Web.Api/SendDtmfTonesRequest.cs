namespace Web.Api;

public class SendDtmfTonesRequest
{
    public string TargetIdentity {get; set;} = string.Empty;

    public IEnumerable<string> DtmfTones {get; set;} = Enumerable.Empty<string>();
}
