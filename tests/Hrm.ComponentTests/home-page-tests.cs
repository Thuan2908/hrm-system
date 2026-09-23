using Bunit;
using Hrm.Web.Authorization;
using Hrm.Web.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Hrm.ComponentTests;

public sealed class HomePageTests : BunitContext
{
    [Fact]
    public void RendersLoadingSpinnerInitially()
    {
        JSInterop.Setup<string?>("sessionStorage.getItem", _ => true).SetResult(null);
        Services.AddSingleton<BrowserSessionStore>();

        var component = Render<Home>();

        Assert.NotNull(component.Find(".spinner-border"));
    }
}
