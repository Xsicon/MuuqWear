namespace MuuqWear.Model.Archive;


public static class ArchiveCollections
{
    public static readonly IReadOnlyList<ArchiveCollectionDefinition> All =
        new List<ArchiveCollectionDefinition>
        {
            new()
            {
                CollectionKey = "The Essentials Collection",
                Title         = "The Essentials Collection",
                Description   = "Where timeless cultural silhouettes meet everyday minimalism — " +
                                "elevated basics rooted in centuries of tradition.",
                UseSoftBackground = false
            },
            new()
            {
                CollectionKey = "The Technical Series",
                Title         = "The Technical Series",
                Description   = "Performance meets heritage — technical fabrics engineered " +
                                "for the modern explorer, informed by centuries of functional craft.",
                UseSoftBackground = true
            },
            new()
            {
                CollectionKey = "The Veil Capsule",
                Title         = "The Veil Capsule",
                Description   = "The crown jewels of Muuqwear — limited edition sapphire-dyed " +
                                "pieces that trace indigo’s journey across civilizations.",
                UseSoftBackground = false
            },
            new()
            {
                CollectionKey = "Cultural Collaborations",
                Title         = "Cultural Collaborations",
                Description   = "Direct partnerships with artisan communities worldwide — " +
                                "co-created pieces where traditional craft meets contemporary design.",
                UseSoftBackground = true
            },
            new()
            {
                CollectionKey = "Limited Editions",
                Title         = "Limited Editions",
                Description   = "Rare, one-off creations that push the boundaries of fashion " +
                                "as art — collectible pieces with stories that transcend seasonal trends.",
                UseSoftBackground = false
            }
        };
}
