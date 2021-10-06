using System;
using System.Collections.Generic;

namespace ConvertMaps
{
    using System.IO;
    using System.Linq;
    using System.Xml.Serialization;
    using Newtonsoft.Json;
    using CommandLine;
    using GameFile;
    using Models;
    using TiledCS;
    using Point = GameFile.Point;
    using TileInfo = GameFile.TileInfo;

    
    
    public class Program
    {
        public enum OutputType
        {
            all,
            none,
            custom,
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
        }
        

        public static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunOptions);
        }

        private static void RunOptions(Options opts)
        {
            const string MapsDirectory = "maps";
            var spells = LoadSpells(Path.Combine(opts.InputDirectory, "spells.dat"));
            var items = LoadItems(Path.Combine(opts.InputDirectory, "items.dat"));
            var tiles = LoadTiles(opts.InputDirectory);
            var maps = new List<Map>();
            var inputMapPath = Path.Combine(opts.InputDirectory, MapsDirectory);
            
            if (opts.MapId == -1)
            {
                for (var i = 0; i < 100; i++)
                {
                    if (i == 1 || i == 2 || i == 3 || i == 4 || i == 5)
                    {
                        continue;
                    }
                    
                    var map = LoadMap(i, inputMapPath, tiles);
                    if (map == null)
                    {
                        continue;
                    }

                    if (i == 0)
                    {
                        LoadSprites(1, map, inputMapPath, spells, tiles, Biome.Grassland);
                        LoadSprites(2, map, inputMapPath, spells, tiles, Biome.Water);
                        LoadSprites(3, map, inputMapPath, spells, tiles, Biome.Desert);
                        LoadSprites(4, map, inputMapPath, spells, tiles, Biome.Hills);
                        LoadSprites(5, map, inputMapPath, spells, tiles, Biome.Forest);
                        LoadSprites(4, map, inputMapPath, spells, tiles, Biome.Swamp);
                    }
                    else
                    {
                        LoadSprites(i, map, inputMapPath, spells, tiles);
                    }
                    
                    maps.Add(map);
                }
            }
            else
            {
                var map = LoadMap(opts.MapId, inputMapPath, tiles);
                if (map != null)
                {
                    if (opts.MapId == 0)
                    {
                        LoadSprites(1, map, inputMapPath, spells, tiles, Biome.Grassland);
                        LoadSprites(2, map, inputMapPath, spells, tiles, Biome.Water);
                        LoadSprites(3, map, inputMapPath, spells, tiles, Biome.Desert);
                        LoadSprites(4, map, inputMapPath, spells, tiles, Biome.Hills);
                        LoadSprites(5, map, inputMapPath, spells, tiles, Biome.Forest);
                        LoadSprites(4, map, inputMapPath, spells, tiles, Biome.Swamp);
                    }
                    else
                    {
                        LoadSprites(opts.MapId, map, inputMapPath, spells, tiles);
                    }
                    
                    maps.Add(map);    
                }
            }
            
            Directory.CreateDirectory(opts.OutputDirectory);
            Directory.CreateDirectory(Path.Combine(opts.OutputDirectory, MapsDirectory));
            Console.WriteLine($"writing {Path.Combine(opts.OutputDirectory, "spells.json")}");
            File.WriteAllText( Path.Combine(opts.OutputDirectory, "spells.json"),JsonConvert.SerializeObject(spells, Formatting.Indented, new JsonSerializerSettings { 
                NullValueHandling = NullValueHandling.Ignore
            }));
            
            Console.WriteLine($"writing {Path.Combine(opts.OutputDirectory, "items.json")}");
            File.WriteAllText( Path.Combine(opts.OutputDirectory, "items.json"),JsonConvert.SerializeObject(items, Formatting.Indented, new JsonSerializerSettings { 
                NullValueHandling = NullValueHandling.Ignore
            }));


            {
                var tileset = ToTileSet(tiles);
                Console.WriteLine($"writing {Path.Combine(opts.OutputDirectory, MapsDirectory, "tiles.tsx.json")}");
                File.WriteAllText(Path.Combine(opts.OutputDirectory, MapsDirectory, "tiles.tsx.json"),
                    JsonConvert.SerializeObject(tileset, Formatting.Indented, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    }));


                Console.WriteLine($"writing {Path.Combine(opts.OutputDirectory, MapsDirectory, "tiles.tsx")}");
                var serializer = new XmlSerializer(typeof(TiledTileset));
                using var reader = new StreamWriter(Path.Combine(opts.OutputDirectory, "maps", "tiles.tsx"), false);
                serializer.Serialize(reader, tileset);
            }

            foreach (var map in maps)
            {
                Console.WriteLine($"writing { Path.Combine(opts.OutputDirectory, MapsDirectory, $"map{map.Id}.json")}");
                File.WriteAllText( Path.Combine(opts.OutputDirectory, MapsDirectory, $"map{map.Id}.json"),JsonConvert.SerializeObject(map, Formatting.Indented, new JsonSerializerSettings { 
                    NullValueHandling = NullValueHandling.Ignore
                }));

                if (map.RandomMonsters.Count != 0)
                {
                    var monstTileset = ToMonsterTileSet(map.RandomMonstersTileInfo, map.RandomMonsters);
                    Console.WriteLine(
                        $"writing {Path.Combine(opts.OutputDirectory, MapsDirectory, $"monsters{map.Id}.tsx.json")}");
                    File.WriteAllText(Path.Combine(opts.OutputDirectory, MapsDirectory, $"monsters{map.Id}.tsx.json"),
                        JsonConvert.SerializeObject(monstTileset, Formatting.Indented,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            }));
                    
                    Console.WriteLine($"writing { Path.Combine(opts.OutputDirectory, MapsDirectory, $"monsters{map.Id}.tsx")}");
                    var serializer = new XmlSerializer(typeof(TiledTileset));
                    using var reader = new StreamWriter(Path.Combine(opts.OutputDirectory, MapsDirectory, $"monsters{map.Id}.tsx"), false);
                    serializer.Serialize( reader, monstTileset);
                }

                {

                    var tilemap = ToTileMap(map);
                    Console.WriteLine(
                        $"writing {Path.Combine(opts.OutputDirectory, MapsDirectory, $"map{map.Id}.tmx.json")}");
                    File.WriteAllText(Path.Combine(opts.OutputDirectory, MapsDirectory, $"map{map.Id}.tmx.json"),
                        JsonConvert.SerializeObject(tilemap, Formatting.Indented, new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        }));

                    Console.WriteLine($"writing {Path.Combine(opts.OutputDirectory, MapsDirectory, $"map{map.Id}.tmx")}");
                    var serializer = new XmlSerializer(typeof(TiledMap));
                    using var reader = new StreamWriter(Path.Combine(opts.OutputDirectory, MapsDirectory, $"map{map.Id}.tmx"), false);
                    serializer.Serialize(reader, tilemap);
                }
            }
        }
    
        private static TiledMap ToTileMap(Map map)
        {
            const int offset = 1;
            
            var floor = new List<int>();
            var water = new List<int>();
            var wall = new List<int>();
            for (var y = 0; y < map.Width; y++)
            {
                for (var x = 0; x < map.Height; x++)
                {
                    var tile = map.Tiles.FirstOrDefault(item => item.Position.X == x && item.Position.Y == y);
                    floor.Add(tile != null && tile.Type == TileType.Ground ? tile.Id + offset : 0);
                    water.Add(tile != null && tile.Type == TileType.Water ? tile.Id + offset : 0);
                    wall.Add(tile != null && tile.Type == TileType.Wall ? tile.Id + offset : 0);
                }
            }

            var objects = new List<TiledObject>();
            var sprites = new List<TiledObject>();
            foreach (var objSprite in map.Sprites)
            {
                var size = map.TileInfo.Where(item => item.Id == objSprite.Id).Select(item => (float)item.size).FirstOrDefault();

                var properties = new List<TiledProperty>
                {
                    new TiledProperty {name = "CanMove", type = "bool", value = objSprite.CanMove.ToString()},
                    new TiledProperty {name = "Collideable", type = "bool", value = objSprite.Collideable.ToString()}
                };
                if (!string.IsNullOrWhiteSpace(objSprite.Text))
                {
                    properties.Add(new TiledProperty {name = "Text", type = "string", value = objSprite.Text});
                }
                
                if (objSprite.Warp != null)
                {
                    properties.Add(new TiledProperty {name = "WarpMap", type = "int", value = objSprite.Warp.MapId.ToString()});
                    if (objSprite.Warp.Location != null)
                    {
                        properties.Add(new TiledProperty
                            {name = "WarpMapX", type = "int", value = objSprite.Warp.Location.X.ToString()});
                        properties.Add(new TiledProperty
                            {name = "WarpMapY", type = "int", value = objSprite.Warp.Location.Y.ToString()});
                    }
                }

                var obj = new TiledObject
                {
                    gid = objSprite.Id + offset,
                    name = objSprite.Name,
                    x = objSprite.StartPosition.X * 32,
                    y = (objSprite.StartPosition.Y+(size/32)) * 32,
                    width = size,
                    height = size,
                    type = objSprite.Type.ToString(),
                    visible = true,
                    properties = properties.ToArray()
                };
                
                if (objSprite.Type == SpriteType.NPC || objSprite.Type == SpriteType.Monster)
                {
                    sprites.Add(obj);
                }
                else
                {
                    objects.Add(obj);
                }
            }

            var tiledMap = new TiledMap
            {
                height = map.Height,
                width = map.Width,
                orientation = "orthogonal",
                renderorder = "right-down",
                tiledversion = "1.7.2",
                tileheight = 32,
                tilewidth = 32,
                type = "map",
                version = "1.6",
                //tilesets = new[] {new TiledTileset {firstgid = 0, source = "tiles.tsx.json"}},
                tilesets = new[] {ToTileSet(map.TileInfo)},
                layers = new TiledLayer[]
                {
                    new TiledLayerGroup
                    {
                        height = map.Height,
                        width = map.Width,
                        id = 1,
                        name = "floor",
                        visible = true,
                        opacity = 1,
                        type = "tilelayer",
                        x = 0,
                        y = 0,
                        data = floor.ToArray(),
                        layerData = new TiledLayerData
                        {
                            data = string.Join(",", floor)
                        },
                        properties = new[]
                        {
                            new TiledProperty {name = "LayerType", type = "string", value = TileType.Ground.ToString()},
                        }
                    },
                    new TiledLayerGroup
                    {
                        height = map.Height,
                        width = map.Width,
                        id = 2,
                        name = "water",
                        visible = true,
                        opacity = 1,
                        type = "tilelayer",
                        x = 0,
                        y = 0,
                        data = water.ToArray(),
                        layerData = new TiledLayerData
                        {
                            data = string.Join(",", water)
                        },
                        properties = new[]
                        {
                            new TiledProperty {name = "LayerType", type = "string", value = TileType.Water.ToString()},
                        }
                    },
                    new TiledLayerGroup
                    {
                        height = map.Height,
                        width = map.Width,
                        id = 3,
                        name = "wall",
                        visible = true,
                        opacity = 1,
                        type = "tilelayer",
                        x = 0,
                        y = 0,
                        data = wall.ToArray(),
                        layerData = new TiledLayerData
                        {
                            data = string.Join(",", wall)
                        },
                        properties = new[]
                        {
                            new TiledProperty {name = "LayerType", type = "string", value = TileType.Wall.ToString()},
                        }
                    },
                    new ObjectGroup
                    {
                        id = 4,
                        name = "objects",
                        type = "objectgroup",
                        visible = true,
                        opacity = 1,
                        x = 0,
                        y = 0,
                        objects = objects.ToArray(),
                        draworder = "topdown",
                    },
                    new ObjectGroup
                    {
                        id = 5,
                        name = "sprites",
                        type = "objectgroup",
                        visible = true,
                        opacity = 1,
                        x = 0,
                        y = 0,
                        objects = sprites.ToArray(),
                        draworder = "topdown"
                    }
                },
                properties = new[]
                {
                    new TiledProperty {name = "DefaultTileId", type = "int", value = map.DefaultTileId.ToString()},
                    new TiledProperty {name = "DefaultStartX", type = "int", value = map.DefaultStart.X.ToString()},
                    new TiledProperty {name = "DefaultStartY", type = "int", value = map.DefaultStart.Y.ToString()}
                }
            };


            return tiledMap;
        }

        private static TiledTileset ToTileSet(IEnumerable<TileInfo> mapTileInfo)
        {
            var tileInfos = mapTileInfo as TileInfo[] ?? mapTileInfo.ToArray();
            var tiledSet = new TiledTileset
            {
                firstgid = 1,
                tilewidth = tileInfos.Max(item=> item.size),
                tileheight = tileInfos.Max(item=> item.size),
                tilecount = tileInfos.Length,
                name = "tiles",
                //tiledversion = "1.7.2",
                //version = "1.6",
                //type = "tileset",
                tiles = tileInfos.Select(mapTile => new TiledTile {id = mapTile.Id, imageObj = new TiledImage {source = mapTile.ImageFile, height = mapTile.size, width = mapTile.size},image = mapTile.ImageFile, imageheight = mapTile.size, imagewidth = mapTile.size}).ToArray()
            };

            return tiledSet;
        }
        
        private static TiledTileset ToMonsterTileSet(IEnumerable<TileInfo> mapTileInfo, IEnumerable<Monster> monsters)
        {
            var tileInfos = mapTileInfo as TileInfo[] ?? mapTileInfo.ToArray();
            var tiles = new List<TiledTile>();
            var monsterList = monsters.ToList();
            
            foreach (var monster in monsterList)
            {
                var mapTile = tileInfos.FirstOrDefault(item => item.Id == monster.Id);
                if (mapTile != null)
                {
                    var totalMonsters = monsterList.Where(item => item.Biome == monster.Biome).Sum(item => item.Chance);
                    
                    var properties = new List<TiledProperty>
                    {
                        new TiledProperty {name = "Biome", type = "string", value = monster.Biome.ToString()},
                        new TiledProperty {name = "Heath", type = "int", value = monster.Heath.ToString()},
                        new TiledProperty {name = "HeathConst", type = "int", value = monster.HeathConst.ToString()},
                        new TiledProperty {name = "Attack", type = "int", value = monster.Attack.ToString()},
                        new TiledProperty {name = "XP", type = "int", value = monster.XP.ToString()},
                        new TiledProperty {name = "Gold", type = "int", value = monster.Gold.ToString()},
                        new TiledProperty {name = "Heath", type = "int", value = monster.Heath.ToString()}
                    };

                    if (monster.Spells != null)
                    {
                        var spell = 0;
                        properties.AddRange(monster.Spells.Select(monsterSpell => new TiledProperty {name = $"Spell{spell++}", type = "int", value = monsterSpell.Id}));
                    }
                    
                    var tile = new TiledTile
                    {
                        type = monster.Name,
                        id = mapTile.Id, 
                        image = mapTile.ImageFile, 
                        imageheight = mapTile.size,
                        imagewidth = mapTile.size,
                        probability =  monster.Chance/(double)totalMonsters,
                        properties = properties.ToArray(),
                        imageObj = new TiledImage {source = mapTile.ImageFile, height = mapTile.size, width = mapTile.size}
                    };

                    tiles.Add(tile);
                }
            }


            var tiledSet = new TiledTileset
            {
                firstgid = 1,
                tilewidth = tileInfos.Max(item=> item.size),
                tileheight = tileInfos.Max(item=> item.size),
                tilecount = tileInfos.Length,
                name = "tiles",
                //tiledversion = "1.7.2",
                //version = "1.6",
                //type = "tileset",
                tiles = tiles.ToArray()
            };

            return tiledSet;
        }

        private static List<TileInfo> LoadTiles(string inputDirectory)
        {
            var iconList = IconFile.Deserialize(Path.Combine(inputDirectory,"Icons.xml"));
            var tileList = TileInfoFile.Deserialize(Path.Combine(inputDirectory,"Tiles.xml"));
            foreach (var tileInfo in tileList.Tiles)
            {
                var icon = iconList.Icons.FirstOrDefault(item => item.Id == tileInfo.IconId);
                if (icon != null)
                {
                    tileInfo.FileName = icon.FileName;
                }
            }

            var gameTiles = new List<TileInfo>();

            foreach (var tile in tileList.Tiles.Where(item=> item.FileName != null))
            {
                var gameTile = gameTiles.FirstOrDefault(item => item.ImageFile == $"images/tiles/{tile.FileName}");
                if (gameTile != null)
                {
                    gameTile.OldIds.Add(tile.OldId);
                }
                else
                {
                    gameTile = new TileInfo
                    {
                        ImageFile = $"images/tiles/{tile.FileName}",
                        Image = $"images/tiles/{Path.GetFileNameWithoutExtension(tile.FileName)}", Id = GenerateTileId(),
                        OldIds = new List<int> {tile.OldId},
                        size = 32
                    };
                    
                    gameTiles.Add(gameTile);
                }
            }

            return gameTiles;
        }

        private static int TileId = 1;
        private static int GenerateTileId()
        {
            return TileId++;
        }

        private static List<Item> LoadItems(string fileName)
        {
            var items = new List<Item>();
            var lines = File.ReadLines(fileName).ToList();
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                
                var lineItems = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var item = new Item
                {
                    Id = GenerateId(),
                    Name = lineItems[0].Replace('_', ' '),
                    Health = int.Parse(lineItems[1]),
                    Defence = int.Parse(lineItems[2]),
                    Attack = int.Parse(lineItems[3]),
                    Agility = int.Parse(lineItems[4]),
                    Cost = int.Parse(lineItems[5]),
                    Type = (ItemType) int.Parse(lineItems[6]),
                    Image = $"images/items/{Path.GetFileNameWithoutExtension(lineItems[7])}"
                };

                items.Add(item);
            }
            return items;
        }

        private static List<Spell> LoadSpells(string fileName)
        {
            var spells = new List<Spell>();
            var lines = File.ReadLines(fileName).ToList();
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                
                var lineItems = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var spell = new Spell
                {
                    Id = GenerateId(),
                    Name = lineItems[0],
                    Type = int.Parse(lineItems[1]),
                    Power = int.Parse(lineItems[2]),
                    Cost = int.Parse(lineItems[3]),
                    Image = $"images/items/{Path.GetFileNameWithoutExtension(lineItems[4])}"
                };

                spells.Add(spell);
            }
            return spells;
        }

        private static TileInfo GetSprite(ICollection<TileInfo> mapTiles, ICollection<TileInfo> tiles, string image , int size = 32)
        {
            var imagePath = $"images/sprites/{Path.GetFileNameWithoutExtension(image)}";

            var info = mapTiles.FirstOrDefault(item => item.Image == imagePath);
            if (info != null)
            {
                return info;
            }

            var gameTileInfo = tiles.FirstOrDefault(item => item.OldImage == image);
            if (gameTileInfo == null)
            {
                gameTileInfo = new TileInfo
                {
                    Id = GenerateTileId(),
                    OldImage = image,
                    ImageFile = $"images/sprites/{image}",
                    Image = imagePath,
                    size = size
                };
                tiles.Add(gameTileInfo);
            }

            mapTiles.Add(gameTileInfo);
            return gameTileInfo;
        }

        static TileInfo GetTileInfo(ICollection<TileInfo> mapTiles, IEnumerable<TileInfo> tiles, int id)
        {
            var info = mapTiles.FirstOrDefault(item => item.OldIds?.Contains(id) ?? false);
            if (info != null)
            {
                return info;
            }

            info = tiles.FirstOrDefault(item => item.OldIds?.Contains(id) ?? false);
            if (info != null)
            {
                mapTiles.Add(info);
            }
            
            return info;
        }

        private static string GenerateId()
        {
            return Guid.NewGuid().ToString("N");
        }

        private static Map LoadMap(int id, string directory, ICollection<TileInfo> tiles)
        {
            var fileName = Path.Combine(directory, $"map{id}.dat");
            if (!File.Exists(fileName))
            {
                return null;
            }

            var lines = File.ReadLines(fileName).ToList();
            var line = 0;
            var map = new Map
            {
                Id = id,
                Height = int.Parse(lines[line].Split(' ')[0]),
                Width = int.Parse(lines[line].Split(' ')[1])
            };
            
            var defaultTile = GetTileInfo( map.TileInfo, tiles, int.Parse(lines[line].Split(' ')[2]));
            map.DefaultTileId = defaultTile.Id;
            line++;
            map.DefaultStart.X = int.Parse(lines[line].Split(' ')[0]);
            map.DefaultStart.Y = int.Parse(lines[line].Split(' ')[1]);
            var exits = new List<Exit>();
            line++;
            for (; line < 11; line++)
            {
                var lineString = lines[line].Split(' ');
                var exit = new Exit
                {
                    TileId = (char) (line - 2 + '1'),
                    MapId = int.Parse(lineString[2])
                };

                var x = int.Parse(lineString[0]);
                var y = int.Parse(lineString[1]);
                if (x != 0 || y != 0 || exit.MapId != 0)
                {
                    exit.Location = new Point {X = x, Y = y};
                }

                exits.Add(exit);
            }

            if ((map.Height != lines.Count - line) || (map.Width != lines[line].Length))
            {
                var v = "Width";
                if ((map.Height != lines.Count - line))
                    v = "Height";
                Console.WriteLine(
                    $"Warning: Map {id} Mismatched {v} line size Expected: h:{map.Height} w:{map.Width} Actual: h:{lines.Count - line} w:{lines[line].Length} ");
            }

            for (; line < lines.Count; line++)
            {
                var lineString = lines[line];
                var yPos = line - 11;
                var xPos = 0;
                foreach (var tileId in lineString)
                {
                    var tile = new Tile
                    {
                        Position = {X = xPos, Y = yPos},
                        Type = GetTileType(tileId)
                    };

                    
                    var (sprite, spriteTileId) = CreateSpriteInstance(tileId, exits, map.TileInfo, tiles, map.DefaultTileId);
                    // is it a sprite
                    if (sprite != null)
                    {
                        sprite.StartPosition.X = xPos;
                        sprite.StartPosition.Y = yPos;
                        map.Sprites.Add(sprite);

                        if (spriteTileId == 0)
                        {
                            tile.Id = map.DefaultTileId;
                        }
                        else
                        {
                            var info = GetTileInfo(map.TileInfo, tiles, spriteTileId);
                            tile.Id = info.Id;
                        }
                    }
                    else
                    {
                        var info = GetTileInfo(map.TileInfo, tiles, tileId);
                        if (info == null)
                        {
                            Console.WriteLine(
                                $"Warning: Map {id} unable to find tile {(int) tileId} ({tileId}) at ({xPos},{yPos}) Using Default { map.DefaultTileId }");
                            tile.Id = map.DefaultTileId;
                        }
                        else
                        {
                            tile.Id = info.Id;
                        }
                    }

                    map.Tiles.Add(tile);
                    xPos++;
                }
            }

            if (map.Tiles.Count != map.Height * map.Width)
            {
                Console.WriteLine(
                    $"Loaded {map.Tiles.Count} Tiles for map {id} Expected {map.Height * map.Width}");
            }
            
            return map;
        }

        private static Warp GetWarp(char tileId, IEnumerable<Exit> exits)
        {
            var warp = exits.FirstOrDefault(item => item.TileId == tileId);
            if (warp != null)
            {
                return warp;
            }

            switch (tileId)
            {
                case 'I':
                case 'J':
                case 'K':
                case 'L':
                case 'M':
                case 'N':
                case 'O':
                case 'P':
                case 'Q':
                case 'R':
                case 'S':
                case 'T':
                case 'U':
                case 'V':
                case 'W':
                case 'X':
                case 'Y':
                case 'Z':
                    return new Warp {MapId = tileId - 'I' + 6};
                case 'a':
                    return new Warp {MapId = 24};
                case 'c':
                    return new Warp {MapId = 25};
                case 'y':
                case 'z':
                    return new Warp {MapId = 0};
            }

            return null;
        }

        private static void LoadSprites(int id, Map map, string directory, IReadOnlyCollection<Spell> spells,
            ICollection<TileInfo> tiles, Biome biome = Biome.All)
        {
            var spriteFileName = Path.Combine(directory, $"monstset{id}.dat");
            if (!File.Exists(spriteFileName))
            {
                return;
            }

            var lines = File.ReadLines(spriteFileName).ToList();
            foreach (var line in lines)
            {
                var lineItems = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var image = lineItems[4];
                var name = lineItems[5];
                var npcType = int.Parse(lineItems[11]);
                var posX = int.Parse(lineItems[8]);
                var posY = int.Parse(lineItems[9]);
                var canMove = int.Parse(lineItems[10]) == 1;
                var size = int.Parse(lineItems[7]);

                var spriteType = SpriteType.NPC;
                switch (npcType)
                {
                    case 0:
                    case 7: // projectile 1
                    case 8: // projectile 2
                        spriteType = SpriteType.Monster;
                        break;
                    case 6: // end monster
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                        spriteType = SpriteType.NPC;
                        break;
                }

                string text = null;
                if (npcType == 5)
                {
                    name = "npc";
                    text = name.Replace('-', ' ').Replace('#', '\n');
                }
                

                if (spriteType == SpriteType.Monster)
                {
                    var spriteInfo = GetSprite(map.RandomMonstersTileInfo, tiles, image,  size*32);
                    var monster = map.RandomMonsters.FirstOrDefault(item => item.Id == spriteInfo.Id && item.Name == name && item.Biome == biome);
                    if (monster == null)
                    {
                        List<SpriteSpell> spriteSpells = null;
                        switch (npcType)
                        {
                            case 7:
                            {
                                var spell = spells.FirstOrDefault(item => item.Name == "LitBlast");
                                if (spell != null)
                                {
                                    spriteSpells = new List<SpriteSpell> {new SpriteSpell {Id = spell.Id}};
                                }

                                break;
                            }
                            case 8:
                            {
                                var spell = spells.FirstOrDefault(item => item.Name == "FireBlast");
                                if (spell != null)
                                {
                                    spriteSpells = new List<SpriteSpell> {new SpriteSpell {Id = spell.Id}};
                                }

                                break;
                            }
                        }
                        
                        monster = new Monster
                        {
                            Id = spriteInfo.Id,
                            Name = name,
                            Chance = 1,
                            Biome = biome,
                            Heath = int.Parse(lineItems[0]),
                            HeathConst = int.Parse(lineItems[1]),
                            Attack = int.Parse(lineItems[2]),
                            XP = int.Parse(lineItems[3]),
                            Gold = int.Parse(lineItems[6]),
                            Spells = spriteSpells
                        };
                        map.RandomMonsters.Add(monster);
                    }
                    else
                    {
                        monster.Chance++;
                    }
                }
                else
                {
                    if (posX != 0 && posY != 0)
                    {
                        var spriteInfo = GetSprite(map.TileInfo, tiles, image,  size*32);
                        var sprite = new Sprite
                        {
                            Id = spriteInfo.Id,
                            StartPosition = {X = posX, Y = posY},
                            Name = name,
                            Type = spriteType,
                            Text = text,
                            CanMove = canMove,
                        };
                        map.Sprites.Add(sprite);
                    }
                    else
                    {
                        Console.WriteLine(
                            $"Warning: Map {id} no position set for sprite {name}");
                    }
                }
            }
        }

        private static (Sprite, char ) CreateSpriteInstance(char tileId, IEnumerable<Exit> exits, ICollection<TileInfo> mapTiles, ICollection<TileInfo> tiles, int defaultTileId)
        {
            TileInfo sprite;
            switch (tileId)
            {
                case 'd': // door
                    sprite = GetSprite(mapTiles, tiles,"door.bmp");
                    return (new Sprite {Id = sprite.Id, State = 1, Name = "Door", Collideable = true, Type = SpriteType.Door}, 'b');
                case 'k': // key
                    sprite = GetSprite(mapTiles, tiles, "chest.bmp");
                    return (new Sprite {Id = sprite.Id, Name = "Key Chest", State = 2, Collideable = false, Type = SpriteType.Chest}, 'b');
                case 'G': // chest
                    sprite = GetSprite(mapTiles, tiles, "chest.bmp");
                    return (new Sprite {Id = sprite.Id, Name = "Chest", State = 1, Collideable = false, Type = SpriteType.Chest}, 'b');
                case '0': // open chest
                    sprite = GetSprite(mapTiles, tiles, "ochest.bmp");
                    return (new Sprite {Id = sprite.Id, Name = "Open Chest", State = 0, Collideable = false, Type = SpriteType.Chest}, 'b');
                case '#': // ship
                    sprite = GetSprite(mapTiles, tiles, "ship1.bmp");
                    return (new Sprite {Id = sprite.Id, Name = "Ship", State = 1, Collideable = false, Type = SpriteType.Ship}, 'w');
            }

            var warp = GetWarp(tileId, exits);
            if (warp != null)
            {
                int spriteTileId;
                var name = "Warp";
                switch (tileId)
                {
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                        name = $"Stairs to {warp.MapId}";
                        spriteTileId = GetTileInfo(mapTiles, tiles, tileId)?.Id ?? 0;
                        break;
                    case '9':
                        name = $"Warp to {warp.MapId}";
                        spriteTileId = GetTileInfo(mapTiles, tiles, tileId)?.Id ?? 0;
                        break;
                    case 'I':
                    case 'J':
                    case 'K':
                    case 'L':
                    case 'M':
                    case 'N':
                    case 'O':
                        name = $"Town {warp.MapId}";
                        spriteTileId = GetTileInfo(mapTiles, tiles, tileId)?.Id ?? 0;
                        break;
                    case 'P':
                    case 'Q':
                    case 'R':
                    case 'S':
                        name = $"Shrine {warp.MapId}";
                        spriteTileId = GetTileInfo(mapTiles, tiles, tileId)?.Id ?? 0;
                        break;
                    case 'T':
                    case 'U':
                    case 'V':
                    case 'W':
                    case 'X':
                    case 'Y':
                    case 'Z':
                        name = $"Cave {warp.MapId}";
                        spriteTileId = GetTileInfo(mapTiles, tiles, tileId)?.Id ?? 0;
                        break;
                    case 'a':
                        name = $"Tower {warp.MapId}";
                        spriteTileId = GetTileInfo(mapTiles, tiles, tileId)?.Id ?? 0;
                        break;
                    case 'c':
                        name = $"Tower {warp.MapId}";
                        spriteTileId = GetTileInfo(mapTiles, tiles, tileId)?.Id ?? 0;
                        break;
                    case 'z':
                        name = $"Water Exit {warp.MapId}";
                        spriteTileId = GetTileInfo(mapTiles, tiles, 'w')?.Id ?? 0;
                        break;
                    case 'y':
                        name = $"Exit {warp.MapId}";
                        spriteTileId = defaultTileId;
                        break;
                    default:
                        Console.WriteLine($"Unknown Warp Type {tileId}");
                        spriteTileId = GetTileInfo(mapTiles, tiles, tileId)?.Id ?? 0;
                        break;
                }
                
                return (new Sprite {Id = spriteTileId, Type=SpriteType.Warp, Name = name, State = 0, Collideable = false, Warp = warp}, (char)0);
            }

            return (null, (char)0);
        }

        private static TileType GetTileType(char tileIndex)
        {
            switch(tileIndex)
            {
                case (char)0:
                case (char)10:
                case 'w':
                    return TileType.Water;
                case 'd':
                case 'I':
                case 'J':
                case 'K':
                case 'L':
                case 'M':
                case 'N':
                case 'O':
                case 'P':
                case 'Q':
                case 'R':
                case 'S':
                case 'T':
                case 'U':
                case 'V':
                case 'W':
                case 'X':
                case 'Y':
                case 'Z':
                case 'a':
                case 'c':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                case 'z':
                case 'y':
                case '#':
                case '<':
                case ':':
                case '_':
                case ',':
                case 'H':
                case 'F':
                case 't':
                case 'k':
                case 'b':
                case 'g':
                case 'G':
                case '0':
                case '*':
                case 'f':
                case 'B':
                case ' ':
                case '.':
                case 'i':
                    return TileType.Ground;
                default:
                return TileType.Wall;
            }
        }
    }
}