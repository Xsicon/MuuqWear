using MuuqWear.Model.ContentItem;

namespace MuuqWear.Application.Content;

public readonly record struct JournalSeoInput(
    string? Title,
    string? SeoTitle,
    string? Content,
    string? Category,
    string? ImageUrl,
    string? Excerpt = null);

public static class JournalSeoScorer
{
    private const int IdealTitleMin = 30;
    private const int IdealTitleMax = 65;
    private const int IdealMetaMin = 50;
    private const int IdealMetaMax = 60;
    private const int IdealMetaDescriptionMin = 120;
    private const int IdealMetaDescriptionMax = 160;
    private const int MinBodyWords = 300;

    public static int Score(ContentItemModel item)
    {
        var seoTitle = !string.IsNullOrWhiteSpace(item.SeoTitle)
            ? item.SeoTitle
            : string.IsNullOrWhiteSpace(item.Title)
                ? null
                : $"{item.Title.Trim()} | Muuqwear Journal";

        return Score(new JournalSeoInput(
            item.Title,
            seoTitle,
            item.Content,
            item.Category,
            item.ImageUrl,
            item.Excerpt));
    }

    public static int Score(JournalSeoInput input)
    {
        var points = 0.0;

        points += ScoreTitle(input.Title);
        points += ScoreMetaTitle(input.SeoTitle, input.Title);
        points += ScoreSlug(input.Title);
        points += ScoreCategory(input.Category);
        points += ScoreFeaturedImage(input.ImageUrl);
        points += ScoreBodyContent(input.Content);
        points += ScoreMetaDescription(input.Excerpt, input.Content);
        points += ScoreKeywordAlignment(input.Title, input.Content);

        return (int)Math.Round(Math.Clamp(points, 0, 100));
    }

    public static string GetBarColor(int score) =>
        score >= 85 ? "#22C55E" : score >= 65 ? "#F59E0B" : "#EF4444";

    public static string GetTopSuggestion(JournalSeoInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Title))
            return "Add an article title.";

        if (string.IsNullOrWhiteSpace(input.ImageUrl))
            return "Add a featured image for social previews and search snippets.";

        var metaLen = input.SeoTitle?.Trim().Length ?? 0;
        if (metaLen < IdealMetaMin)
            return $"Extend the SEO title toward {IdealMetaMin}–{IdealMetaMax} characters (currently {metaLen}).";

        if (metaLen > 70)
            return "Shorten the SEO title so it won't truncate in search results.";

        var words = CountWords(input.Content);
        if (words < MinBodyWords)
            return $"Add more body content — aim for at least {MinBodyWords} words (currently {words}).";

        var description = GetMetaDescriptionCandidate(input.Excerpt, input.Content);
        if (description.Length < IdealMetaDescriptionMin)
            return "Write a stronger opening paragraph (120+ characters) for the meta description.";

        if (string.IsNullOrWhiteSpace(input.Category))
            return "Select a category to improve discoverability.";

        if (ScoreKeywordAlignment(input.Title, input.Content) < 8)
            return "Use key terms from the title in the article body.";

        return "SEO looks strong — review slug and publish when ready.";
    }

    private static double ScoreTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return 0;

        var len = title.Trim().Length;
        if (len < 10)
            return 4;
        if (len < IdealTitleMin)
            return 10;
        if (len <= IdealTitleMax)
            return 20;
        if (len <= 80)
            return 14;

        return 8;
    }

    private static double ScoreMetaTitle(string? seoTitle, string? title)
    {
        if (string.IsNullOrWhiteSpace(seoTitle))
            return 0;

        var trimmed = seoTitle.Trim();
        if (string.IsNullOrWhiteSpace(title) || trimmed.Length < 10)
            return 3;

        var len = trimmed.Length;
        if (len >= IdealMetaMin && len <= IdealMetaMax)
            return 15;

        if (len >= 40 && len < IdealMetaMin)
            return 11;

        if (len > IdealMetaMax && len <= 70)
            return 10;

        if (len >= 20)
            return 6;

        return 2;
    }

    private static double ScoreSlug(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return 0;

        var slug = BuildSlug(title);
        if (string.IsNullOrEmpty(slug))
            return 0;

        if (slug.Length > 75)
            return 4;

        if (slug.Contains("--", StringComparison.Ordinal))
            return 5;

        if (slug.Length >= 3)
            return 10;

        return 3;
    }

    private static double ScoreCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return 0;

        var valid = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Culture", "Design", "Innovation", "Lifestyle", "Tech"
        };

        return valid.Contains(category.Trim()) ? 5 : 2;
    }

    private static double ScoreFeaturedImage(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return 0;

        var url = imageUrl.Trim();
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return 8;

        if (uri.Scheme is not ("http" or "https"))
            return 8;

        var path = uri.AbsolutePath;
        if (path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
            || path.Contains("unsplash", StringComparison.OrdinalIgnoreCase)
            || path.Contains("image", StringComparison.OrdinalIgnoreCase))
        {
            return 15;
        }

        return 12;
    }

    private static double ScoreBodyContent(string? content)
    {
        var words = CountWords(content);
        if (words == 0)
            return 0;

        if (words >= 800)
            return 25;

        if (words >= 500)
            return 22;

        if (words >= MinBodyWords)
            return 18;

        if (words >= 150)
            return 10;

        if (words >= 50)
            return 5;

        return 2;
    }

    private static double ScoreMetaDescription(string? excerpt, string? content)
    {
        var description = GetMetaDescriptionCandidate(excerpt, content);
        if (description.Length == 0)
            return 0;

        if (description.Length >= IdealMetaDescriptionMin
            && description.Length <= IdealMetaDescriptionMax)
        {
            return 10;
        }

        if (description.Length >= 80)
            return 7;

        if (description.Length >= 40)
            return 4;

        return 1;
    }

    private static double ScoreKeywordAlignment(string? title, string? content)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
            return 0;

        var keywords = title
            .Split([' ', '-', '—', ':', ',', '.'], StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim().ToLowerInvariant())
            .Where(w => w.Length > 3)
            .Distinct()
            .ToList();

        if (keywords.Count == 0)
            return 5;

        var body = content.ToLowerInvariant();
        var matched = keywords.Count(k => body.Contains(k, StringComparison.Ordinal));
        var ratio = (double)matched / keywords.Count;

        if (ratio >= 0.75)
            return 10;

        if (ratio >= 0.5)
            return 7;

        if (ratio >= 0.25)
            return 4;

        return 1;
    }

    private static string GetMetaDescriptionCandidate(string? excerpt, string? content)
    {
        if (!string.IsNullOrWhiteSpace(excerpt))
            return excerpt.Trim();

        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        var normalized = content.Trim().ReplaceLineEndings(" ");
        var firstSentence = normalized.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? normalized;
        return firstSentence.Length > IdealMetaDescriptionMax
            ? firstSentence[..IdealMetaDescriptionMax]
            : firstSentence;
    }

    public static int CountWords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;
    }

    public static int EstimateReadTimeMinutes(string? content) =>
        Math.Max(1, (int)Math.Ceiling(CountWords(content) / 200.0));

    public static string BuildSlug(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        var slug = title.Trim().ToLowerInvariant();
        var chars = slug
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray();

        slug = new string(chars);
        while (slug.Contains("--", StringComparison.Ordinal))
            slug = slug.Replace("--", "-", StringComparison.Ordinal);

        return slug.Trim('-');
    }
}
