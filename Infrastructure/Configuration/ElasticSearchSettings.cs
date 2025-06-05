namespace Infrastructure.Configuration;

public class ElasticSearchSettings
{
    public const string SectionName = "ElasticSearch";

    public string Uri { get; set; } = string.Empty;
    public string IndexName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
