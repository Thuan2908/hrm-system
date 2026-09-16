namespace Hrm.E2ETests;

public sealed class ApplicationSmokeTests
{
    [Fact(Skip = "Requires the locally running API, Blazor app, PostgreSQL and installed Playwright browsers.")]
    public void LoginAndDashboardSmokeTest()
    {
    }
}
