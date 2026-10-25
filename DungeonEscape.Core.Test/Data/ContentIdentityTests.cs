using System.Collections.Generic;
using System.Linq;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.Rules;
using Redpoint.DungeonEscape.State;
using Xunit;

namespace DungeonEscape.Core.Test.Data;

public class ContentIdentityTests
{
    [Fact]
    public void PreparedSpellsSurviveRenamesAndIdenticalDisplayNames()
    {
        var first = new Spell { Id = "spell/base/cure-wounds", Name = "Heal", MinLevel = 1, SpellLevel = 1, Classes = new List<string> { "Cleric" } };
        var second = new Spell { Id = "spell/original/heal", Name = "Heal", MinLevel = 1, SpellLevel = 1, Classes = new List<string> { "Cleric" } };
        var hero = new Hero { Class = Class.Cleric, Level = 3, Wisdom = 16, PreparedSpells = new List<string> { second.Id } };
        second.Name = "Restoring Light";

        Assert.Same(second, hero.GetPreparedSpells(new[] { first, second }).Single());
        Assert.False(hero.IsSpellPrepared(first));
        Assert.True(hero.UnprepareSpell(second));
        Assert.Empty(hero.PreparedSpells);
    }

    [Fact]
    public void PartyItemReferencesAndShipAccessUseDefinitionIds()
    {
        var ship = new Item { Id = "item/original/deed-to-the-ship", Name = "Renamed Papers" };
        var other = new Item { Id = "item/other/deed-to-the-ship", Name = ship.Name };
        var party = new Party();
        party.Members.Add(new Hero { IsActive = true, Health = 10, Items = new List<ItemInstance> { new(other), new(ship) } });

        Assert.True(party.HasShip);
        Assert.Same(ship, party.RemoveItem(ship.Id).Item1);
        Assert.False(party.HasShip);
        Assert.Null(party.RemoveItem(other.Name).Item1);
    }

    [Theory]
    [InlineData("quest/original/find-the-ship", "quest", true)]
    [InlineData("map/original/forest-tower-floor1", "map", true)]
    [InlineData("dialog/base/guard", "quest", false)]
    [InlineData("quest/Original/find-ship", "quest", false)]
    [InlineData("quest/original/../ship", "quest", false)]
    public void ContentIdsUseOneCanonicalFormat(string id, string kind, bool expected)
    {
        Assert.Equal(expected, ContentId.IsValid(id, kind));
    }
}
