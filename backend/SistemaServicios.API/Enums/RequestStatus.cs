namespace SistemaServicios.API.Enums
{
    public enum RequestStatus
    {
        Pending = 0,    // Pendiente de aprobación
        Accepted = 1,   // El profesional aceptó el trabajo
        Rejected = 2,   // El profesional rechazó el trabajo
        Completed = 3,  // Trabajo terminado
        Cancelled = 4   // El cliente canceló
    }
}