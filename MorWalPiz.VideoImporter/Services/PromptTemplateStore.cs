using System.Text.Json;
using System.IO;

namespace MorWalPiz.VideoImporter.Services;

public sealed record PromptTemplate(string Name, string Prompt);

public interface IPromptTemplateStore
{
    IReadOnlyList<PromptTemplate> GetAll();
    void Save(PromptTemplate template);
    void Delete(string name);
}

public sealed class PromptTemplateStore : IPromptTemplateStore
{
    private readonly string _path;
    private readonly object _gate = new();

    public PromptTemplateStore(string? path = null)
    {
        _path = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MorWalPiz.VideoImporter", "image-prompt-templates.json");
    }

    public IReadOnlyList<PromptTemplate> GetAll()
    {
        lock (_gate)
        {
            if (!File.Exists(_path)) return [];
            try { return JsonSerializer.Deserialize<List<PromptTemplate>>(File.ReadAllText(_path)) ?? []; }
            catch (JsonException) { return []; }
        }
    }

    public void Save(PromptTemplate template)
    {
        if (string.IsNullOrWhiteSpace(template.Name) || string.IsNullOrWhiteSpace(template.Prompt)) throw new ArgumentException("Nome e prompt sono obbligatori.");
        lock (_gate)
        {
            var templates = GetAll().Where(item => !string.Equals(item.Name, template.Name, StringComparison.OrdinalIgnoreCase)).Append(template).OrderBy(item => item.Name).ToList();
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(templates, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    public void Delete(string name)
    {
        lock (_gate)
        {
            var templates = GetAll().Where(item => !string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();
            if (templates.Count == 0 && !File.Exists(_path)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(templates, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}