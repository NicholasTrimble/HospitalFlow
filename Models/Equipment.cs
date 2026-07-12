using System.ComponentModel.DataAnnotations;

namespace HospitalFlow.Models;

public class Equipment
{
    public int Id { get; set; }

    [Required]
    public string SerialNumber { get; set; } = string.Empty;

    [Required]
    public string Type { get; set; } = string.Empty;

    [Required]
    public string Status { get; set; } = "Available"; // Available, In Use, Cleaning, Maintenance
    public int? RoomId { get; set; }

    public virtual Room? Room { get; set; }
}