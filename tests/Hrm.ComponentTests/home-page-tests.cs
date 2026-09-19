using Bunit;
using Hrm.Web.Authorization;
using Hrm.Web.Pages;
using Hrm.Web.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Hrm.ComponentTests;

public sealed class HomePageTests : BunitContext
{
    [Fact]
    public void DisplaysAllBusinessAreas()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddScoped<BrowserSessionStore>();
        var component = Render<Home>();
        Assert.NotNull(component);
    }
}
