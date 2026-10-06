using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media.Imaging;

namespace HomeNotes.Desktop.Markdown.Storage;

public static class MarkdownImageLoader
{
    private static readonly Dictionary<string, Bitmap?> Cache = new();
 
    public static Bitmap? Load(string url, string? vaultRootPath)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
 
        var key = $"{vaultRootPath}|{url}";
        if (Cache.TryGetValue(key, out var cached)) return cached;
 
        try
        {
            var path = Path.IsPathRooted(url) || string.IsNullOrEmpty(vaultRootPath)
                ? url
                : Path.Combine(vaultRootPath, url);
 
            var bitmap = File.Exists(path) ? new Bitmap(path) : null;
            Cache[key] = bitmap;
            return bitmap;
        }
        catch
        {
            Cache[key] = null;
            return null;
        }
    }
 
    // Вызывайте при удалении/переименовании вложений — иначе старые битмапы
    // могут держаться в памяти дольше, чем нужно.
    public static void ClearCache() => Cache.Clear();
}