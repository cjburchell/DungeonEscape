using System.Linq;
using Redpoint.DungeonEscape.Data;
using Xunit;

namespace DungeonEscape.Core.Test.Rules;

public class TiledObjectClassTests
{
    [Theory]
    [InlineData("Spawn")]
    [InlineData("Warp")]
    [InlineData("Npc")]
    [InlineData("NpcInn")]
    [InlineData("Monster")]
    public void TiledResavedObjectClassesKeepTheirGameplayMeaning(string objectClass)
    {
        var map = TiledMapInfo.Parse("<map class=\"Overworld\"><objectgroup><object id=\"1\" name=\"start\" type=\"" + objectClass +
            "\"><properties><property name=\"DefaultSpawn\" type=\"bool\" value=\"true\"/></properties></object></objectgroup></map>");
        var item = map.ObjectGroups.Single().Objects.Single();
        Assert.Equal("Overworld", map.Class);
        Assert.Equal(objectClass, item.Class);
        Assert.Equal("start", item.Name);
        Assert.Equal("true", item.Properties["DefaultSpawn"]);
    }

    [Fact]
    public void CanonicalClassTakesPrecedenceIfBothAttributesExist()
    {
        var map = TiledMapInfo.Parse("<map><objectgroup><object id=\"1\" class=\"Spawn\" type=\"Npc\"/></objectgroup></map>");
        Assert.Equal("Spawn", map.ObjectGroups.Single().Objects.Single().Class);
    }
}
