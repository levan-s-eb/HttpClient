using HttpClient.UsabilityTest.ServiceClients;
using Microsoft.AspNetCore.Mvc;

namespace HttpClient.UsabilityTest.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController(IWeatherService weatherService) : ControllerBase
    {
        [HttpGet("test-fake-service")]
        public async Task<IActionResult> Get(CancellationToken ct)
        {
            for (var i = 0; i < 50_000; i++)
            {
                try
                {
                    var response = await weatherService.Forecast(ct);
                    Console.WriteLine($"Received {response.Count} weather forecasts");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.GetType().ToString());
                }
            }

            return Ok();
        }
    }
}
