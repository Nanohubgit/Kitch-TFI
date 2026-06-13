namespace Kitch.Domain.Entities
{
    public class Ingrediente
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; } = string.Empty;

        public ICollection<IngredienteReceta> IngredientesReceta { get; set; } = new List<IngredienteReceta>();
    }
}
