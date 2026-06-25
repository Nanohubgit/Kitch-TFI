using Kitch.Application.DTOs.Sustitutos;
using Kitch.Application.Interfaces;
using Kitch.Application.Mappings;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Infrastructure.Services;

public class SustitutoService : ISustitutoService
{
    private readonly IRepository<IngredienteSustituto> _repository;
    private readonly IRepository<Ingrediente> _ingredienteRepository;

    public SustitutoService(
        IRepository<IngredienteSustituto> repository,
        IRepository<Ingrediente> ingredienteRepository)
    {
        _repository = repository;
        _ingredienteRepository = ingredienteRepository;
    }

    public async Task<IEnumerable<SustitutoResponseDto>> GetAllAsync()
    {
        var sustitutos = await _repository.FindWithIncludesAsync(
            sustituto => true,
            sustituto => sustituto.Ingrediente,
            sustituto => sustituto.Sustituto);

        return sustitutos.Select(sustituto => sustituto.ToResponseDto());
    }

    public async Task<IEnumerable<SustitutoResponseDto>> GetByIngredienteIdAsync(int ingredienteId)
    {
        var sustitutos = await _repository.FindWithIncludesAsync(
            sustituto => sustituto.IngredienteId == ingredienteId,
            sustituto => sustituto.Ingrediente,
            sustituto => sustituto.Sustituto);

        return sustitutos.Select(sustituto => sustituto.ToResponseDto());
    }

    public async Task<SustitutoResponseDto?> GetByIdAsync(int id)
    {
        var sustitutos = await _repository.FindWithIncludesAsync(
            sustituto => sustituto.Id == id,
            sustituto => sustituto.Ingrediente,
            sustituto => sustituto.Sustituto);

        return sustitutos.FirstOrDefault()?.ToResponseDto();
    }

    public async Task<SustitutoResponseDto> CreateAsync(SustitutoCreateDto sustituto)
    {
        await ValidateSustitutoAsync(sustituto.IngredienteId, sustituto.SustitutoId);

        if (await _repository.AnyAsync(existing =>
                existing.IngredienteId == sustituto.IngredienteId &&
                existing.SustitutoId == sustituto.SustitutoId))
        {
            throw new InvalidOperationException("Ese sustituto ya esta registrado para el ingrediente.");
        }

        var entity = new IngredienteSustituto
        {
            IngredienteId = sustituto.IngredienteId,
            SustitutoId = sustituto.SustitutoId,
            Motivo = sustituto.Motivo?.Trim()
        };

        await _repository.AddAsync(entity);
        var created = await GetByPairAsync(entity.IngredienteId, entity.SustitutoId);

        return created?.ToResponseDto()
            ?? throw new InvalidOperationException("No se pudo cargar el sustituto creado.");
    }

    public async Task<bool> UpdateAsync(int id, SustitutoUpdateDto sustituto)
    {
        var existingSustituto = await _repository.GetByIdAsync(id);

        if (existingSustituto is null)
        {
            return false;
        }

        await ValidateSustitutoAsync(sustituto.IngredienteId, sustituto.SustitutoId);

        if (await _repository.AnyAsync(existing =>
                existing.Id != id &&
                existing.IngredienteId == sustituto.IngredienteId &&
                existing.SustitutoId == sustituto.SustitutoId))
        {
            throw new InvalidOperationException("Ese sustituto ya esta registrado para el ingrediente.");
        }

        existingSustituto.IngredienteId = sustituto.IngredienteId;
        existingSustituto.SustitutoId = sustituto.SustitutoId;
        existingSustituto.Motivo = sustituto.Motivo?.Trim();

        await _repository.UpdateAsync(existingSustituto);

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var sustituto = await _repository.GetByIdAsync(id);

        if (sustituto is null)
        {
            return false;
        }

        await _repository.DeleteAsync(sustituto);

        return true;
    }

    private async Task ValidateSustitutoAsync(int ingredienteId, int sustitutoId)
    {
        if (ingredienteId == sustitutoId)
        {
            throw new InvalidOperationException("Un ingrediente no puede ser sustituto de si mismo.");
        }

        if (!await _ingredienteRepository.AnyAsync(ingrediente => ingrediente.Id == ingredienteId))
        {
            throw new InvalidOperationException("El ingrediente principal no existe.");
        }

        if (!await _ingredienteRepository.AnyAsync(ingrediente => ingrediente.Id == sustitutoId))
        {
            throw new InvalidOperationException("El ingrediente sustituto no existe.");
        }
    }

    private async Task<IngredienteSustituto?> GetByPairAsync(int ingredienteId, int sustitutoId)
    {
        var sustitutos = await _repository.FindWithIncludesAsync(
            sustituto => sustituto.IngredienteId == ingredienteId && sustituto.SustitutoId == sustitutoId,
            sustituto => sustituto.Ingrediente,
            sustituto => sustituto.Sustituto);

        return sustitutos.FirstOrDefault();
    }
}
