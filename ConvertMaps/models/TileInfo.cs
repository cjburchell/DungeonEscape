namespace ConvertMaps.Models
{
    using System.Xml.Serialization;

    public class TileFileInfo
    {
        [XmlAttribute("index")] public int OldId { get; set; }
        [XmlAttribute("icon")] public int IconId { get; set; }

        public string FileName { get; set; }
    }
}