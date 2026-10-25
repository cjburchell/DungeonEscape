using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.State;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace DungeonEscape.Core.Test.Data;

public class SkillReferenceTests
{
    [Fact]
    public void ItemsSpellsAndHeroSkillsKeepTheirIdentityAfterRename()
    {
        var first = new Skill { Id = "skill/base/knock", Name = "Knock", Type = SkillType.Heal };
        var second = new Skill { Id = "skill/campaign1/knock", Name = "Knock", Type = SkillType.Open };
        var skills = new[] { first, second };
        var item = new Item { SkillId = second.Id };
        var spell = new Spell { SkillId = second.Id };
        var hero = new Hero { Skills = new List<string> { second.Id } };

        second.Name = "Open Sesame";
        item.Setup(skills);
        spell.Setup(skills);

        Assert.Same(second, item.Skill);
        Assert.Equal(SkillType.Open, spell.Type);
        Assert.Same(second, hero.GetSkills(skills).Single());
    }

    [Fact]
    public void NameReferencesDoNotResolveAndRelinkingClearsOldEffects()
    {
        var skill = new Skill { Id = "skill/base/knock", Name = "Knock", Type = SkillType.Open };
        var item = new Item { SkillId = skill.Id };
        var spell = new Spell { SkillId = skill.Id };
        item.Setup(new[] { skill });
        spell.Setup(new[] { skill });
        item.SkillId = skill.Name;
        spell.SkillId = skill.Name;
        item.Setup(new[] { skill });
        spell.Setup(new[] { skill });

        Assert.Null(item.Skill);
        Assert.Equal(SkillType.None, spell.Type);
    }
}
