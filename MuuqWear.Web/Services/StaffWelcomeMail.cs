using System.Net.Http.Json;
using System.Text.Json.Serialization;
using MuuqWear.Web.Constants;

namespace MuuqWear.Web.Services;

public sealed class StaffWelcomeMail
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public StaffWelcomeMail(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    public async Task SendPasswordResetAsync(string email, string name, string link, CancellationToken cancellationToken)
    {
        var token = _config["POSTMARK_SERVER_TOKEN"]
            ?? _config["Postmark:ServerToken"]
            ?? _config["postmark-server-token"];
        var from = _config["POSTMARK_FROM"] ?? _config["Postmark:FromEmail"] ?? "MuuqWear <noreply@muuqwear.com>";
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Password email is not configured.");

        var safeName = string.IsNullOrWhiteSpace(name) ? email : name;
        var client = _http.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.postmarkapp.com/email");
        request.Headers.TryAddWithoutValidation("X-Postmark-Server-Token", token);
        request.Content = JsonContent.Create(new PostmarkPayload
        {
            From = from,
            To = email,
            Subject = "Set your MuuqWear dashboard password",
            HtmlBody = Html(safeName, email, link),
            TextBody = Text(safeName, email, link),
            MessageStream = "outbound",
            Tag = "staff-password"
        });
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(body.Length > 180 ? body[..180] : body);
        }
    }

    private static string Html(string name, string email, string link)
    {
        var safeName = System.Net.WebUtility.HtmlEncode(name);
        var safeEmail = System.Net.WebUtility.HtmlEncode(email);
        var safeLink = System.Net.WebUtility.HtmlEncode(link);
        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <body style="margin:0;padding:0;background:#F5F6F8;">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#F5F6F8;">
                <tr><td align="center" style="padding:32px 16px;">
                  <table role="presentation" width="560" cellspacing="0" cellpadding="0" style="max-width:560px;width:100%;background:#ffffff;border-radius:8px;">
                    <tr><td align="center" style="padding:40px 32px 12px;">
                      <img src="https://muuqwear.com/apple-touch-icon.png" width="56" height="56" alt="MuuqWear" style="display:block;border:0;">
                    </td></tr>
                    <tr><td align="center" style="padding:8px 32px 0;font-family:Arial,Helvetica,sans-serif;color:#1E2A47;font-size:24px;font-weight:700;">
                      Set your dashboard password
                    </td></tr>
                    <tr><td align="center" style="padding:12px 40px 28px;font-family:Arial,Helvetica,sans-serif;color:#6c7a92;font-size:15px;line-height:22px;">
                      {safeName}, use this link to set a password for {safeEmail} and open the MuuqWear admin dashboard.
                    </td></tr>
                    <tr><td align="center" style="padding:0 32px 28px;">
                      <a href="{safeLink}" style="display:inline-block;background:#1E2A47;color:#ffffff;text-decoration:none;font-family:Arial,Helvetica,sans-serif;font-size:16px;font-weight:700;padding:14px 28px;border-radius:4px;">Set password</a>
                    </td></tr>
                    <tr><td align="center" style="padding:0 40px 36px;font-family:Arial,Helvetica,sans-serif;color:#A9B7CC;font-size:12px;line-height:18px;">
                      After you set a password, sign in at<br>
                      <a href="https://muuqwear.com{AdminPortalPaths.Login}" style="color:#1E2A47;">{System.Net.WebUtility.HtmlEncode("https://muuqwear.com" + AdminPortalPaths.Login)}</a>
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;
    }

    private static string Text(string name, string email, string link) =>
        $"Set your dashboard password\n\n{name}, set a password for {email}:\n{link}\n\nThen sign in at https://muuqwear.com{AdminPortalPaths.Login}\n";

    private sealed class PostmarkPayload
    {
        [JsonPropertyName("From")] public string From { get; set; } = "";
        [JsonPropertyName("To")] public string To { get; set; } = "";
        [JsonPropertyName("Subject")] public string Subject { get; set; } = "";
        [JsonPropertyName("HtmlBody")] public string HtmlBody { get; set; } = "";
        [JsonPropertyName("TextBody")] public string TextBody { get; set; } = "";
        [JsonPropertyName("MessageStream")] public string MessageStream { get; set; } = "outbound";
        [JsonPropertyName("Tag")] public string Tag { get; set; } = "";
    }
}
