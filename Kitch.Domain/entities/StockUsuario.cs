namespace Kitch.Domain.Entities
{
    public class StockUsuario
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int IngredienteId { get; set; }
        public decimal Cantidad { get; set; }
        public string UnidadMedida { get; set; } = string.Empty;
        public DateTime? FechaCaducidad { get; set; }

        public Usuario Usuario { get; set; } = null!;
        public Ingrediente Ingrediente { get; set; } = null!;
    }
}
