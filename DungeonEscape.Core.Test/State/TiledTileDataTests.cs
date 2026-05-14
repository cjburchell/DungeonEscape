using System.Linq;
using Redpoint.DungeonEscape.State;
using Xunit;

namespace DungeonEscape.Core.Test.State
{
    public sealed class TiledTileDataTests
    {
        [Fact]
        public void ParseCsvTileDataHandlesWhitespaceAndInvalidValues()
        {
            var gids = TiledTileData.ParseCsvTileData("1, 2\nbad\t3");

            Assert.Equal(new[] { 1, 2, 0, 3 }, gids);
        }

        [Fact]
        public void ParseGidRemovesTiledFlipFlags()
        {
            Assert.Equal(42, TiledTileData.ParseGid("2147483690"));
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("TRUE", true)]
        [InlineData("1", true)]
        [InlineData("false", false)]
        [InlineData("0", false)]
        [InlineData(null, false)]
        public void IsTrueSupportsTiledBooleanForms(string value, bool expected)
        {
            Assert.Equal(expected, TiledTileData.IsTrue(value));
        }

        [Fact]
        public void GetObjectBoundsTileIndexesUsesRectangleObjectBounds()
        {
            var mapObject = new TiledObjectInfo
            {
                X = 32,
                Y = 32,
                Width = 32,
                Height = 64
            };

            var indexes = TiledTileData.GetObjectBoundsTileIndexes(mapObject, 32, 32, 5, 5);

            Assert.Equal(new[] { 6, 11 }, indexes);
        }

        [Fact]
        public void GetObjectBoundsTileIndexesUsesGidObjectBottomAlignment()
        {
            var mapObject = new TiledObjectInfo
            {
                Gid = 10,
                X = 32,
                Y = 64,
                Width = 32,
                Height = 64
            };

            var indexes = TiledTileData.GetObjectBoundsTileIndexes(mapObject, 32, 32, 5, 5);

            Assert.Equal(new[] { 1, 6 }, indexes);
        }

        [Fact]
        public void GetObjectBoundsTileIndexesClipsToMapBounds()
        {
            var mapObject = new TiledObjectInfo
            {
                X = -16,
                Y = 0,
                Width = 64,
                Height = 32
            };

            var indexes = TiledTileData.GetObjectBoundsTileIndexes(mapObject, 32, 32, 2, 2);

            Assert.Equal(new[] { 0, 1 }, indexes.ToArray());
        }
    }
}
