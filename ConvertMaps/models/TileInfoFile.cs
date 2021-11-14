using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace ConvertMaps.Models
{
    [XmlRoot("TileList")]
    public class TileInfoFile
    {
        [XmlElement("Tile")]
        public List<TileFileInfo> Tiles;
        
        public static TileInfoFile Deserialize(string file)
        {
            var serializer =
                new XmlSerializer(typeof(TileInfoFile));
            using Stream reader = new FileStream(file, FileMode.Open);
            return (TileInfoFile)serializer.Deserialize(reader);
        }
    }
}