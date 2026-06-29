using Kitch.Application.DTOs.ContratosSub;
using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContratosSubController : ApiControllerBase
{
    private readonly IContratoSubService _contratoSubService;

    public ContratosSubController(IContratoSubService contratoSubService)
    {
        _contratoSubService = contratoSubService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ContratoSubResponseDto>>> GetAll()
    {
        var contratos = await _contratoSubService.GetAllAsync();
        return Ok(contratos);
    }

    // Tus propios contratos. El usuario sale del token; no se pasa id ni email por la URL.
    [HttpGet("mios")]
    public async Task<ActionResult<IEnumerable<ContratoSubResponseDto>>> GetMios()
    {
        if (!TryGetUsuarioId(out var usuarioId))
        {
            return Unauthorized("No se pudo identificar al usuario a partir del token.");
        }

        var contratos = await _contratoSubService.GetByUsuarioIdAsync(usuarioId);
        return Ok(contratos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ContratoSubResponseDto>> GetById(int id)
    {
        var contrato = await _contratoSubService.GetByIdAsync(id);

        if (contrato is null)
        {
            return NotFound();
        }

        return Ok(contrato);
    }

    [HttpPost]
    public async Task<ActionResult<ContratoSubResponseDto>> Create([FromBody] ContratoSubCreateDto contratoSub)
    {
        var createdContratoSub = await _contratoSubService.CreateAsync(contratoSub);
        return Created(string.Empty, createdContratoSub);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ContratoSubUpdateDto contratoSub)
    {
        var updated = await _contratoSubService.UpdateAsync(id, contratoSub);

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _contratoSubService.DeleteAsync(id);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
