using Kitch.Domain.Constants;

namespace Kitch.Application.Common;

public sealed class PlanSuscripcionConfig
{
    public decimal PrecioProfesionalArs { get; init; } = PrecioSuscripcion.ProfesionalArs;

    public string Moneda { get; init; } = PrecioSuscripcion.MonedaArs;

    public string TipoProfesional { get; init; } = PrecioSuscripcion.TipoProfesional;
}
