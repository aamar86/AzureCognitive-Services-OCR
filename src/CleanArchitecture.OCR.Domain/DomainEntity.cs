namespace CleanArchitecture.OCR.Domain;

public class DomainEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
