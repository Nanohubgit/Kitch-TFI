namespace Kitch.Domain.Entities
{
    public class Pago
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public DateTime FechaPago { get; set; }
        public decimal Monto { get; set; }
        public EstadoPago EstadoPago { get; set; } = EstadoPago.Pendiente;
        public MetodoPago MetodoPago { get; set; } = MetodoPago.NoEspecificado;

        public Usuario Usuario { get; set; } = null!;
    }

    public enum EstadoPago
    {
        Pendiente,
        Aprobado,
        Rechazado,
        Cancelado,
        Reembolsado
    }

    public enum MetodoPago
    {
        NoEspecificado,
        TarjetaCredito,
        TarjetaDebito,
        TransferenciaBancaria,
        BilleteraVirtual,
        PayPal
    }
}
