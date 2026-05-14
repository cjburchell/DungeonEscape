using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.State;
using Redpoint.DungeonEscape.ViewModels;
using Xunit;

namespace DungeonEscape.Core.Test.ViewModels
{
    public sealed class HealerViewModelTests
    {
        [Fact]
        public void MetadataUsesDefaultsAndObjectOverrides()
        {
            var viewModel = new HealerViewModel();
            var healer = new TiledObjectInfo
            {
                Name = "Temple",
                Properties = new Dictionary<string, string>
                {
                    { "Text", "Rest here." },
                    { "Cost", "40" }
                }
            };

            Assert.Equal("Healer", viewModel.GetHealerName(null));
            Assert.Equal("Do you require my services as a healer?", viewModel.GetHealerText(null));
            Assert.Equal(25, viewModel.GetHealerCost(null));
            Assert.Equal("Temple", viewModel.GetHealerName(healer));
            Assert.Equal("Rest here.", viewModel.GetHealerText(healer));
            Assert.Equal(40, viewModel.GetHealerCost(healer));
        }

        [Fact]
        public void BuildServicesCreatesRowsForPartyNeeds()
        {
            var party = new Party();
            var wounded = CreateHero("Wounded", health: 5, maxHealth: 10, magic: 10, maxMagic: 10);
            var magicMissing = CreateHero("Mage", health: 10, maxHealth: 10, magic: 2, maxMagic: 10);
            var status = CreateHero("Status", health: 10, maxHealth: 10, magic: 10, maxMagic: 10);
            status.Status.Add(new StatusEffect { Type = EffectType.Sleep });
            var dead = CreateHero("Dead", health: 0, maxHealth: 10, magic: 0, maxMagic: 10);
            party.Members.Add(wounded);
            party.Members.Add(magicMissing);
            party.Members.Add(status);
            party.Members.Add(dead);

            var rows = new HealerViewModel().BuildServices(party, new TiledObjectInfo
            {
                Properties = new Dictionary<string, string> { { "Cost", "30" } }
            });

            Assert.Equal(
                new[] { HealerService.Heal, HealerService.RenewMagic, HealerService.Cure, HealerService.Revive },
                rows.Select(row => row.Service).ToArray());
            Assert.Equal(30, rows[0].Cost);
            Assert.Equal(new[] { wounded }, rows[0].Targets);
            Assert.Equal(60, rows[1].Cost);
            Assert.Equal(new[] { status }, rows[2].Targets);
            Assert.Equal(300, rows[3].Cost);
            Assert.Equal(new[] { dead }, rows[3].Targets);
        }

        [Fact]
        public void BuildServicesAddsHealAllWhenMultipleHeroesAreWounded()
        {
            var party = new Party();
            party.Members.Add(CreateHero("One", health: 5, maxHealth: 10, magic: 10, maxMagic: 10));
            party.Members.Add(CreateHero("Two", health: 6, maxHealth: 10, magic: 10, maxMagic: 10));

            var rows = new HealerViewModel().BuildServices(party, null);

            Assert.Equal(new[] { HealerService.Heal, HealerService.HealAll }, rows.Select(row => row.Service).ToArray());
            Assert.Equal(50, rows[1].Cost);
            Assert.False(rows[1].NeedsTarget);
        }

        [Fact]
        public void SelectionMethodsClampAndResetTargetSelection()
        {
            var viewModel = new HealerViewModel();
            viewModel.SetSelectedTargetIndex(3);

            viewModel.SetSelectedServiceIndex(2);

            Assert.Equal(2, viewModel.SelectedServiceIndex);
            Assert.Equal(0, viewModel.SelectedTargetIndex);

            var service = new HealerServiceRow { Targets = new List<Hero> { new Hero(), new Hero() } };
            viewModel.SetSelectedTargetIndex(10);
            viewModel.ClampTargetSelection(service);

            Assert.Equal(1, viewModel.SelectedTargetIndex);
        }

        private static Hero CreateHero(string name, int health, int maxHealth, int magic, int maxMagic)
        {
            return new Hero
            {
                Name = name,
                IsActive = true,
                Health = health,
                MaxHealth = maxHealth,
                Magic = magic,
                MaxMagic = maxMagic,
                Items = new List<ItemInstance>()
            };
        }
    }
}
