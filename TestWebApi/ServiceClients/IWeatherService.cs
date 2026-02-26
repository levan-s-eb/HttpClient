using Refit;

namespace TestWebApi.ServiceClients;

public interface IWeatherService
{
    [Get("/weatherforecast")]
    Task<List<WeatherDTO>> Forecast(CancellationToken ct = default);
}

public class WeatherDTO
{
    public DateTime Date { get; set; }
    public int TemperatureC { get; set; }
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    public string Summary { get; set; } = null!;
}
