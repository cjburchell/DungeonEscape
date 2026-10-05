using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Redpoint.DungeonEscape.Data;
using Xunit;

namespace DungeonEscape.Core.Test.Data;

public class GameDataReferenceTests
{
    [Fact]
    public void DataFilesContainDndNamedSkillsAndIronKey()
    {
        var repoRoot = FindRepositoryRoot();
        var dataFolder = Path.Combine(repoRoot, "DungeonEscape.Unity", "Assets", "DungeonEscape", "Data");

        var skills = ReadJson<List<Skill>>(Path.Combine(dataFolder, "skills.json"));
        var spells = ReadJson<List<Spell>>(Path.Combine(dataFolder, "spells.json"));
        var items = ReadJson<List<Item>>(Path.Combine(dataFolder, "customitems.json"));

        Assert.True(skills.Exists(skill => skill.Name == "Protection from Evil and Good"),
            "Protection from Evil and Good should exist in the skill catalog.");

        Assert.True(spells.Exists(spell =>
            spell.Name == "Protection from Evil and Good" && spell.SkillId == "Protection from Evil and Good"),
            "Protection from Evil and Good should reference the matching D&D-named skill.");

        Assert.True(items.Exists(item => item.Name == "Iron Key" && item.SkillId == "Knock"),
            "Iron Key should exist as a key item that satisfies the chest lock through Knock.");
    }

    private static T ReadJson<T>(string path)
    {
        using var reader = File.OpenText(path);
        var serializer = new JsonSerializer();
        return (T)serializer.Deserialize(reader, typeof(T))!;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "DungeonEscape.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find DungeonEscape.sln above the test output directory.");
    }
}
