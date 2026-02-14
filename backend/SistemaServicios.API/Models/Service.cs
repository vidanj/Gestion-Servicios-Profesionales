using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaServicios.API.Models
{
    public class Service
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Category { get; set; } = string.Empty;
        
        public int ProfessionalId { get; set; }
        public bool IsActive { get; set; } = true;
    }
}