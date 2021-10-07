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
                tilesets = new[] {ToTileSet(map.TileInfo, $"Map Tiles")},
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
                        name = "items",
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
                    },
                    new ObjectGroup
                    {
                        id = 6,
                        name = "objects",
                        type = "objectgroup",
                        visible = false,
                        opacity = 1,
                        x = 0,
                        y = 0,
                        objects =new []{
                            new TiledObject
                            {
                                name = "spawn",
                                x = map.DefaultStart.X * 32,
                                y = (map.DefaultStart.Y+1) * 32,
                                width = 32,
                                height = 32,
                                type = "Spawn",
                                visible = false,
                            }},
                        draworder = "topdown"
                    }
                }
            };


            return tiledMap;
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
                tiles = tileInfos.Select(mapTile => new TiledTile {id = mapTile.Id, imageObj = new TiledImage {source = mapTile.ImageFile, height = mapTile.size, width = mapTile.size},image = mapTile.ImageFile, imageheight = mapTile.size, imagewidth = mapTile.size}).ToArray()
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
                name = name,
                transparentcolor = "#FF00FF",
                //tiledversion = "1.7.2",
                //version = "1.6",
                //type = "tileset",
                tiles = tiles.ToArray()
            };

            return tiledSet;
        }
    }
}