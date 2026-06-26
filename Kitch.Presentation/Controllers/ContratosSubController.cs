using Kitch.Application.DTOs.ContratosSub;
using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Kitch.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContratosSubController : ControllerBase
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

    [HttpGet("usuario/{usuarioId:int}")]
    public async Task<ActionResult<IEnumerable<ContratoSubResponseDto>>> GetByUsuarioId(int usuarioId)
    {
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
