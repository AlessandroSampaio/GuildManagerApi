namespace GuildManagerApi.Domain.Entities;

public class Specialization
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string SlugName { get; set; } = null!;
    public int ClassId { get; set; }

    public virtual Class Class { get; set; } = null!;
}
