using System;
using System.Collections;
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

        [SerializeField]
        private bool keyboardPanningEnabled;

        private int mapWidth;
        private int mapHeight;
        private readonly HashSet<string> fallbackBlockingLayerNames = new HashSet<string> { "wall", "water", "water2" };
        private HashSet<int> blockedTiles = new HashSet<int>();
        private Coroutine viewportScroll;

        public int StartColumn
        {
            get { return startColumn; }
        }

        public int StartRow
        {
            get { return startRow; }
        }

        public Vector3 ViewportOffset
        {
            get { return transform.position; }
        }

        private void Start()
        {
            RenderPreview();
        }

        private void Update()
        {
            if (!keyboardPanningEnabled)
            {
                return;
            }

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

            var nextStartColumn = Math.Max(0, Math.Min(startColumn + columnDelta, Math.Max(0, mapWidth - columns)));
            var nextStartRow = Math.Max(0, Math.Min(startRow + rowDelta, Math.Max(0, mapHeight - rows)));
            SetViewport(nextStartColumn, nextStartRow, true);
        }

        public void CenterOn(Redpoint.DungeonEscape.State.WorldPosition position)
        {
            var nextStartColumn = Math.Max(0, Math.Min((int)position.X - columns / 2, Math.Max(0, mapWidth - columns)));
            var nextStartRow = Math.Max(0, Math.Min((int)position.Y - rows / 2, Math.Max(0, mapHeight - rows)));
            SetViewport(nextStartColumn, nextStartRow, false);
        }

        public void EnsureVisible(Redpoint.DungeonEscape.State.WorldPosition position)
        {
            const int margin = 3;
            var column = (int)position.X;
            var row = (int)position.Y;
            var newStartColumn = startColumn;
            var newStartRow = startRow;

            if (column < startColumn + margin)
            {
                newStartColumn = column - margin;
            }
            else if (column >= startColumn + columns - margin)
            {
                newStartColumn = column - columns + margin + 1;
            }

            if (row < startRow + margin)
            {
                newStartRow = row - margin;
            }
            else if (row >= startRow + rows - margin)
            {
                newStartRow = row - rows + margin + 1;
            }

            newStartColumn = Math.Max(0, Math.Min(newStartColumn, Math.Max(0, mapWidth - columns)));
            newStartRow = Math.Max(0, Math.Min(newStartRow, Math.Max(0, mapHeight - rows)));

            if (newStartColumn == startColumn && newStartRow == startRow)
            {
                return;
            }

            SetViewport(newStartColumn, newStartRow, true);
        }

        public bool CanMoveTo(int column, int row)
        {
            if (column < 0 || row < 0 || column >= mapWidth || row >= mapHeight)
            {
                return false;
            }

            return !blockedTiles.Contains(row * mapWidth + column);
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
            var spriteSets = TiledTilesetSprites.LoadSpriteSets(tilesets, tileWidth, tileHeight);
            var renderedTileCount = 0;
            var layerOrder = 0;

            blockedTiles = TiledMapCollision.BuildBlockedTiles(
                map,
                bootstrap == null || bootstrap.Data == null ? null : bootstrap.Data.TestMap,
                mapWidth,
                mapHeight,
                fallbackBlockingLayerNames);
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
                        if (!TiledTilesetSprites.TryGetSprite(gid, spriteSets, out sprite))
                        {
                            continue;
                        }

                        var tileObject = new GameObject("Tile_" + GetString(layer, "name") + "_" + column + "_" + row);
                        tileObject.transform.SetParent(transform, false);
                        tileObject.transform.localPosition = new Vector3(column, -row, 0);

                        var renderer = tileObject.AddComponent<SpriteRenderer>();
                        renderer.sprite = sprite;
                        renderer.sortingOrder = layerOrder;
                        renderedTileCount++;
                    }
                }

                layerOrder++;
            }

            RenderObjectSprites(spriteSets, layerOrder + 10);
            PositionCamera(Math.Min(columns, mapWidth), rows);
            Debug.Log("Rendered TMX preview at " + startColumn + "," + startRow + " with " + layers.Count + " visible layers, " + spriteSets.Count + " tilesets, and " + renderedTileCount + " tiles.");
        }

        private void RenderObjectSprites(IList<TiledTilesetSpriteSet> spriteSets, int sortingOrder)
        {
            if (bootstrap == null || bootstrap.Data == null || bootstrap.Data.TestMap == null)
            {
                return;
            }

            foreach (var group in bootstrap.Data.TestMap.ObjectGroups)
            {
                foreach (var mapObject in group.Objects)
                {
                    if (mapObject.Gid == 0)
                    {
                        continue;
                    }

                    Sprite sprite;
                    if (!TiledTilesetSprites.TryGetSprite(mapObject.Gid, spriteSets, out sprite))
                    {
                        continue;
                    }

                    var column = Mathf.FloorToInt(mapObject.X / bootstrap.Data.TestMap.TileWidth);
                    var row = Mathf.FloorToInt((mapObject.Y - mapObject.Height) / bootstrap.Data.TestMap.TileHeight);

                    if (column < startColumn || column >= startColumn + columns ||
                        row < startRow || row >= startRow + rows)
                    {
                        continue;
                    }

                    var markerObject = new GameObject("Object_" + group.Name + "_" + mapObject.Name);
                    markerObject.transform.SetParent(transform, false);
                    markerObject.transform.localPosition = new Vector3(column - startColumn, -(row - startRow), -0.1f);

                    var renderer = markerObject.AddComponent<SpriteRenderer>();
                    renderer.sprite = sprite;
                    renderer.sortingOrder = sortingOrder;
                }
            }
        }

        private void ClearPreview()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }

        private void SetViewport(int newStartColumn, int newStartRow, bool animate)
        {
            var oldStartColumn = startColumn;
            var oldStartRow = startRow;
            startColumn = newStartColumn;
            startRow = newStartRow;

            RenderPreview();

            if (!animate)
            {
                StopViewportScroll();
                transform.position = Vector3.zero;
                return;
            }

            StartViewportScroll(oldStartColumn, oldStartRow, newStartColumn, newStartRow);
        }

        private void StartViewportScroll(int oldStartColumn, int oldStartRow, int newStartColumn, int newStartRow)
        {
            StopViewportScroll();

            var startOffset = new Vector3(
                newStartColumn - oldStartColumn,
                oldStartRow - newStartRow,
                0f);

            transform.position = startOffset;
            viewportScroll = StartCoroutine(AnimateViewportScroll(startOffset));
        }

        private IEnumerator AnimateViewportScroll(Vector3 startOffset)
        {
            const float duration = 0.15f;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var progress = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.Lerp(startOffset, Vector3.zero, progress);
                yield return null;
            }

            transform.position = Vector3.zero;
            viewportScroll = null;
        }

        private void StopViewportScroll()
        {
            if (viewportScroll == null)
            {
                return;
            }

            StopCoroutine(viewportScroll);
            viewportScroll = null;
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

    }
}
