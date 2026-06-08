namespace MuuqWear.Model.Archive;

public class ArchiveCollectionDefinition
{
    /// <summary>
    /// Matches the value stored in DesignHistory.Collection column.
    /// Used to group items into their section.
    /// </summary>
    public string CollectionKey { get; set; } = string.Empty;

    /// <summary>Section heading displayed on the page.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Short editorial description shown below the heading.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// When true, this section renders with a light-grey background.
    /// Used to create visual rhythm between alternating sections.
    /// </summary>
    public bool UseSoftBackground { get; set; }
}
