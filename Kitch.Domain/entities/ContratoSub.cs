namespace Kitch.Domain.Entities
{
    public class ContratoSub
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public int SuscripcionId { get; set; }
        public DateTime FechaContratacion { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public decimal Monto { get; set; }
        public EstadoContratoSub Estado { get; set; } = EstadoContratoSub.Pendiente;

        public Usuario Usuario { get; set; } = null!;
        public Suscripcion Suscripcion { get; set; } = null!;
        public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
    }

    public enum EstadoContratoSub
    {
        Pendiente,
        Activo,
        Cancelado,
        Vencido
    }
}
