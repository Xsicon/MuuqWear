using ClosedXML.Excel;
using MuuqWear.Model.OrderReturn;
using MuuqWear.Model.Orders;
using MuuqWear.Model.Refund;
using System.Globalization;
using System.Text;

namespace MuuqWear.Web.Services;

public static class AdminSalesOrdersExportBuilder
{
    private const string BrandColor = "#1E2A47";
    private const string MutedColor = "#4A5C7A";
    private const string AltRowColor = "#F4F6F9";

    public static byte[] BuildCsv(
        string tab,
        IReadOnlyList<OrderModel> orders,
        IReadOnlyList<OrderReturnModel> returns,
        IReadOnlyList<RefundModel> refunds,
        string? statusFilter)
    {
        var sb = new StringBuilder();
        var stamp = DateTime.Now.ToString("MMMM dd, yyyy · h:mm tt", CultureInfo.InvariantCulture);

        sb.AppendLine($"MuuqWear Sales & Orders Export");
        sb.AppendLine($"Section,{GetSectionLabel(tab)}");
        sb.AppendLine($"Status Filter,{FormatFilter(statusFilter)}");
        sb.AppendLine($"Generated,{stamp}");
        sb.AppendLine();

        switch (tab)
        {
            case "returns":
                AppendReturnsCsv(sb, returns);
                break;
            case "refunds":
                AppendRefundsCsv(sb, refunds);
                break;
            default:
                AppendOrdersCsv(sb, orders);
                break;
        }

        return Encoding.UTF8.GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(sb.ToString()))
            .ToArray();
    }

    public static byte[] BuildExcel(
        string tab,
        IReadOnlyList<OrderModel> orders,
        IReadOnlyList<OrderReturnModel> returns,
        IReadOnlyList<RefundModel> refunds,
        string? statusFilter)
    {
        using var workbook = new XLWorkbook();
        var sheetName = tab switch
        {
            "returns" => "Returns",
            "refunds" => "Refunds",
            _ => "Orders"
        };

        var ws = workbook.Worksheets.Add(sheetName);
        var data = tab switch
        {
            "returns" => BuildReturnsRows(returns),
            "refunds" => BuildRefundsRows(refunds),
            _ => BuildOrdersRows(orders)
        };

        WriteStyledSheet(
            ws,
            $"MuuqWear — {GetSectionLabel(tab)}",
            statusFilter,
            data.Headers,
            data.Rows);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static string BuildFileName(
        string tab,
        string extension,
        string? statusFilter,
        bool selectedOnly = false)
    {
        var section = tab switch
        {
            "returns" => "returns",
            "refunds" => "refunds",
            _ => "orders"
        };

        var filter = selectedOnly
            ? "selected"
            : string.IsNullOrWhiteSpace(statusFilter) ? "all" : statusFilter.ToLowerInvariant();
        var date = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return $"muuqwear-{section}-{filter}-{date}.{extension}";
    }

    private static void AppendOrdersCsv(StringBuilder sb, IReadOnlyList<OrderModel> orders)
    {
        sb.AppendLine("Order Number,Customer Email,Customer Name,Items,Subtotal,Shipping,Tax,Total,Status,Order Date");
        foreach (var o in orders)
        {
            sb.AppendLine(string.Join(",",
                Csv(o.OrderNumber),
                Csv(o.Email),
                Csv(FullName(o.FirstName, o.LastName)),
                Csv(o.ItemsSummary),
                CsvMoney(o.Subtotal),
                CsvMoney(o.Shipping),
                CsvMoney(o.Tax),
                CsvMoney(o.Total),
                Csv(TitleCase(o.Status)),
                Csv(FormatDate(o.CreatedAt))));
        }
    }

    private static void AppendReturnsCsv(StringBuilder sb, IReadOnlyList<OrderReturnModel> returns)
    {
        sb.AppendLine("Return Number,Order Number,Customer Name,Email,Items,Reason,Comments,Status,Submitted");
        foreach (var r in returns)
        {
            sb.AppendLine(string.Join(",",
                Csv(r.ReturnNumber),
                Csv(r.OrderNumber),
                Csv(r.FullName),
                Csv(r.Email),
                Csv(r.ItemsToReturn),
                Csv(r.Reason),
                Csv(r.Comments),
                Csv(TitleCase(r.Status)),
                Csv(FormatDate(r.CreatedAt))));
        }
    }

    private static void AppendRefundsCsv(StringBuilder sb, IReadOnlyList<RefundModel> refunds)
    {
        sb.AppendLine("Refund Number,Order Number,Customer Name,Email,Amount,Currency,Status,Created,Processed,Failure Reason");
        foreach (var r in refunds)
        {
            sb.AppendLine(string.Join(",",
                Csv(r.RefundNumber),
                Csv(r.OrderNumber),
                Csv(r.FullName),
                Csv(r.Email),
                CsvMoney(r.Amount),
                Csv(r.Currency ?? "usd"),
                Csv(TitleCase(r.Status)),
                Csv(FormatDate(r.CreatedAt)),
                Csv(FormatDate(r.ProcessedAt)),
                Csv(r.FailureReason)));
        }
    }

    private static (string[] Headers, List<object?[]> Rows) BuildOrdersRows(IReadOnlyList<OrderModel> orders)
    {
        var headers = new[]
        {
            "Order Number", "Customer Email", "Customer Name", "Items",
            "Subtotal", "Shipping", "Tax", "Total", "Status", "Order Date"
        };

        var rows = orders.Select(o => new object?[]
        {
            o.OrderNumber,
            o.Email,
            FullName(o.FirstName, o.LastName),
            o.ItemsSummary,
            o.Subtotal,
            o.Shipping,
            o.Tax,
            o.Total,
            TitleCase(o.Status),
            FormatDate(o.CreatedAt)
        }).ToList();

        return (headers, rows);
    }

    private static (string[] Headers, List<object?[]> Rows) BuildReturnsRows(
        IReadOnlyList<OrderReturnModel> returns)
    {
        var headers = new[]
        {
            "Return Number", "Order Number", "Customer Name", "Email",
            "Items", "Reason", "Comments", "Status", "Submitted"
        };

        var rows = returns.Select(r => new object?[]
        {
            r.ReturnNumber,
            r.OrderNumber,
            r.FullName,
            r.Email,
            r.ItemsToReturn,
            r.Reason,
            r.Comments,
            TitleCase(r.Status),
            FormatDate(r.CreatedAt)
        }).ToList();

        return (headers, rows);
    }

    private static (string[] Headers, List<object?[]> Rows) BuildRefundsRows(
        IReadOnlyList<RefundModel> refunds)
    {
        var headers = new[]
        {
            "Refund Number", "Order Number", "Customer Name", "Email",
            "Amount", "Currency", "Status", "Created", "Processed", "Failure Reason"
        };

        var rows = refunds.Select(r => new object?[]
        {
            r.RefundNumber,
            r.OrderNumber,
            r.FullName,
            r.Email,
            r.Amount,
            (r.Currency ?? "usd").ToUpperInvariant(),
            TitleCase(r.Status),
            FormatDate(r.CreatedAt),
            FormatDate(r.ProcessedAt),
            r.FailureReason
        }).ToList();

        return (headers, rows);
    }

    private static void WriteStyledSheet(
        IXLWorksheet ws,
        string title,
        string? statusFilter,
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

        ws.Cell(2, 1).Value = $"Generated: {stamp}";
        ws.Cell(2, 2).Value = $"Filter: {FormatFilter(statusFilter)}";
        ws.Cell(2, 3).Value = $"Records: {rows.Count}";
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

        var lastRow = Math.Max(dataStart + rows.Count - 1, headerRow);
        ws.Range(1, 1, lastRow, colCount).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }

    private static void WriteCellValue(IXLCell cell, object? value, string header)
    {
        if (value is null)
        {
            cell.Value = string.Empty;
            return;
        }

        if (value is decimal or double or float)
        {
            cell.Value = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            cell.Style.NumberFormat.Format = header.Contains("Amount", StringComparison.OrdinalIgnoreCase)
                                               || header is "Subtotal" or "Shipping" or "Tax" or "Total"
                ? "$#,##0.00"
                : "#,##0.00";
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            return;
        }

        cell.Value = value.ToString();
    }

    private static string GetSectionLabel(string tab) => tab switch
    {
        "returns" => "Process Returns",
        "refunds" => "Refunds",
        _ => "All Orders"
    };

    private static string FormatFilter(string? statusFilter) =>
        string.IsNullOrWhiteSpace(statusFilter) ? "All" : TitleCase(statusFilter);

    private static string FormatDate(DateTime? value) =>
        value?.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture) ?? "";

    private static string FullName(string? first, string? last) =>
        $"{first} {last}".Trim();

    private static string TitleCase(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? ""
            : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.ToLowerInvariant());

    private static string Csv(string? value)
    {
        value ??= string.Empty;
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static string CsvMoney(decimal value) =>
        value.ToString("0.00", CultureInfo.InvariantCulture);
}
