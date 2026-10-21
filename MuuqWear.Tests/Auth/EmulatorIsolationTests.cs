using Xunit;

namespace MuuqWear.Tests.Auth;

public class EmulatorIsolationTests
{
    [Fact]
    public void ProductionClientSettings_DoNotUseTheEmulatorSuite()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        string? path = null;
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "MuuqWear.Client", "wwwroot", "appsettings.Production.json");
            if (File.Exists(candidate))
            {
                path = candidate;
                break;
            }
            dir = dir.Parent;
        }
        Assert.False(string.IsNullOrEmpty(path), "appsettings.Production.json was not found");
        var json = File.ReadAllText(path!);

        Assert.DoesNotContain("127.0.0.1:8080", json, StringComparison.Ordinal);
        Assert.DoesNotContain("127.0.0.1:9099", json, StringComparison.Ordinal);
        Assert.DoesNotContain("127.0.0.1:5001", json, StringComparison.Ordinal);
        Assert.DoesNotContain("UseEmulators", json, StringComparison.OrdinalIgnoreCase);
    }
}
