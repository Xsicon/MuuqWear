namespace MuuqWear.Client.Catalog;

public sealed class CatalogDoc
{
    public string Id { get; set; } = "";
    public string? Name { get; set; }
    public string? Title { get; set; }
    public string? Body { get; set; }
    public long Price { get; set; }
    public string? Status { get; set; }
    public Dictionary<string, long>? SizeStock { get; set; }
    public List<string>? ImagePaths { get; set; }
    public string? ImageUrl { get; set; }
    public List<string>? Colors { get; set; }
    public List<string>? CollectionTags { get; set; }
    public string? CollectionId { get; set; }
    public string? Description { get; set; }
    public string? Sku { get; set; }
    public string? Category { get; set; }
    public string? Author { get; set; }
    public string? Excerpt { get; set; }
    public string? Slug { get; set; }
    public DateTime? CreatedAt { get; set; }
}
