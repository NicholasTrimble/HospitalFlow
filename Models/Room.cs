using System.ComponentModel.DataAnnotations;

namespace HospitalFlow.Models;

public enum RoomStatus
{
    Available,
    Occupied,
    CleaningRequired,
    Maintenance
}

public class Room
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public int MaxCapacity { get; set; } = 5;

    [Required]
    public string Department { get; set; } = string.Empty;

    public RoomStatus Status { get; set; } = RoomStatus.Available;

    public virtual ICollection<Equipment> Equipments { get; set; } = new HashSet<Equipment>();

    public ICollection<Patient> Patients { get; set; } = new List<Patient>();
}