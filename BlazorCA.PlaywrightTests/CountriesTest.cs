using Microsoft.Playwright;
using System.Diagnostics;
using System.Net.Http;
using Xunit;

public class CountriesTests : IAsyncLifetime
{
    private IPlaywright _playwright;
    private IBrowser _browser;
    private IBrowserContext _context;
    private Process _blazorApp;

    private const string Url = "http://localhost:5188/countries";

    public async Task InitializeAsync()
    {
        _blazorApp = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --urls http://localhost:5188",
            WorkingDirectory = @"..\..\..\..\Blazor CA X00182575",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        await WaitUntilServerIsReady("http://localhost:5188");

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Environment.GetEnvironmentVariable("CI") == "true"
        });
        _context = await _browser.NewContextAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.CloseAsync();
        await _browser.CloseAsync();

        if (!_blazorApp.HasExited)
            _blazorApp.Kill();
    }

    private async Task WaitUntilServerIsReady(string url)
    {
        using var client = new HttpClient();
        for (int i = 0; i < 60; i++)
        {
            try
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                    return;
            }
            catch { }

            await Task.Delay(500);
        }

        throw new Exception("Blazor server did not start in time.");
    }

    [Fact]
    public async Task CountriesPage_ShouldDisplayCountries()
    {
        var page = await _context.NewPageAsync();
        await page.GotoAsync(Url);
        await page.WaitForSelectorAsync(".list-group-item.d-flex");
        var count = await page.Locator(".list-group-item").CountAsync();
        Assert.True(count > 0);
    }

    [Fact]
    public async Task Search_ShouldFilterCountries()
    {
        var page = await _context.NewPageAsync();
        await page.GotoAsync(Url);
        await page.FillAsync("input[placeholder='Type a country name...']", "Ireland");

        var text = await page.Locator(".list-group-item h5 a").First.InnerTextAsync();
        Assert.Contains("Ireland", text, StringComparison.OrdinalIgnoreCase);
    }
}