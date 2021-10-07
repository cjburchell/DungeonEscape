using System.Xml.Serialization;

namespace ConvertMaps.Models
{
    public class TileFileInfo
    {
        [XmlAttribute("index")] public int OldId { get; set; }
        [XmlAttribute("icon")] public int IconId { get; set; }

        public string FileName { get; set; }
    }
}