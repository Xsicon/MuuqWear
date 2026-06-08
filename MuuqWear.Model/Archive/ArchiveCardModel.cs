namespace MuuqWear.Model.Archive;

public class ArchiveCardModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;

    // Card display
    public string Designer { get; set; } = string.Empty;
    public string Year { get; set; } = string.Empty;
    public string Inspiration { get; set; } = string.Empty;
    public string Collection { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;

    // Story panel
    public string? SecondImageUrl { get; set; }
    public string? StoryBody { get; set; }              // long form text, the `Content` field
    public string? TechnicalFabric { get; set; }
    public string? TechnicalTechniques { get; set; }
    public string? TechnicalProduction { get; set; }
    public string? TechnicalAvailability { get; set; }
}
