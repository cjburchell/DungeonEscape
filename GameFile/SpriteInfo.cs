namespace GameFile
{
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public enum SpriteType
    {
        Ship,
        Door,
        Chest,
        NPC,
        Monster,
    }
    
    public class SpriteInfo
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public SpriteType Type { get; set; }
        public string Id { get; set; }
        
        public string Name { get; set; }
        
        public string Image { get; set; }
        public int? Heath { get; set; }
        public int? HeathConst { get; set; }
        public int? Attack { get; set; }
        public int? XP { get; set; }
        public int? Gold { get; set; }
        
        public List<SpriteSpell> Spells { get; set; }
    }
}