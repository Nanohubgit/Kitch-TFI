using Kitch.Application.Interfaces;
using Kitch.Domain.Entities;
using Kitch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Kitch.Infrastructure.Services;

public class FavoritoService : IFavoritoService
{
    private readonly KitchDbContext _context;

    public FavoritoService(KitchDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<RecetaFavorita>> GetByUsuarioIdAsync(int usuarioId)
    {
        return await _context.RecetasFavoritas
            .Where(favorito => favorito.UsuarioId == usuarioId)
            .ToListAsync();
    }

    public async Task<RecetaFavorita?> GetByIdAsync(int id)
    {
        return await _context.RecetasFavoritas.FindAsync(id);
    }

    public async Task<RecetaFavorita> AddFavoritoAsync(RecetaFavorita favorito)
    {
        if (await ExisteFavoritoAsync(favorito.UsuarioId, favorito.RecetaId))
        {
            throw new InvalidOperationException("La receta ya está marcada como favorita para este usuario.");
        }

        await _context.RecetasFavoritas.AddAsync(favorito);
        await _context.SaveChangesAsync();

        return favorito;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var favorito = await _context.RecetasFavoritas.FindAsync(id);

        if (favorito is null)
        {
            return false;
        }

        _context.RecetasFavoritas.Remove(favorito);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ExisteFavoritoAsync(int usuarioId, int recetaId)
    {
        return await _context.RecetasFavoritas.AnyAsync(favorito =>
            favorito.UsuarioId == usuarioId && favorito.RecetaId == recetaId);
    }
}
