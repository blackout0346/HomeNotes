using HomeNotes.Core.DTOs.Attachments;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace HomeNotes.Desktop.Services
{
    public class AttachmentClientService
    {
        private readonly HttpClient _httpClient;

        public AttachmentClientService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<AttachmentsResponse?> CreateAttachment(Guid notesId, AttachmentsRequest request)
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(notesId.ToString()), nameof(AttachmentsRequest.NotesId));
            form.Add(new StringContent(request.FileName), nameof(AttachmentsRequest.FileName));
            form.Add(new StringContent(request.MimeType), nameof(AttachmentsRequest.MimeType));
            form.Add(new StringContent(request.Size.ToString()), nameof(AttachmentsRequest.Size));
            form.Add(new StreamContent(request.FileStream), nameof(AttachmentsRequest.FileStream), request.FileName);

            var response = await _httpClient.PostAsync("/api/attachments", form);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AttachmentsResponse>();
        }

        public async Task<AttachmentsResponse?> GetAttachment(Guid id)
        {
            var response = await _httpClient.GetAsync($"/api/attachments/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AttachmentsResponse>();
        }

        public async Task<AttachmentsResponse?> UpdateAttachment(Guid id, AttachmentsRequest request)
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(request.FileName), nameof(AttachmentsRequest.FileName));
            form.Add(new StringContent(request.MimeType), nameof(AttachmentsRequest.MimeType));
            form.Add(new StringContent(request.Size.ToString()), nameof(AttachmentsRequest.Size));
            if (request.FileStream != null)
                form.Add(new StreamContent(request.FileStream), nameof(AttachmentsRequest.FileStream), request.FileName);

            var response = await _httpClient.PutAsync($"/api/attachments/{id}", form);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AttachmentsResponse>();
        }

        public async Task DeleteAttachment(Guid id)
        {
            var response = await _httpClient.DeleteAsync($"/api/attachments/{id}");
            response.EnsureSuccessStatusCode();
        }

        public async Task<Stream> GetAttachmentContent(Guid id)
        {
            var response = await _httpClient.GetAsync($"/api/attachments/{id}/content");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync();
        }

        public async Task UpdateAttachmentContent(Guid id, Stream contentStream)
        {
            var content = new StreamContent(contentStream);
            var response = await _httpClient.PutAsync($"/api/attachments/{id}/content", content);
            response.EnsureSuccessStatusCode();
        }
    }
}