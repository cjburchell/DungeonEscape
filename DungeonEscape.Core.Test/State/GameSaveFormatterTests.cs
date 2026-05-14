using System;
using System.Collections.Generic;
using Redpoint.DungeonEscape.Rules;
using Redpoint.DungeonEscape.State;
using Xunit;

namespace DungeonEscape.Core.Test.State
{
    public sealed class GameSaveFormatterTests
    {
        [Fact]
        public void GetTitleReturnsEmptyForUnusableSave()
        {
            Assert.Equal("Empty", GameSaveFormatter.GetTitle(null));
            Assert.Equal("Empty", GameSaveFormatter.GetTitle(new GameSave()));
        }

        [Fact]
        public void GetTitleUsesSaveNameForUsableSave()
        {
            var save = CreateUsableSave("Ada", false);

            Assert.Equal("Ada", GameSaveFormatter.GetTitle(save));
        }

        [Fact]
        public void GetTitleIncludesQuickSavePrefix()
        {
            var save = CreateUsableSave("Ada", true);

            Assert.Equal("Quick (Ada)", GameSaveFormatter.GetTitle(save));
        }

        [Fact]
        public void GetSummaryReturnsNoSaveDataForUnusableSave()
        {
            Assert.Equal("No save data.", GameSaveFormatter.GetSummary(new GameSave()));
        }

        [Fact]
        public void GetSummaryIncludesSaveTimeAndHighestActiveLevel()
        {
            var save = CreateUsableSave("Ada", false);
            save.Time = new DateTime(2026, 5, 14, 9, 30, 0);

            var summary = GameSaveFormatter.GetSummary(save);

            Assert.Contains("Level 5", summary);
            Assert.Contains(save.Time.Value.ToString("g"), summary);
        }

        [Fact]
        public void GetSummaryUsesUnknownTimeWhenSaveHasNoTime()
        {
            var save = CreateUsableSave("Ada", false);

            Assert.Equal("Unknown time    Level 5", GameSaveFormatter.GetSummary(save));
        }

        [Theory]
        [InlineData(null, "Unknown")]
        [InlineData("", "Unknown")]
        [InlineData("towns/isis", "Isis")]
        [InlineData("forest_tower/floor-2", "Floor 2")]
        [InlineData("dungeon\\first_floor", "First Floor")]
        public void FormatLocationNameUsesFinalMapSegment(string mapId, string expected)
        {
            Assert.Equal(expected, GameSaveFormatter.FormatLocationName(mapId));
        }

        private static GameSave CreateUsableSave(string playerName, bool isQuick)
        {
            var party = new Party
            {
                PlayerName = playerName,
                CurrentMapId = "overworld",
                CurrentPosition = WorldPosition.Zero
            };
            party.Members.Add(new Hero
            {
                Name = playerName,
                IsActive = true,
                Level = 5,
                Items = new List<ItemInstance>()
            });

            return new GameSave
            {
                IsQuick = isQuick,
                Party = party
            };
        }
    }
}
