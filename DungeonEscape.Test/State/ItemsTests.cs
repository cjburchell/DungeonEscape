﻿using Redpoint.DungeonEscape;

namespace DungeonEscape.Test.State
{
    using System.Collections.Generic;
    using System.IO;
    using Newtonsoft.Json;
    using Redpoint.DungeonEscape.State;
    using Xunit;
    using Xunit.Abstractions;

    public class ItemsTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ItemsTests(ITestOutputHelper testOutputHelper)
        {
            this._testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void CreateRandomItem()
        {
            using var game = new Game();
            game.StatNames =
                JsonConvert.DeserializeObject<List<StatName>>(File.ReadAllText("Content/data/statnames.json"));
            game.ItemDefinitions =
                JsonConvert.DeserializeObject<List<ItemDefinition>>(File.ReadAllText("Content/data/itemdef.json"));;
            game.CustomItems =
                JsonConvert.DeserializeObject<List<Item>>(File.ReadAllText("Content/data/customitems.json"));
            
            var item = game.CreateRandomItem(30);

            this._testOutputHelper.WriteLine($"Name: {item.Name}");
            this._testOutputHelper.WriteLine($"Level: {item.MinLevel}");
            this._testOutputHelper.WriteLine($"Type: {item.Type}");
            this._testOutputHelper.WriteLine(JsonConvert.SerializeObject(item, Formatting.Indented));
            
            Assert.NotNull(item);
        }
        
        [Fact]
        public void Create100RandomItems()
        {
            using var game = new Game();
            game.StatNames =
                JsonConvert.DeserializeObject<List<StatName>>(File.ReadAllText("Content/data/statnames.json"));
            game.ItemDefinitions =
                JsonConvert.DeserializeObject<List<ItemDefinition>>(File.ReadAllText("Content/data/itemdef.json"));;
            game.CustomItems =
                JsonConvert.DeserializeObject<List<Item>>(File.ReadAllText("Content/data/customitems.json"));
            
            for (var i = 0; i < 100; i++)
            {
                var item = game.CreateRandomItem(30);
                this._testOutputHelper.WriteLine($"{item.MinLevel}{item.Rarity.ToString()[0]}: {item.Name}");
                Assert.NotNull(item);
            }
            
        }
    }
}