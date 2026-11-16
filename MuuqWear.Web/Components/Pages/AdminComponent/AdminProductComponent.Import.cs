using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using MuuqWear.Model.Products;
using MuuqWear.Web.Services;
using System.Globalization;
using System.Text;

namespace MuuqWear.Web.Components.Pages.AdminComponent;

public partial class AdminProductComponent
{
    private InputFile? csvImportInput;
    private bool isImporting;
    private string importMessage = string.Empty;

    public const string CsvSampleHeader =
        "Name,Price,Stock,Sizes,Category,Description,IsActive,IsNewArrival,IsFeatured,IsBestSeller,Colors";

    public const string CsvSampleBody =
        "Essential Tee,49.00,10,S;M;L,Apparel,Soft cotton everyday tee,true,true,false,false,Black;White\n" +
        "Studio Pant,89.00,5,28;30;32,Bottoms,Tapered technical pant,true,false,true,false,Navy";

    private async Task DownloadSampleCsvAsync()
    {
        var csv = CsvSampleHeader + "\n" + CsvSampleBody + "\n";
        var bytes = Encoding.UTF8.GetBytes(csv);
        await JS.InvokeVoidAsync(
            "adminExport.downloadBytes",
            "muuqwear-products-sample.csv",
            Convert.ToBase64String(bytes),
            "text/csv;charset=utf-8");
        exportMessage = "Sample CSV downloaded.";
    }

    private async Task OpenCsvImportPickerAsync()
    {
        if (isImporting || csvImportInput is null)
            return;
        await JS.InvokeVoidAsync("eval", "document.querySelector('.products-csv-import-input input[type=file]')?.click()");
    }

    private async Task HandleCsvImportAsync(InputFileChangeEventArgs e)
    {
        if (isImporting)
            return;

        isImporting = true;
        importMessage = string.Empty;
        exportMessage = string.Empty;
        StateHasChanged();

        try
        {
            var file = e.File;
            if (file.Size > 2 * 1024 * 1024)
            {
                importMessage = "CSV must be under 2MB.";
                return;
            }

            await using var stream = file.OpenReadStream(2 * 1024 * 1024);
            using var reader = new StreamReader(stream);
            var text = await reader.ReadToEndAsync();
            var rows = ParseProductCsv(text);
            if (rows.Count == 0)
            {
                importMessage = "No product rows found in the CSV.";
                return;
            }

            var created = 0;
            var failed = 0;
            string? lastError = null;

            foreach (var row in rows)
            {
                var result = await ProductService.Add(row);
                if (result.Success)
                    created++;
                else
                {
                    failed++;
                    lastError = result.Message;
                }
            }

            await LoadProducts();
            importMessage = failed == 0
                ? $"Imported {created} product{(created == 1 ? "" : "s")}."
                : $"Imported {created}, failed {failed}.{(string.IsNullOrWhiteSpace(lastError) ? "" : " " + lastError)}";
            exportMessage = importMessage;
        }
        catch (Exception ex)
        {
            importMessage = "Import failed: " + AdminUiErrorHelper.FromException(ex);
            exportMessage = importMessage;
        }
        finally
        {
            isImporting = false;
            StateHasChanged();
        }
    }

    private static List<AddProductModel> ParseProductCsv(string text)
    {
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length == 0)
            return [];

        var start = 0;
        if (lines[0].Contains("Name", StringComparison.OrdinalIgnoreCase)
            && lines[0].Contains("Price", StringComparison.OrdinalIgnoreCase))
            start = 1;

        var products = new List<AddProductModel>();
        for (var i = start; i < lines.Length; i++)
        {
            var cols = SplitCsvLine(lines[i]);
            if (cols.Count == 0 || string.IsNullOrWhiteSpace(cols[0]))
                continue;

            var name = cols[0].Trim();
            if (!decimal.TryParse(Get(cols, 1), NumberStyles.Number, CultureInfo.InvariantCulture, out var price)
                || price <= 0)
                continue;

            _ = int.TryParse(Get(cols, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out var stock);
            var sizes = SplitList(Get(cols, 3));
            if (sizes.Count == 0)
                sizes = ["OS"];

            products.Add(new AddProductModel
            {
                Name = name,
                Price = price,
                Stock = Math.Max(0, stock),
                Sizes = sizes,
                Category = NullIfBlank(Get(cols, 4)),
                Description = NullIfBlank(Get(cols, 5)),
                IsActive = ParseBool(Get(cols, 6), true),
                IsNewArrival = ParseBool(Get(cols, 7), false),
                IsFeatured = ParseBool(Get(cols, 8), false),
                IsBestSeller = ParseBool(Get(cols, 9), false),
                ColorOptions = SplitList(Get(cols, 10))
            });
        }

        return products;
    }

    private static string Get(IReadOnlyList<string> cols, int index) =>
        index < cols.Count ? cols[index] : "";

    private static string? NullIfBlank(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static List<string> SplitList(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split([';', ',', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

    private static bool ParseBool(string value, bool fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;
        if (bool.TryParse(value, out var b))
            return b;
        return value.Trim() switch
        {
            "1" or "yes" or "y" or "Y" or "Yes" => true,
            "0" or "no" or "n" or "N" or "No" => false,
            _ => fallback
        };
    }

    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                    inQuotes = !inQuotes;
                continue;
            }

            if (c == ',' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
                continue;
            }

            sb.Append(c);
        }

        result.Add(sb.ToString());
        return result;
    }
}
