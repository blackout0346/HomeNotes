using HomeNotes.Core.DTOs.Notes;
using HomeNotes.Core.DTOs.Restore; // Добавлен для RestoreNoteRequest
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace HomeNotes.Desktop.Services
{
    public class NoteClientService
    {
        private readonly HttpClient _httpClient;

        public NoteClientService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }


        public async Task<IEnumerable<NotesResponse>> GetNotesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/notes");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<IEnumerable<NotesResponse>>();
            }
            catch (Exception ex)
            {

                throw new ApplicationException("An error occurred while fetching notes.", ex);
            }

        }

        public async Task<NotesResponse> GetNoteByIdAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/notes/{id}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<NotesResponse>();

            }
            catch (HttpRequestException ex)
            {
                throw new ApplicationException($"An error occurred while fetching the note with ID {id}.", ex);

            }
        }
        public async Task<NotesResponse> CreateNoteAsync(NotesRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/notes", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<NotesResponse>();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("An error occurred while creating the note.", ex);
            }

        }

        public async Task<NotesResponse> UpdateNoteAsync(Guid id, NotesRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"/api/notes/{id}", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<NotesResponse>();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"An error occurred while updating the note with ID {id}.", ex);
            }
        }

        public async Task DeleteNoteAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/api/notes/{id}");
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"An error occurred while deleting the note with ID {id}.", ex);
            }
        }


        public async Task<Stream> GetNoteContentAsync(Guid id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/notes/{id}/content");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStreamAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"An error occurred while fetching the content of the note with ID {id}.", ex);
            }

        }


        public async Task UpdateNoteContentAsync(Guid id, Stream contentStream)
        {
            try
            {
                var content = new StreamContent(contentStream);
                var response = await _httpClient.PutAsync($"/api/notes/{id}/content", content);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"An error occurred while updating the content of the note with ID {id}.", ex);
            }
        }



        public async Task<IEnumerable<NotesResponse>> RestoreNotesAsync(RestoreNoteRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync("/api/notes/restore", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<IEnumerable<NotesResponse>>();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("An error occurred while restoring notes.", ex);
            }

        }
    }
}