using System.Diagnostics;
using MannaHp.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

namespace MannaHp.E2E.Tests.Fixtures;

/// <summary>
/// Starts the API server and Next.js dev server as child processes,
/// prepares the test database, and provides Playwright browser pages.
///
/// Prerequisites:
///   - PostgreSQL running on localhost:5432
///   - Node.js / npm available in PATH
///   - Playwright browsers installed: pwsh bin/.../playwright.ps1 install
/// </summary>
public class E2EFixture : IAsyncLifetime
{
    private const string TestConnectionString =
        "Host=localhost;Port=5432;Database=restaurant_e2e;Username=app;Password=devpassword123";

    public const int ApiPort = 5099;
    public const int NextPort = 3099;

    public string ApiBaseUrl => $"http://localhost:{ApiPort}";
    public string NextBaseUrl => $"http://localhost:{NextPort}";

    private Process? _apiProcess;
    private Process? _nextProcess;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    private static string ServerProjectDir => Path.Combine(RepoRoot, "src", "Server");
    private static string NextClientDir => Path.Combine(RepoRoot, "src", "next-client");

    public async ValueTask InitializeAsync()
    {
        // 1. Prepare the test database
        await PrepareDatabaseAsync();

        // 2. Start API server
        _apiProcess = StartProcess(
            fileName: "dotnet",
            arguments: $"run --project \"{ServerProjectDir}\"",
            workingDirectory: RepoRoot,
            environment: new Dictionary<string, string>
            {
                ["ASPNETCORE_URLS"] = $"http://localhost:{ApiPort}",
                ["ConnectionStrings__DefaultConnection"] = TestConnectionString,
                ["Stripe__SecretKey"] = "sk_test_fake_key_for_e2e",
                ["Stripe__PublishableKey"] = "pk_test_fake_key_for_e2e",
                ["Stripe__WebhookSecret"] = "whsec_fake_secret_for_e2e",
                ["Jwt__Key"] = "e2e-test-secret-key-for-local-testing-only-32chars!!",
                ["Jwt__Issuer"] = "MannaHp",
                ["Jwt__Audience"] = "MannaHp",
                ["Jwt__ExpiresInMinutes"] = "1440",
                ["CorsOrigins"] = $"http://localhost:{NextPort}",
            });

        await WaitForUrlAsync($"{ApiBaseUrl}/api/categories", timeoutSeconds: 60);

        // 3. Start Next.js dev server
        _nextProcess = StartProcess(
            fileName: "npx",
            arguments: $"next dev --port {NextPort}",
            workingDirectory: NextClientDir,
            environment: new Dictionary<string, string>
            {
                ["NEXT_PUBLIC_API_URL"] = ApiBaseUrl,
            });

        await WaitForUrlAsync(NextBaseUrl, timeoutSeconds: 60);

        // 4. Set up Playwright
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });
    }

    /// <summary>
    /// Creates a fresh Playwright page in an isolated browser context.
    /// </summary>
    public async Task<IPage> CreatePageAsync()
    {
        var context = await _browser!.NewContextAsync();
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(15_000);
        return page;
    }

    public async ValueTask DisposeAsync()
    {
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();

        KillProcess(_nextProcess);
        KillProcess(_apiProcess);

        // Clean up test database
        var options = new DbContextOptionsBuilder<MannaDbContext>()
            .UseNpgsql(TestConnectionString)
            .Options;
        await using var db = new MannaDbContext(options);
        await db.Database.EnsureDeletedAsync();
    }

    // ── Helpers ────────────────────────────────────────────────────

    private static async Task PrepareDatabaseAsync()
    {
        var options = new DbContextOptionsBuilder<MannaDbContext>()
            .UseNpgsql(TestConnectionString,
                npgsql => npgsql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
            .Options;

        await using var db = new MannaDbContext(options);
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }

    private static Process StartProcess(string fileName, string arguments,
        string workingDirectory, Dictionary<string, string> environment)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        foreach (var (key, value) in environment)
            psi.Environment[key] = value;

        var process = new Process { StartInfo = psi };
        process.Start();

        // Drain stdout/stderr to avoid buffer deadlocks
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return process;
    }

    private static async Task WaitForUrlAsync(string url, int timeoutSeconds)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var resp = await http.GetAsync(url);
                if ((int)resp.StatusCode < 500) return; // Any non-5xx means the server is up
            }
            catch
            {
                // Not ready yet
            }
            await Task.Delay(1000);
        }

        throw new TimeoutException($"Server at {url} did not start within {timeoutSeconds}s");
    }

    private static void KillProcess(Process? process)
    {
        if (process is null || process.HasExited) return;
        try
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit(5000);
        }
        catch { /* best effort */ }
        finally
        {
            process.Dispose();
        }
    }
}
