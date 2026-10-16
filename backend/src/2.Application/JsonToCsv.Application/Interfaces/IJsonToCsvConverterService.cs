namespace JsonToCsv.Application.Interfaces;

public interface IJsonToCsvConverterService
{
    Task<byte[]> ConvertJsonToCsvAsync(Stream jsonStream, string delimiter = ",", CancellationToken cancellationToken = default);
}
