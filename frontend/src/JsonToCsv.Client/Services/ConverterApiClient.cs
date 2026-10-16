using System.Net.Http.Headers;
using System.Text.Json;

namespace JsonToCsv.Client.Services;

public sealed record ConversionResult(bool Success, byte[]? Csv, string? Error);

/// <summary>Cliente HTTP del backend: envía el JSON y el delimitador, devuelve el CSV.</summary>
public sealed class ConverterApiClient
{
    public const long MaxFileBytes = 15 * 1024 * 1024;
    private readonly HttpClient _http;

    public ConverterApiClient(HttpClient http) => _http = http;

    public async Task<ConversionResult> ConvertAsync(Stream json, string fileName, string delimiter, CancellationToken ct = default)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var file = new StreamContent(json);
            file.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            content.Add(file, "file", fileName);
            content.Add(new StringContent(delimiter), "delimiter");

            using var response = await _http.PostAsync("api/converter/transform", content, ct);
            if (response.IsSuccessStatusCode)
                return new ConversionResult(true, await response.Content.ReadAsByteArrayAsync(ct), null);

            return new ConversionResult(false, null, await ReadErrorAsync(response));
        }
        catch (HttpRequestException)
        {
            return new ConversionResult(false, null,
                "No se pudo contactar el servicio de conversión. Verifique que el backend esté en ejecución y que ApiBaseUrl sea correcta.");
        }
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var e) && e.GetString() is { } msg) return msg;
        }
        catch (JsonException) { /* cuerpo no JSON: se usa el texto crudo */ }
        return string.IsNullOrWhiteSpace(body) ? $"Error {(int)response.StatusCode}: {response.ReasonPhrase}" : body;
    }
}
