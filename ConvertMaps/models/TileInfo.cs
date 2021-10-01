namespace ConvertMaps.Models
{
    using System.Xml.Serialization;

    public class TileInfo
    {
        [XmlAttribute("text")] public string Name { get; set; }
        [XmlAttribute("index")] public int OldId { get; set; }
        [XmlAttribute("icon")] public int IconId { get; set; }

        public string FileName { get; set; }

        public string NewId { get; set; }
        public int Size { get; set; }
    }
}