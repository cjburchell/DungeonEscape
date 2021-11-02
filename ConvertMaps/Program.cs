using System;
using System.Collections.Generic;
using GameFile;
using System.IO;
using System.Xml.Serialization;
using Newtonsoft.Json;
using CommandLine;
using ConvertMaps.Tiled;

namespace ConvertMaps
{
    public class Program
    {
        private class Options
        {
            [Option('o', "output", Required = true, HelpText = "Directory to save game data")]
            public string OutputDirectory { get; set; }
            [Option('i', "input", Required = true, HelpText = "Directory to be processed.")]
            public string InputDirectory { get; set; }

            [Option('m', "map", Required = false, HelpText = "Map to convert", Default = -1)]
            public int MapId { get; set; }
            
            [Option('c', "clear", Required = false, HelpText = "Clear directory", Default = false)]
            public bool Clear { get; set; }
            
            [Option('s', "spells", Required = false, HelpText = "Output spells", Default = false)]
            public bool Spells { get; set; }
            
            [Option('w', "items", Required = false, HelpText = "Output itmes", Default = false)]
            public bool Items { get; set; }
            
            [Option('r', "monsters", Required = false, HelpText = "Output monsters", Default = false)]
            public bool Monsters { get; set; }
        }
        

        public static void Main(string[] args)
        {
           var parser = new Parser(settings =>
           {
               settings.HelpWriter = Console.Error;
               settings.CaseInsensitiveEnumValues = true;
           });
           parser.ParseArguments<Options>(args)
                .WithParsed(RunOptions);
        }
        
        private static void CleanUp(string directory, bool items, bool spells,bool monsters)
        {
            if (!Directory.Exists(directory))
            {
                return;
            }

            var di = new DirectoryInfo(directory);
            foreach (var file in di.GetFiles())
            {
                if ((file.Name.EndsWith(".json") || file.Name.EndsWith(".tsx") || file.Name.EndsWith(".tmx")) && (file.Name.StartsWith("map") || file.Name.StartsWith("monster")))
                {
                    file.Delete();
                } 
            }

            if (spells)
            {
                File.Delete(Path.Combine(directory, "spells.tsx"));
            }

            if (items)
            {
                File.Delete(Path.Combine(directory, "items.tsx"));
            }
            
            if (monsters)
            {
                File.Delete(Path.Combine(directory, "allmonsters.tsx"));
            }
        }
        
        private static void RunOptions(Options opts)
        {
            if (opts.Clear)
            {
                CleanUp(opts.OutputDirectory, opts.Items, opts.Spells, opts.Monsters);
            }

            var tileIdGenerator = new IdGenerator();
            var spells = OldFormat.LoadSpells(opts.InputDirectory);
            var items = OldFormat.LoadItems(opts.InputDirectory);
            var tiles = OldFormat.LoadTiles(opts.InputDirectory, tileIdGenerator);
            
            var monsterIdGenerator = new IdGenerator();
            var monsters = new List<Monster>();
            
            var maps = OldFormat.LoadMaps(opts.InputDirectory, spells, tiles, monsters, tileIdGenerator, monsterIdGenerator);

            Directory.CreateDirectory(opts.OutputDirectory);
            if (opts.MapId == -1)
            {
                if (opts.Spells)
                {
                    WriteSpells(spells, opts.OutputDirectory);
                }

                if (opts.Items)
                {
                    WriteItems(items, opts.OutputDirectory);
                }
                
                if (opts.Monsters)
                {
                    WriteMonsters(monsters, opts.OutputDirectory);
                }

                WriteAllTileSet(tiles, opts.OutputDirectory);
            }

            foreach (var map in maps)
            {
                WriteMap(map, opts.OutputDirectory);
                WriteRandomMonsters(map, opts.OutputDirectory);
            }
        }

        private static void WriteMonsters(IEnumerable<Monster> monsters, string outputDirectory)
        {
            var monsterTileset = TiledConverter.ToMonsterTileSet(monsters, "allmonsters");
            var filename = Path.Combine(outputDirectory, "allmonsters.tsx");
            Console.WriteLine($"writing {filename}");
            var serializer = new XmlSerializer(typeof(TiledTileset));
            using var reader =
                new StreamWriter(filename, false);
            serializer.Serialize(reader, monsterTileset);
        }

        private static void WriteRandomMonsters(Map map, string outputDirectory)
        {
            if (map.RandomMonsters.Count == 0)
            {
                return;
            }

            Console.WriteLine(
                $"writing {Path.Combine(outputDirectory, $"monsters{map.Id}.json")}");
            File.WriteAllText(Path.Combine(outputDirectory, $"monsters{map.Id}.json"),
                JsonConvert.SerializeObject(map.RandomMonsters, Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    }));
        }

        private static void WriteMap(Map map, string outputDirectory)
        {
            var tilemap = TiledConverter.ToTileMap(map);
            Console.WriteLine($"writing {Path.Combine(outputDirectory, $"map{map.Id}.tmx")}");
            var serializer = new XmlSerializer(typeof(TiledMap));
            using var reader = new StreamWriter(Path.Combine(outputDirectory, $"map{map.Id}.tmx"), false);
            serializer.Serialize(reader, tilemap);
        }

        private static void WriteAllTileSet(IEnumerable<TileInfo> tiles, string outputDirectory)
        {
            var tileset = TiledConverter.ToTileSet(tiles, "All Tiles");
            Console.WriteLine($"writing {Path.Combine(outputDirectory, "tiles.tsx")}");
            var serializer = new XmlSerializer(typeof(TiledTileset));
            using var reader = new StreamWriter(Path.Combine(outputDirectory, "tiles.tsx"), false);
            serializer.Serialize(reader, tileset);

        }

        private static void WriteItems(IReadOnlyCollection<Item> items,
            string outputDirectory)
        {
            var itemTileSet = TiledConverter.ToItemTileset(items);
            Console.WriteLine($"writing {Path.Combine(outputDirectory, $"items.tsx")}");
            var serializer = new XmlSerializer(typeof(TiledTileset));
            using var reader =
                new StreamWriter(Path.Combine(outputDirectory, $"items.tsx"), false);
            serializer.Serialize(reader, itemTileSet);

        }

        private static void WriteSpells(IReadOnlyCollection<Spell> spells, string outputDirectory)
        {
            var itemTileSet = TiledConverter.ToSpellTileset(spells);
            Console.WriteLine($"writing {Path.Combine(outputDirectory, $"spells.tsx")}");
                var serializer = new XmlSerializer(typeof(TiledTileset));
                using var reader =
                    new StreamWriter(Path.Combine(outputDirectory, $"spells.tsx"), false);
                serializer.Serialize(reader, itemTileSet);
            }
    }
}