namespace AgroMove.API.Models
{
    public class Hub
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty; // e.g., "Lagos Central Hub"
        public string Address { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string ContactPhone { get; set; } = string.Empty;
        
        // Coordinates for the Driver App to navigate
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        
        public bool IsActive { get; set; } = true;
    }
}

