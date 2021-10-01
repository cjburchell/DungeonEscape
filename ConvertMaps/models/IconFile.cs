namespace ConvertMaps.Models
{
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Serialization;

    [XmlRoot("IconList")]
    public class IconFile
    {
        [XmlElement("Icon")]
        public List<Icon> Icons;

        public static IconFile Deserialize(string file)
        {
            var serializer =
                new XmlSerializer(typeof(IconFile));
            using Stream reader = new FileStream(file, FileMode.Open);
            return (IconFile)serializer.Deserialize(reader);
        }
    }
}