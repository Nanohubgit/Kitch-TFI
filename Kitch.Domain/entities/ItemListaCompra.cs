namespace Kitch.Domain.Entities
{
    public class ItemListaCompra
    {
        public int Id { get; set; }
        public string NombreArticulo { get; set; } = string.Empty;
        public float CantidadFaltante { get; set; }
        public bool EstaComprado { get; set; } = false;

        public int UsuarioId { get; set; }

        public Usuario Usuario { get; set; } = null!;
    }
}
