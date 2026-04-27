using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class TiledMapPreviewRenderer : MonoBehaviour
    {
        [SerializeField]
        private string mapAssetPath = "Assets/DungeonEscape/Maps/overworld.tmx";

        [SerializeField]
        private string textureAssetPath = "Assets/DungeonEscape/Images/tiles/overworld.png";

        [SerializeField]
        private DungeonEscapeBootstrap bootstrap;

        [SerializeField]
        private int columns = 20;

        [SerializeField]
        private int rows = 15;

        [SerializeField]
        private int startColumn = 25;

        [SerializeField]
        private int startRow = 20;

        [SerializeField]
        private int firstGid = 1;

        private int mapWidth;
        private int mapHeight;

        private void Start()
        {
            RenderPreview();
        }

        private void Update()
        {
            var columnDelta = 0;
            var rowDelta = 0;

            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                columnDelta = -1;
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                columnDelta = 1;
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                rowDelta = -1;
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                rowDelta = 1;
            }

            if (columnDelta == 0 && rowDelta == 0)
            {
                return;
            }

            startColumn = Math.Max(0, Math.Min(startColumn + columnDelta, Math.Max(0, mapWidth - columns)));
            startRow = Math.Max(0, Math.Min(startRow + rowDelta, Math.Max(0, mapHeight - rows)));
            RenderPreview();
        }

        private void RenderPreview()
        {
            var mapPath = ToFullAssetPath(mapAssetPath);

            if (!File.Exists(mapPath))
            {
                Debug.LogError("Map preview TMX not found: " + mapAssetPath);
                return;
            }

            var mapDocument = XDocument.Parse(File.ReadAllText(mapPath));
            var map = mapDocument.Root;
            var tileWidth = GetInt(map, "tilewidth");
            var tileHeight = GetInt(map, "tileheight");
            mapWidth = GetInt(map, "width");
            mapHeight = GetInt(map, "height");

            var layers = map.Elements("layer").Where(IsRenderableLayer).ToList();
            if (layers.Count == 0)
            {
                Debug.LogError("Map preview could not find a tile layer.");
                return;
            }

            var tilesets = GetValidatedTilesets();
            var spriteSets = LoadSpriteSets(tilesets, tileWidth, tileHeight);
            var renderedTileCount = 0;
            var layerOrder = 0;

            ClearPreview();

            foreach (var layer in layers)
            {
                var gids = ParseCsvTileData(layer);
                var clampedStartColumn = Math.Max(0, Math.Min(startColumn, mapWidth - 1));
                var clampedStartRow = Math.Max(0, Math.Min(startRow, mapHeight - 1));
                var visibleColumns = Math.Min(columns, mapWidth - clampedStartColumn);
                var visibleRows = Math.Min(rows, mapHeight - clampedStartRow);

                for (var row = 0; row < visibleRows; row++)
                {
                    for (var column = 0; column < visibleColumns; column++)
                    {
                        var sourceColumn = clampedStartColumn + column;
                        var sourceRow = clampedStartRow + row;
                        var gid = gids[sourceRow * mapWidth + sourceColumn];
                        if (gid == 0)
                        {
                            continue;
                        }

                        Sprite sprite;
                        if (!TryGetSprite(gid, spriteSets, out sprite))
                        {
                            continue;
                        }

                        var tileObject = new GameObject("Tile_" + GetString(layer, "name") + "_" + column + "_" + row);
                        tileObject.transform.SetParent(transform, false);
                        tileObject.transform.position = new Vector3(column, -row, 0);

                        var renderer = tileObject.AddComponent<SpriteRenderer>();
                        renderer.sprite = sprite;
                        renderer.sortingOrder = layerOrder;
                        renderedTileCount++;
                    }
                }

                layerOrder++;
            }

            RenderObjectMarkers(layerOrder + 10);
            PositionCamera(Math.Min(columns, mapWidth), rows);
            Debug.Log("Rendered TMX preview at " + startColumn + "," + startRow + " with " + layers.Count + " visible layers, " + spriteSets.Count + " tilesets, and " + renderedTileCount + " tiles.");
        }

        private void RenderObjectMarkers(int sortingOrder)
        {
            if (bootstrap == null || bootstrap.Data == null || bootstrap.Data.TestMap == null)
            {
                return;
            }

            foreach (var group in bootstrap.Data.TestMap.ObjectGroups)
            {
                foreach (var mapObject in group.Objects)
                {
                    var column = Mathf.FloorToInt(mapObject.X / bootstrap.Data.TestMap.TileWidth);
                    var row = Mathf.FloorToInt((mapObject.Y - mapObject.Height) / bootstrap.Data.TestMap.TileHeight);

                    if (column < startColumn || column >= startColumn + columns ||
                        row < startRow || row >= startRow + rows)
                    {
                        continue;
                    }

                    var markerObject = new GameObject("Object_" + group.Name + "_" + mapObject.Name);
                    markerObject.transform.SetParent(transform, false);
                    markerObject.transform.position = new Vector3(column - startColumn, -(row - startRow), -0.1f);
                    markerObject.transform.localScale = new Vector3(0.45f, 0.45f, 1f);

                    var renderer = markerObject.AddComponent<SpriteRenderer>();
                    renderer.sprite = CreateMarkerSprite();
                    renderer.color = GetMarkerColor(mapObject.Class);
                    renderer.sortingOrder = sortingOrder;
                }
            }
        }

        private static Sprite markerSprite;

        private static Sprite CreateMarkerSprite()
        {
            if (markerSprite != null)
            {
                return markerSprite;
            }

            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            markerSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return markerSprite;
        }

        private static Color GetMarkerColor(string objectClass)
        {
            if (string.Equals(objectClass, "Warp", StringComparison.OrdinalIgnoreCase))
            {
                return Color.magenta;
            }

            if (string.Equals(objectClass, "Monster", StringComparison.OrdinalIgnoreCase))
            {
                return Color.red;
            }

            return Color.yellow;
        }

        private void ClearPreview()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }

        private static bool IsRenderableLayer(XElement layer)
        {
            return GetString(layer, "visible") != "0";
        }

        private List<TiledTilesetInfo> GetValidatedTilesets()
        {
            if (bootstrap == null)
            {
                bootstrap = FindObjectOfType<DungeonEscapeBootstrap>();
            }

            if (bootstrap != null && bootstrap.Data != null && bootstrap.Data.TestMap != null)
            {
                return bootstrap.Data.TestMap.Tilesets
                    .Where(tileset => tileset.TilesetFound && tileset.ImageFound && tileset.Document != null)
                    .OrderBy(tileset => tileset.FirstGid)
                    .ToList();
            }

            return new List<TiledTilesetInfo>
            {
                new TiledTilesetInfo
                {
                    FirstGid = firstGid,
                    UnityImagePath = textureAssetPath,
                    ImageFound = File.Exists(ToFullAssetPath(textureAssetPath)),
                    Document = new TiledTilesetDocumentInfo
                    {
                        Name = "overworld",
                        TileWidth = 32,
                        TileHeight = 32
                    }
                }
            };
        }

        private static List<TilesetSpriteSet> LoadSpriteSets(IEnumerable<TiledTilesetInfo> tilesets, int fallbackTileWidth, int fallbackTileHeight)
        {
            var spriteSets = new List<TilesetSpriteSet>();

            foreach (var tileset in tilesets)
            {
                if (string.IsNullOrEmpty(tileset.UnityImagePath))
                {
                    continue;
                }

                var texturePath = ToFullAssetPath(tileset.UnityImagePath);
                if (!File.Exists(texturePath))
                {
                    continue;
                }

                var tileWidth = tileset.Document.TileWidth == 0 ? fallbackTileWidth : tileset.Document.TileWidth;
                var tileHeight = tileset.Document.TileHeight == 0 ? fallbackTileHeight : tileset.Document.TileHeight;
                var texture = LoadTexture(texturePath);
                spriteSets.Add(new TilesetSpriteSet
                {
                    FirstGid = tileset.FirstGid,
                    Sprites = SliceTexture(texture, tileWidth, tileHeight)
                });
            }

            return spriteSets.OrderBy(set => set.FirstGid).ToList();
        }

        private static bool TryGetSprite(int gid, IList<TilesetSpriteSet> spriteSets, out Sprite sprite)
        {
            sprite = null;

            TilesetSpriteSet selected = null;
            foreach (var spriteSet in spriteSets)
            {
                if (spriteSet.FirstGid <= gid)
                {
                    selected = spriteSet;
                }
                else
                {
                    break;
                }
            }

            if (selected == null)
            {
                return false;
            }

            return selected.Sprites.TryGetValue(gid - selected.FirstGid, out sprite);
        }

        private static Texture2D LoadTexture(string path)
        {
            var bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2);
            texture.filterMode = FilterMode.Point;
            texture.LoadImage(bytes);
            return texture;
        }

        private static Dictionary<int, Sprite> SliceTexture(Texture2D texture, int tileWidth, int tileHeight)
        {
            var sprites = new Dictionary<int, Sprite>();
            var columns = texture.width / tileWidth;
            var rows = texture.height / tileHeight;

            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    var tileId = row * columns + column;
                    var rect = new Rect(
                        column * tileWidth,
                        texture.height - ((row + 1) * tileHeight),
                        tileWidth,
                        tileHeight);

                    sprites[tileId] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), tileWidth);
                }
            }

            return sprites;
        }

        private static List<int> ParseCsvTileData(XElement layer)
        {
            var data = layer.Element("data");
            if (data == null)
            {
                return new List<int>();
            }

            return data.Value
                .Split(new[] {',', '\n', '\r', '\t', ' '}, StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .ToList();
        }

        private static void PositionCamera(int visibleColumns, int visibleRows)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            camera.orthographic = true;
            camera.orthographicSize = visibleRows / 2f + 1f;
            camera.transform.position = new Vector3((visibleColumns - 1) / 2f, -(visibleRows - 1) / 2f, -10);
        }

        private static string ToFullAssetPath(string assetPath)
        {
            return Path.Combine(Application.dataPath, assetPath.Replace("Assets/", ""));
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

        private sealed class TilesetSpriteSet
        {
            public int FirstGid { get; set; }
            public Dictionary<int, Sprite> Sprites { get; set; }
        }
    }
}
