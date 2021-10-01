using System;
using System.Collections.Generic;

namespace ConvertMaps
{
    using System.IO;
    using System.Linq;
    using GameFile;
    using Newtonsoft.Json;
    using CommandLine;
    using Models;
    using TileInfo = GameFile.TileInfo;

    public class Program
    {
        private class Options
        {
            [Option('o', "output", Required = true, HelpText = "Directory to save game data")]
            public string OutputDirectory { get; set; }
            [Option('i', "input", Required = true, HelpText = "Directory to be processed.")]
            public string InputDirectory { get; set; }
        }
        

        public static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<Options>(args)
                .WithParsed(RunOptions);
        }

        private static void RunOptions(Options opts)
        {
            var gameData = new GameData
            {
                Spells = LoadSpells(Path.Combine(opts.InputDirectory, "spells.dat")),
                Items = LoadItems(Path.Combine(opts.InputDirectory, "items.dat"))
            };
            

            for (var i = 0; i < 100; i++)
            {
                if (i == 1 || i == 2 || i == 3 || i == 4 || i == 5)
                {
                    continue;
                }
                
                var mapPath = Path.Combine(opts.InputDirectory, "maps");
                var tiles = LoadTiles(opts.InputDirectory);
                var map = LoadMap(i, mapPath, tiles);
                if (map != null)
                {
                    if (i == 0)
                    {
                        LoadSprites(1, map, mapPath, gameData.Spells, Biome.Grassland);
                        LoadSprites(2, map, mapPath, gameData.Spells, Biome.Water);
                        LoadSprites(3, map, mapPath, gameData.Spells, Biome.Desert);
                        LoadSprites(4, map, mapPath, gameData.Spells, Biome.Hills);
                        LoadSprites(5, map, mapPath, gameData.Spells, Biome.Forest);
                        LoadSprites(4, map, mapPath, gameData.Spells, Biome.Swamp);
                    }
                    else
                    {
                        LoadSprites(i, map, mapPath, gameData.Spells);
                    }
                    
                    gameData.Maps.Add(map);    
                }
            }

            Directory.CreateDirectory(opts.OutputDirectory);
            Directory.CreateDirectory(Path.Combine(opts.OutputDirectory, "maps"));
            File.WriteAllText( Path.Combine(opts.OutputDirectory, "spells.json"),JsonConvert.SerializeObject(gameData.Spells, Formatting.Indented, new JsonSerializerSettings { 
                NullValueHandling = NullValueHandling.Ignore
            }));
            File.WriteAllText( Path.Combine(opts.OutputDirectory, "items.json"),JsonConvert.SerializeObject(gameData.Items, Formatting.Indented, new JsonSerializerSettings { 
                NullValueHandling = NullValueHandling.Ignore
            }));
            foreach (var map in gameData.Maps)
            {
                File.WriteAllText( Path.Combine(opts.OutputDirectory, "maps", $"map{map.Id}.json"),JsonConvert.SerializeObject(map, Formatting.Indented, new JsonSerializerSettings { 
                    NullValueHandling = NullValueHandling.Ignore
                }));
            }
            
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

                tileInfo.Size = 32;
                tileInfo.NewId = GenerateId();
            }

            return tileList.Tiles.Select(tile => new GameFile.TileInfo {Image = tile.FileName==null?null:$"images/tiles/{Path.GetFileNameWithoutExtension(tile.FileName)}", Id = tile.NewId, Name = tile.Name, OldId = tile.OldId}).ToList();
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

        private static SpriteInfo GetSprite(Map map, SpriteType type, string name, string image)
        {
            var info = map.SpriteInfo.FirstOrDefault(item => item.Type == type && item.Name == name && item.Image == image);
            if (info == null)
            {
                info = new SpriteInfo {Type = type, Name = name, Id = GenerateId(), Image = image};
                map.SpriteInfo.Add(info);
            }
                
            return info;
        }

        static TileInfo GetTileInfo(Map map, IReadOnlyCollection<TileInfo> tiles, int id)
        {
            var info = map.TileInfo.FirstOrDefault(item => item.OldId == id);
            if (info != null)
            {
                return info;
            }

            info = tiles.FirstOrDefault(item => item.OldId == id);
            if (info != null)
            {
                if (info.Image == null)
                {
                    var defaultTile = tiles.FirstOrDefault(item => item.Id == map.DefaultTileId);
                    info.Image = defaultTile?.Image;
                }
                
                map.TileInfo.Add(info);
            }
            
            return info;
        }

        private static string GenerateId()
        {
            return Guid.NewGuid().ToString("N");
        }

        private static Map LoadMap(int id, string directory, IReadOnlyCollection<TileInfo> tiles)
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
            
            var defaultTile = GetTileInfo(map, tiles, int.Parse(lines[line].Split(' ')[2]));
            map.DefaultTileId = defaultTile?.Id;
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
                        Type = GetTileType(tileId),
                        Warp = GetWarp(tileId, exits)
                    };

                    
                    var (sprite, spriteTileId) = CreateSpriteInstance(tileId, map);
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
                            var info = GetTileInfo(map, tiles, spriteTileId);
                            tile.Id = info.Id;
                        }
                       
                    }
                    else
                    {
                        var info = GetTileInfo(map, tiles, tileId);
                        if (info == null)
                        {
                            Console.WriteLine(
                                $"Warning: Map {id} unable to find tile {(int) tileId} ({tileId}) at ({xPos},{yPos})");
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

        private static Warp GetWarp(char tileId, List<Exit> exits)
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

        private static void LoadSprites(in int id, Map map, string directory, IReadOnlyCollection<Spell> spells, Biome biome = Biome.All)
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

                image = $"images/sprites/{Path.GetFileNameWithoutExtension(image)}";
                
                var spriteInfo = GetSprite(map, spriteType, name, image);
                spriteInfo.Heath = int.Parse(lineItems[0]);
                spriteInfo.HeathConst = int.Parse(lineItems[1]);
                spriteInfo.Attack = int.Parse(lineItems[2]);
                spriteInfo.XP = int.Parse(lineItems[3]);
                spriteInfo.Gold = int.Parse(lineItems[6]);
                switch (npcType)
                {
                    case 7:
                    {
                        var spell = spells.FirstOrDefault(item => item.Name == "LitBlast");
                        if (spell != null)
                        {
                            spriteInfo.Spells = new List<SpriteSpell> {new SpriteSpell {Id = spell.Id}};
                        }

                        break;
                    }
                    case 8:
                    {
                        var spell = spells.FirstOrDefault(item => item.Name == "FireBlast");
                        if (spell != null)
                        {
                            spriteInfo.Spells = new List<SpriteSpell> {new SpriteSpell {Id = spell.Id}};
                        }

                        break;
                    }
                }

                if (spriteType == SpriteType.Monster)
                {
                    var monster = map.RandomMonsters.FirstOrDefault(item => item.Id == spriteInfo.Id && item.Biome == biome);
                    if (monster == null)
                    {
                        monster = new Monster {Id = spriteInfo.Id, Chance = 1, Biome=biome};
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
                        var sprite = new Sprite
                        {
                            Id = spriteInfo.Id,
                            StartPosition = {X = posX, Y = posY},
                            Text = text,
                            CanMove = canMove
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

        private static (Sprite, char ) CreateSpriteInstance(char tileId, Map map)
        {
            SpriteInfo sprite;
            switch (tileId)
            {
                case 'd': // door
                    sprite = GetSprite(map, SpriteType.Door, "Door", "images/sprites/door");
                    return (new Sprite {Id = sprite.Id, State = 1, Collideable = true}, 'b');
                case 'k': // key
                    sprite = GetSprite(map, SpriteType.Chest, "Chest", "images/sprites/chest");
                    return (new Sprite {Id = sprite.Id, State = 2, Collideable = false}, 'b');
                case 'G': // chest
                    sprite = GetSprite(map, SpriteType.Chest, "Chest", "images/sprites/chest");
                    return (new Sprite {Id = sprite.Id, State = 1, Collideable = false}, 'b');
                case '0': // open chest
                    sprite = GetSprite(map, SpriteType.Chest, "Chest", "images/sprites/ochest");
                    return (new Sprite {Id = sprite.Id, State = 0, Collideable = false}, 'b');
                case '#': // ship
                    sprite = GetSprite(map, SpriteType.Ship, "Ship", "images/sprites/ship1");
                    return (new Sprite {Id = sprite.Id, State = 1, Collideable = false}, 'w');
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