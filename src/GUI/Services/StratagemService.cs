using GUI.Models;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace GUI.Services;

public class StratagemService
{
    private List<Stratagem> _all = [];

    public IEnumerable<Stratagem> Offensive => _all.Where(s => s.Category == "Offensive");
    public IEnumerable<Stratagem> Supply => _all.Where(s => s.Category == "Supply");
    public IEnumerable<Stratagem> Defensive => _all.Where(s => s.Category == "Defensive");

    public void Load()
    {
        var uri = new Uri("pack://application:,,,/Data/stratagems.json");
        var resource = Application.GetResourceStream(uri);
        using var reader = new StreamReader(resource.Stream);
        var json = reader.ReadToEnd();
        _all = JsonSerializer.Deserialize<List<Stratagem>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    }

    public IEnumerable<Stratagem> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return _all;

        query = query.ToLower();
        return _all.Where(s =>
            s.Name.ToLower().Contains(query) ||
            s.Tags.Any(t => t.ToLower().Contains(query)));
    }
}
