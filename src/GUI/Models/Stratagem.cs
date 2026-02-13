using System;
using System.Collections.Generic;
using System.Text;

namespace GUI.Models;

public class Stratagem
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> Inputs { get; set; } = [];
    public string Icon { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public int DemoForce { get; set; }

    // Full path to SVG on disk, resolved at runtime
    public string IconPath =>
        System.IO.Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Assets", "icons", Icon);
}
