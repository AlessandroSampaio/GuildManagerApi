using System.ComponentModel.DataAnnotations;

namespace GuildManagerApi.Domain.Entities;

public class Class
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string SlugName { get; set; } = null!;

    // Navigation
    public ICollection<Character> Characters { get; set; } = [];
    public ICollection<Specialization> Specializations { get; set; } = [];
}
