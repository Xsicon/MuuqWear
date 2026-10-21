namespace MuuqWear.Model.Products;

public static class SizeGuideData
{
    public const string Intro = "Find your size with the charts below. All measurements are garment measurements, in centimeters and inches.";
    public const string FitNote = "Between sizes? Compare the measurements to a garment you own that fits well.";
    public const string Help = "Still unsure? Write to Info@muuqwear.com with your height, weight, and the item you are eyeing; we reply within 2 business days.";

    public static readonly string[] FitNotes =
    [
        "Between sizes? Compare the garment measurements above to a piece you own that fits well.",
        "Length: if you are taller than the recommended height for your size, consider sizing up for length.",
        "Fabric: natural fabrics may shrink slightly on the first wash; care labels give exact guidance.",
    ];

    public static readonly List<MeasurementGuide> MeasurementGuides =
    [
        new("Chest", "Around the fullest part of your chest, tape level under your arms."),
        new("Length", "From the shoulder down to where you want the hem to fall."),
        new("Sleeve length", "From the shoulder seam to the wrist."),
        new("Cuff", "Around the wrist where the cuff sits."),
        new("Bottom", "The width of the garment hem, measured flat."),
    ];

    public static readonly List<GarmentSizeRow> Centimeters =
    [
        new("S", "52", "170-175", "65-73", "141", "108", "144", "63", "35.6"),
        new("M", "54", "175-180", "73-82", "146", "114", "150", "64", "36.8"),
        new("L", "56", "180-185", "82-91", "151", "120", "156", "65", "38.0"),
        new("XL", "58", "185-190", "91-100", "156", "126", "162", "66", "39.2"),
        new("XXL", "60", "190+", "100+", "161", "132", "168", "67", "40.4"),
    ];

    public static readonly List<GarmentSizeRow> Inches =
    [
        new("S", "52", "5'7-5'9", "140-160", "55.5", "42.5", "56.5", "24.8", "14.0"),
        new("M", "54", "5'9-5'11", "160-180", "57.5", "44.5", "58.5", "25.2", "14.5"),
        new("L", "56", "5'11-6'1", "180-200", "59.5", "46.5", "60.5", "25.6", "15.0"),
        new("XL", "58", "6'1-6'3", "200-220", "61.5", "48.5", "62.5", "26.0", "15.5"),
        new("XXL", "60", "6'3+", "220+", "63.5", "50.5", "64.5", "26.4", "16.0"),
    ];
}

public record GarmentSizeRow(
    string Size,
    string Numeric,
    string Height,
    string Weight,
    string Length,
    string Chest,
    string Bottom,
    string Sleeve,
    string Cuff);

public record MeasurementGuide(string Title, string Description);
