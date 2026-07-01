using Kitch.Application.DTOs.Sustitutos;
using Kitch.Application.Interfaces;
using Kitch.Application.Mappings;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class SustitutoService : ISustitutoService
{
    private readonly IRepository<SustitutoIngrediente> _repository;
    private readonly IRepository<Ingrediente> _ingredienteRepository;

    public SustitutoService(
        IRepository<SustitutoIngrediente> repository,
        IRepository<Ingrediente> ingredienteRepository)
    {
        _repository = repository;
        _ingredienteRepository = ingredienteRepository;
    }

    public async Task<IEnumerable<SustitutoResponseDto>> GetAllAsync()
    {
        var sustitutos = await _repository.FindWithIncludesAsync(
            sustituto => true,
            sustituto => sustituto.IngredienteOriginal,
            sustituto => sustituto.IngredienteSustituto);

        return sustitutos.Select(sustituto => sustituto.ToResponseDto());
    }

    public async Task<IEnumerable<SustitutoResponseDto>> GetByIngredienteIdAsync(int ingredienteId)
    {
        var sustitutos = await _repository.FindWithIncludesAsync(
            sustituto => sustituto.IngredienteOriginalId == ingredienteId,
            sustituto => sustituto.IngredienteOriginal,
            sustituto => sustituto.IngredienteSustituto);

        return sustitutos.Select(sustituto => sustituto.ToResponseDto());
    }

    public async Task<SustitutoResponseDto?> GetByIdAsync(int id)
    {
        var sustitutos = await _repository.FindWithIncludesAsync(
            sustituto => sustituto.Id == id,
            sustituto => sustituto.IngredienteOriginal,
            sustituto => sustituto.IngredienteSustituto);

        return sustitutos.FirstOrDefault()?.ToResponseDto();
    }

    public async Task<SustitutoResponseDto> CreateAsync(SustitutoCreateDto sustituto)
    {
        await ValidateSustitutoAsync(
            sustituto.IngredienteOriginalId,
            sustituto.IngredienteSustitutoId,
            sustituto.FactorEquivalencia);

        if (await _repository.AnyAsync(existing =>
                existing.IngredienteOriginalId == sustituto.IngredienteOriginalId &&
                existing.IngredienteSustitutoId == sustituto.IngredienteSustitutoId))
        {
            throw new InvalidOperationException("Ese sustituto ya esta registrado para el ingrediente.");
        }

        var entity = new SustitutoIngrediente
        {
            IngredienteOriginalId = sustituto.IngredienteOriginalId,
            IngredienteSustitutoId = sustituto.IngredienteSustitutoId,
            FactorEquivalencia = sustituto.FactorEquivalencia,
            Notas = sustituto.Notas?.Trim()
        };

        await _repository.AddAsync(entity);
        var created = await GetByPairAsync(entity.IngredienteOriginalId, entity.IngredienteSustitutoId);

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

        await ValidateSustitutoAsync(
            sustituto.IngredienteOriginalId,
            sustituto.IngredienteSustitutoId,
            sustituto.FactorEquivalencia);

        if (await _repository.AnyAsync(existing =>
                existing.Id != id &&
                existing.IngredienteOriginalId == sustituto.IngredienteOriginalId &&
                existing.IngredienteSustitutoId == sustituto.IngredienteSustitutoId))
        {
            throw new InvalidOperationException("Ese sustituto ya esta registrado para el ingrediente.");
        }

        existingSustituto.IngredienteOriginalId = sustituto.IngredienteOriginalId;
        existingSustituto.IngredienteSustitutoId = sustituto.IngredienteSustitutoId;
        existingSustituto.FactorEquivalencia = sustituto.FactorEquivalencia;
        existingSustituto.Notas = sustituto.Notas?.Trim();

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

    private async Task ValidateSustitutoAsync(int ingredienteOriginalId, int ingredienteSustitutoId, decimal factorEquivalencia)
    {
        if (ingredienteOriginalId == ingredienteSustitutoId)
        {
            throw new InvalidOperationException("Un ingrediente no puede ser sustituto de si mismo.");
        }

        if (factorEquivalencia <= 0)
        {
            throw new InvalidOperationException("El factor de equivalencia debe ser mayor a cero.");
        }

        if (!await _ingredienteRepository.AnyAsync(ingrediente => ingrediente.Id == ingredienteOriginalId))
        {
            throw new InvalidOperationException("El ingrediente principal no existe.");
        }

        if (!await _ingredienteRepository.AnyAsync(ingrediente => ingrediente.Id == ingredienteSustitutoId))
        {
            throw new InvalidOperationException("El ingrediente sustituto no existe.");
        }
    }

    private async Task<SustitutoIngrediente?> GetByPairAsync(int ingredienteOriginalId, int ingredienteSustitutoId)
    {
        var sustitutos = await _repository.FindWithIncludesAsync(
            sustituto => sustituto.IngredienteOriginalId == ingredienteOriginalId &&
                         sustituto.IngredienteSustitutoId == ingredienteSustitutoId,
            sustituto => sustituto.IngredienteOriginal,
            sustituto => sustituto.IngredienteSustituto);

        return sustitutos.FirstOrDefault();
    }
}
