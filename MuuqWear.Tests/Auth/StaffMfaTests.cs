using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using MuuqWear.Web.ServerAuth;
using Xunit;

namespace MuuqWear.Tests.Auth;

public class StaffMfaTests
{
    [Fact]
    public void SecondFactorIsReadFromDictionaryJsonElementAndJsonText()
    {
        Assert.True(StaffMfaPolicy.FirebaseHasSecondFactor(new Dictionary<string, object>
        {
            ["firebase"] = new Dictionary<string, object> { ["sign_in_second_factor"] = "totp" }
        }));

        var element = JsonDocument.Parse("""{"sign_in_second_factor":"totp"}""").RootElement;
        Assert.True(StaffMfaPolicy.FirebaseHasSecondFactor(new Dictionary<string, object> { ["firebase"] = element }));

        // FirebaseAdmin hands back a Newtonsoft JObject, whose ToString() is JSON.
        Assert.True(StaffMfaPolicy.FirebaseHasSecondFactor(new Dictionary<string, object>
        {
            ["firebase"] = new JsonText("""{"sign_in_provider":"password","sign_in_second_factor":"totp"}""")
        }));
    }

    [Fact]
    public void NoSecondFactorWhenMissing()
    {
        Assert.False(StaffMfaPolicy.FirebaseHasSecondFactor(null));
        Assert.False(StaffMfaPolicy.FirebaseHasSecondFactor(new Dictionary<string, object>()));
        Assert.False(StaffMfaPolicy.FirebaseHasSecondFactor(new Dictionary<string, object>
        {
            ["firebase"] = new JsonText("""{"sign_in_provider":"password"}""")
        }));
        Assert.False(StaffMfaPolicy.FirebaseHasSecondFactor(new Dictionary<string, object>
        {
            ["firebase"] = new JsonText("not json")
        }));
    }

    [Fact]
    public async Task PasswordSignInReportsPendingTotp()
    {
        var service = Service("""
            {"localId":"uid-1","email":"ops@muuqwear.com","mfaPendingCredential":"pending-123",
             "mfaInfo":[{"mfaEnrollmentId":"enroll-9","displayName":"Authenticator app","totpInfo":{}}]}
            """);

        var result = await service.SignInWithPasswordAsync("ops@muuqwear.com", "pw");

        Assert.Equal(StaffPasswordOutcome.MfaRequired, result.Outcome);
        Assert.Equal("pending-123", result.PendingCredential);
        Assert.Equal("enroll-9", result.EnrollmentId);
        Assert.Null(result.IdToken);
    }

    [Fact]
    public async Task PasswordSignInWithoutFactorReturnsToken()
    {
        var service = Service("""{"idToken":"id-abc","email":"ops@muuqwear.com"}""");

        var result = await service.SignInWithPasswordAsync("ops@muuqwear.com", "pw");

        Assert.Equal(StaffPasswordOutcome.SignedIn, result.Outcome);
        Assert.Equal("id-abc", result.IdToken);
    }

    [Fact]
    public async Task WrongPasswordIsInvalid()
    {
        var service = Service("""{"error":{"message":"INVALID_LOGIN_CREDENTIALS"}}""", HttpStatusCode.BadRequest);

        var result = await service.SignInWithPasswordAsync("ops@muuqwear.com", "pw");

        Assert.Equal(StaffPasswordOutcome.Invalid, result.Outcome);
    }

    [Fact]
    public async Task EnrollStartReturnsSecretAndSession()
    {
        var service = Service("""
            {"totpSessionInfo":{"sharedSecretKey":"JBSWY3DPEHPK3PXP","verificationCodeLength":6,
             "hashingAlgorithm":"SHA1","periodSec":30,"sessionInfo":"session-1"}}
            """);

        var start = await service.StartEnrollAsync("id-abc");

        Assert.True(start.Ok);
        Assert.Equal("JBSWY3DPEHPK3PXP", start.SecretKey);
        Assert.Equal("session-1", start.SessionInfo);
    }

    [Fact]
    public async Task EnrollStartExplainsUnverifiedEmail()
    {
        var service = Service("""{"error":{"message":"UNVERIFIED_EMAIL : Need to verify email first."}}""", HttpStatusCode.BadRequest);

        var start = await service.StartEnrollAsync("id-abc");

        Assert.False(start.Ok);
        Assert.Equal("unverified", start.Error);
    }

    [Fact]
    public async Task FinalizeSignInSendsCodeAndReturnsToken()
    {
        var handler = new FakeHandler("""{"idToken":"id-mfa","refreshToken":"r"}""");
        var service = new StaffMfaService(new HttpClient(handler), Config());

        var result = await service.FinalizeSignInAsync("pending-123", "enroll-9", "123456");

        Assert.True(result.Ok);
        Assert.Equal("id-mfa", result.IdToken);
        Assert.Contains("/v2/accounts/mfaSignIn:finalize", handler.LastUrl);
        Assert.Contains("\"verificationCode\":\"123456\"", handler.LastBody);
        Assert.Contains("\"mfaEnrollmentId\":\"enroll-9\"", handler.LastBody);
    }

    [Fact]
    public async Task WrongCodeIsReportedAsCode()
    {
        var service = Service("""{"error":{"message":"INVALID_MFA_VERIFICATION_CODE"}}""", HttpStatusCode.BadRequest);

        var result = await service.FinalizeEnrollAsync("id-abc", "session-1", "000000");

        Assert.False(result.Ok);
        Assert.Equal("code", result.Error);
    }

    [Fact]
    public void OtpAuthUriNamesIssuerAndAccount()
    {
        var uri = StaffMfaService.OtpAuthUri("ops@muuqwear.com", "JBSWY3DPEHPK3PXP");

        Assert.StartsWith("otpauth://totp/MuuqWear%20Admin:ops%40muuqwear.com?", uri);
        Assert.Contains("secret=JBSWY3DPEHPK3PXP", uri);
        Assert.Contains("issuer=MuuqWear%20Admin", uri);
    }

    [Fact]
    public void TicketRoundTripsAndRejectsTampering()
    {
        var tickets = new StaffMfaTicket(new EphemeralDataProtectionProvider());
        var protectedText = tickets.Protect(new StaffMfaStep(
            StaffMfaStep.SignIn, "ops@muuqwear.com", PendingCredential: "pending-123", EnrollmentId: "enroll-9"));

        var step = tickets.Unprotect(protectedText);

        Assert.NotNull(step);
        Assert.Equal("pending-123", step!.PendingCredential);
        Assert.DoesNotContain("pending-123", protectedText);
        Assert.Null(tickets.Unprotect(protectedText[..^4] + "AAAA"));
        Assert.Null(tickets.Unprotect(null));
        Assert.Null(new StaffMfaTicket(new EphemeralDataProtectionProvider()).Unprotect(protectedText));
    }

    [Fact]
    public void StaffIdentityReadsFirebaseIdToken()
    {
        var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(claims:
        [
            new("user_id", "uid-7"),
            new("email", "ops@muuqwear.com"),
            new("name", "Ops"),
            new("role", "support_team")
        ]));

        var identity = StaffIdentity.FromIdToken(token);

        Assert.Equal("uid-7", identity.Uid);
        Assert.Equal("ops@muuqwear.com", identity.Email);
        Assert.Equal("support_team", identity.Role);
        Assert.Equal("user", StaffIdentity.FromIdToken("garbage").Role);
    }

    private static StaffMfaService Service(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(new HttpClient(new FakeHandler(json, status)), Config());

    private static IConfiguration Config() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Firebase:ApiKey"] = "test-key" })
            .Build();

    private sealed class JsonText(string json)
    {
        public override string ToString() => json;
    }

    private sealed class FakeHandler(string json, HttpStatusCode status = HttpStatusCode.OK) : HttpMessageHandler
    {
        public string LastUrl { get; private set; } = "";
        public string LastBody { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastUrl = request.RequestUri?.ToString() ?? "";
            LastBody = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }
    }
}
