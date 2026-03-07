namespace SistemaServicios.API.Enums;

public enum RequestStatus
{
    Pending = 1, // Cuando el cliente la crea
    Accepted = 2, // Cuando el profesional dice "Va, yo lo hago"
    Rejected = 3, // Cuando el profesional dice "No puedo"
    Completed = 4, // Cuando se termina el trabajo
    Cancelled = 5, // Si el cliente se arrepiente antes de empezar
}
