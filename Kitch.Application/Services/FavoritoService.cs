using Kitch.Application.DTOs.Favoritos;
using Kitch.Application.Exceptions;
using Kitch.Application.Interfaces;
using Kitch.Application.Mappings;
using Kitch.Domain.Constants;
using Kitch.Domain.Entities;
using Kitch.Domain.Interfaces;

namespace Kitch.Application.Services;

public class FavoritoService : IFavoritoService
{
    private readonly IRepository<RecetaFavorita> _repository;
    private readonly IRepository<Usuario> _usuarioRepository;
    private readonly IRepository<Receta> _recetaRepository;

    public FavoritoService(
        IRepository<RecetaFavorita> repository,
        IRepository<Usuario> usuarioRepository,
        IRepository<Receta> recetaRepository)
    {
        _repository = repository;
        _usuarioRepository = usuarioRepository;
        _recetaRepository = recetaRepository;
    }

    public async Task<IEnumerable<FavoritoResponseDto>> GetByUsuarioIdAsync(int usuarioId)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId);
        var favoritos = await _repository.FindWithIncludesAsync(
            favorito => favorito.UsuarioId == usuarioId,
            favorito => favorito.Usuario,
            favorito => favorito.Receta);

        return favoritos
            .Where(favorito =>
                favorito.Receta is not null &&
                LimitesPlan.PuedeUsarDificultad(usuario?.Rol, favorito.Receta.Dificultad))
            .Select(favorito => favorito.ToResponseDto());
    }

    public async Task<FavoritoResponseDto?> GetByIdAsync(int id, int usuarioId)
    {
        var favoritos = await _repository.FindWithIncludesAsync(
            favorito => favorito.Id == id && favorito.UsuarioId == usuarioId,
            favorito => favorito.Usuario,
            favorito => favorito.Receta);

        return favoritos.FirstOrDefault()?.ToResponseDto();
    }

    public async Task<FavoritoResponseDto> AddFavoritoAsync(FavoritoCreateDto favorito)
    {
        if (await ExisteFavoritoAsync(favorito.UsuarioId, favorito.RecetaId))
        {
            throw new InvalidOperationException("La receta ya está marcada como favorita para este usuario.");
        }

        await AsegurarCupoFavoritosAsync(favorito.UsuarioId);
        await AsegurarRecetaVisibleAsync(favorito.UsuarioId, favorito.RecetaId);

        var entity = new RecetaFavorita
        {
            UsuarioId = favorito.UsuarioId,
            RecetaId = favorito.RecetaId
        };

        var created = await _repository.AddAsync(entity);
        return created.ToResponseDto();
    }

    public async Task<bool> ToggleFavoritoAsync(int usuarioId, int recetaId)
    {
        var favoritoExistente = await _repository.FirstOrDefaultAsync(favorito =>
            favorito.UsuarioId == usuarioId && favorito.RecetaId == recetaId);

        if (favoritoExistente is not null)
        {
            await _repository.DeleteAsync(favoritoExistente);
            return false;
        }

        await AsegurarCupoFavoritosAsync(usuarioId);
        await AsegurarRecetaVisibleAsync(usuarioId, recetaId);

        await _repository.AddAsync(new RecetaFavorita
        {
            UsuarioId = usuarioId,
            RecetaId = recetaId
        });

        return true;
    }

    public async Task<bool> DeleteAsync(int id, int usuarioId)
    {
        var favorito = await _repository.GetByIdAsync(id);

        if (favorito is null || favorito.UsuarioId != usuarioId)
        {
            return false;
        }

        await _repository.DeleteAsync(favorito);
        return true;
    }

    public async Task<bool> ExisteFavoritoAsync(int usuarioId, int recetaId)
    {
        return await _repository.AnyAsync(favorito =>
            favorito.UsuarioId == usuarioId && favorito.RecetaId == recetaId);
    }

    public async Task AsegurarCupoFavoritosAsync(int usuarioId)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId)
            ?? throw new InvalidOperationException("El usuario no existe.");

        if (RolUsuario.TieneAccesoPremium(usuario.Rol))
        {
            return;
        }

        var cantidad = await _repository.CountAsync(favorito => favorito.UsuarioId == usuarioId);
        if (cantidad >= LimitesPlan.MaxFavoritosBasico)
        {
            throw new ForbiddenException(LimitesPlan.MensajeLimiteFavoritos);
        }
    }

    private async Task AsegurarRecetaVisibleAsync(int usuarioId, int recetaId)
    {
        var receta = await _recetaRepository.GetByIdAsync(recetaId)
            ?? throw new InvalidOperationException("La receta no existe.");
        var usuario = await _usuarioRepository.GetByIdAsync(usuarioId)
            ?? throw new InvalidOperationException("El usuario no existe.");

        if (!LimitesPlan.PuedeUsarDificultad(usuario.Rol, receta.Dificultad))
        {
            throw new ForbiddenException(LimitesPlan.MensajeDificultadPremium);
        }
    }
}
