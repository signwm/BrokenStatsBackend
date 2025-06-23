using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace BrokenStatsFrontendWinForms.Services;

public class HttpBackendService(HttpClient client) : IBackendService
{
    private readonly HttpClient _client = client;

    public async Task<List<InstanceDto>> GetInstancesAsync(DateTime from, DateTime to)
    {
        var url = $"/api/instances/range?from={from:o}&to={to:o}";
        return await _client.GetFromJsonAsync<List<InstanceDto>>(url) ?? new();
    }

    public async Task<List<InstanceFightDto>> GetFightsAsync(int instanceId)
    {
        var url = $"/api/instances/{instanceId}/fights";
        return await _client.GetFromJsonAsync<List<InstanceFightDto>>(url) ?? new();
    }
}
