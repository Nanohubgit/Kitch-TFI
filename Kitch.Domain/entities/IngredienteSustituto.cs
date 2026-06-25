namespace Kitch.Domain.Entities
{
    public class IngredienteSustituto
    {
        public int Id { get; set; }
        public int IngredienteId { get; set; }
        public int SustitutoId { get; set; }
        public string? Motivo { get; set; }

        public Ingrediente Ingrediente { get; set; } = null!;
        public Ingrediente Sustituto { get; set; } = null!;
    }
}
