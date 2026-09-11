using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace HomeNotes.Desktop.Services
{
    public class SyncClientService
    {
        private readonly HttpClient _httpClient;
        public SyncClientService(HttpClient httpClient)
        { 
            _httpClient = httpClient;
        }
    }
}
