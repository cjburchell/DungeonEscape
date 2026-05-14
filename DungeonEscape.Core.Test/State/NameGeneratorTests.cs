using System.Collections.Generic;
using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.State;
using Redpoint.DungeonEscape.Tools;
using Xunit;

namespace DungeonEscape.Core.Test.State
{
    public sealed class NameGeneratorTests
    {
        [Fact]
        public void GenerateReturnsDeterministicMaleNameWhenOnlyOneNameExists()
        {
            var generator = new NameGenerator(new Names
            {
                Male = new List<string> { "Ada" },
                Female = new List<string> { "Eve" }
            });

            Assert.Equal("Ada", generator.Generate(Gender.Male));
        }

        [Fact]
        public void GenerateReturnsDeterministicFemaleNameWhenOnlyOneNameExists()
        {
            var generator = new NameGenerator(new Names
            {
                Male = new List<string> { "Ada" },
                Female = new List<string> { "Eve" }
            });

            Assert.Equal("Eve", generator.Generate(Gender.Female));
        }

        [Fact]
        public void GeneratePreservesMultiPartNameShape()
        {
            var generator = new NameGenerator(new Names
            {
                Male = new List<string> { "Al Bo" },
                Female = new List<string> { "Cy Da" }
            });

            var parts = generator.Generate(Gender.Male).Split(' ');

            Assert.Equal(2, parts.Length);
            Assert.All(parts, part => Assert.Equal(2, part.Length));
        }

        [Fact]
        public void GenerateReturnsEmptyStringWhenRequestedNameListIsMissing()
        {
            var generator = new NameGenerator(new Names
            {
                Male = new List<string> { "Ada" }
            });

            Assert.Equal("", generator.Generate(Gender.Female));
        }

        [Fact]
        public void GenerateReturnsEmptyStringWhenDataIsMissing()
        {
            var generator = new NameGenerator(null);

            Assert.Equal("", generator.Generate(Gender.Male));
        }
    }
}
