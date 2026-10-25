using System.Collections.Generic;
using Redpoint.DungeonEscape.Data;

namespace Redpoint.DungeonEscape.Rules
{
    public sealed class CampaignContentContext
    {
        public CampaignDefinition Campaign { get; }
        public DungeonEscapeDataSet Data { get; }
        public MapCatalog Maps { get; }
        public IReadOnlyList<Monster> OverworldMonsters { get; }

        internal CampaignContentContext(CampaignDefinition campaign, DungeonEscapeDataSet data, MapCatalog maps, List<Monster> roster)
        {
            Campaign = campaign;
            Data = data;
            Maps = maps;
            OverworldMonsters = roster.AsReadOnly();
        }
    }
}
