using System.Net;
using System.Text;
using System.Text.Json;

namespace Psycheflow.Api.UnitTests.Features.Ai;

/// <summary>Responde com um JSON fixo e guarda a requisição enviada pelo SDK do provedor.</summary>
internal sealed class StubHttpHandler(string responseJson, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
{
    public HttpRequestMessage? Request { get; private set; }

    public JsonElement Body { get; private set; }

    public string Header(string name) =>
        Request!.Headers.TryGetValues(name, out IEnumerable<string>? values) ? string.Join(",", values) : string.Empty;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Request = request;
        string body = request.Content is null ? "{}" : await request.Content.ReadAsStringAsync(cancellationToken);
        Body = JsonDocument.Parse(body).RootElement.Clone();

        return new HttpResponseMessage(status)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
        };
    }
}
