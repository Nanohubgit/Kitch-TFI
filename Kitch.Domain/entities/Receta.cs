namespace Kitch.Domain.Entities
{
    public class Receta
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public int CaloriasEstimadas { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public int TiempoPreparacionMinutos { get; set; }
        public int Porciones { get; set; }
        
        public DificultadReceta Dificultad { get; set; } = DificultadReceta.Medio;

        
        public ICollection<IngredienteReceta> IngredientesReceta { get; set; } = new List<IngredienteReceta>();
        public ICollection<PreparacionReceta> Preparaciones { get; set; } = new List<PreparacionReceta>();
    }


    public enum DificultadReceta
    {
        Facil,
        Medio,
        Dificil
    }
}