using JsonToCsv.Application.UseCases;
using JsonToCsv.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace JsonToCsv.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConverterController : ControllerBase
{
    private readonly ConvertJsonToCsvUseCase _useCase;
    private readonly ILogger<ConverterController> _logger;

    public ConverterController(ConvertJsonToCsvUseCase useCase, ILogger<ConverterController> logger)
    {
        _useCase = useCase;
        _logger = logger;
    }

    [HttpPost("transform")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> Transform(IFormFile? file, [FromForm] string? delimiter, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Debe adjuntar un archivo JSON válido." });

        try
        {
            await using var stream = file.OpenReadStream();
            var csv = await _useCase.ExecuteAsync(stream, delimiter, ct);
            var name = Path.GetFileNameWithoutExtension(file.FileName) + ".csv";
            return File(csv, "text/csv; charset=utf-8", name);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error procesando la conversión");
            return StatusCode(500, new { error = "Ocurrió un error interno procesando la solicitud." });
        }
    }
}
