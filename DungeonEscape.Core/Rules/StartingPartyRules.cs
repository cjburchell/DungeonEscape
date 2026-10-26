using System;
using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.Data;

namespace Redpoint.DungeonEscape.Rules
{
    public static class StartingPartyRules
    {
        public static int GetLimit(CampaignDefinition campaign, int activePartyLimit)
        {
            if (campaign == null || campaign.MaxStartingPartyMembers < 1)
                throw new ArgumentException("Maximum starting party members must be at least one.");
            return Math.Min(campaign.MaxStartingPartyMembers, Math.Max(1, activePartyLimit));
        }

        public static void Validate(IReadOnlyList<StartingCharacter> characters, CampaignDefinition campaign, int activePartyLimit)
        {
            var limit = GetLimit(campaign, activePartyLimit);
            if (characters == null || characters.Count < 1 || characters.Count > limit || characters.Any(character => character == null))
                throw new ArgumentException("Create between one and " + limit + " starting party members.");
        }
    }
}
