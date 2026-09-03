using Kitch.Application.DTOs.ContratosSub;
using Kitch.Application.DTOs.Favoritos;
using Kitch.Application.DTOs.Ingredientes;
using Kitch.Application.DTOs.ListaCompra;
using Kitch.Application.DTOs.Pagos;
using Kitch.Application.DTOs.Planificador;
using Kitch.Application.DTOs.Recetas;
using Kitch.Application.DTOs.StockUsuarios;
using Kitch.Application.DTOs.Sustitutos;
using Kitch.Application.DTOs.Suscripciones;
using Kitch.Application.DTOs.Usuarios;
using Kitch.Domain.Constants;
using Kitch.Domain.Entities;

namespace Kitch.Application.Mappings;

public static class DtoMappings
{
    public static UsuarioResponseDto ToResponseDto(this Usuario usuario) => new()
    {
        Id = usuario.Id,
        Nombre = usuario.Nombre,
        Apellido = usuario.Apellido,
        NombreUsuario = usuario.NombreUsuario,
        Email = usuario.Email,
        PreferenciaDietetica = usuario.PreferenciaDietetica,
        Activo = usuario.Activo,
        Rol = usuario.Rol
    };

    public static RecetaResponseDto ToResponseDto(this Receta receta) => new()
    {
        Id = receta.Id,
        Titulo = receta.Titulo,
        CaloriasEstimadas = receta.CaloriasEstimadas,
        Descripcion = receta.Descripcion,
        TiempoPreparacionMinutos = receta.TiempoPreparacionMinutos,
        Porciones = receta.Porciones,
        Dificultad = receta.Dificultad,
        Categoria = CategoriasReceta.Normalizar(receta.Categoria),
        Ingredientes = receta.IngredientesReceta
            .Select(ingrediente => new IngredienteRecetaResponseDto
            {
                Id = ingrediente.Id,
                IngredienteId = ingrediente.IngredienteId,
                Nombre = ingrediente.Ingrediente?.Nombre ?? string.Empty,
                Cantidad = ingrediente.Cantidad,
                UnidadMedida = ingrediente.UnidadMedida
            })
            .ToList(),
        Preparaciones = receta.Preparaciones
            .OrderBy(preparacion => preparacion.NumeroPaso)
            .Select(preparacion => new PreparacionRecetaResponseDto
            {
                Id = preparacion.Id,
                NumeroPaso = preparacion.NumeroPaso,
                DescripcionPaso = preparacion.DescripcionPaso
            })
            .ToList()
    };

    public static StockUsuarioResponseDto ToResponseDto(this StockUsuario stock) => new()
    {
        Id = stock.Id,
        IngredienteId = stock.IngredienteId,
        NombreIngrediente = stock.Ingrediente?.Nombre ?? string.Empty,
        Cantidad = stock.Cantidad,
        UnidadMedida = stock.UnidadMedida,
        FechaCaducidad = stock.FechaCaducidad
    };

    public static FavoritoResponseDto ToResponseDto(this RecetaFavorita favorito) => new()
    {
        Id = favorito.Id,
        RecetaId = favorito.RecetaId,
        UsuarioEmail = favorito.Usuario?.Email ?? string.Empty,
        RecetaTitulo = favorito.Receta?.Titulo ?? string.Empty
    };

    public static ComidaPlanificadaResponseDto ToResponseDto(this ComidaPlanificada comida) => new()
    {
        Id = comida.Id,
        RecetaId = comida.RecetaId,
        RecetaTitulo = comida.Receta?.Titulo ?? string.Empty,
        FechaAsignada = comida.FechaAsignada,
        Turno = comida.Turno
    };

    public static ItemListaCompraResponseDto ToResponseDto(this ItemListaCompra item) => new()
    {
        Id = item.Id,
        IngredienteId = item.IngredienteId,
        NombreArticulo = item.NombreArticulo,
        CantidadFaltante = item.CantidadFaltante,
        UnidadMedida = item.UnidadMedida,
        EstaComprado = item.EstaComprado
    };

    public static SuscripcionResponseDto ToResponseDto(this Suscripcion suscripcion) => new()
    {
        Id = suscripcion.Id,
        UsuarioId = suscripcion.UsuarioId,
        FechaInicio = suscripcion.FechaInicio,
        FechaFin = suscripcion.FechaFin,
        Activa = suscripcion.Activa,
        Tipo = suscripcion.Tipo
    };

    public static ContratoSubResponseDto ToResponseDto(this ContratoSub contrato) => new()
    {
        Id = contrato.Id,
        SuscripcionId = contrato.SuscripcionId,
        FechaContratacion = contrato.FechaContratacion,
        FechaInicio = contrato.FechaInicio,
        FechaFin = contrato.FechaFin,
        Monto = contrato.Monto,
        Estado = contrato.Estado,
        DiasRestantes = contrato.FechaFin.HasValue
            ? Math.Max(0, (contrato.FechaFin.Value.Date - DateTime.UtcNow.Date).Days)
            : 0
    };

    public static PagoResponseDto ToResponseDto(this Pago pago) => new()
    {
        Id = pago.Id,
        ContratoSubId = pago.ContratoSubId,
        FechaPago = pago.FechaPago,
        Monto = pago.Monto,
        EstadoPago = pago.EstadoPago,
        MetodoPago = pago.MetodoPago
    };

    public static IngredienteResponseDto ToResponseDto(this Ingrediente ingrediente) => new()
    {
        Id = ingrediente.Id,
        Nombre = ingrediente.Nombre,
        Descripcion = ingrediente.Descripcion
    };

    public static SustitutoResponseDto ToResponseDto(this SustitutoIngrediente sustituto) => new()
    {
        Id = sustituto.Id,
        IngredienteOriginal = sustituto.IngredienteOriginal?.Nombre ?? string.Empty,
        IngredienteSustituto = sustituto.IngredienteSustituto?.Nombre ?? string.Empty,
        FactorEquivalencia = sustituto.FactorEquivalencia,
        Notas = sustituto.Notas
    };
}
