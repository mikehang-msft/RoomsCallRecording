using Azure.Communication.CallAutomation;

namespace Web.Api;

public static class ToneConverter
{
    public static DtmfTone ConvertToDtmfTone(this string tone)
    {
        if (tone == DtmfTone.One) return DtmfTone.One;
        if (tone == DtmfTone.Two) return DtmfTone.Two;
        if (tone == DtmfTone.Three) return DtmfTone.Three;
        if (tone == DtmfTone.Four) return DtmfTone.Four;
        if (tone == DtmfTone.Five) return DtmfTone.Five;
        if (tone == DtmfTone.Six) return DtmfTone.Six;
        if (tone == DtmfTone.Seven) return DtmfTone.Seven;
        if (tone == DtmfTone.Eight) return DtmfTone.Eight;
        if (tone == DtmfTone.Nine) return DtmfTone.Nine;
        if (tone == DtmfTone.Zero) return DtmfTone.Zero;

        throw new ApplicationException("Could not determine DTMF tone from the input provided.");
    }
}
