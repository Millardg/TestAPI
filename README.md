# Weather Forecast API

[![CI](https://github.com/Millardg/TestAPI/actions/workflows/ci.yml/badge.svg)](https://github.com/Millardg/TestAPI/actions/workflows/ci.yml)

A small ASP.NET Core 8 Web API that returns daily weather forecasts for any city, backed by the free
[Open-Meteo](https://open-meteo.com/) weather and geocoding APIs. No API key required.

It started as the `dotnet new webapi` template and was rebuilt to show how a real service is put
together: a thin controller over an injected service layer, typed `HttpClient`s, validated options,
in-memory caching, RFC 7807 problem responses, Swagger docs generated from XML comments, and a test
suite that runs without touching the network.

## Running it

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
dotnet run --project TestAPI.csproj
```

Swagger UI opens at `http://localhost:5009/swagger`. The `TestAPI.http` file has ready-made requests
you can send from Visual Studio or VS Code.

## Endpoint

```
GET /api/weatherforecast/{city}?days=7
```

| Parameter | Where | Rules | Default |
| --------- | ----- | ----- | ------- |
| `city`    | route | 2 to 100 characters, any place name Open-Meteo can geocode | required |
| `days`    | query | 1 to 16 | 7 |

### Example

```
GET /api/weatherforecast/Seattle?days=2
```

```json
{
  "location": {
    "name": "Seattle",
    "region": "Washington",
    "country": "United States",
    "latitude": 47.60621,
    "longitude": -122.33207,
    "timeZone": "America/Los_Angeles"
  },
  "days": [
    {
      "date": "2026-10-19",
      "temperatureMinC": 6.8,
      "temperatureMaxC": 13.7,
      "temperatureMinF": 44.2,
      "temperatureMaxF": 56.7,
      "weatherCode": 3,
      "summary": "Overcast",
      "windSpeedMaxKph": 10.5,
      "precipitationMm": 0,
      "precipitationProbability": 6
    },
    {
      "date": "2026-10-20",
      "temperatureMinC": 7.2,
      "temperatureMaxC": 17,
      "temperatureMinF": 45,
      "temperatureMaxF": 62.6,
      "weatherCode": 3,
      "summary": "Overcast",
      "windSpeedMaxKph": 11.5,
      "precipitationMm": 0,
      "precipitationProbability": 2
    }
  ],
  "retrievedAt": "2026-10-19T20:22:38.896Z"
}
```

### Responses

| Status | When | Body |
| ------ | ---- | ---- |
| 200 | City resolved and forecast fetched | `CityForecast` |
| 400 | `city` or `days` fails validation | `ValidationProblemDetails` |
| 404 | Geocoder found no match for `city` | `ProblemDetails` |
| 502 | Open-Meteo unreachable or returned an error | `ProblemDetails` |

All error bodies follow [RFC 7807](https://www.rfc-editor.org/rfc/rfc7807) and include a `traceId`.

## How it works

```
Request ─▶ WeatherForecastController
               │  validates city/days, maps null → 404
               ▼
           IWeatherForecastService (OpenMeteoWeatherForecastService)
               │  1. IGeocodingService resolves city → lat/lon  (cached 24 h)
               │  2. GET api.open-meteo.com/v1/forecast          (cached 15 min)
               │  3. zips Open-Meteo's parallel arrays into one object per day
               ▼
           CityForecast
```

- **Service layer and DI.** The controller depends only on `IWeatherForecastService`. Both services are
  registered as typed `HttpClient`s in `Program.cs`, so the base URLs and timeouts come from configuration
  and the handlers are pooled by `IHttpClientFactory`.
- **Options with validation.** The `OpenMeteo` section of `appsettings.json` binds to `OpenMeteoOptions`,
  which is validated with data annotations at startup. A bad URL or out-of-range cache duration fails fast.
- **Caching.** Geocoding results are cached for a day (places do not move) and forecasts for fifteen
  minutes, keyed by rounded coordinates and day count. Misses are cached too, so a repeated unknown
  city never reaches the upstream twice.
- **Error handling.** A global exception handler converts `HttpRequestException` and timeouts into a
  502 problem response and everything else into a generic 500 that never leaks internal details.
- **Weather codes.** Open-Meteo reports WMO 4677 codes. `WeatherCodes` maps them to plain-English
  summaries such as "Partly cloudy" or "Thunderstorm with slight hail".

## Configuration

```json
"OpenMeteo": {
  "GeocodingBaseUrl": "https://geocoding-api.open-meteo.com/",
  "ForecastBaseUrl": "https://api.open-meteo.com/",
  "GeocodingCacheDuration": "1.00:00:00",
  "ForecastCacheDuration": "00:15:00",
  "RequestTimeout": "00:00:10"
}
```

Any value can be overridden with environment variables, e.g. `OpenMeteo__ForecastCacheDuration=00:05:00`.

## Tests

```bash
dotnet test
```

The suite in `tests/TestAPI.Tests` has two layers:

- **Unit tests** for the services, temperature conversion, and weather-code mapping. HTTP is replaced with
  a stub `HttpMessageHandler` fed real Open-Meteo payloads, so they verify the exact query strings sent
  and the exact mapping back.
- **Endpoint tests** that boot the full pipeline with `WebApplicationFactory` and a scripted
  `IWeatherForecastService`, asserting the 200, 400, 404, 502, and 500 paths and that the default day
  count is seven.

CI runs the same build and test on every push and pull request to `main`.

## Project layout

```
TestAPI/
├── Controllers/        HTTP surface only
├── Models/             Public response types + TemperatureConverter
├── Options/            OpenMeteoOptions (bound + validated config)
├── Services/           Interfaces, Open-Meteo implementations, WeatherCodes
│   └── OpenMeteo/      Internal DTOs mirroring the upstream JSON
├── Program.cs          DI registration, Swagger, exception handling
└── tests/TestAPI.Tests Unit + endpoint tests
```

## Credits

Weather data by [Open-Meteo.com](https://open-meteo.com/), licensed under
[CC BY 4.0](https://creativecommons.org/licenses/by/4.0/).

## License

[MIT](LICENSE)
