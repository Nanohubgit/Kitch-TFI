namespace Kitch.Domain.Entities
{
    public class Suscripcion
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public bool Activa { get; set; } = true;
        public string Tipo { get; set; } = string.Empty;

        public Usuario Usuario { get; set; } = null!;
    }
}
