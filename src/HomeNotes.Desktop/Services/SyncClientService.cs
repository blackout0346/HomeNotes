using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using HomeNotes.Core.DTOs.Sync;
namespace HomeNotes.Desktop.Services
{
    public class SyncClientService
    {
        private readonly HttpClient _httpClient;
        public SyncClientService(HttpClient httpClient)
        { 
            _httpClient = httpClient;
        }
        public async Task<SyncResponse> SyncNotesAsync(SyncRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/sync", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<SyncResponse>();
        }
    }
}
