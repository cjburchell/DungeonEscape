using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class TiledMapView : MonoBehaviour
    {
        [SerializeField]
        private string mapAssetPath = "Assets/DungeonEscape/Maps/overworld.tmx";

        [SerializeField]
        private int columns = 20;

        [SerializeField]
        private int rows = 15;

        [SerializeField]
        private int startColumn = 25;

        [SerializeField]
        private int startRow = 20;

        [SerializeField]
        private bool keyboardPanningEnabled;

        private int mapWidth;
        private int mapHeight;
        private int objectSortingOrder = 10;
        private bool mapLoaded;
        private TiledLoadedMap currentMap;
        private readonly HashSet<string> fallbackBlockingLayerNames = new HashSet<string> { "wall", "water", "water2" };
        private HashSet<int> blockedTiles = new HashSet<int>();
        private Coroutine viewportScroll;
        private readonly TiledMapViewport viewport = new TiledMapViewport();

        public int StartColumn
        {
            get { return viewport.StartColumn; }
        }

        public int StartRow
        {
            get { return viewport.StartRow; }
        }

        public Vector3 ViewportOffset
        {
            get { return transform.position; }
        }

        public int ObjectSortingOrder
        {
            get { return objectSortingOrder; }
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

            Vector3 startOffset;
            if (viewport.PanBy(columnDelta, rowDelta, out startOffset))
            {
                SetViewport(startOffset, true);
            }
        }

        public void CenterOn(Redpoint.DungeonEscape.State.WorldPosition position)
        {
            EnsureMapLoaded();

            Vector3 startOffset;
            viewport.CenterOn(position, out startOffset);
            SetViewport(startOffset, false);
        }

        public void EnsureVisible(Redpoint.DungeonEscape.State.WorldPosition position)
        {
            EnsureMapLoaded();

            Vector3 startOffset;
            if (viewport.EnsureVisible(position, out startOffset))
            {
                SetViewport(startOffset, true);
            }
        }

        public bool CanMoveTo(int column, int row)
        {
            EnsureMapLoaded();

            if (column < 0 || row < 0 || column >= mapWidth || row >= mapHeight)
            {
                return false;
            }

            return !blockedTiles.Contains(row * mapWidth + column);
        }

        public void LoadMap(string mapIdOrAssetPath, string spawnId)
        {
            mapAssetPath = TiledMapLoader.NormalizeMapAssetPath(mapIdOrAssetPath);
            mapLoaded = false;
            currentMap = null;
            mapWidth = 0;
            mapHeight = 0;
            RenderPreview();
            StopViewportScroll();
            transform.position = Vector3.zero;

            WorldPosition spawnPosition;
            if (TryGetSpawnPosition(spawnId, out spawnPosition))
            {
                CenterOn(spawnPosition);
            }
        }

        public bool TryGetWarpAt(WorldPosition position, out TiledMapWarp warp)
        {
            EnsureMapLoaded();
            warp = null;

            if (currentMap == null || currentMap.Info == null || currentMap.Info.ObjectGroups == null)
            {
                return false;
            }

            foreach (var group in currentMap.Info.ObjectGroups)
            {
                foreach (var mapObject in group.Objects)
                {
                    if (mapObject.Class != "Warp" || !ContainsTile(mapObject, position))
                    {
                        continue;
                    }

                    string mapId;
                    if (mapObject.Properties == null ||
                        !mapObject.Properties.TryGetValue("WarpMap", out mapId) ||
                        string.IsNullOrEmpty(mapId))
                    {
                        continue;
                    }

                    string spawnId;
                    mapObject.Properties.TryGetValue("SpawnId", out spawnId);
                    warp = new TiledMapWarp
                    {
                        MapId = mapId,
                        SpawnId = spawnId,
                        Name = mapObject.Name
                    };
                    return true;
                }
            }

            return false;
        }

        public bool TryGetSpawnPosition(string spawnId, out WorldPosition position)
        {
            EnsureMapLoaded();
            position = WorldPosition.Zero;
            var targetSpawnId = string.IsNullOrEmpty(spawnId) ? "spawn" : spawnId;

            if (currentMap == null || currentMap.Info == null || currentMap.Info.ObjectGroups == null)
            {
                return false;
            }

            foreach (var group in currentMap.Info.ObjectGroups)
            {
                foreach (var mapObject in group.Objects)
                {
                    if (mapObject.Class != "Spawn" || mapObject.Name != targetSpawnId)
                    {
                        continue;
                    }

                    position = new WorldPosition(
                        Mathf.FloorToInt(mapObject.X / currentMap.Info.TileWidth),
                        Mathf.FloorToInt((mapObject.Y - mapObject.Height) / currentMap.Info.TileHeight));
                    return true;
                }
            }

            return false;
        }

        private void EnsureMapLoaded()
        {
            if (mapLoaded)
            {
                return;
            }

            RenderPreview();
        }

        private void RenderPreview()
        {
            var loadedMap = TiledMapLoader.Load(mapAssetPath);
            if (loadedMap == null)
            {
                return;
            }

            currentMap = loadedMap;
            var viewportStartColumn = mapWidth == 0 && mapHeight == 0 ? startColumn : viewport.StartColumn;
            var viewportStartRow = mapWidth == 0 && mapHeight == 0 ? startRow : viewport.StartRow;
            mapWidth = loadedMap.Width;
            mapHeight = loadedMap.Height;
            viewport.Initialize(mapWidth, mapHeight, columns, rows, viewportStartColumn, viewportStartRow);
            var tilesets = GetValidatedTilesets();
            var spriteSets = TiledTilesetSprites.LoadSpriteSets(tilesets, loadedMap.TileWidth, loadedMap.TileHeight);

            blockedTiles = TiledMapCollision.BuildBlockedTiles(
                loadedMap.Root,
                loadedMap.Info,
                mapWidth,
                mapHeight,
                fallbackBlockingLayerNames);
            ClearPreview();

            var renderedTileCount = TiledMapRenderer.RenderVisibleTileLayers(
                transform,
                loadedMap.VisibleLayers,
                spriteSets,
                mapWidth,
                mapHeight,
                viewport.StartColumn,
                viewport.StartRow,
                columns,
                rows);

            objectSortingOrder = loadedMap.VisibleLayers.Count + 10;
            TiledMapRenderer.RenderObjectSprites(
                transform,
                loadedMap.Info,
                spriteSets,
                viewport.StartColumn,
                viewport.StartRow,
                columns,
                rows,
                objectSortingOrder);
            PositionCamera(Math.Min(columns, mapWidth), rows);
            mapLoaded = true;
            Debug.Log("Rendered TMX preview at " + viewport.StartColumn + "," + viewport.StartRow + " with " + loadedMap.VisibleLayers.Count + " visible layers, " + spriteSets.Count + " tilesets, and " + renderedTileCount + " tiles.");
        }

        private void ClearPreview()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }

        private void SetViewport(Vector3 startOffset, bool animate)
        {
            mapLoaded = false;
            RenderPreview();

            if (!animate)
            {
                StopViewportScroll();
                transform.position = Vector3.zero;
                return;
            }

            StartViewportScroll(startOffset);
        }

        private void StartViewportScroll(Vector3 startOffset)
        {
            StopViewportScroll();

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

        private bool ContainsTile(TiledObjectInfo mapObject, WorldPosition position)
        {
            if (currentMap == null || currentMap.Info == null)
            {
                return false;
            }

            var centerX = position.X * currentMap.Info.TileWidth + currentMap.Info.TileWidth / 2f;
            var centerY = position.Y * currentMap.Info.TileHeight + currentMap.Info.TileHeight / 2f;
            return centerX >= mapObject.X &&
                   centerX < mapObject.X + mapObject.Width &&
                   centerY >= mapObject.Y - mapObject.Height &&
                   centerY < mapObject.Y;
        }

        private List<TiledTilesetInfo> GetValidatedTilesets()
        {
            if (currentMap != null && currentMap.Info != null && currentMap.Info.Tilesets != null)
            {
                return currentMap.Info.Tilesets
                    .Where(tileset => tileset.TilesetFound && tileset.ImageFound && tileset.Document != null)
                    .OrderBy(tileset => tileset.FirstGid)
                    .ToList();
            }

            return new List<TiledTilesetInfo>();
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

    }

    public sealed class TiledMapWarp
    {
        public string Name { get; set; }
        public string MapId { get; set; }
        public string SpawnId { get; set; }
    }
}
