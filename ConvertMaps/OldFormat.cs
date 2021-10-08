using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ConvertMaps.Models;
using GameFile;

namespace ConvertMaps
{
    public static class OldFormat
    {
        public static List<Map> LoadMaps(string inputDirectory, List<Spell> spells, List<TileInfo> tiles)
        {
            var maps = new List<Map>();
            var inputMapPath = Path.Combine(inputDirectory, "maps");

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
            
            return maps;
        }

        public static List<TileInfo> LoadTiles(string inputDirectory)
        {
            var iconList = IconFile.Deserialize(Path.Combine(inputDirectory, "Icons.xml"));
            var tileList = TileInfoFile.Deserialize(Path.Combine(inputDirectory, "Tiles.xml"));
            foreach (var tileInfo in tileList.Tiles)
            {
                var icon = iconList.Icons.FirstOrDefault(item => item.Id == tileInfo.IconId);
                if (icon != null)
                {
                    tileInfo.FileName = icon.FileName;
                }
            }

            var gameTiles = new List<TileInfo>();

            foreach (var tile in tileList.Tiles.Where(item => item.FileName != null))
            {

                var imageFileName = $"images/tiles/{Path.GetFileNameWithoutExtension(tile.FileName)}.png";
                var gameTile = gameTiles.FirstOrDefault(item => item.Image == imageFileName);
                if (gameTile != null)
                {
                    gameTile.OldIds.Add(tile.OldId);
                }
                else
                {
                    gameTile = new TileInfo
                    {
                        Image = imageFileName,
                        Id = IdGenerator.New(),
                        OldIds = new List<int> {tile.OldId},
                        size = 32
                    };

                    gameTiles.Add(gameTile);
                }
            }

            return gameTiles;
        }

        public static List<Item> LoadItems(string inputDirectory, ICollection<TileInfo> tiles)
        {
            var fileName = Path.Combine(inputDirectory, "items.dat");
            var items = new List<Item>();
            var lines = File.ReadLines(fileName).ToList();
            var lindIndex = 0;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var lineItems = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var info = GetSprite(null, tiles, lineItems[7], 32, "images/items/");
                var item = new Item
                {
                    Id = info.Id,
                    Name = lineItems[0].Replace('_', ' '),
                    Health = int.Parse(lineItems[1]),
                    Defence = int.Parse(lineItems[2]),
                    Attack = int.Parse(lineItems[3]),
                    Agility = int.Parse(lineItems[4]),
                    Cost = int.Parse(lineItems[5]),
                    Type = (ItemType) int.Parse(lineItems[6]),
                    MinLevel = (lindIndex/7) * 6
                };

                lindIndex++;
                items.Add(item);
            }

            return items;
        }

        public static List<Spell> LoadSpells(string inputDirectory, ICollection<TileInfo> tiles)
        {    
            var fileName = Path.Combine(inputDirectory, "spells.dat");
            var spells = new List<Spell>();
            var lines = File.ReadLines(fileName).ToList();
            int spellIndex = 0;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var lineItems = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var info = GetSprite(null, tiles, lineItems[4], 32, "images/items/");
                var spell = new Spell
                {
                    Id = info.Id,
                    Name = lineItems[0],
                    Type = (SpellType) int.Parse(lineItems[1]),
                    Power = int.Parse(lineItems[2]),
                    Cost = int.Parse(lineItems[3]),
                    MinLevel = spellIndex*2 + 2,
                };

                spellIndex++;
                spells.Add(spell);
            }

            return spells;
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

            var defaultTile = GetTileInfo(map.TileInfo, tiles, int.Parse(lines[line].Split(' ')[2]));
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
                if (map.Height != lines.Count - line)
                {
                    v = "Height";
                }

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


                    var (sprite, spriteTileId) =
                        CreateSpriteInstance(tileId, exits, map.TileInfo, tiles, map.DefaultTileId);
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
                                $"Warning: Map {id} unable to find tile {(int) tileId} ({tileId}) at ({xPos},{yPos}) Using Default {map.DefaultTileId}");
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
                    var spriteInfo = GetSprite(map.RandomMonstersTileInfo, tiles, image, size * 32, "images/monsters/");
                    var monster = map.RandomMonsters.FirstOrDefault(item =>
                        item.Id == spriteInfo.Id && item.Name == name && item.Biome == biome);
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
                        var spriteInfo = GetSprite(map.TileInfo, tiles, image, size * 32);
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

        private static TileInfo GetSprite(ICollection<TileInfo> mapTiles, ICollection<TileInfo> tiles, string image,
            int size = 32, string path="images/sprites/")
        {
            var imagePath = $"{path}{Path.GetFileNameWithoutExtension(image)}.png";

            var info = mapTiles?.FirstOrDefault(item => item.Image == imagePath);
            if (info != null)
            {
                return info;
            }

            var gameTileInfo = tiles.FirstOrDefault(item => item.Image == imagePath);
            if (gameTileInfo == null)
            {
                gameTileInfo = new TileInfo
                {
                    Id = IdGenerator.New(),
                    Image = imagePath,
                    size = size
                };
                tiles.Add(gameTileInfo);
            }

            mapTiles?.Add(gameTileInfo);
            
            return gameTileInfo;
        }

        private static TileInfo GetTileInfo(ICollection<TileInfo> mapTiles, IEnumerable<TileInfo> tiles, int id)
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

        private static (Sprite, char ) CreateSpriteInstance(char tileId, IEnumerable<Exit> exits,
            ICollection<TileInfo> mapTiles, ICollection<TileInfo> tiles, int defaultTileId)
        {
            TileInfo sprite;
            switch (tileId)
            {
                case 'd': // door
                    sprite = GetSprite(mapTiles, tiles, "door.bmp");
                    return (
                        new Sprite
                        {
                            Id = sprite.Id, State = 1, Name = "Door", Collideable = true, Type = SpriteType.Door
                        }, 'b');
                case 'k': // key
                    sprite = GetSprite(mapTiles, tiles, "chest.bmp");
                    return (
                        new Sprite
                        {
                            Id = sprite.Id, Name = "Key Chest", State = 2, Collideable = false, Type = SpriteType.Chest
                        }, 'b');
                case 'G': // chest
                    sprite = GetSprite(mapTiles, tiles, "chest.bmp");
                    return (
                        new Sprite
                        {
                            Id = sprite.Id, Name = "Chest", State = 1, Collideable = false, Type = SpriteType.Chest
                        }, 'b');
                case '0': // open chest
                    sprite = GetSprite(mapTiles, tiles, "ochest.bmp");
                    return (
                        new Sprite
                        {
                            Id = sprite.Id, Name = "Open Chest", State = 0, Collideable = false, Type = SpriteType.Chest
                        }, 'b');
                case '#': // ship
                    sprite = GetSprite(mapTiles, tiles, "ship1.bmp");
                    return (
                        new Sprite
                        {
                            Id = sprite.Id, Name = "Ship", State = 1, Collideable = false, Type = SpriteType.Ship
                        }, 'w');
            }
            
            var warp = GetWarp(tileId, exits);
            if (warp == null)
            {
                return (null, (char) 0);
            }

            int spriteTileId;
            var warpBackground = (char) 0;
            var name = "Warp";
            switch (tileId)
            {
                case '1':
                case '2':
                case '3':
                case '4':
                    name = $"Stairs up to {warp.MapId}";
                    spriteTileId = GetSprite(mapTiles, tiles, "stairup.bmp")?.Id ?? 0;
                    break;
                case '5':
                case '6':
                case '7':
                case '8':
                    name = $"Stairs down to {warp.MapId}";
                    spriteTileId = GetSprite(mapTiles, tiles, "stairdw.bmp")?.Id ?? 0;
                    break;
                case '9':
                    name = $"Warp to {warp.MapId}";
                    spriteTileId = GetSprite(mapTiles, tiles, "warp.bmp")?.Id ?? 0;
                    break;
                case 'I':
                case 'J':
                case 'K':
                case 'L':
                case 'M':
                case 'N':
                case 'O':
                    name = $"Town {warp.MapId}";
                    spriteTileId = GetSprite(mapTiles, tiles, "town.bmp")?.Id ?? 0;
                    warpBackground = ' ';
                    break;
                case 'P':
                case 'Q':
                case 'R':
                case 'S':
                    name = $"Shrine {warp.MapId}";
                    spriteTileId = GetSprite(mapTiles, tiles, "shrin.bmp")?.Id ?? 0;
                    warpBackground = ' ';
                    break;
                case 'T':
                case 'U':
                case 'V':
                case 'W':
                case 'X':
                case 'Y':
                case 'Z':
                    name = $"Cave {warp.MapId}";
                    spriteTileId = GetSprite(mapTiles, tiles, "cave.bmp")?.Id ?? 0;
                    warpBackground = ' ';
                    break;
                case 'a':
                    name = $"Tower {warp.MapId}";
                    spriteTileId = GetSprite(mapTiles, tiles, "tower.bmp")?.Id ?? 0;
                    warpBackground = ' ';
                    break;
                case 'c':
                    name = $"Tower {warp.MapId}";
                    spriteTileId = GetSprite(mapTiles, tiles, "tower.bmp")?.Id ?? 0;
                    warpBackground = ' ';
                    break;
                case 'z':
                    name = $"Water Exit {warp.MapId}";
                    spriteTileId = GetTileInfo(mapTiles, tiles, 'w')?.Id ?? 0;
                    warpBackground = 'w';
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

            return (
                new Sprite
                {
                    Id = spriteTileId, Type = SpriteType.Warp, Name = name, State = 0, Collideable = false,
                    Warp = warp
                }, warpBackground);

        }

        private static TileType GetTileType(char tileIndex)
        {
            switch (tileIndex)
            {
                case (char) 0:
                case (char) 10:
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