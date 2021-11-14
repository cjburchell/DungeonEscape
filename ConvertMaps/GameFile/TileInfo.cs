using System.Collections.Generic;
using Newtonsoft.Json;

namespace GameFile
{
    public class TileInfo
    {
        public int Id { get; set; }
        public string Image { get; set; }
        
        public List<int> OldIds { get; set; }
    }
}