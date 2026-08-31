using Kitch.Application.DTOs.ContratosSub;
using Kitch.Application.Interfaces;
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
        var adminId = GetUsuarioIdOrThrow();
        var contratos = await _contratoSubService.GetAllAsync(adminId);
        return Ok(contratos);
    }

    [HttpGet("mios")]
    public async Task<ActionResult<IEnumerable<ContratoSubResponseDto>>> GetMios()
    {
        var usuarioId = GetUsuarioIdOrThrow();
        var contratos = await _contratoSubService.GetByUsuarioIdAsync(usuarioId);
        return Ok(contratos);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ContratoSubResponseDto>> GetById(int id)
    {
        var adminId = GetUsuarioIdOrThrow();
        var contrato = await _contratoSubService.GetByIdAsync(id, adminId);

        if (contrato is null)
        {
            return NotFound();
        }

        return Ok(contrato);
    }

    [HttpPost]
    public async Task<ActionResult<ContratoSubResponseDto>> Create([FromBody] ContratoSubCreateDto contratoSub)
    {
        var adminId = GetUsuarioIdOrThrow();
        var createdContratoSub = await _contratoSubService.CreateAsync(contratoSub, adminId);
        return CreatedAtAction(nameof(GetById), new { id = createdContratoSub.Id }, createdContratoSub);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ContratoSubUpdateDto contratoSub)
    {
        var adminId = GetUsuarioIdOrThrow();
        var updated = await _contratoSubService.UpdateAsync(id, contratoSub, adminId);

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var adminId = GetUsuarioIdOrThrow();
        var deleted = await _contratoSubService.DeleteAsync(id, adminId);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
