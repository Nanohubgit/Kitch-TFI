namespace Kitch.Domain.Entities
{
    public class PreparacionReceta
    {
        public int Id { get; set; }
        public int RecetaId { get; set; }
        public int NumeroPaso { get; set; }
        public string DescripcionPaso { get; set; } = string.Empty;

        public Receta Receta { get; set; } = null!;
    }
}
