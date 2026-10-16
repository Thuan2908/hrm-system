using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Bunit;
using Hrm.Contracts;
using Hrm.Web.Features.Leave;
using Hrm.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Hrm.ComponentTests;

public sealed class LeaveApprovalPageTests : BunitContext
{
    [Fact]
    public async Task LoadingDoesNotShowAccessDeniedWhileOptionsOrRequestsArePending()
    {
        using var handler = new DelayedApprovalHandler();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://test.invalid/") };
        ConfigureServices(client);
        var component = Render<CascadingAuthenticationState>(p => p.AddChildContent<LeaveApprovalsPage>());

        component.WaitForAssertion(() => Assert.Single(component.FindAll("[role='status']")));
        Assert.Empty(component.FindAll(".alert-warning"));
        Assert.Empty(component.FindAll(".emp-table"));

        handler.Options.SetResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(ApiResponse.Ok(new LeaveApprovalOptionsDto(true, [])))
        });
        await handler.PendingRequested.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        Assert.Single(component.FindAll("[role='status']"));
        Assert.Empty(component.FindAll(".alert-warning"));
        Assert.Empty(component.FindAll(".emp-table"));

        handler.Pending.SetResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(ApiResponse.Ok<IReadOnlyList<PendingLeaveApprovalDto>>([]))
        });
        component.WaitForAssertion(() => Assert.Single(component.FindAll(".emp-table")));
        Assert.Empty(component.FindAll("[role='status']"));
        Assert.Empty(component.FindAll(".alert-warning"));
    }

    [Fact]
    public void FailedLoadShowsErrorWithoutIncorrectAccessDeniedWarning()
    {
        using var handler = new DelayedApprovalHandler();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://test.invalid/") };
        ConfigureServices(client);
        var component = Render<CascadingAuthenticationState>(p => p.AddChildContent<LeaveApprovalsPage>());

        handler.Options.SetException(new HttpRequestException("Network unavailable"));

        component.WaitForAssertion(() => Assert.Single(component.FindAll(".alert-danger")));
        Assert.Empty(component.FindAll("[role='status']"));
        Assert.Empty(component.FindAll(".alert-warning"));
        Assert.Empty(component.FindAll(".emp-table"));
    }

    private void ConfigureServices(HttpClient client)
    {
        Services.AddAuthorizationCore();
        Services.AddSingleton<IAuthorizationService, DefaultAuthorizationService>();
        Services.AddSingleton<AuthenticationStateProvider>(new SignedInProvider(true));
        Services.AddSingleton<ILeaveApiClient>(new LeaveApiClient(client));
        Services.AddSingleton<IFileApiClient>(new FileApiClient(client, JSInterop.JSRuntime));
    }

    private sealed class DelayedApprovalHandler : HttpMessageHandler
    {
        public TaskCompletionSource<HttpResponseMessage> Options { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<HttpResponseMessage> Pending { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource PendingRequested { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri!.PathAndQuery.Contains("approval-options", StringComparison.Ordinal))
            {
                return Options.Task;
            }

            PendingRequested.TrySetResult();
            return Pending.Task;
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GlobalScopeShowsFiltersAndSendsSelectionsToApi(bool global)
    {
        using var handler = new ApprovalHandler(global);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://test.invalid/") };
        Services.AddAuthorizationCore();
        Services.AddSingleton<IAuthorizationService, DefaultAuthorizationService>();
        Services.AddSingleton<AuthenticationStateProvider>(new SignedInProvider(global));
        Services.AddSingleton<ILeaveApiClient>(new LeaveApiClient(client));
        Services.AddSingleton<IFileApiClient>(new FileApiClient(client, JSInterop.JSRuntime));
        var component = Render<CascadingAuthenticationState>(p => p.AddChildContent<LeaveApprovalsPage>());
        component.WaitForAssertion(() => Assert.Contains(handler.Requests, r => r.Contains("/leave/pending", StringComparison.Ordinal)));

        if (global)
        {
            component.WaitForAssertion(() => Assert.Single(component.FindAll("#approval-department")));
            component.Find("#approval-department").Change("2");
            component.WaitForAssertion(() => Assert.Contains(handler.Requests, r => r.Contains("departmentId=2", StringComparison.Ordinal)));
            component.Find("#approval-sort").Change("employeeName");
            component.WaitForAssertion(() => Assert.Contains(handler.Requests, r => r.Contains("sortBy=employeeName", StringComparison.Ordinal)));
            component.Find("#approval-direction").Change("asc");
            component.WaitForAssertion(() => Assert.Contains(handler.Requests, r => r.Contains("sortBy=employeeName&descending=false&departmentId=2", StringComparison.Ordinal)));
        }
        else
        {
            Assert.Empty(component.FindAll("#approval-department"));
            Assert.Empty(component.FindAll("#approval-sort"));
        }
    }

    private sealed class SignedInProvider(bool global) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(
            new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("permission", global ? PermissionCodes.LeaveApproveAll : PermissionCodes.LeaveApprove)], "Test"))));
    }

    private sealed class ApprovalHandler(bool global) : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.PathAndQuery;
            Requests.Add(path);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = path.Contains("approval-options", StringComparison.Ordinal)
                    ? JsonContent.Create(ApiResponse.Ok(new LeaveApprovalOptionsDto(global, [new LeaveApprovalDepartmentDto(2, "HR")])))
                    : JsonContent.Create(ApiResponse.Ok<IReadOnlyList<PendingLeaveApprovalDto>>([]))
            };
            return Task.FromResult(response);
        }
    }
}
