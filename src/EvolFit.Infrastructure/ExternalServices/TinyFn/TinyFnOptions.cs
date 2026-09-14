namespace EvolFit.Infrastructure.ExternalServices.TinyFn;

public class TinyFnOptions
{
    public string BaseUrl { get; set; } = "https://api.tinyfn.io/v1/health/";
    public string ApiKey { get; set; } = string.Empty;
    public int CacheTtlHours { get; set; } = 24;
    public int TimeOutSeconds { get; set; } = 30;
}
