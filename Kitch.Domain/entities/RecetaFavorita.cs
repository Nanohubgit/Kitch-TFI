namespace Kitch.Domain.Entities
{
    public class RecetaFavorita
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int RecetaId { get; set; }

        public Usuario Usuario { get; set; } = null!;
        public Receta Receta { get; set; } = null!;
    }
}