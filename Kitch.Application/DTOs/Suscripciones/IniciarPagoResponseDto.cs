namespace Kitch.Application.DTOs.Suscripciones;

/// <summary>
/// Respuesta del inicio de checkout. El frontend lee <see cref="InitPoint"/> y redirige.
/// </summary>
public class IniciarPagoResponseDto
{
    public string InitPoint { get; set; } = string.Empty;
    public string PreferenceId { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string Moneda { get; set; } = "ARS";
    public string Mensaje { get; set; } = string.Empty;
}
