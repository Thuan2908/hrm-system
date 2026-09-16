using Bunit;
using Hrm.Web.Pages;

namespace Hrm.ComponentTests;

public sealed class HomePageTests : BunitContext
{
    [Fact]
    public void DisplaysAllBusinessAreas()
    {
        var component = Render<Home>();

        Assert.Contains("Trung tâm điều hành", component.Markup, StringComparison.Ordinal);
        Assert.Equal(6, component.FindAll("section.card").Count);
    }
}
