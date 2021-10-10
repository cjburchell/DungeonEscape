using System.Collections.Generic;
using System.Linq;
using GameFile;

namespace ConvertMaps.Tiled
{
    public static class TiledConverter
    {
        public static TiledMap ToTileMap(Map map)
        {
            const int offset = 1;
            
            var floor = new List<int>();
            var water = new List<int>();
            var wall = new List<int>();
            
            //var waterBiome = new List<int>();
            //var hills = new List<int>();
            //var desert = new List<int>();
            //var forest = new List<int>();
            //var swamp = new List<int>();
            //var grassland = new List<int>();
            var biomes = new List<int>();

            for (var y = 0; y < map.Width; y++)
            {
                for (var x = 0; x < map.Height; x++)
                {
                    var tile = map.Tiles.FirstOrDefault(item => item.Position.X == x && item.Position.Y == y);
                    floor.Add(tile != null && tile.Type == TileType.Ground ? tile.Id + offset : 0);
                    water.Add(tile != null && tile.Type == TileType.Water ? tile.Id + offset : 0);
                    wall.Add(tile != null && tile.Type == TileType.Wall ? tile.Id + offset : 0);
                    if (map.Id != 0)
                    {
                        continue;
                    }

                    //waterBiome.Add(tile != null && tile.Biome == Biome.Water ? tile.Id + offset : 0);
                    //hills.Add(tile != null && tile.Biome == Biome.Hills ? tile.Id + offset : 0);
                    //desert.Add(tile != null && tile.Biome == Biome.Desert ? tile.Id + offset : 0);
                    //forest.Add(tile != null && tile.Biome == Biome.Forest ? tile.Id + offset : 0);
                    //swamp.Add(tile != null && tile.Biome == Biome.Swamp ? tile.Id + offset : 0);
                    //grassland.Add(tile != null && tile.Biome == Biome.Grassland ? tile.Id + offset : 0);
                    biomes.Add(tile != null && tile.Biome != Biome.None ? (int) tile.Biome : 0);
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
            
            var layers = new List<TiledLayer>();
            var id = 1;
            layers.Add(ToTiledLayer(id++, "floor", floor, map, TileType.Ground));
            layers.Add(ToTiledLayer(id++, "water", water, map, TileType.Water));
            layers.Add(ToTiledLayer(id++, "wall",wall, map, TileType.Wall));
            if (map.Id == 0)
            {
                //layers.Add(ToTiledLayer(id++, "biome_water", waterBiome,  map, TileType.Ground, false));
                //layers.Add(ToTiledLayer(id++, "biome_hills", hills,  map, TileType.Ground, false));
                //layers.Add(ToTiledLayer(id++, "biome_desert", desert,  map, TileType.Ground, false));
                //layers.Add(ToTiledLayer(id++, "biome_forest", forest,  map, TileType.Ground, false));
                //layers.Add(ToTiledLayer(id++, "biome_swamp", swamp,  map, TileType.Ground, false));
                //layers.Add(ToTiledLayer(id++, "biome_grassland", grassland,  map, TileType.Ground, false));
                layers.Add(ToTiledLayer(id++, "biomes", biomes,  map, TileType.Ground, false));
            }
            
            layers.Add(ToObjectGroup(id++, "items", objects));
            layers.Add(ToObjectGroup(id++,"sprites", sprites));
            layers.Add(ToObjectGroup(id,"objects", new List<TiledObject>(){new TiledObject
            {
                name = "spawn",
                x = map.DefaultStart.X * 32,
                y = (map.DefaultStart.Y+1) * 32,
                width = 32,
                height = 32,
                type = "Spawn",
                visible = false,
            }}, false));
            
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
                tilesets = new[] {ToTileSet(map.TileInfo, $"Map Tiles")},
                layers = layers.ToArray()
            };


            return tiledMap;
        }

        private static TiledLayer ToObjectGroup(int id, string name, List<TiledObject> objects, bool visible = true)
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

        private static TiledLayer ToTiledLayer(int id, string name, List<int> tiles, Map map, TileType type, bool visible = true )
        {
            return new TiledLayerGroup
            {
                height = map.Height,
                width = map.Width,
                id = id,
                name = name,
                visible = visible ? 1: 0,
                opacity = 1,
                type = "tilelayer",
                x = 0,
                y = 0,
                data = tiles.ToArray(),
                layerData = new TiledLayerData
                {
                    data = string.Join(",", tiles)
                },
                properties = new[]
                {
                    new TiledProperty {name = "LayerType", type = "string", value = TileType.Ground.ToString()},
                }
            };
        }

        public static TiledTileset ToTileSet(IEnumerable<TileInfo> mapTileInfo, string name)
        {
            var tileInfos = mapTileInfo as TileInfo[] ?? mapTileInfo.ToArray();
            var tiledSet = new TiledTileset
            {
                firstgid = 1,
                tilewidth = tileInfos.Max(item=> item.size),
                tileheight = tileInfos.Max(item=> item.size),
                tilecount = tileInfos.Length,
                name = name,
                transparentcolor = "#FF00FF",
                //tiledversion = "1.7.2",
                //version = "1.6",
                //type = "tileset",
                tiles = tileInfos.Select(mapTile => new TiledTile {id = mapTile.Id, imageObj = new TiledImage {source = mapTile.Image, height = mapTile.size, width = mapTile.size},image = mapTile.Image, imageheight = mapTile.size, imagewidth = mapTile.size}).ToArray()
            };

            return tiledSet;
        }
        
        public static TiledTileset ToMonsterTileSet(IEnumerable<TileInfo> mapTileInfo, IEnumerable<Monster> monsters, string name)
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
                        properties.AddRange(monster.Spells.Select(monsterSpell => new TiledProperty {name = $"Spell{spell++}", type = "int", value = monsterSpell.Id.ToString()}));
                    }
                    
                    var tile = new TiledTile
                    {
                        type = monster.Name,
                        id = mapTile.Id, 
                        image = mapTile.Image, 
                        imageheight = mapTile.size,
                        imagewidth = mapTile.size,
                        probability =  monster.Chance/(double)totalMonsters,
                        properties = properties.ToArray(),
                        imageObj = new TiledImage {source = mapTile.Image, height = mapTile.size, width = mapTile.size}
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
                name = name,
                transparentcolor = "#FF00FF",
                tiles = tiles.ToArray()
            };

            return tiledSet;
        }

        public static TiledTileset ToSpellTileset(IEnumerable<Spell> spells, IEnumerable<TileInfo> tileInfo)
        {
            var tileInfos = tileInfo.ToList();
            var tiles = new List<TiledTile>();
            foreach (var spell in spells)
            {
                var spellTile = tileInfos.FirstOrDefault(item => item.Id == spell.Id);
                var size = spellTile?.size ?? 0;
                
                var properties = new List<TiledProperty>
                {
                    new TiledProperty {name = "Name", type = "string", value = spell.Name},
                    new TiledProperty {name = "Power", type = "int", value = spell.Power.ToString()},
                    new TiledProperty {name = "Cost", type = "int", value = spell.Cost.ToString()},
                    new TiledProperty {name = "MinLevel", type = "int", value = spell.MinLevel.ToString()}
                };
                
                var tile = new TiledTile
                {
                    type = spell.Type.ToString(),
                    id = spell.Id,
                    image = spellTile?.Image, 
                    imageheight = size,
                    imagewidth = size,
                    properties = properties.ToArray(),
                    imageObj = new TiledImage {source = spellTile?.Image, height = size, width = size}
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
        
        public static TiledTileset ToItemTileset(IEnumerable<Item> items, IEnumerable<TileInfo> tileInfo)
        {
            var tileInfos = tileInfo.ToList();
            var tiles = new List<TiledTile>();
            foreach (var item in items)
            {
                var itemTile = tileInfos.FirstOrDefault(i => i.Id == item.Id);
                var size = itemTile?.size ?? 0;
                
                var properties = new List<TiledProperty>
                {
                    new TiledProperty {name = "Name", type = "string", value = item.Name},
                    new TiledProperty {name = "Defence", type = "int", value = item.Defence.ToString()},
                    new TiledProperty {name = "Health", type = "int", value = item.Health.ToString()},
                    new TiledProperty {name = "Attack", type = "int", value = item.Attack.ToString()},
                    new TiledProperty {name = "Attack", type = "int", value = item.Attack.ToString()},
                    new TiledProperty {name = "Agility", type = "int", value = item.Agility.ToString()},
                    new TiledProperty {name = "Cost", type = "int", value = item.Cost.ToString()},
                    new TiledProperty {name = "MinLevel", type = "int", value = item.MinLevel.ToString()}
                };
                
                var tile = new TiledTile
                {
                    type = item.Type.ToString(),
                    id = item.Id,
                    image = itemTile?.Image, 
                    imageheight = size,
                    imagewidth = size,
                    properties = properties.ToArray(),
                    imageObj = new TiledImage {source = itemTile?.Image, height = size, width = size}
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