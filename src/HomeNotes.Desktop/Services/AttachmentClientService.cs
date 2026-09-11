using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace HomeNotes.Desktop.Services
{
    public class AttachmentClientService
    {
        public readonly HttpClient _httpClient;
        public AttachmentClientService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
       
    }
}
