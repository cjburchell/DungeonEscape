using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace TiledCS
{
    [XmlRoot("tileset")]
    /// <summary>
    /// Represents a Tiled tileset
    /// </summary>
    public class TiledTileset
    {
        public string version { get; set; }
        public string type { get; set; }
        
        /// <summary>
        /// The Tiled version used to create this tileset
        /// </summary>
        [XmlAttribute] public string tiledversion { get; set; }

        /// <summary>
        /// The tileset name
        /// </summary>
        [XmlAttribute] public string name { get; set; }

        /// <summary>
        /// The tile width in pixels
        /// </summary>
        [XmlAttribute] public int tilewidth { get; set; }

        /// <summary>
        /// The tile height in pixels
        /// </summary>
        [XmlAttribute] public int tileheight { get; set; }

        /// <summary>
        /// The total amount of tiles
        /// </summary>
        [XmlAttribute] public int tilecount { get; set; }

        /// <summary>
        /// The amount of horizontal tiles
        /// </summary>
        [XmlAttribute] public int columns { get; set; }

        /// <summary>
        /// The amount of spacing between the tiles in pixels
        /// </summary>
        [XmlAttribute] public int spacing { get; set; }

        /// <summary>
        /// The amount of margin between the tiles in pixels
        /// </summary>
        [XmlAttribute] public int margin { get; set; }

        /// <summary>
        /// An array of tile definitions
        /// </summary>
        /// <remarks>Not all tiles within a tileset have definitions. Only those with properties, animations, terrains, ...</remarks>
        [XmlElement("tile")] public TiledTile[] tiles { get; set; }

        /// <summary>
        /// An array of tileset properties
        /// </summary>
        [XmlArrayItem(ElementName="property", Type = typeof(TiledProperty))][XmlArray("property")] public TiledProperty[] properties { get; set; }

        [XmlAttribute] public string transparentcolor { get; set; }
        [XmlAttribute] public string source { get; set; }
        [XmlAttribute] public int firstgid { get; set; }
    }
}