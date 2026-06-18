using Kitch.Application.DTOs.Suscripciones;
using Kitch.Domain.Entities;

namespace Kitch.Application.Interfaces;

public interface ISuscripcionService
{
    Task<IEnumerable<Suscripcion>> GetAllAsync();
    Task<Suscripcion?> GetByIdAsync(int id);
    Task<Suscripcion> CreateAsync(Suscripcion suscripcion);
    Task<bool> UpdateAsync(int id, Suscripcion suscripcion);
    Task<bool> DeleteAsync(int id);

    // Orquesta el alta de la suscripción: contrato + pago simulado y, si se aprueba,
    // eleva el rol del usuario a "Profesional".
    Task<ContratarSuscripcionResult> ContratarAsync(int usuarioId, ContratarSuscripcionRequest request);
}
