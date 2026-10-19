using System.Net;
using System.Text;

namespace TestAPI.Tests.Support;

/// <summary>
/// An <see cref="HttpMessageHandler"/> that answers every request with a canned response
/// and records what was asked, so services can be tested without touching the network.
/// </summary>
public sealed class StubHttpMessageHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
{
    public List<Uri> Requests { get; } = [];

    public static StubHttpMessageHandler Json(string body) => new(HttpStatusCode.OK, body);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request.RequestUri!);

        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        return Task.FromResult(response);
    }
}
