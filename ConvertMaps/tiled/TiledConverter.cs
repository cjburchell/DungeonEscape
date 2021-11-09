using System.Collections.Generic;
using System.Linq;
using GameFile;

namespace ConvertMaps.Tiled
{
    public static class TiledConverter
    {
        const int BiomeOffset = 900;
        private const int NpcOffset = 1000;
        public const int ObjectsOffset = 2000;
        private const int ShipOffset = 3000;
        
        public static TiledMap ToTileMap(Map map)
        {
            const int offset = 1;

            var floor = new List<int>();
            var water = new List<int>();
            var wall = new List<int>();
            var biomes = new List<int>();

            for (var y = 0; y < map.Height; y++)
            {
                for (var x = 0; x < map.Width; x++)
                {
                    var floorTile = map.FloorLayer.FirstOrDefault(item => item.Position.X == x && item.Position.Y == y);
                    floor.Add(floorTile?.Id + offset ?? 0);

                    var waterTile = map.WaterLayer.FirstOrDefault(item => item.Position.X == x && item.Position.Y == y);
                    water.Add(waterTile?.Id + offset ?? 0);

                    var wallTile = map.WallLayer.FirstOrDefault(item => item.Position.X == x && item.Position.Y == y);
                    wall.Add(wallTile?.Id + offset ?? 0);
                    if (map.Id != 0)
                    {
                        continue;
                    }

                    var biome = Biome.None;
                    if (floorTile != null)
                    {
                        biome = floorTile.Biome;
                    }
                    else if (waterTile != null)
                    {
                        biome = waterTile.Biome;
                    }

                    biomes.Add(biome == Biome.None? 0: (int) biome + BiomeOffset);
                }
            }

            var objects = new List<TiledObject>();
            var sprites = new List<TiledObject>();
            var tileId = new IdGenerator();
            foreach (var objSprite in map.Sprites)
            {
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
                    properties.Add(new TiledProperty
                        {name = "WarpMap", type = "int", value = objSprite.Warp.MapId.ToString()});
                    if (objSprite.Warp.Location != null)
                    {
                        properties.Add(new TiledProperty
                            {name = "WarpMapX", type = "int", value = objSprite.Warp.Location.X.ToString()});
                        properties.Add(new TiledProperty
                            {name = "WarpMapY", type = "int", value = objSprite.Warp.Location.Y.ToString()});
                    }
                }
                
                switch (objSprite.Type)
                {
                    case SpriteType.Ship:
                    {
                        var obj = new TiledObject
                        {
                            id = tileId.New(),
                            gid = ShipOffset,
                            name = objSprite.Name,
                            x = objSprite.StartPosition.X * 32,
                            y = (objSprite.StartPosition.Y + 1) * 32,
                            width = 32,
                            height = 56,
                            type = objSprite.Type.ToString(),
                            visible = true,
                            properties = properties.ToArray()
                        };
                        objects.Add(obj);
                        break;
                    }
                    case SpriteType.Door:
                    case SpriteType.Chest:
                    case SpriteType.Warp:
                    case SpriteType.Static:
                    {
                        var obj = new TiledObject
                        {
                            id = tileId.New(),
                            gid = objSprite.Id==0?0:objSprite.Id + ObjectsOffset,
                            name = objSprite.Name,
                            x = objSprite.StartPosition.X * 32,
                            y = (objSprite.StartPosition.Y + 1) * 32,
                            width = objSprite.Width,
                            height = objSprite.Height,
                            type = objSprite.Type.ToString(),
                            visible = true,
                            properties = properties.ToArray()
                        };

                        objects.Add(obj);
                        break;
                    }
                    default:
                    {
                        var obj = new TiledObject
                        {
                            id = tileId.New(),
                            gid = objSprite.Id + NpcOffset,
                            name = objSprite.Name,
                            x = objSprite.StartPosition.X * 32,
                            y = (objSprite.StartPosition.Y + (32 / 32)) * 32,
                            width = 32,
                            height = 48,
                            type = objSprite.Type.ToString(),
                            visible = true,
                            properties = properties.ToArray()
                        };
                        sprites.Add(obj);
                        break;
                    }
                }
            }

            var layers = new List<TiledLayer>();
            var id = 1;
            layers.Add(ToTiledLayer(id++, "floor", floor, map));
            layers.Add(ToTiledLayer(id++, "water", water, map));
            layers.Add(ToTiledLayer(id++, "wall", wall, map));
            if (map.Id == 0)
            {
                layers.Add(ToTiledLayer(id++, "biomes", biomes, map, false, 0.5f));
            }

            layers.Add(ToObjectGroup(id++, "items", objects));
            layers.Add(ToObjectGroup(id++, "sprites", sprites));
            layers.Add(ToObjectGroup(id, "objects", new List<TiledObject>()
            {
                new TiledObject
                {
                    name = "spawn",
                    x = map.DefaultStart.X * 32,
                    y = (map.DefaultStart.Y + 1) * 32,
                    width = 32,
                    height = 32,
                    type = "Spawn",
                    visible = false,
                }
            }, false));

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
                tilesets = map.Id == 0
                    ? new[] {GetTiledTileset(), ToBiomeTileSet(), GetNpcTileset(), GetObjectTileset(), GetShipTileset()}
                    : new[] {GetTiledTileset(), GetNpcTileset(), GetObjectTileset(), GetShipTileset()},
                layers = layers.ToArray()
            };


            return tiledMap;
        }

        private static TiledTileset GetNpcTileset()
        {
            return new TiledTileset
            {
                firstgid = NpcOffset,
                source = "npc.tsx"
            };
        }
        
        private static TiledTileset GetTiledTileset()
        {
            return new TiledTileset
            {
                firstgid = 1,
                source = "tiles.tsx"
            };
        }
        
        private static TiledTileset GetObjectTileset()
        {
            return new TiledTileset
            {
                firstgid = ObjectsOffset,
                source = "objects.tsx"
            };
        }
        
        private static TiledTileset GetShipTileset()
        {
            return new TiledTileset
            {
                firstgid = ShipOffset,
                source = "ship.tsx"
            };
        }

        private static TiledLayer ToObjectGroup(int id, string name, List<TiledObject> objects, bool visible = true, float opacity = 1)
        {
            return new ObjectGroup
            {
                id = id,
                name = name,
                type = "objectgroup",
                visible = visible ? 1: 0,
                opacity = 1,
                x = 0,
                y = 0,
                objects = objects.ToArray(),
                draworder = "topdown"
            };
        }

        private static TiledLayer ToTiledLayer(int id, string name, List<int> tiles, Map map, bool visible = true, float opacity = 1 )
        {
            return new TiledLayerGroup
            {
                height = map.Height,
                width = map.Width,
                id = id,
                name = name,
                visible = visible ? 1: 0,
                opacity = opacity,
                type = "tilelayer",
                x = 0,
                y = 0,
                data = tiles.ToArray(),
                layerData = new TiledLayerData
                {
                    data = string.Join(",", tiles)
                }
            };
        }


        public static TiledTile BiomeTile(Biome biome, string image)
        {
            return new TiledTile
            {
                type = "biome",
                id = (int)biome,
                image = image, 
                imageheight = 32,
                imagewidth = 32,
                imageObj = new TiledImage {source = image, height = 32, width = 32}
            };
        }
        
        public static TiledTileset ToBiomeTileSet()
        {
            var tiles = new List<TiledTile>();
            
            tiles.Add(BiomeTile(Biome.Grassland, "images/tiles/grass.png"));
            tiles.Add(BiomeTile(Biome.Forest, "images/tiles/tree.png"));
            tiles.Add(BiomeTile(Biome.Water, "images/tiles/water.png"));
            tiles.Add(BiomeTile(Biome.Hills, "images/tiles/snow.png"));
            tiles.Add(BiomeTile(Biome.Desert, "images/tiles/desert.png"));
            tiles.Add(BiomeTile(Biome.Swamp, "images/tiles/swamp.png"));
            
            var tiledSet = new TiledTileset
            {
                firstgid = BiomeOffset,
                tilewidth = 32,
                tileheight = 32,
                tilecount = tiles.Count,
                name = "biomes",
                transparentcolor = "#FF00FF",
                tiles = tiles.ToArray()
            };
            return tiledSet;
        }

        public static TiledTileset ToTileSet(IEnumerable<TileInfo> mapTileInfo, int size, string name, int offset = 1)
        {
            var tileInfos = mapTileInfo as TileInfo[] ?? mapTileInfo.ToArray();
            var tiledSet = new TiledTileset
            {
                firstgid = offset,
                tilewidth = size,
                tileheight = size,
                tilecount = tileInfos.Length,
                name = name,
                tiles = tileInfos.Select(mapTile => new TiledTile {id = mapTile.Id, imageObj = new TiledImage {source = mapTile.Image, height = size, width = size},image = mapTile.Image, imageheight = size, imagewidth = size}).ToArray()
            };

            return tiledSet;
        }
        
        public static TiledTileset ToMonsterTileSet(IEnumerable<Monster> monsters, string name)
        {
            var tiles = new List<TiledTile>();
            var monsterList = monsters.ToList();
            
            foreach (var monster in monsterList)
            {
                if (monster.Info != null)
                {
                    var tile = new TiledTile
                    {
                        type = monster.Name,
                        id = monster.Info.Id, 
                        image = monster.Info.Image,
                        imageObj = new TiledImage {source = monster.Info.Image}
                    };

                    tiles.Add(tile);
                }
            }


            var tiledSet = new TiledTileset
            {
                firstgid = 1,
                tilecount = monsterList.Count,
                name = name,
                transparentcolor = "#FF00FF",
                tiles = tiles.ToArray()
            };

            return tiledSet;
        }

        public static TiledTileset ToSpellTileset(IEnumerable<Spell> spells)
        {
            var tiles = new List<TiledTile>();
            foreach (var spell in spells)
            {
                const int size = 32;
                var tile = new TiledTile
                {
                    type = spell.Name,
                    id = spell.Info.Id,
                    image = spell.Info.Image, 
                    imageheight = size,
                    imagewidth = size,
                    imageObj = new TiledImage {source = spell.Info.Image, height = size, width = size}
                };
                
                tiles.Add(tile);
            }
            
            var tiledSet = new TiledTileset
            {
                firstgid = 1,
                tilewidth = tiles.Max(item=> item.imagewidth),
                tileheight = tiles.Max(item=> item.imageheight),
                tilecount = tiles.Count,
                name = "spells",
                transparentcolor = "#FF00FF",
                tiles = tiles.ToArray()
            };
            return tiledSet;
        }
        
        public static TiledTileset ToItemTileset(IEnumerable<Item> items)
        {
            var tiles = new List<TiledTile>();
            foreach (var item in items)
            {
                const int size = 32;

                var tile = new TiledTile
                {
                    type = item.Name,
                    id = item.Info.Id,
                    image = item.Info.Image, 
                    imageheight = size,
                    imagewidth = size,
                    imageObj = new TiledImage {source = item.Info.Image, height = size, width = size}
                };
                
                tiles.Add(tile);
            }
            
            var tiledSet = new TiledTileset
            {
                firstgid = 1,
                tilewidth = tiles.Max(item=> item.imagewidth),
                tileheight = tiles.Max(item=> item.imageheight),
                tilecount = tiles.Count,
                name = "items",
                transparentcolor = "#FF00FF",
                tiles = tiles.ToArray()
            };
            return tiledSet;
        }
    }
}