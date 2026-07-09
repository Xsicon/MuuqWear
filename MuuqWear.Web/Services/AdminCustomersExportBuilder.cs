using ClosedXML.Excel;
using MuuqWear.Model.Customer;
using MuuqWear.Web.Helpers;
using System.Globalization;
using System.Text;

namespace MuuqWear.Web.Services;

public static class AdminCustomersExportBuilder
{
    private const string BrandColor = "#1E2A47";
    private const string MutedColor = "#4A5C7A";
    private const string AltRowColor = "#F4F6F9";

    public static byte[] BuildCsv(IReadOnlyList<CustomerModel> customers, string? searchFilter)
    {
        var sb = new StringBuilder();
        var stamp = DateTime.Now.ToString("MMMM dd, yyyy · h:mm tt", CultureInfo.InvariantCulture);

        sb.AppendLine("MuuqWear Customers Export");
        sb.AppendLine($"Search,{FormatFilter(searchFilter)}");
        sb.AppendLine($"Generated,{stamp}");
        sb.AppendLine();
        sb.AppendLine("Name,Email,Orders,Total Spent,Joined,Last Order,Note Count,Latest Note,Latest Note Author,Latest Note Date");

        foreach (var customer in customers)
        {
            sb.AppendLine(string.Join(",",
                Csv(customer.FullName),
                Csv(customer.Email),
                Csv(customer.OrderCount),
                CsvMoney(customer.TotalSpent),
                Csv(FormatDate(customer.CreatedAt)),
                Csv(FormatDate(customer.LastOrderAt)),
                Csv(customer.NoteCount),
                Csv(customer.LatestNotePreview),
                Csv(FormatNoteAuthor(customer)),
                Csv(FormatDate(customer.LatestNoteAt))));
        }

        return Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(sb.ToString()))
            .ToArray();
    }

    public static byte[] BuildExcel(IReadOnlyList<CustomerModel> customers, string? searchFilter)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Customers");
        var data = BuildRows(customers);

        WriteStyledSheet(
            ws,
            "MuuqWear — Customers",
            searchFilter,
            data.Headers,
            data.Rows);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static string BuildFileName(string extension, string? searchFilter)
    {
        var filter = string.IsNullOrWhiteSpace(searchFilter) ? "all" : "search";
        var date = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return $"muuqwear-customers-{filter}-{date}.{extension}";
    }

    private static (string[] Headers, List<object?[]> Rows) BuildRows(IReadOnlyList<CustomerModel> customers)
    {
        var headers = new[]
        {
            "Name", "Email", "Orders", "Total Spent", "Joined", "Last Order",
            "Note Count", "Latest Note", "Latest Note Author", "Latest Note Date"
        };

        var rows = customers.Select(c => new object?[]
        {
            c.FullName,
            c.Email,
            c.OrderCount,
            c.TotalSpent,
            FormatDate(c.CreatedAt),
            FormatDate(c.LastOrderAt),
            c.NoteCount,
            c.LatestNotePreview,
            FormatNoteAuthor(c),
            FormatDate(c.LatestNoteAt)
        }).ToList();

        return (headers, rows);
    }

    private static void WriteStyledSheet(
        IXLWorksheet ws,
        string title,
        string? searchFilter,
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

        ws.Cell(2, 1).Value = $"Search: {FormatFilter(searchFilter)} · Generated {stamp} · Records {rows.Count}";
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
            ws.Range(headerRow, 1, dataStart + rows.Count - 1, colCount).SetAutoFilter();

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

        if (header is "Total Spent")
        {
            cell.Value = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            cell.Style.NumberFormat.Format = "$#,##0.00";
            return;
        }

        if (header is "Orders" or "Note Count")
        {
            cell.Value = Convert.ToInt64(value, CultureInfo.InvariantCulture);
            return;
        }

        cell.Value = value.ToString();
    }

    private static string FormatFilter(string? filter) =>
        string.IsNullOrWhiteSpace(filter) ? "All" : filter;

    private static string FormatDate(DateTime? value) =>
        value?.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture) ?? "—";

    private static string FormatNoteAuthor(CustomerModel customer) =>
        string.IsNullOrWhiteSpace(customer.LatestNotePreview)
            ? "—"
            : CustomerNoteFormatter.FormatAuthorLabel(
                customer.LatestNoteAuthorName,
                customer.LatestNoteAuthorRole);

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
