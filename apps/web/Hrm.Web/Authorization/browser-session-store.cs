using System.Text.Json;
using Hrm.Contracts;
using Microsoft.JSInterop;

namespace Hrm.Web.Authorization;

public sealed class BrowserSessionStore(IJSRuntime jsRuntime)
{
    private const string StorageKey = "hrm.auth.session";
    private const string DeviceKey = "hrm.auth.device_id";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<string> GetOrCreateDeviceIdAsync()
    {
        try
        {
            var deviceId = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", DeviceKey);
            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                return deviceId;
            }

            var newId = Guid.NewGuid().ToString("N");
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", DeviceKey, newId);
            return newId;
        }
        catch
        {
            return Guid.NewGuid().ToString("N");
        }
    }

    public async ValueTask<AuthTokenResponse?> GetAsync()
    {
        var json = await jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", StorageKey);
        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<AuthTokenResponse>(json, SerializerOptions);
    }

    public ValueTask SaveAsync(AuthTokenResponse session) =>
        jsRuntime.InvokeVoidAsync("sessionStorage.setItem", StorageKey, JsonSerializer.Serialize(session, SerializerOptions));

    public ValueTask ClearAsync() =>
        jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", StorageKey);
}
