namespace HomeNotes.Desktop.Interface;

public interface IStorage
{
    public string? StoragePath { get; set; }
    public void Save();
    public void Load();
}