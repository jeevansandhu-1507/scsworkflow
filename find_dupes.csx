using System;
using System.Linq;
using System.Collections.Generic;

var lines = System.IO.File.ReadAllLines("Models/GlMappingService.cs");

var mappings = new List<(int GlCode, string SupportType)>();

foreach(var line in lines)
{
    if (line.Contains("new() {") && line.Contains("GlCode ="))
    {
        var glMatch = System.Text.RegularExpressions.Regex.Match(line, @"GlCode\s*=\s*(\d+)");
        var supportMatch = System.Text.RegularExpressions.Regex.Match(line, @"SupportType\s*=\s*""([^""]+)""");
        if (glMatch.Success && supportMatch.Success)
        {
            int gl = int.Parse(glMatch.Groups[1].Value);
            string support = supportMatch.Groups[1].Value;
            mappings.Add((gl, support));
        }
    }
}

var grouped = mappings.GroupBy(m => m.GlCode)
                      .Select(g => new { GlCode = g.Key, SupportTypes = g.Select(x => x.SupportType).Distinct().ToList() })
                      .Where(g => g.SupportTypes.Count > 1)
                      .ToList();

foreach(var group in grouped)
{
    Console.WriteLine($"GL Code: {group.GlCode}");
    foreach(var st in group.SupportTypes)
    {
        Console.WriteLine($"  - {st}");
    }
}
