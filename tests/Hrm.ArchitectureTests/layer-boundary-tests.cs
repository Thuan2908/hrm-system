using Hrm.Api.Controllers;
using NetArchTest.Rules;

namespace Hrm.ArchitectureTests;

public sealed class LayerBoundaryTests
{
    [Fact]
    public void ControllersDoNotDependOnEntityFrameworkCore()
    {
        var result = Types.InAssembly(typeof(SystemController).Assembly)
            .That()
            .ResideInNamespace("Hrm.Api.Controllers")
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }
}
