public class PassportResult
{
    public string PassportNumber { get; set; } = "";
    public string CountryCode { get; set; } = "";
    public string Nationality { get; set; } = "";
    public string Surname { get; set; } = "";
    public string GivenNames { get; set; } = "";
    public DateTime? DateOfBirth { get; set; }
    public string Sex { get; set; } = "";
    public DateTime? ExpiryDate { get; set; }
    public string MrzLine1 { get; set; } = "";
    public string MrzLine2 { get; set; } = "";
}
