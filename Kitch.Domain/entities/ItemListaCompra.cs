namespace Kitch.Domain.Entities
{
    public class ItemListaCompra
    {
        public int Id { get; set; }
        public string NombreArticulo { get; set; } = string.Empty;
        public float CantidadFaltante { get; set; }
        public bool EstaComprado { get; set; } = false;
        public string UnidadMedida { get; set; } = string.Empty;
        public int? IngredienteId { get; set; }

        public int UsuarioId { get; set; }

        public Usuario Usuario { get; set; } = null!;
        public Ingrediente? Ingrediente { get; set; }
    }
}
