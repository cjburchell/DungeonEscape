using System.Collections.Generic;
using System.Xml.Linq;

namespace Redpoint.DungeonEscape.State
{
    public sealed class TiledMapInfo
    {
        private const uint TiledGidMask = 0x1FFFFFFF;

        public string Class { get; set; }
        public string Orientation { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int TileWidth { get; set; }
        public int TileHeight { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public List<TiledTilesetInfo> Tilesets { get; set; }
        public List<TiledLayerInfo> Layers { get; set; }
        public List<TiledObjectGroupInfo> ObjectGroups { get; set; }

        public TiledMapInfo()
        {
            Properties = new Dictionary<string, string>();
            Tilesets = new List<TiledTilesetInfo>();
            Layers = new List<TiledLayerInfo>();
            ObjectGroups = new List<TiledObjectGroupInfo>();
        }

        public static TiledMapInfo Parse(string xml)
        {
            var document = XDocument.Parse(xml);
            var map = document.Root;
            var info = new TiledMapInfo
            {
                Class = GetString(map, "class"),
                Orientation = GetString(map, "orientation"),
                Width = GetInt(map, "width"),
                Height = GetInt(map, "height"),
                TileWidth = GetInt(map, "tilewidth"),
                TileHeight = GetInt(map, "tileheight"),
                Properties = ReadProperties(map)
            };

            foreach (var tileset in map.Elements("tileset"))
            {
                info.Tilesets.Add(new TiledTilesetInfo
                {
                    FirstGid = GetInt(tileset, "firstgid"),
                    Name = GetString(tileset, "name"),
                    Source = GetString(tileset, "source"),
                    Document = GetString(tileset, "source") == null
                        ? TiledTilesetDocumentInfo.Parse(tileset.ToString())
                        : null
                });
            }

            foreach (var layer in map.Elements("layer"))
            {
                info.Layers.Add(new TiledLayerInfo
                {
                    Id = GetInt(layer, "id"),
                    Name = GetString(layer, "name"),
                    Width = GetInt(layer, "width"),
                    Height = GetInt(layer, "height"),
                    Visible = GetVisible(layer),
                    Properties = ReadProperties(layer)
                });
            }

            foreach (var objectGroup in map.Elements("objectgroup"))
            {
                var group = new TiledObjectGroupInfo
                {
                    Id = GetInt(objectGroup, "id"),
                    Name = GetString(objectGroup, "name"),
                    ObjectCount = CountElements(objectGroup, "object")
                };

                foreach (var mapObject in objectGroup.Elements("object"))
                {
                    group.Objects.Add(new TiledObjectInfo
                    {
                        Id = GetInt(mapObject, "id"),
                        Name = GetString(mapObject, "name"),
                        Class = GetString(mapObject, "class"),
                        Gid = GetGid(mapObject, "gid"),
                        X = GetFloat(mapObject, "x"),
                        Y = GetFloat(mapObject, "y"),
                        Width = GetFloat(mapObject, "width"),
                        Height = GetFloat(mapObject, "height"),
                        Properties = ReadProperties(mapObject)
                    });
                }

                info.ObjectGroups.Add(group);
            }

            return info;
        }

        private static Dictionary<string, string> ReadProperties(XElement element)
        {
            var result = new Dictionary<string, string>();
            var properties = element.Element("properties");
            if (properties == null)
            {
                return result;
            }

            foreach (var property in properties.Elements("property"))
            {
                var name = GetString(property, "name");
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                var value = GetString(property, "value");
                result[name] = value ?? property.Value;
            }

            return result;
        }

        private static string GetString(XElement element, string name)
        {
            var attribute = element.Attribute(name);
            return attribute == null ? null : attribute.Value;
        }

        private static int GetInt(XElement element, string name)
        {
            var value = GetString(element, name);
            int result;
            return int.TryParse(value, out result) ? result : 0;
        }

        private static int GetGid(XElement element, string name)
        {
            var value = GetString(element, name);
            uint result;
            return uint.TryParse(value, out result) ? (int)(result & TiledGidMask) : 0;
        }

        private static float GetFloat(XElement element, string name)
        {
            var value = GetString(element, name);
            float result;
            return float.TryParse(value, out result) ? result : 0;
        }

        private static bool GetVisible(XElement element)
        {
            var value = GetString(element, "visible");
            return value != "0";
        }

        private static int CountElements(XElement element, string name)
        {
            var count = 0;
            foreach (var ignored in element.Elements(name))
            {
                count++;
            }

            return count;
        }
    }

    public sealed class TiledTilesetInfo
    {
        public int FirstGid { get; set; }
        public string Name { get; set; }
        public string Source { get; set; }
        public TiledTilesetDocumentInfo Document { get; set; }
        public string UnityTilesetPath { get; set; }
        public string UnityImagePath { get; set; }
        public bool TilesetFound { get; set; }
        public bool ImageFound { get; set; }
    }

    public sealed class TiledTilesetDocumentInfo
    {
        public string Name { get; set; }
        public int TileWidth { get; set; }
        public int TileHeight { get; set; }
        public int TileCount { get; set; }
        public int Columns { get; set; }
        public int Spacing { get; set; }
        public int Margin { get; set; }
        public string ImageSource { get; set; }
        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public Dictionary<int, TiledTileInfo> Tiles { get; set; }
        public Dictionary<int, List<TiledTileAnimationFrameInfo>> Animations { get; set; }

        public static TiledTilesetDocumentInfo Parse(string xml)
        {
            var document = XDocument.Parse(xml);
            var tileset = document.Root;
            var image = tileset.Element("image");

            return new TiledTilesetDocumentInfo
            {
                Name = GetString(tileset, "name"),
                TileWidth = GetInt(tileset, "tilewidth"),
                TileHeight = GetInt(tileset, "tileheight"),
                TileCount = GetInt(tileset, "tilecount"),
                Columns = GetInt(tileset, "columns"),
                Spacing = GetInt(tileset, "spacing"),
                Margin = GetInt(tileset, "margin"),
                ImageSource = image == null ? null : GetString(image, "source"),
                ImageWidth = image == null ? 0 : GetInt(image, "width"),
                ImageHeight = image == null ? 0 : GetInt(image, "height"),
                Properties = ReadProperties(tileset),
                Tiles = ReadTiles(tileset),
                Animations = ReadAnimations(tileset)
            };
        }

        private static Dictionary<string, string> ReadProperties(XElement element)
        {
            var result = new Dictionary<string, string>();
            var properties = element.Element("properties");
            if (properties == null)
            {
                return result;
            }

            foreach (var property in properties.Elements("property"))
            {
                var name = GetString(property, "name");
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                result[name] = GetString(property, "value") ?? property.Value;
            }

            return result;
        }

        private static Dictionary<int, TiledTileInfo> ReadTiles(XElement tileset)
        {
            var result = new Dictionary<int, TiledTileInfo>();
            foreach (var tile in tileset.Elements("tile"))
            {
                var tileInfo = new TiledTileInfo
                {
                    Id = GetInt(tile, "id"),
                    Class = GetString(tile, "class") ?? GetString(tile, "type"),
                    Properties = ReadProperties(tile)
                };

                if (!string.IsNullOrEmpty(tileInfo.Class) || tileInfo.Properties.Count > 0)
                {
                    result[tileInfo.Id] = tileInfo;
                }
            }

            return result;
        }

        private static Dictionary<int, List<TiledTileAnimationFrameInfo>> ReadAnimations(XElement tileset)
        {
            var result = new Dictionary<int, List<TiledTileAnimationFrameInfo>>();
            foreach (var tile in tileset.Elements("tile"))
            {
                var animation = tile.Element("animation");
                if (animation == null)
                {
                    continue;
                }

                var frames = new List<TiledTileAnimationFrameInfo>();
                foreach (var frame in animation.Elements("frame"))
                {
                    frames.Add(new TiledTileAnimationFrameInfo
                    {
                        TileId = GetInt(frame, "tileid"),
                        Duration = GetInt(frame, "duration")
                    });
                }

                if (frames.Count > 0)
                {
                    result[GetInt(tile, "id")] = frames;
                }
            }

            return result;
        }

        private static string GetString(XElement element, string name)
        {
            var attribute = element.Attribute(name);
            return attribute == null ? null : attribute.Value;
        }

        private static int GetInt(XElement element, string name)
        {
            var value = GetString(element, name);
            int result;
            return int.TryParse(value, out result) ? result : 0;
        }
    }

    public sealed class TiledTileAnimationFrameInfo
    {
        public int TileId { get; set; }
        public int Duration { get; set; }
    }

    public sealed class TiledTileInfo
    {
        public int Id { get; set; }
        public string Class { get; set; }
        public Dictionary<string, string> Properties { get; set; }

        public TiledTileInfo()
        {
            Properties = new Dictionary<string, string>();
        }
    }

    public sealed class TiledLayerInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool Visible { get; set; }
        public Dictionary<string, string> Properties { get; set; }

        public TiledLayerInfo()
        {
            Properties = new Dictionary<string, string>();
        }
    }

    public sealed class TiledObjectGroupInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ObjectCount { get; set; }
        public List<TiledObjectInfo> Objects { get; set; }

        public TiledObjectGroupInfo()
        {
            Objects = new List<TiledObjectInfo>();
        }
    }

    public sealed class TiledObjectInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Class { get; set; }
        public int Gid { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public Dictionary<string, string> Properties { get; set; }

        public TiledObjectInfo()
        {
            Properties = new Dictionary<string, string>();
        }
    }
}
