namespace TiktokExplode.IntegrationTests.Fixtures;

public sealed class PlaywrightFixture
{
    public PlaywrightFixture()
    {
        try
        {
            using var playwright = Microsoft.Playwright.Playwright.CreateAsync()
                .GetAwaiter()
                .GetResult();

            var browser = playwright.Chromium
                .LaunchAsync(new()
                {
                    Headless = true
                })
                .GetAwaiter()
                .GetResult();

            browser.CloseAsync()
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                """
                Playwright is installed but Chromium is missing.

                Run:

                    pwsh playwright.ps1 install

                before executing integration tests.
                """,
                ex);
        }
    }
}
