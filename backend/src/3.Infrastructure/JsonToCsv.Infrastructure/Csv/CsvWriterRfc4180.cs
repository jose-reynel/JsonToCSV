using System.Text;

namespace JsonToCsv.Infrastructure.Csv;

/// <summary>
/// Escritor CSV propio (RFC 4180) sin dependencias externas.
/// Entrecomilla campos con delimitador, comillas, CR o LF; duplica las comillas internas; fin de línea CRLF.
/// </summary>
public sealed class CsvWriterRfc4180
{
    private readonly StringBuilder _sb = new();
    private readonly char _delimiter;

    public CsvWriterRfc4180(char delimiter) => _delimiter = delimiter;

    public void WriteRow(IEnumerable<string?> fields)
    {
        var first = true;
        foreach (var field in fields)
        {
            if (!first) _sb.Append(_delimiter);
            first = false;
            AppendField(field);
        }
        _sb.Append("\r\n");
    }

    private void AppendField(string? value)
    {
        if (string.IsNullOrEmpty(value)) return;

        var needsQuotes = NeedsQuotes(value);
        if (!needsQuotes) { _sb.Append(value); return; }

        _sb.Append('"').Append(value.Replace("\"", "\"\"")).Append('"');
    }

    private bool NeedsQuotes(string value)
    {
        foreach (var c in value)
            if (c == _delimiter || c == '"' || c == '\r' || c == '\n') return true;
        return false;
    }

    /// <summary>UTF-8 con BOM para que Excel/LibreOffice detecten tildes y ñ al abrir con doble clic.</summary>
    public byte[] ToUtf8BomBytes()
    {
        var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        var body = utf8.GetBytes(_sb.ToString());
        var preamble = utf8.GetPreamble();
        var result = new byte[preamble.Length + body.Length];
        preamble.CopyTo(result, 0);
        body.CopyTo(result, preamble.Length);
        return result;
    }
}
