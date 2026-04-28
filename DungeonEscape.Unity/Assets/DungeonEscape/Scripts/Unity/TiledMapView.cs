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
        private string pooledMapAssetPath;
        private HashSet<int> blockedTiles = new HashSet<int>();
        private Coroutine viewportScroll;
        private TiledSpriteRendererPool rendererPool;
        private DungeonEscapeGameState gameState;
        private readonly TiledMapViewport viewport = new TiledMapViewport();
        private readonly List<TiledNpcController> runtimeNpcs = new List<TiledNpcController>();
        private readonly Dictionary<int, TiledNpcController> occupiedNpcTiles = new Dictionary<int, TiledNpcController>();
        private string runtimeNpcMapAssetPath;

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
            ClearRenderedChildren();
            rendererPool = new TiledSpriteRendererPool(transform);
            RenderPreview();
        }

        private void Update()
        {
            if (!keyboardPanningEnabled)
            {
                if (Input.GetKeyDown(KeyCode.F5))
                {
                    ReloadMapAssets();
                }

                return;
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                ReloadMapAssets();
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

            var tileKey = row * mapWidth + column;
            return !blockedTiles.Contains(tileKey) && !occupiedNpcTiles.ContainsKey(tileKey);
        }

        public bool CanNpcMoveTo(int column, int row, TiledNpcController npc)
        {
            EnsureMapLoaded();

            if (column < 0 || row < 0 || column >= mapWidth || row >= mapHeight)
            {
                return false;
            }

            var tileKey = row * mapWidth + column;
            TiledNpcController occupiedNpc;
            return !blockedTiles.Contains(tileKey) &&
                   !IsPlayerAt(column, row) &&
                   (!occupiedNpcTiles.TryGetValue(tileKey, out occupiedNpc) || occupiedNpc == npc);
        }

        public void UpdateNpcTile(TiledNpcController npc, int oldColumn, int oldRow, int newColumn, int newRow)
        {
            if (npc == null)
            {
                return;
            }

            occupiedNpcTiles.Remove(oldRow * mapWidth + oldColumn);
            occupiedNpcTiles[newRow * mapWidth + newColumn] = npc;
        }

        public void ReloadMapAssets()
        {
            TiledMapLoader.ClearCache();
            TiledTilesetSprites.ClearCache();
            ClearRuntimeNpcs();
            ClearRenderedChildren();
            rendererPool = new TiledSpriteRendererPool(transform);
            mapLoaded = false;
            currentMap = null;
            RenderPreview();
        }

        public void RefreshRender()
        {
            mapLoaded = false;
            RenderPreview();
        }

        public void LoadMap(string mapIdOrAssetPath, string spawnId)
        {
            LoadMap(mapIdOrAssetPath, spawnId, true);
        }

        public void LoadMap(string mapIdOrAssetPath, string spawnId, bool centerOnSpawn)
        {
            mapAssetPath = TiledMapLoader.NormalizeMapAssetPath(mapIdOrAssetPath);
            ClearRuntimeNpcs();
            ClearRenderedChildren();
            rendererPool = new TiledSpriteRendererPool(transform);
            mapLoaded = false;
            currentMap = null;
            mapWidth = 0;
            mapHeight = 0;
            RenderPreview();
            StopViewportScroll();
            transform.position = Vector3.zero;

            WorldPosition spawnPosition;
            if (centerOnSpawn && TryGetSpawnPosition(spawnId, out spawnPosition))
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

        public bool TryGetObjectAt(WorldPosition position, out TiledObjectInfo result)
        {
            EnsureMapLoaded();
            result = null;

            if (currentMap == null || currentMap.Info == null || currentMap.Info.ObjectGroups == null)
            {
                return false;
            }

            foreach (var group in currentMap.Info.ObjectGroups)
            {
                foreach (var mapObject in group.Objects)
                {
                    var runtimeNpc = runtimeNpcs.FirstOrDefault(npc =>
                        npc.MapObject != null &&
                        Mathf.FloorToInt(position.X) == npc.Column &&
                        Mathf.FloorToInt(position.Y) == npc.Row);
                    if (runtimeNpc != null)
                    {
                        result = runtimeNpc.MapObject;
                        return true;
                    }

                    if (IsSpawnObject(mapObject) || !ContainsTile(mapObject, position))
                    {
                        continue;
                    }

                    result = mapObject;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetSpawnPosition(string spawnId, out WorldPosition position)
        {
            EnsureMapLoaded();
            position = WorldPosition.Zero;
            if (currentMap == null || currentMap.Info == null || currentMap.Info.ObjectGroups == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(spawnId))
            {
                if (TryGetDefaultSpawnPosition(out position))
                {
                    return true;
                }

                Debug.LogWarning("Default spawn not found. Mark one Spawn object with DefaultSpawn=true in " + currentMap.AssetPath + ".");
                return false;
            }

            foreach (var group in currentMap.Info.ObjectGroups)
            {
                foreach (var mapObject in group.Objects)
                {
                    if (!IsSpawnObject(mapObject) || mapObject.Name != spawnId)
                    {
                        continue;
                    }

                    position = GetObjectTilePosition(mapObject);
                    return true;
                }
            }

            Debug.LogWarning("Spawn not found in " + currentMap.AssetPath + ": " + spawnId);
            return false;
        }

        public bool TryGetFirstSpawnPosition(out WorldPosition position)
        {
            EnsureMapLoaded();
            position = WorldPosition.Zero;

            if (currentMap == null || currentMap.Info == null || currentMap.Info.ObjectGroups == null)
            {
                return false;
            }

            foreach (var group in currentMap.Info.ObjectGroups)
            {
                foreach (var mapObject in group.Objects)
                {
                    if (!IsSpawnObject(mapObject) || !IsTrueProperty(mapObject, "DefaultSpawn"))
                    {
                        continue;
                    }

                    position = GetObjectTilePosition(mapObject);
                    return true;
                }
            }

            return false;
        }

        private bool TryGetDefaultSpawnPosition(out WorldPosition position)
        {
            position = WorldPosition.Zero;

            foreach (var group in currentMap.Info.ObjectGroups)
            {
                foreach (var mapObject in group.Objects)
                {
                    if (!IsSpawnObject(mapObject) || !IsTrueProperty(mapObject, "DefaultSpawn"))
                    {
                        continue;
                    }

                    position = GetObjectTilePosition(mapObject);
                    return true;
                }
            }

            return false;
        }

        private static bool IsSpawnObject(TiledObjectInfo mapObject)
        {
            return string.Equals(mapObject.Class, "Spawn", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsMovingNpcObject(TiledObjectInfo mapObject)
        {
            return mapObject != null &&
                   mapObject.Class != null &&
                   mapObject.Class.StartsWith("Npc", StringComparison.OrdinalIgnoreCase) &&
                   IsTrueProperty(mapObject, "CanMove");
        }

        private static bool IsPlayerAt(int column, int row)
        {
            var player = FindObjectOfType<PlayerGridController>();
            return player != null && player.Column == column && player.Row == row;
        }

        private static bool IsTrueProperty(TiledObjectInfo mapObject, string propertyName)
        {
            string value;
            return mapObject.Properties != null &&
                   mapObject.Properties.TryGetValue(propertyName, out value) &&
                   string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
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
            if (gameState == null)
            {
                gameState = DungeonEscapeGameState.GetOrCreate();
            }

            if (gameState != null)
            {
                gameState.InitializeMapObjects(TiledMapLoader.NormalizeMapId(loadedMap.AssetPath), loadedMap.Info);
            }

            if (pooledMapAssetPath != loadedMap.AssetPath)
            {
                ClearRuntimeNpcs();
                ClearRenderedChildren();
                rendererPool = new TiledSpriteRendererPool(transform);
                pooledMapAssetPath = loadedMap.AssetPath;
            }

            var viewportStartColumn = mapWidth == 0 && mapHeight == 0 ? startColumn : viewport.StartColumn;
            var viewportStartRow = mapWidth == 0 && mapHeight == 0 ? startRow : viewport.StartRow;
            mapWidth = loadedMap.Width;
            mapHeight = loadedMap.Height;
            var viewportColumns = GetViewportColumns();
            viewport.Initialize(mapWidth, mapHeight, viewportColumns, rows, viewportStartColumn, viewportStartRow);
            var tilesets = GetValidatedTilesets();
            var spriteSets = TiledTilesetSprites.LoadSpriteSets(tilesets, loadedMap.TileWidth, loadedMap.TileHeight);

            if (loadedMap.BlockedTiles == null)
            {
                loadedMap.BlockedTiles = TiledMapCollision.BuildBlockedTiles(
                    loadedMap.Root,
                    loadedMap.Info,
                    mapWidth,
                    mapHeight);
            }

            blockedTiles = loadedMap.BlockedTiles;
            EnsureRendererPool();
            rendererPool.Begin();

            int spritesSortingOrder;
            var renderedSpriteCount = TiledMapRenderer.RenderVisibleLayers(
                rendererPool,
                loadedMap.RenderableElements,
                spriteSets,
                mapWidth,
                mapHeight,
                loadedMap.TileWidth,
                loadedMap.TileHeight,
                viewport.StartColumn,
                viewport.StartRow,
                viewport.Columns,
                rows,
                DungeonEscapeSettingsCache.Current.ShowHiddenObjects,
                TiledMapLoader.NormalizeMapId(loadedMap.AssetPath),
                gameState,
                out spritesSortingOrder);

            rendererPool.End();
            objectSortingOrder = spritesSortingOrder;
            SyncRuntimeNpcs(loadedMap, spriteSets);
            PositionCamera(Math.Min(viewport.Columns, mapWidth), rows);
            mapLoaded = true;
        }

        private void EnsureRendererPool()
        {
            if (rendererPool == null)
            {
                rendererPool = new TiledSpriteRendererPool(transform);
            }
        }

        private void ClearRenderedChildren()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.GetComponent<SpriteRenderer>() != null)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private void ClearRuntimeNpcs()
        {
            foreach (var npc in runtimeNpcs)
            {
                if (npc != null)
                {
                    Destroy(npc.gameObject);
                }
            }

            runtimeNpcs.Clear();
            occupiedNpcTiles.Clear();
            runtimeNpcMapAssetPath = null;
        }

        private void SyncRuntimeNpcs(TiledLoadedMap loadedMap, IList<TiledTilesetSpriteSet> spriteSets)
        {
            if (loadedMap == null || loadedMap.Info == null || loadedMap.Info.ObjectGroups == null)
            {
                return;
            }

            if (runtimeNpcMapAssetPath != loadedMap.AssetPath)
            {
                ClearRuntimeNpcs();
                runtimeNpcMapAssetPath = loadedMap.AssetPath;
                foreach (var group in loadedMap.Info.ObjectGroups)
                {
                    foreach (var mapObject in group.Objects)
                    {
                        if (!IsMovingNpcObject(mapObject))
                        {
                            continue;
                        }

                        var npcObject = new GameObject("Npc_" + mapObject.Name + "_" + mapObject.Id);
                        npcObject.transform.SetParent(transform, false);
                        var npc = npcObject.AddComponent<TiledNpcController>();
                        npc.Configure(
                            this,
                            mapObject,
                            spriteSets,
                            loadedMap.TileWidth,
                            loadedMap.TileHeight,
                            objectSortingOrder,
                            TiledMapLoader.NormalizeMapId(loadedMap.AssetPath),
                            gameState);
                        runtimeNpcs.Add(npc);
                        occupiedNpcTiles[npc.Row * mapWidth + npc.Column] = npc;
                    }
                }
            }

            foreach (var npc in runtimeNpcs)
            {
                if (npc != null)
                {
                    npc.UpdateVisualPosition();
                }
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
            var topY = mapObject.Gid == 0 ? mapObject.Y : mapObject.Y - mapObject.Height;
            var bottomY = mapObject.Gid == 0 ? mapObject.Y + mapObject.Height : mapObject.Y;
            return centerX >= mapObject.X &&
                   centerX < mapObject.X + mapObject.Width &&
                   centerY >= topY &&
                   centerY < bottomY;
        }

        private WorldPosition GetObjectTilePosition(TiledObjectInfo mapObject)
        {
            var row = mapObject.Gid == 0
                ? Mathf.FloorToInt(mapObject.Y / currentMap.Info.TileHeight)
                : Mathf.FloorToInt((mapObject.Y - mapObject.Height) / currentMap.Info.TileHeight);

            return new WorldPosition(
                Mathf.FloorToInt(mapObject.X / currentMap.Info.TileWidth),
                row);
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

        private int GetViewportColumns()
        {
            var camera = Camera.main;
            var aspect = camera == null ? (float)Screen.width / Math.Max(1, Screen.height) : camera.aspect;
            var aspectColumns = Mathf.CeilToInt(rows * aspect);
            return Math.Max(columns, aspectColumns);
        }

        private static void PositionCamera(int visibleColumns, int visibleRows)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            camera.orthographic = true;
            camera.orthographicSize = visibleRows / 2f;
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
