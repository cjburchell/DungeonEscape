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
        public enum OutputType
        {
            all,
            none,
            json,
            tmx,
        }
        
        private class Options
        {
            [Option('o', "output", Required = true, HelpText = "Directory to save game data")]
            public string OutputDirectory { get; set; }
            [Option('i', "input", Required = true, HelpText = "Directory to be processed.")]
            public string InputDirectory { get; set; }
            
            [Option('t', "type", Required = false, HelpText = "Type of output.", Default = OutputType.all)]
            public OutputType OutputType { get; set; }
            
            [Option('m', "map", Required = false, HelpText = "Map to convert", Default = -1)]
            public int MapId { get; set; }
            
            [Option('c', "clear", Required = false, HelpText = "Clear directory", Default = true)]
            public bool Clear { get; set; }
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
        
        private static void CleanUp(string directory)
        {
            if (!Directory.Exists(directory))
            {
                return;
            }

            var di = new DirectoryInfo(directory);
            foreach (var file in di.GetFiles())
            {
                if (file.Name.EndsWith(".json") || file.Name.EndsWith(".tsx") || file.Name.EndsWith(".tmx"))
                {
                    file.Delete();
                } 
            }
                
            File.Delete(Path.Combine(directory, "spells.json"));
            File.Delete(Path.Combine(directory, "items.json"));
        }
        
        private static void RunOptions(Options opts)
        {
            if (opts.Clear)
            {
                CleanUp(opts.OutputDirectory);
            }
            
            if (opts.OutputType == OutputType.none)
            {
                return;
            }
            
            var tiles = OldFormat.LoadTiles(opts.InputDirectory);
            var spells = OldFormat.LoadSpells(opts.InputDirectory, tiles);
            var items = OldFormat.LoadItems(opts.InputDirectory, tiles);
            var maps = OldFormat.LoadMaps(opts.InputDirectory, spells, tiles);

            Directory.CreateDirectory(opts.OutputDirectory);
            if (opts.MapId == -1)
            {
                WriteSpells(spells, tiles, opts.OutputDirectory, opts.OutputType);
                WriteItems(items, tiles, opts.OutputDirectory, opts.OutputType);
                WriteAllTileSet(tiles, opts.OutputDirectory, opts.OutputType);
            }

            foreach (var map in maps)
            {
                WriteMap(map, opts.OutputDirectory, opts.OutputType);
                WriteRandomMonsters(map, opts.OutputDirectory, opts.OutputType);
            }
        }

        private static void WriteRandomMonsters(Map map, string outputDirectory, OutputType outputType)
        {
            if (map.RandomMonsters.Count == 0)
            {
                return;
            }

            var monstTileset = TiledConverter.ToMonsterTileSet(map.RandomMonstersTileInfo, map.RandomMonsters, $"Monsters {map.Id}");
            if (outputType == OutputType.json || outputType == OutputType.all)
            {
                Console.WriteLine(
                    $"writing {Path.Combine(outputDirectory, $"monsters{map.Id}.tsx.json")}");
                File.WriteAllText(Path.Combine(outputDirectory, $"monsters{map.Id}.tsx.json"),
                    JsonConvert.SerializeObject(monstTileset, Formatting.Indented,
                        new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        }));
            }

            if (outputType == OutputType.tmx || outputType == OutputType.all)
            {
                Console.WriteLine($"writing {Path.Combine(outputDirectory, $"monsters{map.Id}.tsx")}");
                var serializer = new XmlSerializer(typeof(TiledTileset));
                using var reader =
                    new StreamWriter(Path.Combine(outputDirectory, $"monsters{map.Id}.tsx"), false);
                serializer.Serialize(reader, monstTileset);
            }
        }

        private static void WriteMap(Map map, string outputDirectory, OutputType outputType)
        {
            var tilemap = TiledConverter.ToTileMap(map);
            if (outputType == OutputType.json || outputType == OutputType.all)
            {
                Console.WriteLine(
                    $"writing {Path.Combine(outputDirectory, $"map{map.Id}.tmx.json")}");
                File.WriteAllText(Path.Combine(outputDirectory, $"map{map.Id}.tmx.json"),
                    JsonConvert.SerializeObject(tilemap, Formatting.Indented, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    }));
            }

            if (outputType == OutputType.tmx || outputType == OutputType.all)
            {
                Console.WriteLine($"writing {Path.Combine(outputDirectory, $"map{map.Id}.tmx")}");
                var serializer = new XmlSerializer(typeof(TiledMap));
                using var reader = new StreamWriter(Path.Combine(outputDirectory, $"map{map.Id}.tmx"), false);
                serializer.Serialize(reader, tilemap);
            }
        }

        private static void WriteAllTileSet(IEnumerable<TileInfo> tiles, string outputDirectory, OutputType outputType)
        {
            var tileset = TiledConverter.ToTileSet(tiles, "All Tiles");
            if (outputType == OutputType.json || outputType == OutputType.all)
            {
                Console.WriteLine($"writing {Path.Combine(outputDirectory, "tiles.tsx.json")}");
                File.WriteAllText(Path.Combine("tiles.tsx.json"),
                    JsonConvert.SerializeObject(tileset, Formatting.Indented, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    }));
            }
                
            if (outputType == OutputType.tmx || outputType == OutputType.all)
            {
                Console.WriteLine($"writing {Path.Combine(outputDirectory, "tiles.tsx")}");
                var serializer = new XmlSerializer(typeof(TiledTileset));
                using var reader = new StreamWriter(Path.Combine(outputDirectory, "tiles.tsx"), false);
                serializer.Serialize(reader, tileset);
            }
        }

        private static void WriteItems(IReadOnlyCollection<Item> items, IEnumerable<TileInfo> tiles,
            string outputDirectory, OutputType outputType)
        {
            var itemTileSet = TiledConverter.ToItemTileset(items, tiles);
            if (outputType == OutputType.json || outputType == OutputType.all)
            {
                Console.WriteLine(
                    $"writing {Path.Combine(outputDirectory, $"items.tsx.json")}");
                File.WriteAllText(Path.Combine(outputDirectory, $"items.tsx.json"),
                    JsonConvert.SerializeObject(itemTileSet, Formatting.Indented,
                        new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        }));
            }

            if (outputType == OutputType.tmx || outputType == OutputType.all)
            {
                Console.WriteLine($"writing {Path.Combine(outputDirectory, $"items.tsx")}");
                var serializer = new XmlSerializer(typeof(TiledTileset));
                using var reader =
                    new StreamWriter(Path.Combine(outputDirectory, $"items.tsx"), false);
                serializer.Serialize(reader, itemTileSet);
            }
        }

        private static void WriteSpells(IReadOnlyCollection<Spell> spells, IEnumerable<TileInfo> tiles, string outputDirectory, OutputType outputType)
        {
            var itemTileSet = TiledConverter.ToSpellTileset(spells, tiles);
            if (outputType == OutputType.json || outputType == OutputType.all)
            {
                Console.WriteLine(
                    $"writing {Path.Combine(outputDirectory, $"spells.tsx.json")}");
                File.WriteAllText(Path.Combine(outputDirectory, $"spells.tsx.json"),
                    JsonConvert.SerializeObject(itemTileSet, Formatting.Indented,
                        new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        }));
            }

            if (outputType == OutputType.tmx || outputType == OutputType.all)
            {
                Console.WriteLine($"writing {Path.Combine(outputDirectory, $"spells.tsx")}");
                var serializer = new XmlSerializer(typeof(TiledTileset));
                using var reader =
                    new StreamWriter(Path.Combine(outputDirectory, $"spells.tsx"), false);
                serializer.Serialize(reader, itemTileSet);
            }
        }
    }
}