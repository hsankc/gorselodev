using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DisKlinigiYonetimSistemi.Models;

namespace DisKlinigiYonetimSistemi.Data;

public sealed class SupabaseClinicClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly SupabaseSettings _settings;

    public SupabaseClinicClient(SupabaseSettings settings)
    {
        _settings = settings;
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        using var response = await client.GetAsync("clinic_data?select=id&limit=1", cancellationToken);
        return response.IsSuccessStatusCode;
    }

    public async Task<DataSnapshot?> PullAsync(CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        using var response = await client.GetAsync("clinic_data?id=eq.default&select=payload", cancellationToken);
        response.EnsureSuccessStatusCode();

        var rows = await response.Content.ReadFromJsonAsync<List<SupabasePayloadRow>>(JsonOptions, cancellationToken);
        var row = rows?.FirstOrDefault();
        return row?.Payload.Deserialize<DataSnapshot>(JsonOptions);
    }

    public async Task PushAsync(DataSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        using var client = CreateClient();
        var payload = new
        {
            id = "default",
            payload = snapshot,
            updated_at = DateTime.UtcNow
        };

        using var response = await client.PostAsJsonAsync("clinic_data?on_conflict=id", payload, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private HttpClient CreateClient()
    {
        if (!_settings.Enabled)
        {
            throw new InvalidOperationException("Supabase URL ve API key bos olamaz.");
        }

        var baseUrl = _settings.Url.TrimEnd('/') + "/rest/v1/";
        var client = new HttpClient { BaseAddress = new Uri(baseUrl) };
        client.DefaultRequestHeaders.Add("apikey", _settings.ApiKey);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        client.DefaultRequestHeaders.Add("Prefer", "return=representation,resolution=merge-duplicates");
        return client;
    }

    private sealed class SupabasePayloadRow
    {
        public JsonElement Payload { get; set; }
    }
}
