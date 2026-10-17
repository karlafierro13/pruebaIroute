using Iroute.Commerce.Application.Abstractions;
using Iroute.Commerce.Application.Dtos;
using Iroute.Commerce.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Iroute.Commerce.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/commerce")]
[Produces("application/json")]
public sealed class CommerceController(ICommerceService commerceService) : ControllerBase
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;

    /// <summary>
    /// Carga el archivo commerce_DDMMYYYY.csv y registra sus filas mediante sp_create_commerce.
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileSizeBytes)]
    [ProducesResponseType<UploadResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UploadResultDto>> Upload(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return Problem(title: "El archivo se encuentra vacío.", statusCode: StatusCodes.Status400BadRequest);

        await using var stream = file.OpenReadStream();
        return Ok(await commerceService.UploadAsync(file.FileName, stream, ct));
    }

    /// <summary>
    /// Valida los registros de la fecha de proceso (sp_validate_commerce) y
    /// retorna la cantidad de registros enviados a commerce_quarantine.
    /// </summary>
    [HttpPost("process")]
    [ProducesResponseType<ProcessResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProcessResultDto>> Process([FromBody] ProcessRequestDto request, CancellationToken ct)
        => Ok(await commerceService.ProcessAsync(request.ProcessDate, ct));

    /// <summary>
    /// Lista los comercios de commerce_quarantine (filtro opcional por fecha de proceso).
    /// </summary>
    [HttpGet("quarantine")]
    [ProducesResponseType<IReadOnlyList<QuarantineRecord>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<QuarantineRecord>>> GetQuarantine(
        [FromQuery] DateOnly? processDate, CancellationToken ct)
        => Ok(await commerceService.GetQuarantineAsync(processDate, ct));

    /// <summary>
    /// Corrige un registro en cuarentena y lo vuelve a validar. Si queda válido
    /// regresa a commerce; si no, se actualiza su motivo.
    /// </summary>
    [HttpPut("quarantine/{id:long}")]
    [ProducesResponseType<UpdateQuarantineResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateQuarantineResultDto>> UpdateQuarantine(
        long id, [FromBody] UpdateQuarantineDto request, CancellationToken ct)
        => Ok(await commerceService.UpdateQuarantineAsync(id, request, ct));

    /// <summary>
    /// Descarta definitivamente un registro en cuarentena.
    /// </summary>
    [HttpDelete("quarantine/{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteQuarantine(long id, CancellationToken ct)
    {
        await commerceService.DeleteQuarantineAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// Fechas de proceso cargadas en commerce con su cantidad de registros.
    /// </summary>
    [HttpGet("process-dates")]
    [ProducesResponseType<IReadOnlyList<ProcessDateSummary>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProcessDateSummary>>> GetProcessDates(CancellationToken ct)
        => Ok(await commerceService.GetProcessDatesAsync(ct));
}
