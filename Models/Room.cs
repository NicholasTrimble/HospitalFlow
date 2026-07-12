using System.ComponentModel.DataAnnotations;

namespace HospitalFlow.Models;

public class Room
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    public int MaxCapacity { get; set; } = 5;

    [Required]
    public string Department { get; set; } = string.Empty;
    public virtual ICollection<Equipment> Equipments { get; set; } = new HashSet<Equipment>();
}