namespace Kitch.Domain.Entities
{
    public class SustitutoIngrediente
    {
        public int Id { get; set; }
        public int IngredienteOriginalId { get; set; }
        public int IngredienteSustitutoId { get; set; }

        // Cuánto del sustituto equivale a 1 unidad del original (ej: 0.80 => 80 ml por cada 100 g).
        public decimal FactorEquivalencia { get; set; } = 1m;
        public string? Notas { get; set; }

        public Ingrediente IngredienteOriginal { get; set; } = null!;
        public Ingrediente IngredienteSustituto { get; set; } = null!;
    }
}
