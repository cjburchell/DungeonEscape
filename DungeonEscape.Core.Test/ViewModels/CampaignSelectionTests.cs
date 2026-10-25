using System;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.Rules;
using Redpoint.DungeonEscape.ViewModels;
using Xunit;

namespace DungeonEscape.Core.Test.ViewModels;

public class CampaignSelectionTests
{
    [Fact]
    public void OneCampaignSkipsSelectionAndMultipleCampaignsRequireAChoice()
    {
        var model = new TitleViewModel();
        var first = new CampaignDefinition { Id = "campaign/first/main", Name = "First" };
        var second = new CampaignDefinition { Id = "campaign/second/main", Name = "Second" };
        model.BeginNewQuest(new[] { first });
        Assert.Equal(TitleMode.Create, model.Mode);
        Assert.Equal(first.Id, model.SelectedCampaignId);
        model.BeginNewQuest(new[] { first, second });
        Assert.Equal(TitleMode.Campaign, model.Mode);
        Assert.Null(model.SelectedCampaignId);
        Assert.Equal(2, model.GetCampaignNavigationIndex(99)); // Back button follows the campaign rows.
        model.SelectCampaign(1);
        Assert.Equal(second.Id, model.SelectedCampaignId);
        Assert.Equal(TitleMode.Create, model.Mode);
        model.ShowMainMenu();
        Assert.Null(model.SelectedCampaignId);
    }

    [Fact]
    public void NoCampaignsLeavesOnlyBackAndCannotEnterCharacterCreation()
    {
        var catalog = new CampaignCatalog(Array.Empty<CampaignDefinition>(), null);
        Assert.Null(catalog.Default);
        var model = new TitleViewModel();
        model.BeginNewQuest(catalog.Campaigns);
        Assert.Equal(TitleMode.Campaign, model.Mode);
        Assert.Equal(1, model.GetOptionCount(false, 0));
        Assert.Equal(0, model.GetCampaignNavigationIndex(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => model.SelectCampaign(0));
    }
}
