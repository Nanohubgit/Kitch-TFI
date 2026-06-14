namespace Kitch.Domain.Entities
{
    public class IngredienteReceta
    {
        public int Id { get; set; }
        public int RecetaId { get; set; }
        public int IngredienteId { get; set; }
        public decimal Cantidad { get; set; }
        public string UnidadMedida { get; set; } = string.Empty;

        public Receta Receta { get; set; } = null!;
        public Ingrediente Ingrediente { get; set; } = null!;
    }
}
