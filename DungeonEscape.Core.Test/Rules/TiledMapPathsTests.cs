using Redpoint.DungeonEscape.Rules;
using Xunit;

namespace DungeonEscape.Core.Test.Rules
{
    public sealed class TiledMapPathsTests
    {
        [Theory]
        [InlineData(null, "Assets/DungeonEscape/Maps/overworld.tmx")]
        [InlineData("", "Assets/DungeonEscape/Maps/overworld.tmx")]
        [InlineData("overworld", "Assets/DungeonEscape/Maps/overworld.tmx")]
        [InlineData("maps/towns/isis", "Assets/DungeonEscape/Maps/towns/isis.tmx")]
        [InlineData("towns\\isis.tmx", "Assets/DungeonEscape/Maps/towns/isis.tmx")]
        [InlineData("Assets/DungeonEscape/Maps/towns/isis", "Assets/DungeonEscape/Maps/towns/isis.tmx")]
        [InlineData("Assets/DungeonEscape/Maps/towns/isis.tmx", "Assets/DungeonEscape/Maps/towns/isis.tmx")]
        public void NormalizeMapAssetPathAcceptsMapIdsAndAssetPaths(string input, string expected)
        {
            Assert.Equal(expected, TiledMapPaths.NormalizeMapAssetPath(input));
        }

        [Theory]
        [InlineData(null, "overworld")]
        [InlineData("", "overworld")]
        [InlineData("overworld", "overworld")]
        [InlineData("maps/towns/isis", "towns/isis")]
        [InlineData("towns\\isis.tmx", "towns/isis")]
        [InlineData("Assets/DungeonEscape/Maps/towns/isis.tmx", "towns/isis")]
        public void NormalizeMapIdAcceptsMapIdsAndAssetPaths(string input, string expected)
        {
            Assert.Equal(expected, TiledMapPaths.NormalizeMapId(input));
        }

        [Theory]
        [InlineData("../Tilesets/items.tsx", "Assets/DungeonEscape/Tilesets/items.tsx")]
        [InlineData("items.tsx", "Assets/DungeonEscape/Tilesets/items.tsx")]
        public void ResolveTilesetAssetPathUsesUnityTilesetFolder(string input, string expected)
        {
            Assert.Equal(expected, TiledMapPaths.ResolveTilesetAssetPath(input));
        }

        [Theory]
        [InlineData("images/items.png", "Assets/DungeonEscape/Images/items.png")]
        [InlineData("../Images/ui/splash.png", "Assets/DungeonEscape/Images/ui/splash.png")]
        [InlineData("monsters/all.png", "Assets/DungeonEscape/Images/monsters/all.png")]
        public void ResolveTilesetImageAssetPathUsesUnityImagesFolder(string input, string expected)
        {
            Assert.Equal(expected, TiledMapPaths.ResolveTilesetImageAssetPath(input));
        }
    }
}
