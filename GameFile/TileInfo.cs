namespace GameFile
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class TileInfo
    {
        public int Id { get; set; }
        public string Image { get; set; }
        
        public List<int> OldIds { get; set; }

        [JsonIgnore]
        public string ImageFile { get; set; }
        
        [JsonIgnore]
        public int size { get; set; }
        
        [JsonIgnore]
        public string OldImage { get; set; }
    }
}