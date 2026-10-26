using System;
using System.Linq;
using Newtonsoft.Json;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.Rules;
using Xunit;

namespace DungeonEscape.Core.Test.Rules;

public class StartingPartyRulesTests
{
    [Fact]
    public void MissingManifestSettingPreservesSingleCharacterStart()
    {
        var campaign = JsonConvert.DeserializeObject<CampaignDefinition>("{\"Name\":\"Existing campaign\"}");
        Assert.Equal(1, StartingPartyRules.GetLimit(campaign, 4));
    }

    [Theory]
    [InlineData(3, 4, 3)]
    [InlineData(6, 4, 4)]
    [InlineData(2, 1, 1)]
    public void CampaignLimitIsCappedByActivePartyCapacity(int campaignLimit, int activeLimit, int expected)
    {
        var campaign = new CampaignDefinition { MaxStartingPartyMembers = campaignLimit };
        StartingPartyRules.Validate(new[] { new StartingCharacter() }, campaign, activeLimit);
        Assert.Equal(expected, StartingPartyRules.GetLimit(campaign, activeLimit));
        StartingPartyRules.Validate(Enumerable.Range(0, expected).Select(_ => new StartingCharacter()).ToList(), campaign, activeLimit);
        Assert.Throws<ArgumentException>(() => StartingPartyRules.Validate(
            Enumerable.Range(0, expected + 1).Select(_ => new StartingCharacter()).ToList(), campaign, activeLimit));
    }

    [Fact]
    public void EmptyNullAndInvalidPartiesAreRejected()
    {
        var campaign = new CampaignDefinition { MaxStartingPartyMembers = 2 };
        Assert.Throws<ArgumentException>(() => StartingPartyRules.Validate(null, campaign, 4));
        Assert.Throws<ArgumentException>(() => StartingPartyRules.Validate(Array.Empty<StartingCharacter>(), campaign, 4));
        Assert.Throws<ArgumentException>(() => StartingPartyRules.Validate(new StartingCharacter[] { null }, campaign, 4));
        campaign.MaxStartingPartyMembers = 0;
        Assert.Throws<ArgumentException>(() => StartingPartyRules.GetLimit(campaign, 4));
    }
}
