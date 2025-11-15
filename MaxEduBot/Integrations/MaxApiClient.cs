using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EduMaxBot.Integrations;

public class MaxApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<MaxApiClient> _log;
    private readonly MaxApiOptions _opt;

    public MaxApiClient(HttpClient http, IOptions<MaxApiOptions> opt, ILogger<MaxApiClient> log)
    {
        _http = http;
        _opt = opt.Value;
        _log = log;
    }

    public async Task<bool> SendTextAsync(long userId, string text)
    {
        var payload = new { text };
        var url = $"messages?user_id={userId}";
        try
        {
            var resp = await _http.PostAsJsonAsync(url, payload);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                _log.LogError("MaxApi send failed {Status}: {Body}", resp.StatusCode, body);
                return false;
            }
            _log.LogInformation("MaxApi OK: {Body}", body);
            return true;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "MaxApi send exception");
            return false;
        }
    }
}
