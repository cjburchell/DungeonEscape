using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Redpoint.DungeonEscape.State;
using UnityEngine;

namespace Redpoint.DungeonEscape.Unity
{
    public sealed class TiledMapView : MonoBehaviour
    {
        private const uint TiledGidMask = 0x1FFFFFFF;
        private const float CameraMarginTiles = 4f;

        [SerializeField]
        private string mapAssetPath = "Assets/DungeonEscape/Maps/overworld.tmx";

        [SerializeField]
        private int columns = 32;

        [SerializeField]
        private int rows = 18;

        [SerializeField]
        private int startColumn = 25;

        [SerializeField]
        private int startRow = 20;

        private int mapWidth;
        private int mapHeight;
        private int objectSortingOrder = 10;
        private bool mapLoaded;
        private TiledLoadedMap currentMap;
        private string pooledMapAssetPath;
        private HashSet<int> blockedTiles = new HashSet<int>();
        private TiledSpriteRendererPool rendererPool;
        private DungeonEscapeGameState gameState;
        private readonly TiledMapViewport viewport = new TiledMapViewport();
        private readonly List<TiledNpcController> runtimeNpcs = new List<TiledNpcController>();
        private readonly Dictionary<int, TiledNpcController> occupiedNpcTiles = new Dictionary<int, TiledNpcController>();
        private string runtimeNpcMapAssetPath;
        private bool cameraWindowInitialized;
        private float cameraStartColumn;
        private float cameraStartRow;
        private float targetCameraStartColumn;
        private float targetCameraStartRow;

        public int StartColumn
        {
            get { return viewport.StartColumn; }
        }

        public int StartRow
        {
            get { return viewport.StartRow; }
        }

        public int ObjectSortingOrder
        {
            get { return objectSortingOrder; }
        }

        public int GetObjectSortingOrder(int row)
        {
            return objectSortingOrder * 1000 + row;
        }

        private void Start()
        {
            transform.position = Vector3.zero;
            ClearRenderedChildren();
            rendererPool = new TiledSpriteRendererPool(transform);
            RenderPreview();
        }

        private void LateUpdate()
        {
            if (!cameraWindowInitialized || mapWidth <= 0 || mapHeight <= 0)
            {
                return;
            }

            cameraStartColumn = targetCameraStartColumn;
            cameraStartRow = targetCameraStartRow;
            ApplyCameraPosition();
        }

        public void CenterOn(Redpoint.DungeonEscape.State.WorldPosition position)
        {
            EnsureMapLoaded();

            cameraStartColumn = ClampCameraStartColumn(position.X - viewport.Columns / 2f + 0.5f);
            cameraStartRow = ClampCameraStartRow(position.Y - viewport.Rows / 2f + 0.5f);
            targetCameraStartColumn = cameraStartColumn;
            targetCameraStartRow = cameraStartRow;
            cameraWindowInitialized = true;
            viewport.SetStart(Mathf.FloorToInt(cameraStartColumn), Mathf.FloorToInt(cameraStartRow));
            mapLoaded = false;
            RenderPreview();
            ApplyCameraPosition();
        }

        public void FollowPosition(Redpoint.DungeonEscape.State.WorldPosition position)
        {
            EnsureMapLoaded();

            if (!cameraWindowInitialized)
            {
                cameraStartColumn = viewport.StartColumn;
                cameraStartRow = viewport.StartRow;
                targetCameraStartColumn = cameraStartColumn;
                targetCameraStartRow = cameraStartRow;
                cameraWindowInitialized = true;
            }

            var columns = Math.Max(1, viewport.Columns);
            var rows = Math.Max(1, viewport.Rows);
            var deadzoneLeft = targetCameraStartColumn + CameraMarginTiles;
            var deadzoneRight = targetCameraStartColumn + columns - CameraMarginTiles - 1f;
            var deadzoneTop = targetCameraStartRow + CameraMarginTiles;
            var deadzoneBottom = targetCameraStartRow + rows - CameraMarginTiles - 1f;

            if (position.X < deadzoneLeft)
            {
                targetCameraStartColumn += position.X - deadzoneLeft;
            }
            else if (position.X > deadzoneRight)
            {
                targetCameraStartColumn += position.X - deadzoneRight;
            }

            if (position.Y < deadzoneTop)
            {
                targetCameraStartRow += position.Y - deadzoneTop;
            }
            else if (position.Y > deadzoneBottom)
            {
                targetCameraStartRow += position.Y - deadzoneBottom;
            }

            targetCameraStartColumn = ClampCameraStartColumn(targetCameraStartColumn);
            targetCameraStartRow = ClampCameraStartRow(targetCameraStartRow);
        }

        public bool CanMoveTo(int column, int row)
        {
            EnsureMapLoaded();

            if (column < 0 || row < 0 || column >= mapWidth || row >= mapHeight)
            {
                return false;
            }

            var tileKey = row * mapWidth + column;
            if (occupiedNpcTiles.ContainsKey(tileKey))
            {
                return false;
            }

            if (IsWaterTile(column, row))
            {
                return gameState != null &&
                       gameState.Party != null &&
                       IsCurrentMapOverworld() &&
                       gameState.Party.HasShip &&
                       !blockedTiles.Contains(tileKey);
            }

            return !blockedTiles.Contains(tileKey);
        }

        public bool IsWaterAt(WorldPosition position)
        {
            EnsureMapLoaded();
            return IsWaterTile(Mathf.FloorToInt(position.X), Mathf.FloorToInt(position.Y));
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
            return !IsWaterTile(column, row) &&
                   !blockedTiles.Contains(tileKey) &&
                   !IsPlayerAt(column, row) &&
                   (!occupiedNpcTiles.TryGetValue(tileKey, out occupiedNpc) || occupiedNpc == npc);
        }

        public int GetDamageAt(WorldPosition position)
        {
            EnsureMapLoaded();
            if (currentMap == null || currentMap.Root == null)
            {
                return 0;
            }

            var column = Mathf.FloorToInt(position.X);
            var row = Mathf.FloorToInt(position.Y);
            var damage = 0;
            foreach (var layer in currentMap.Root.Elements("layer"))
            {
                var gid = GetLayerGidAt(layer, column, row);
                if (gid == 0)
                {
                    continue;
                }

                string damageValue;
                int parsedDamage;
                var tileInfo = GetTileInfo(gid);
                if (tileInfo != null &&
                    tileInfo.Properties != null &&
                    tileInfo.Properties.TryGetValue("damage", out damageValue) &&
                    int.TryParse(damageValue, out parsedDamage))
                {
                    damage += Math.Max(0, parsedDamage);
                }
            }

            return damage;
        }

        public Biome GetBiomeAt(WorldPosition position)
        {
            return GetBiomeInfoAt(position).Type;
        }

        public BiomeInfo GetBiomeInfoAt(WorldPosition position)
        {
            EnsureMapLoaded();
            if (currentMap == null || currentMap.Root == null)
            {
                return new BiomeInfo { Type = Biome.None };
            }

            var column = Mathf.FloorToInt(position.X);
            var row = Mathf.FloorToInt(position.Y);
            foreach (var layer in currentMap.Root.Elements("layer"))
            {
                var gid = GetLayerGidAt(layer, column, row);
                if (gid == 0)
                {
                    continue;
                }

                var tileInfo = GetTileInfo(gid);
                Biome biome;
                if (tileInfo != null &&
                    !string.IsNullOrEmpty(tileInfo.Class) &&
                    TryParseBiome(tileInfo.Class, out biome))
                {
                    return new BiomeInfo
                    {
                        Type = biome,
                        MinMonsterLevel = GetTileIntProperty(tileInfo, "MinMonsterLevel"),
                        MaxMonsterLevel = GetTileIntProperty(tileInfo, "MaxMonsterLevel")
                    };
                }
            }

            return GetMapBiomeInfo();
        }

        public string CurrentMapId
        {
            get { return currentMap == null ? TiledMapLoader.NormalizeMapId(mapAssetPath) : TiledMapLoader.NormalizeMapId(currentMap.AssetPath); }
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

        public void RefreshObjectState()
        {
            EnsureMapLoaded();
            if (currentMap != null)
            {
                currentMap.BlockedTiles = TiledMapCollision.BuildBlockedTiles(
                    currentMap.Root,
                    currentMap.Info,
                    mapWidth,
                    mapHeight,
                    gameState,
                    TiledMapLoader.NormalizeMapId(currentMap.AssetPath));
                currentMap.BlockedTilesAllowShip = CanUseShipOnCurrentMap();
                blockedTiles = currentMap.BlockedTiles;
            }

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
            cameraWindowInitialized = false;
            ClearRuntimeNpcs();
            ClearRenderedChildren();
            rendererPool = new TiledSpriteRendererPool(transform);
            mapLoaded = false;
            currentMap = null;
            mapWidth = 0;
            mapHeight = 0;
            RenderPreview();
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
            var mapId = currentMap == null ? null : TiledMapLoader.NormalizeMapId(currentMap.AssetPath);

            if (currentMap == null || currentMap.Info == null || currentMap.Info.ObjectGroups == null)
            {
                return false;
            }

            var runtimeNpc = runtimeNpcs.FirstOrDefault(npc =>
                npc.MapObject != null &&
                Mathf.FloorToInt(position.X) == npc.Column &&
                Mathf.FloorToInt(position.Y) == npc.Row);
            if (runtimeNpc != null)
            {
                result = runtimeNpc.MapObject;
                return true;
            }

            foreach (var group in currentMap.Info.ObjectGroups)
            {
                foreach (var mapObject in group.Objects)
                {
                    if (!IsMapObjectVisible(mapId, mapObject))
                    {
                        continue;
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

        public void RemoveRuntimeNpc(TiledObjectInfo mapObject)
        {
            if (mapObject == null)
            {
                return;
            }

            var npc = runtimeNpcs.FirstOrDefault(item => item != null && item.ObjectId == mapObject.Id);
            if (npc == null)
            {
                return;
            }

            occupiedNpcTiles.Remove(npc.Row * mapWidth + npc.Column);
            runtimeNpcs.Remove(npc);
            Destroy(npc.gameObject);
        }

        public void FaceNpcAt(WorldPosition position, Direction direction)
        {
            var npc = runtimeNpcs.FirstOrDefault(item =>
                item != null &&
                Mathf.FloorToInt(position.X) == item.Column &&
                Mathf.FloorToInt(position.Y) == item.Row);
            if (npc != null)
            {
                npc.Face(direction);
            }
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

        private static bool IsNpcObject(TiledObjectInfo mapObject)
        {
            return mapObject != null &&
                   mapObject.Class != null &&
                   mapObject.Class.StartsWith("Npc", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsMapObjectVisible(string mapId, TiledObjectInfo mapObject)
        {
            if (IsDoorObject(mapObject) && gameState != null && gameState.IsObjectOpen(mapId, mapObject.Id))
            {
                return false;
            }

            if (IsHiddenItemObject(mapObject) && gameState != null && !gameState.CanPickupMapObject(mapObject))
            {
                return false;
            }

            if (!IsPartyMemberObject(mapObject))
            {
                return true;
            }

            return gameState == null || gameState.IsObjectActive(mapId, mapObject.Id);
        }

        private static bool IsPartyMemberObject(TiledObjectInfo mapObject)
        {
            return mapObject != null &&
                   string.Equals(mapObject.Class, "NpcPartyMember", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDoorObject(TiledObjectInfo mapObject)
        {
            return mapObject != null &&
                   string.Equals(mapObject.Class, "Door", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsHiddenItemObject(TiledObjectInfo mapObject)
        {
            return mapObject != null &&
                   string.Equals(mapObject.Class, "HiddenItem", StringComparison.OrdinalIgnoreCase);
        }

        private static int GetIntProperty(TiledObjectInfo mapObject, string propertyName, int defaultValue)
        {
            string value;
            int result;
            return mapObject.Properties != null &&
                   mapObject.Properties.TryGetValue(propertyName, out value) &&
                   int.TryParse(value, out result)
                ? result
                : defaultValue;
        }

        private static bool IsPlayerAt(int column, int row)
        {
            var player = FindAnyObjectByType<PlayerGridController>();
            return player != null && player.Column == column && player.Row == row;
        }

        private static bool IsTrueProperty(TiledObjectInfo mapObject, string propertyName)
        {
            string value;
            return mapObject.Properties != null &&
                   mapObject.Properties.TryGetValue(propertyName, out value) &&
                   string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTrueProperty(XElement element, string propertyName)
        {
            var properties = element == null ? null : element.Element("properties");
            if (properties == null)
            {
                return false;
            }

            foreach (var property in properties.Elements("property"))
            {
                var name = property.Attribute("name");
                if (name == null || !string.Equals(name.Value, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var value = property.Attribute("value");
                return string.Equals(value == null ? property.Value : value.Value, "true", StringComparison.OrdinalIgnoreCase);
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
            PlayMapMusic(loadedMap, false);
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

            var canUseShip = CanUseShipOnMap(TiledMapLoader.NormalizeMapId(loadedMap.AssetPath));
            if (loadedMap.BlockedTiles == null || loadedMap.BlockedTilesAllowShip != canUseShip)
            {
                loadedMap.BlockedTiles = TiledMapCollision.BuildBlockedTiles(
                    loadedMap.Root,
                    loadedMap.Info,
                    mapWidth,
                    mapHeight,
                    gameState,
                    TiledMapLoader.NormalizeMapId(loadedMap.AssetPath));
                loadedMap.BlockedTilesAllowShip = canUseShip;
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
                DungeonEscapeSettingsCache.Current.ShowHiddenObjects,
                TiledMapLoader.NormalizeMapId(loadedMap.AssetPath),
                gameState,
                out spritesSortingOrder);

            rendererPool.End();
            objectSortingOrder = spritesSortingOrder;
            SyncRuntimeNpcs(loadedMap, spriteSets);
            PositionCamera(Math.Min(viewport.Columns, mapWidth), rows);
            if (cameraWindowInitialized)
            {
                ApplyCameraPosition();
            }

            mapLoaded = true;
        }

        public void PlayCurrentMapMusic()
        {
            PlayMapMusic(currentMap, true);
        }

        private static void PlayMapMusic(TiledLoadedMap loadedMap, bool force)
        {
            if (loadedMap == null || loadedMap.Info == null || loadedMap.Info.Properties == null)
            {
                return;
            }

            if (!force && (DungeonEscapeSplashScreen.IsVisible || DungeonEscapeTitleMenu.IsOpen))
            {
                return;
            }

            string song;
            if (loadedMap.Info.Properties.TryGetValue("song", out song) && !string.IsNullOrEmpty(song))
            {
                DungeonEscapeAudio.GetOrCreate().PlayMapMusic(song);
            }
        }

        private void ApplyCameraPosition()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            var visibleColumns = Math.Min(viewport.Columns, Math.Max(1, mapWidth));
            var visibleRows = Math.Min(viewport.Rows, Math.Max(1, mapHeight));
            camera.transform.position = new Vector3(
                cameraStartColumn + (visibleColumns - 1) / 2f,
                -(cameraStartRow + (visibleRows - 1) / 2f),
                -10f);
        }

        private float ClampCameraStartColumn(float value)
        {
            return Mathf.Clamp(value, 0f, Math.Max(0, mapWidth - viewport.Columns));
        }

        private float ClampCameraStartRow(float value)
        {
            return Mathf.Clamp(value, 0f, Math.Max(0, mapHeight - viewport.Rows));
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
                if (child.GetComponent<TiledNpcController>() != null)
                {
                    continue;
                }

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

        private bool IsWaterTile(int column, int row)
        {
            if (currentMap == null || currentMap.Root == null)
            {
                return false;
            }

            foreach (var layer in currentMap.Root.Elements("layer"))
            {
                if (!IsTrueProperty(layer, "Water") || GetLayerGidAt(layer, column, row) == 0)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        private bool CanUseShipOnCurrentMap()
        {
            return CanUseShipOnMap(CurrentMapId);
        }

        private bool CanUseShipOnMap(string mapId)
        {
            return gameState != null &&
                   gameState.Party != null &&
                   gameState.Party.HasShip &&
                   string.Equals(TiledMapLoader.NormalizeMapId(mapId), "overworld", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsCurrentMapOverworld()
        {
            return string.Equals(CurrentMapId, "overworld", StringComparison.OrdinalIgnoreCase);
        }

        private int GetLayerGidAt(XElement layer, int column, int row)
        {
            if (currentMap == null || layer == null || column < 0 || row < 0 || column >= mapWidth || row >= mapHeight)
            {
                return 0;
            }

            var values = GetLayerGids(layer);
            var index = row * mapWidth + column;
            if (index < 0 || index >= values.Length)
            {
                return 0;
            }

            return values[index];
        }

        private int[] GetLayerGids(XElement layer)
        {
            if (currentMap.LayerGidCache == null)
            {
                currentMap.LayerGidCache = new Dictionary<XElement, int[]>();
            }

            int[] gids;
            if (currentMap.LayerGidCache.TryGetValue(layer, out gids))
            {
                return gids;
            }

            var data = layer.Element("data");
            if (data == null)
            {
                gids = new int[0];
            }
            else
            {
                var values = data.Value
                    .Split(new[] { ',', '\n', '\r', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                gids = new int[values.Length];
                for (var i = 0; i < values.Length; i++)
                {
                    gids[i] = ParseGid(values[i]);
                }
            }

            currentMap.LayerGidCache[layer] = gids;
            return gids;
        }

        private TiledTileInfo GetTileInfo(int gid)
        {
            if (currentMap == null || currentMap.Info == null || currentMap.Info.Tilesets == null || gid <= 0)
            {
                return null;
            }

            TiledTilesetInfo selected = null;
            foreach (var tileset in currentMap.Info.Tilesets)
            {
                if (tileset.FirstGid <= gid)
                {
                    selected = tileset;
                }
            }

            if (selected == null || selected.Document == null || selected.Document.Tiles == null)
            {
                return null;
            }

            TiledTileInfo tileInfo;
            return selected.Document.Tiles.TryGetValue(gid - selected.FirstGid, out tileInfo)
                ? tileInfo
                : null;
        }

        private Biome GetMapBiome()
        {
            return GetMapBiomeInfo().Type;
        }

        private BiomeInfo GetMapBiomeInfo()
        {
            if (currentMap == null || currentMap.Info == null || currentMap.Info.Properties == null)
            {
                return new BiomeInfo { Type = Biome.None };
            }

            string value;
            Biome biome;
            var info = new BiomeInfo
            {
                Type = currentMap.Info.Properties.TryGetValue("biome", out value) && TryParseBiome(value, out biome)
                    ? biome
                    : Biome.None
            };

            info.MinMonsterLevel = GetIntProperty(currentMap.Info.Properties, "MinMonsterLevel");
            info.MaxMonsterLevel = GetIntProperty(currentMap.Info.Properties, "MaxMonsterLevel");
            return info;
        }

        private static int GetTileIntProperty(TiledTileInfo tileInfo, string key)
        {
            return tileInfo == null || tileInfo.Properties == null ? 0 : GetIntProperty(tileInfo.Properties, key);
        }

        private static int GetIntProperty(IDictionary<string, string> properties, string key)
        {
            if (properties == null)
            {
                return 0;
            }

            string value;
            int result;
            return properties.TryGetValue(key, out value) && int.TryParse(value, out result) ? result : 0;
        }

        private static bool TryParseBiome(string value, out Biome biome)
        {
            return Enum.TryParse(value, true, out biome);
        }

        private static int ParseGid(string value)
        {
            uint result;
            return uint.TryParse(value, out result) ? (int)(result & TiledGidMask) : 0;
        }

        private void SyncRuntimeNpcs(TiledLoadedMap loadedMap, IList<TiledTilesetSpriteSet> spriteSets)
        {
            if (loadedMap == null || loadedMap.Info == null || loadedMap.Info.ObjectGroups == null)
            {
                return;
            }

            if (runtimeNpcMapAssetPath != loadedMap.AssetPath || !HasLiveRuntimeNpcs())
            {
                ClearRuntimeNpcs();
                runtimeNpcMapAssetPath = loadedMap.AssetPath;
                foreach (var group in loadedMap.Info.ObjectGroups)
                {
                    foreach (var mapObject in group.Objects)
                    {
                        if (!IsNpcObject(mapObject))
                        {
                            continue;
                        }

                        if (!IsMapObjectVisible(TiledMapLoader.NormalizeMapId(loadedMap.AssetPath), mapObject))
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

        private bool HasLiveRuntimeNpcs()
        {
            return runtimeNpcs.Any(npc => npc != null);
        }

        private bool ContainsTile(TiledObjectInfo mapObject, WorldPosition position)
        {
            if (currentMap == null || currentMap.Info == null)
            {
                return false;
            }

            if (mapObject.Gid != 0 && IsNpcObject(mapObject))
            {
                var objectTile = GetObjectTilePosition(mapObject);
                return Mathf.FloorToInt(position.X) == Mathf.FloorToInt(objectTile.X) &&
                       Mathf.FloorToInt(position.Y) == Mathf.FloorToInt(objectTile.Y);
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
            if (mapObject.Gid != 0 && IsNpcObject(mapObject))
            {
                return new WorldPosition(
                    Mathf.FloorToInt(mapObject.X / currentMap.Info.TileWidth),
                    Mathf.FloorToInt((mapObject.Y - 0.001f) / currentMap.Info.TileHeight));
            }

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
        }

    }

    public sealed class TiledMapWarp
    {
        public string Name { get; set; }
        public string MapId { get; set; }
        public string SpawnId { get; set; }
    }
}
