namespace ConvertMaps.Models
{
    using System.Xml.Serialization;

    public class Icon
    {
        [XmlAttribute("Index")]
        public int Id { get; set; }
        [XmlAttribute("FileName")]
        public string FileName { get; set; }
    }
}