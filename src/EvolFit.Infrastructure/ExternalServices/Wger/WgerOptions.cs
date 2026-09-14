namespace EvolFit.Infrastructure.ExternalServices.Wger;

public class WgerOptions
{
    public string BaseUrl { get; set; } = "https://wger.de/api/v2/";
    public int CacheTtlDays { get; set; } = 7;
    public int TimeOutSeconds { get; set; } = 30;
    public int Language { get; set; } = 2;
}
