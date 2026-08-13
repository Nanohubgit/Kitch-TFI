using System.ComponentModel.DataAnnotations;

namespace Kitch.Application.DTOs.Suscripciones;

/// <summary>
/// Inicia el flujo de compra Premium. El front abre CheckoutUrl / PreferenceId en su modal.
/// </summary>
public class CheckoutSuscripcionRequestDto
{
    [Required]
    [Range(0.01, 999999)]
    public decimal Monto { get; set; } = 9.99m;

    /// <summary>
    /// Stripe | MercadoPago (informativo para el cascarón).
    /// </summary>
    [MaxLength(50)]
    public string Pasarela { get; set; } = "MercadoPago";
}

/// <summary>
/// Respuesta lista para la UI de checkout (animaciones / redirect / WebView).
/// </summary>
public class CheckoutSuscripcionResponseDto
{
    public string CheckoutUrl { get; set; } = string.Empty;
    public string PreferenceId { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
}

/// <summary>
/// Payload simulado de webhook de la pasarela (público, sin JWT).
/// </summary>
public class WebhookPagoRequestDto
{
    [Required]
    public string PreferenceId { get; set; } = string.Empty;

    [Required]
    public int UsuarioId { get; set; }

    /// <summary>
    /// approved | rejected | pending
    /// </summary>
    [Required]
    public string Estado { get; set; } = "approved";

    public decimal Monto { get; set; }
}

public class WebhookPagoResponseDto
{
    public bool Procesado { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public string? RolUsuario { get; set; }
}
