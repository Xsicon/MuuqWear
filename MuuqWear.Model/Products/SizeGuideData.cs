namespace MuuqWear.Model.Products;

/// <summary>
/// Static size guide data
/// Hardcoded for performance — no runtime calculation 
/// Single source of truth for all size tables 
/// </summary>
public static class SizeGuideData
{
    // =============================================
    // TOPS, HOODIES & JACKETS
    // =============================================

    public static readonly List<TopSizeRow> TopsInches = new()
    {
        new("XS", "34-36", "27"),
        new("S",  "36-38", "28"),
        new("M",  "38-40", "29"),
        new("L",  "40-42", "30"),
        new("XL", "42-44", "31"),
        new("XXL","44-46", "32"),
    };

    public static readonly List<TopSizeRow> TopsCm = new()
    {
        new("XS", "86-91",   "69"),
        new("S",  "91-97",   "71"),
        new("M",  "97-102",  "74"),
        new("L",  "102-107", "76"),
        new("XL", "107-112", "79"),
        new("XXL","112-117", "81"),
    };

    // =============================================
    // BOTTOMS
    // =============================================

    public static readonly List<BottomSizeRow> BottomsInches = new()
    {
        new("XS", "28-30", "34-36", "30"),
        new("S",  "30-32", "36-38", "31"),
        new("M",  "32-34", "38-40", "31"),
        new("L",  "34-36", "40-42", "32"),
        new("XL", "36-38", "42-44", "32"),
        new("XXL","38-40", "44-46", "33"),
    };

    public static readonly List<BottomSizeRow> BottomsCm = new()
    {
        new("XS", "71-76",   "86-91",   "76"),
        new("S",  "76-81",   "91-97",   "79"),
        new("M",  "81-86",   "97-102",  "79"),
        new("L",  "86-91",   "102-107", "81"),
        new("XL", "91-97",   "107-112", "81"),
        new("XXL","97-102",  "112-117", "84"),
    };

    // =============================================
    // ACCESSORIES
    // =============================================

    public static readonly List<AccessorySizeRow> AccessoriesInches = new()
    {
        new("Caps",         "Adjustable",  "One Size"),
        new("Beanies",      "Stretch Fit", "One Size"),
        new("Crossbody Bag","—",           "10\" × 7\" × 3\""),
        new("Tote Bag",     "—",           "15\" × 13\" × 5\""),
        new("Backpack 20L", "—",           "18\" × 12\" × 7\""),
    };

    public static readonly List<AccessorySizeRow> AccessoriesCm = new()
    {
        new("Caps",         "Adjustable",  "One Size"),
        new("Beanies",      "Stretch Fit", "One Size"),
        new("Crossbody Bag","—",           "25 × 18 × 8 cm"),
        new("Tote Bag",     "—",           "38 × 33 × 13 cm"),
        new("Backpack 20L", "—",           "46 × 30 × 18 cm"),
    };

    // =============================================
    // HOW TO MEASURE — measurement instructions
    // =============================================

    public static readonly List<MeasurementGuide> MeasurementGuides = new()
    {
        new(
            "Chest",
            "Wrap the tape around the fullest part of your chest, keeping it level under your arms."
        ),
        new(
            "Waist",
            "Measure around your natural waistline, keeping the tape comfortably loose."
        ),
        new(
            "Hip",
            "Stand with feet together and measure around the fullest part of your hips."
        ),
        new(
            "Inseam",
            "Measure from the crotch seam of a well-fitting pair of pants to the bottom of the leg."
        ),
    };
}

// =============================================
// RECORD TYPES — immutable data containers 
// =============================================

/// <summary>Tops size row — Size, Chest, Length</summary>
public record TopSizeRow(string Size, string Chest, string Length);

/// <summary>Bottoms size row — Size, Waist, Hip, Inseam</summary>
public record BottomSizeRow(string Size, string Waist, string Hip, string Inseam);

/// <summary>Accessory row — Item, Size, Dimensions</summary>
public record AccessorySizeRow(string Item, string Size, string Dimensions);

/// <summary>Measurement guide — Title, Description</summary>
public record MeasurementGuide(string Title, string Description);