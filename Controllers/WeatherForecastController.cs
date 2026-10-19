using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TestAPI.Models;
using TestAPI.Services;

namespace TestAPI.Controllers;

/// <summary>Daily weather forecasts by city, sourced from Open-Meteo.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class WeatherForecastController(
    IWeatherForecastService forecastService,
    ILogger<WeatherForecastController> logger) : ControllerBase
{
    /// <summary>Gets a daily forecast for a city.</summary>
    /// <param name="city">City name to look up, e.g. <c>Seattle</c> or <c>Paris</c>.</param>
    /// <param name="days">Number of days to return, 1 to 16. Defaults to 7.</param>
    /// <param name="cancellationToken">Aborts the upstream call if the client disconnects.</param>
    /// <response code="200">Forecast for the resolved location.</response>
    /// <response code="400">The city or day count failed validation.</response>
    /// <response code="404">No place matched the supplied city name.</response>
    /// <response code="502">The weather provider was unreachable or returned an error.</response>
    [HttpGet("{city}", Name = "GetForecastByCity")]
    [ProducesResponseType<CityForecast>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<CityForecast>> GetByCity(
        [FromRoute, Required, StringLength(100, MinimumLength = 2)] string city,
        [FromQuery, Range(IWeatherForecastService.MinDays, IWeatherForecastService.MaxDays)] int days = 7,
        CancellationToken cancellationToken = default)
    {
        var forecast = await forecastService.GetForecastAsync(city, days, cancellationToken);

        if (forecast is null)
        {
            logger.LogInformation("Forecast requested for unknown city {City}", city);
            return Problem(
                title: "City not found",
                detail: $"No location matched '{city}'.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(forecast);
    }
}
