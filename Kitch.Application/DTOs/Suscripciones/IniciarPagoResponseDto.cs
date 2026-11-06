namespace Kitch.Application.DTOs.Suscripciones;

public class IniciarPagoResponseDto
{
    public string InitPoint { get; set; } = string.Empty;
    public string PreferenceId { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string Moneda { get; set; } = "ARS";
    public string Mensaje { get; set; } = string.Empty;
}
