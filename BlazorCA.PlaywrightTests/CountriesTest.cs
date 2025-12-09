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
    private const string statsUrl = "http://localhost:5188/stats";

    public async Task InitializeAsync()
    {
        _blazorApp = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --urls http://localhost:5188",
            WorkingDirectory = @"../../../../Blazor CA X00182575",
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

    [Fact] // Checks list of countries
	public async Task CountriesPage_ShouldDisplayCountries()
    {
        var page = await _context.NewPageAsync();
        await page.GotoAsync(Url);
        await page.WaitForSelectorAsync(".list-group-item.d-flex");
        var count = await page.Locator(".list-group-item").CountAsync();
        Assert.True(count > 0, $"No countries displayed");
    }

    [Fact] // Checks search bar
	public async Task Search_ShouldFilterCountries()
    {
        var page = await _context.NewPageAsync();
        await page.GotoAsync(Url);
        await page.FillAsync("input[placeholder='Type a country name...']", "Ireland");

        var text = await page.Locator(".list-group-item h5 a").First.InnerTextAsync();
        Assert.Contains("Ireland", text, StringComparison.OrdinalIgnoreCase);
    }

	[Fact] // Checks charts
	public async Task StatsPage_ShouldDisplayCharts()
	{
		var page = await _context.NewPageAsync();
		await page.GotoAsync(statsUrl);

		await page.WaitForSelectorAsync(".chart-container canvas",
			new() { State = WaitForSelectorState.Attached });

		var canvases = page.Locator(".chart-container canvas");
		int count = await canvases.CountAsync();

		Assert.True(count == 2, "Charts missing");

		await Assertions.Expect(canvases.First).ToBeVisibleAsync();
	}

	[Fact] // Checks Sorting
	public async Task Sort_ShouldSortCountries()
	{
		var page = await _context.NewPageAsync();
		await page.GotoAsync(Url);

		await page.WaitForSelectorAsync(".list-group-item h5 a");

		var sort = page.Locator("#sortSelect");

		// Sort by name
		await sort.SelectOptionAsync("name");
		await page.WaitForTimeoutAsync(300);

		var firstCountry = await page.Locator(".list-group-item h5 a").First.InnerTextAsync();
		var secondCountry = await page.Locator(".list-group-item h5 a").Nth(1).InnerTextAsync();

		Assert.True(
			string.Compare(firstCountry, secondCountry, StringComparison.OrdinalIgnoreCase) < 0,
			$"Name sort wrong. Got {firstCountry} before {secondCountry}"
		);

		// Sort by poplulation
		await sort.SelectOptionAsync("population");
		await page.WaitForTimeoutAsync(300);

		var firstCountryPop = await page.Locator(".list-group-item .text-end p:nth-child(1) b").First.InnerTextAsync();
		var secondCountryPop = await page.Locator(".list-group-item .text-end p:nth-child(1) b").Nth(1).InnerTextAsync();

		long firstPop = long.Parse(firstCountryPop.Replace(",", ""));
		long secondPop = long.Parse(secondCountryPop.Replace(",", ""));

		Assert.True(firstPop >= secondPop,
			$"Population sort wrong. {firstPop} should be morethan {secondPop}");
	}

	[Fact]
	public async Task ClickingCountry_ShouldOpenGoogleMaps()
	{
		var page = await _context.NewPageAsync();
		await page.GotoAsync(Url);

		await page.WaitForSelectorAsync(".list-group-item h5 a");
		var mapPage = page.Context.WaitForPageAsync();
		await page.Locator(".list-group-item h5 a").First.ClickAsync();

		var newPage = await mapPage;
		await newPage.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

		var mapURL = newPage.Url;

		Assert.True(
			mapURL.StartsWith("https://www.google.com/maps") ||
			mapURL.StartsWith("https://consent.google.com"),
			$"Google maps didn't open but this did: {mapURL}"
		);
	}


}