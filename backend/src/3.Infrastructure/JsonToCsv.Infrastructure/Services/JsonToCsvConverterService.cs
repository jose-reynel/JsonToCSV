using System.Text.Json;
using JsonToCsv.Application.Interfaces;
using JsonToCsv.Domain.Exceptions;
using JsonToCsv.Infrastructure.Csv;

namespace JsonToCsv.Infrastructure.Services;

public class JsonToCsvConverterService : IJsonToCsvConverterService
{
    private const string ScalarColumn = "value";

    public async Task<byte[]> ConvertJsonToCsvAsync(Stream jsonStream, string delimiter = ",", CancellationToken cancellationToken = default)
    {
        JsonDocument doc;
        try
        {
            doc = await JsonDocument.ParseAsync(jsonStream, cancellationToken: cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new DomainException($"Estructura JSON inválida: {ex.Message}");
        }

        using (doc)
        {
            var root = doc.RootElement;
            var records = new List<Dictionary<string, string?>>();

            switch (root.ValueKind)
            {
                case JsonValueKind.Array:
                    foreach (var element in root.EnumerateArray())
                        records.Add(Flatten(element));
                    break;
                case JsonValueKind.Object:
                    records.Add(Flatten(root));
                    break;
                default:
                    throw new DomainException("El JSON debe contener un objeto o un arreglo en la raíz.");
            }

            if (records.Count == 0)
                throw new DomainException("No se encontraron registros para transformar.");

            // Encabezados únicos preservando el orden de primera aparición
            var headers = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var record in records)
                foreach (var key in record.Keys)
                    if (seen.Add(key)) headers.Add(key);

            var writer = new CsvWriterRfc4180(delimiter[0]);
            writer.WriteRow(headers);
            foreach (var record in records)
                writer.WriteRow(headers.Select(h => record.TryGetValue(h, out var v) ? v : null));

            return writer.ToUtf8BomBytes();
        }
    }

    /// <summary>Aplana objetos (a.b) y arreglos (a[0]); un valor escalar suelto usa la columna "value".</summary>
    private static Dictionary<string, string?> Flatten(JsonElement element)
    {
        var result = new Dictionary<string, string?>(StringComparer.Ordinal);
        FlattenInto(element, string.Empty, result);
        return result;
    }

    private static void FlattenInto(JsonElement element, string prefix, Dictionary<string, string?> into)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var any = false;
                foreach (var p in element.EnumerateObject())
                {
                    any = true;
                    FlattenInto(p.Value, prefix.Length == 0 ? p.Name : $"{prefix}.{p.Name}", into);
                }
                if (!any && prefix.Length > 0) into[prefix] = string.Empty; // {} vacío => columna vacía
                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                    FlattenInto(item, $"{prefix}[{index++}]", into);
                if (index == 0 && prefix.Length > 0) into[prefix] = string.Empty; // [] vacío => columna vacía
                break;

            default:
                into[prefix.Length == 0 ? ScalarColumn : prefix] = element.ValueKind switch
                {
                    JsonValueKind.String => element.GetString(),
                    JsonValueKind.Number => element.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => null,
                    _ => element.GetRawText()
                };
                break;
        }
    }
}
