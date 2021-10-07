namespace ConvertMaps.Tiled
{
    using System.Xml.Serialization;

    [XmlRoot("map")]
    /// <summary>
    /// Represents a Tiled map
    /// </summary>
    public class TiledMap
    {
        [XmlAttribute] public string version { get; set; }
        [XmlAttribute] public string type { get; set; }
        const uint FLIPPED_HORIZONTALLY_FLAG = 0b10000000000000000000000000000000;
        const uint FLIPPED_VERTICALLY_FLAG = 0b01000000000000000000000000000000;
        const uint FLIPPED_DIAGONALLY_FLAG = 0b00100000000000000000000000000000;

        /// <summary>
        /// How many times we shift the FLIPPED flags to the right in order to store it in a byte.
        /// For example: 0b10100000000000000000000000000000 >> SHIFT_FLIP_FLAG_TO_BYTE = 0b00000101
        /// </summary>
        const int SHIFT_FLIP_FLAG_TO_BYTE = 29;

        /// <summary>
        /// Returns the Tiled version used to create this map
        /// </summary>
        [XmlAttribute] public string tiledversion { get; set; }
        /// <summary>
        /// Returns an array of properties defined in the map
        /// </summary>
        [XmlArrayItem(ElementName="property", Type = typeof(TiledProperty))]
        [XmlArray("properties")] public TiledProperty[] properties { get; set; }
        /// <summary>
        /// Returns an array of tileset definitions in the map
        /// </summary>
        [XmlElement("tileset")] public TiledTileset[] tilesets { get; set; }
        /// <summary>
        /// Returns an array of layers or null if none were defined
        /// </summary>
        [XmlElement(ElementName="layer", Type = typeof(TiledLayerGroup))]
        [XmlElement(ElementName="objectgroup", Type = typeof(ObjectGroup))]
        public TiledLayer[] layers { get; set; }

        /// <summary>
        /// Returns an array of groups or null if none were defined
        /// </summary>
        public TiledGroup[] groups { get; set; }
        /// <summary>
        /// Returns the defined map orientation as a string
        /// </summary>
        [XmlAttribute] public string orientation { get; set; }
        /// <summary>
        /// Returns the render order as a string
        /// </summary>
        [XmlAttribute] public string renderorder { get; set; }
        /// <summary>
        /// The amount of horizontal tiles
        /// </summary>
        [XmlAttribute] public int width { get; set; }
        /// <summary>
        /// The amount of vertical tiles
        /// </summary>
        [XmlAttribute] public int height { get; set; }
        /// <summary>
        /// The tile width in pixels
        /// </summary>
        [XmlAttribute] public int tilewidth { get; set; }
        /// <summary>
        /// The tile height in pixels
        /// </summary>
        [XmlAttribute] public int tileheight { get; set; }
    }
}