using System.Xml.Serialization;

namespace ConvertMaps.Models
{
    public class Icon
    {
        [XmlAttribute("Index")]
        public int Id { get; set; }
        [XmlAttribute("FileName")]
        public string FileName { get; set; }
    }
}