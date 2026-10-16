using JsonToCsv.Application.Interfaces;
using JsonToCsv.Domain.Exceptions;

namespace JsonToCsv.Application.UseCases;

public class ConvertJsonToCsvUseCase
{
    private readonly IJsonToCsvConverterService _converterService;

    public ConvertJsonToCsvUseCase(IJsonToCsvConverterService converterService)
        => _converterService = converterService;

    /// <summary>Delimitadores admitidos: "," ";" "\t" (o la palabra "tab"). Cualquier otro valor cae a coma.</summary>
    public static string NormalizeDelimiter(string? delimiter) => delimiter switch
    {
        ";" => ";",
        "\t" or "tab" or "\\t" => "\t",
        _ => ","
    };

    public Task<byte[]> ExecuteAsync(Stream jsonStream, string? delimiter = ",", CancellationToken cancellationToken = default)
    {
        if (jsonStream is null || (jsonStream.CanSeek && jsonStream.Length == 0))
            throw new DomainException("El archivo JSON suministrado está vacío.");

        return _converterService.ConvertJsonToCsvAsync(jsonStream, NormalizeDelimiter(delimiter), cancellationToken);
    }
}
