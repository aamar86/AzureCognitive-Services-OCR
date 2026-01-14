public sealed class GPT4AllSettings
{
    public const string SectionName = "GPT4All";

    public bool Enabled { get; set; } = true;
    public string BaseUrl { get; set; } = "http://localhost:4891";
    public string Model { get; set; } = "mini-orca-3b";

    public int MaxTokens { get; set; } = 2048;
    public double Temperature { get; set; } = 0.1;
    public double TopP { get; set; } = 0.9;
    public int TimeoutSeconds { get; set; } = 120;
}
