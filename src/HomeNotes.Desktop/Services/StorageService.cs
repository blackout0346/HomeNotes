using System;
using System.IO;
using System.Text.Json;
using HomeNotes.Desktop.Interface;

namespace HomeNotes.Desktop.Services;

public class StorageService : IStorage
{
    private readonly string _filePath;
  

    public string? StoragePath { get; set; }

    public StorageService()
    {
        var appdata =Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var folder = Path.Combine(appdata, "HomeNotes");
        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "HomeNotes.json");
        Load();
    }
    public void Save()
    {
        var json =JsonSerializer.Serialize(new {StoragePath});
        File.WriteAllText(_filePath, json);
    }

    public void Load()
    {
        if (!File.Exists(_filePath))
        {
            StoragePath = null;
            return;
        }
            try
            {
                var json = File.ReadAllText(_filePath);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("StoragePath", out var storagePath))
                    {
                        StoragePath = storagePath.GetString();
                    }
                }
           
            }catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
           
        
    }
}