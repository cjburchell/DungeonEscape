using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.Rules;
using Redpoint.DungeonEscape.State;
using Xunit;

namespace DungeonEscape.Core.Test.Rules;

public class CampaignSaveRulesTests
{
    [Fact]
    public void QuickSavesReplaceOnlyTheSameCampaignAndContinueChoosesMostRecent()
    {
        var file = new GameFile();
        var first = Save("first");
        var second = Save("second");
        second.Time = first.Time.Value.AddMinutes(1);
        CampaignSaveRules.UpsertQuickSave(file, first);
        CampaignSaveRules.UpsertQuickSave(file, second);
        Assert.Same(second, CampaignSaveRules.GetQuickSave(file));
        Assert.Same(first, CampaignSaveRules.GetQuickSave(file, first.CampaignId));
        var replacement = Save("first");
        replacement.Time = second.Time.Value.AddMinutes(1);
        CampaignSaveRules.UpsertQuickSave(file, replacement);
        Assert.Equal(2, file.Saves.Count);
        Assert.Same(second, CampaignSaveRules.GetQuickSave(file, second.CampaignId));
        Assert.Same(replacement, CampaignSaveRules.GetQuickSave(file));
    }

    [Fact]
    public void MatchingSaveRoundTripsCampaignMetadataAndAllowsGeneratedEquipment()
    {
        var save = Save("first");
        save.Party.Members.Add(new Hero
        {
            Name = "item/second/this-is-a-display-name", IsActive = true,
            Items = new List<ItemInstance> { new(new Item { Id = "item/base/robe", Name = "Starting robe" }) }
        });
        var restored = JsonConvert.DeserializeObject<GameSave>(JsonConvert.SerializeObject(save))!;
        CampaignSaveRules.Validate(restored, Context("first"));
        Assert.Equal(save.CampaignId, restored.CampaignId);
        Assert.Equal(save.CampaignContentVersion, restored.CampaignContentVersion);
        Assert.Contains("First campaign", GameSaveFormatter.GetSummary(restored));
    }

    [Fact]
    public void WrongCampaignAndContentVersionAreRejected()
    {
        Assert.Throws<ArgumentException>(() => CampaignSaveRules.Validate(Save("second"), Context("first")));
        var save = Save("first");
        save.CampaignContentVersion = "0.9";
        Assert.Throws<ArgumentException>(() => CampaignSaveRules.Validate(save, Context("first")));
    }

    [Fact]
    public void CrossCampaignItemsMapsAndUnknownPreparedSpellsAreRejected()
    {
        var save = Save("first");
        save.Party.Members.Add(new Hero { Items = new List<ItemInstance> { new(new Item { Id = "item/second/key" }) } });
        Assert.Throws<ArgumentException>(() => CampaignSaveRules.Validate(save, Context("first")));
        save.Party.Members.Clear();
        save.MapStates.Add(new MapState { Id = "map/second/world" });
        Assert.Throws<KeyNotFoundException>(() => CampaignSaveRules.Validate(save, Context("first")));
        save.MapStates.Clear();
        save.Party.Members.Add(new Hero { PreparedSpells = new List<string> { "spell/base/missing" } });
        Assert.Throws<ArgumentException>(() => CampaignSaveRules.Validate(save, Context("first")));
    }

    private static GameSave Save(string scope) => new()
    {
        CampaignId = "campaign/" + scope + "/main", CampaignName = "First campaign", CampaignContentVersion = "1.0",
        Party = new Party { CurrentMapId = "map/" + scope + "/world", CurrentPosition = WorldPosition.Zero },
        Time = new DateTime(2026, 10, 24, 12, 0, 0)
    };

    private static CampaignContentContext Context(string scope)
    {
        var campaign = new CampaignDefinition
        {
            Id = "campaign/" + scope + "/main", Name = scope, ContentVersion = "1.0",
            StartMapId = "map/" + scope + "/world", OverworldMapId = "map/" + scope + "/world",
            ItemsAssetPath = "Assets/DungeonEscape/Data/items.json", MapsAssetPath = "Assets/DungeonEscape/Data/maps.json",
            QuestsAssetPath = "Assets/DungeonEscape/Data/quests.json", DialogsAssetPath = "Assets/DungeonEscape/Data/dialog.json"
        };
        return CampaignContentResolver.Resolve(campaign, new DungeonEscapeDataSet(), new DungeonEscapeDataSet(),
            new[] { new MapDefinition { Id = campaign.StartMapId, Name = "World", AssetPath = "Assets/DungeonEscape/Maps/" + scope + ".tmx" } });
    }
}
