
namespace Kitch.Domain.Entities
{
    public class ComidaPlanificada
    {
        public int Id { get; set; }
        public DateTime FechaAsignada { get; set; }
        public string Turno { get; set; } = string.Empty;

        public int UsuarioId { get; set; }
        public int RecetaId { get; set; }

        public Usuario Usuario { get; set; } = null!;
        public Receta Receta { get; set; } = null!;
    }
}
