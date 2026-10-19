using System.Reflection;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using TestAPI.Options;
using TestAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuration -----------------------------------------------------------------------------------
builder.Services
    .AddOptions<OpenMeteoOptions>()
    .BindConfiguration(OpenMeteoOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Application services ------------------------------------------------------------------------
builder.Services.AddMemoryCache();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddHttpClient<IGeocodingService, OpenMeteoGeocodingService>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<OpenMeteoOptions>>().Value;
    client.BaseAddress = new Uri(options.GeocodingBaseUrl);
    client.Timeout = options.RequestTimeout;
});

builder.Services.AddHttpClient<IWeatherForecastService, OpenMeteoWeatherForecastService>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<OpenMeteoOptions>>().Value;
    client.BaseAddress = new Uri(options.ForecastBaseUrl);
    client.Timeout = options.RequestTimeout;
});

// Web plumbing --------------------------------------------------------------------------------
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Weather Forecast API",
        Version = "v1",
        Description = "Daily forecasts by city name, backed by the free Open-Meteo API.",
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Map upstream failures to 502 and everything else to a generic 500, always as ProblemDetails.
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var isUpstream = exception is HttpRequestException or TaskCanceledException;

    context.Response.StatusCode = isUpstream
        ? StatusCodes.Status502BadGateway
        : StatusCodes.Status500InternalServerError;

    var problemDetailsService = context.RequestServices.GetRequiredService<IProblemDetailsService>();
    await problemDetailsService.WriteAsync(new ProblemDetailsContext
    {
        HttpContext = context,
        ProblemDetails =
        {
            Title = isUpstream ? "Weather provider unavailable" : "An unexpected error occurred",
            Detail = isUpstream ? "The upstream weather service could not be reached. Try again shortly." : null,
            Status = context.Response.StatusCode,
        },
    });
}));

app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

/// <summary>Exposed so the test project can spin the app up with <c>WebApplicationFactory</c>.</summary>
public partial class Program;
