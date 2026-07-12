using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);
var apiBaseUrl = builder.Configuration["HUSK_API_BASE_URL"] ?? "http://localhost:5081";

builder.Services.AddHttpClient("HuskApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapMethods("/api/{**path}", ["GET", "POST", "PUT", "PATCH", "DELETE"], ProxyApiRequestAsync);
app.MapFallbackToFile("index.html");

app.Run();

static async Task ProxyApiRequestAsync(
    HttpContext context,
    IHttpClientFactory httpClientFactory,
    string path,
    CancellationToken cancellationToken)
{
    var client = httpClientFactory.CreateClient("HuskApi");
    using var request = new HttpRequestMessage(
        new HttpMethod(context.Request.Method),
        $"api/{path}{context.Request.QueryString}");

    CopyRequestHeaders(context, request);

    if (HasRequestBody(context.Request))
    {
        request.Content = new StreamContent(context.Request.Body);
        CopyRequestContentHeaders(context, request.Content.Headers);
    }

    using var response = await client.SendAsync(
        request,
        HttpCompletionOption.ResponseHeadersRead,
        cancellationToken);

    context.Response.StatusCode = (int)response.StatusCode;
    CopyResponseHeaders(context, response);

    await response.Content.CopyToAsync(context.Response.Body, cancellationToken);
}

static bool HasRequestBody(HttpRequest request)
{
    return request.ContentLength > 0
        || HttpMethods.IsPost(request.Method)
        || HttpMethods.IsPut(request.Method)
        || HttpMethods.IsPatch(request.Method);
}

static void CopyRequestHeaders(HttpContext context, HttpRequestMessage request)
{
    foreach (var header in context.Request.Headers)
    {
        if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase)
            || header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
    }
}

static void CopyRequestContentHeaders(HttpContext context, HttpContentHeaders headers)
{
    foreach (var header in context.Request.Headers)
    {
        if (header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
        {
            headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
    }
}

static void CopyResponseHeaders(HttpContext context, HttpResponseMessage response)
{
    foreach (var header in response.Headers)
    {
        context.Response.Headers[header.Key] = header.Value.ToArray();
    }

    foreach (var header in response.Content.Headers)
    {
        context.Response.Headers[header.Key] = header.Value.ToArray();
    }

    context.Response.Headers.Remove("transfer-encoding");
}
