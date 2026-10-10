using System.Collections.Generic;
using Redpoint.DungeonEscape.State;
using Xunit;

namespace DungeonEscape.Core.Test.State
{
    public sealed class RestActionsTests
    {
        [Fact]
        public void PartyShortRestHealsAndClearsStatus()
        {
            var party = new Party();
            var hero = new Hero
            {
                Name = "Alder",
                Class = Class.Paladin,
                MaxHealth = 20,
                Health = 8,
                IsActive = true,
                Status = new List<StatusEffect>
                {
                    new StatusEffect { Name = "Poisoned", Type = EffectType.Poisoned }
                },
                UsedSpellSlots = new List<int> { 2, 1 }
            };
            party.Members.Add(hero);

            var message = party.ShortRest();

            Assert.Contains("short rest", message, System.StringComparison.OrdinalIgnoreCase);
            Assert.True(hero.Health > 8);
            Assert.True(hero.Health <= hero.MaxHealth);
            Assert.Empty(hero.Status);
        }

        [Fact]
        public void PartyLongRestRequiresGoldAndFullyRestoresParty()
        {
            var party = new Party { Gold = 50 };
            var hero = new Hero
            {
                Name = "Alder",
                Class = Class.Paladin,
                MaxHealth = 20,
                Health = 6,
                IsActive = true,
                Status = new List<StatusEffect>
                {
                    new StatusEffect { Name = "Poisoned", Type = EffectType.Poisoned }
                },
                UsedSpellSlots = new List<int> { 2, 1 }
            };
            party.Members.Add(hero);

            var message = party.LongRest(25);

            Assert.Contains("rested", message, System.StringComparison.OrdinalIgnoreCase);
            Assert.Equal(25, party.Gold);
            Assert.Equal(hero.MaxHealth, hero.Health);
            Assert.Empty(hero.Status);
            Assert.All(hero.UsedSpellSlots, slot => Assert.Equal(0, slot));
        }

        [Fact]
        public void PartyLongRestCanMakeCampWithoutGoldCost()
        {
            var party = new Party { Gold = 10 };
            var hero = new Hero
            {
                Name = "Alder",
                Class = Class.Paladin,
                MaxHealth = 20,
                Health = 6,
                IsActive = true,
                UsedSpellSlots = new List<int> { 1 }
            };
            party.Members.Add(hero);

            var message = party.LongRest(0);

            Assert.Contains("makes camp", message, System.StringComparison.OrdinalIgnoreCase);
            Assert.Equal(10, party.Gold);
            Assert.Equal(hero.MaxHealth, hero.Health);
            Assert.All(hero.UsedSpellSlots, slot => Assert.Equal(0, slot));
        }

        [Fact]
        public void PartyLongRestFailsWhenPartyCannotAffordInn()
        {
            var party = new Party { Gold = 10 };
            var hero = new Hero
            {
                Name = "Alder",
                Class = Class.Paladin,
                MaxHealth = 20,
                Health = 6,
                IsActive = true
            };
            party.Members.Add(hero);

            var message = party.LongRest(25);

            Assert.Contains("do not have", message, System.StringComparison.OrdinalIgnoreCase);
            Assert.Equal(10, party.Gold);
            Assert.Equal(6, hero.Health);
        }
    }
}
