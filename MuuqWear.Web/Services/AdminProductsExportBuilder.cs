using ClosedXML.Excel;
using MuuqWear.Application.Shared;
using MuuqWear.Model.Products;
using System.Globalization;
using System.Text;

namespace MuuqWear.Web.Services;

public static class AdminProductsExportBuilder
{
    private const string BrandColor = "#1E2A47";
    private const string MutedColor = "#4A5C7A";
    private const string AltRowColor = "#F4F6F9";

    public static byte[] BuildCsv(
        IReadOnlyList<ProductModel> products,
        string? filterLabel,
        string viewLabel)
    {
        var sb = new StringBuilder();
        var stamp = DateTime.Now.ToString("MMMM dd, yyyy · h:mm tt", CultureInfo.InvariantCulture);

        sb.AppendLine("MuuqWear Products & Inventory Export");
        sb.AppendLine($"View,{viewLabel}");
        sb.AppendLine($"Filter,{FormatFilter(filterLabel)}");
        sb.AppendLine($"Generated,{stamp}");
        sb.AppendLine();
        sb.AppendLine("Name,SKU,Price,Category,Total Stock,Status,Size Breakdown");

        foreach (var product in products)
        {
            sb.AppendLine(string.Join(",",
                Csv(product.Name),
                Csv(product.Sku),
                CsvMoney(product.Price),
                Csv(product.CategoryName ?? product.Category),
                Csv(ProductStockHelper.GetEffectiveStock(product)),
                Csv(GetStatus(product)),
                Csv(FormatSizeBreakdown(product))));
        }

        return Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(sb.ToString()))
            .ToArray();
    }

    public static byte[] BuildExcel(
        IReadOnlyList<ProductModel> products,
        string? filterLabel,
        string viewLabel)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Products");
        var data = BuildRows(products);

        WriteStyledSheet(
            ws,
            "MuuqWear — Products & Inventory",
            $"{viewLabel} · {FormatFilter(filterLabel)}",
            data.Headers,
            data.Rows);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static string BuildFileName(string extension, string? filterLabel, bool selectedOnly)
    {
        var filter = selectedOnly
            ? "selected"
            : string.IsNullOrWhiteSpace(filterLabel) ? "all" : filterLabel.ToLowerInvariant().Replace(' ', '-');
        var date = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return $"muuqwear-products-{filter}-{date}.{extension}";
    }

    private static (string[] Headers, List<object?[]> Rows) BuildRows(IReadOnlyList<ProductModel> products)
    {
        var headers = new[]
        {
            "Name", "SKU", "Price", "Category", "Total Stock", "Status", "Size Breakdown"
        };

        var rows = products.Select(p => new object?[]
        {
            p.Name,
            p.Sku,
            p.Price,
            p.CategoryName ?? p.Category,
            ProductStockHelper.GetEffectiveStock(p),
            GetStatus(p),
            FormatSizeBreakdown(p)
        }).ToList();

        return (headers, rows);
    }

    private static void WriteStyledSheet(
        IXLWorksheet ws,
        string title,
        string subtitle,
        string[] headers,
        List<object?[]> rows)
    {
        var colCount = headers.Length;
        var stamp = DateTime.Now.ToString("MMMM dd, yyyy · h:mm tt", CultureInfo.InvariantCulture);

        ws.Cell(1, 1).Value = title;
        ws.Range(1, 1, 1, colCount).Merge();
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 16;
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml(BrandColor);

        ws.Cell(2, 1).Value = $"{subtitle} · Generated {stamp} · Records {rows.Count}";
        ws.Range(2, 1, 2, colCount).Merge();
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml(MutedColor);
        ws.Cell(2, 1).Style.Font.Italic = true;

        var headerRow = 4;
        for (var c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(headerRow, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml(BrandColor);
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        var dataStart = headerRow + 1;
        for (var r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            for (var c = 0; c < row.Length; c++)
            {
                var cell = ws.Cell(dataStart + r, c + 1);
                WriteCellValue(cell, row[c], headers[c]);
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#E5E7EB");

                if (r % 2 == 1)
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml(AltRowColor);
            }
        }

        if (rows.Count > 0)
        {
            var tableRange = ws.Range(headerRow, 1, dataStart + rows.Count - 1, colCount);
            tableRange.SetAutoFilter();
        }

        ws.SheetView.FreezeRows(headerRow);
        ws.Columns(1, colCount).AdjustToContents(12, 48);
    }

    private static void WriteCellValue(IXLCell cell, object? value, string header)
    {
        if (value is null)
        {
            cell.Value = string.Empty;
            return;
        }

        if (header is "Price" or "Total Stock")
        {
            cell.Value = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            cell.Style.NumberFormat.Format = header == "Price" ? "$#,##0.00" : "#,##0";
            return;
        }

        cell.Value = value.ToString();
    }

    private static string GetStatus(ProductModel product)
    {
        if (ProductStockHelper.IsOutOfStock(product))
            return "Out of Stock";
        if (ProductStockHelper.IsLowStock(product))
            return "Low Stock";
        return "In Stock";
    }

    private static string FormatSizeBreakdown(ProductModel product) =>
        product.SizeStock.Count == 0
            ? string.Empty
            : string.Join("; ", product.SizeStock.Select(s => $"{s.Size}:{s.Quantity}"));

    private static string FormatFilter(string? filter) =>
        string.IsNullOrWhiteSpace(filter) ? "All" : filter;

    private static string Csv(object? value)
    {
        var text = value?.ToString() ?? string.Empty;
        if (text.Contains('"') || text.Contains(',') || text.Contains('\n'))
            return $"\"{text.Replace("\"", "\"\"")}\"";
        return text;
    }

    private static string CsvMoney(decimal value) =>
        value.ToString("0.00", CultureInfo.InvariantCulture);
}
