using System.Xml.Serialization;
using Newtonsoft.Json;

namespace ConvertMaps.Tiled
{
    /// <summary>
    /// Represents an element within the Tilesets array of a TiledMap object
    /// </summary>
    public class TiledMapTileset
    {
        /// <summary>
        /// The first gid defines which gid matches the tile with source vector 0,0. Is used to determine which tileset belongs to which gid
        /// </summary>
        public int firstgid;
        /// <summary>
        /// The tsx file path as defined in the map file itself
        /// </summary>
        public string source;
    }

    
    /// <summary>
    /// Represents a property object in both tilesets, maps, layers and objects. Values are all in string but you can use the 'type' property for conversions
    /// </summary>
    ///
    [XmlRoot("property")]
    public class TiledProperty
    {
        /// <summary>
        /// The property name or key in string format
        /// </summary>
        [XmlAttribute] public string name;
        /// <summary>
        /// The property type as used in Tiled. Can be bool, number, string, ...
        /// </summary>
        [XmlAttribute] public string type;
        /// <summary>
        /// The value in string format
        /// </summary>
        [XmlAttribute] public string value;
    }
    
    public class ObjectGroup : TiledLayer
    {
    }
    
    public class TiledLayerGroup : TiledLayer
    {
    }

    /// <summary>
    /// Represents a tile layer as well as an object layer within a tile map
    /// </summary>
    public class TiledLayer
    {
        /// <summary>
        /// The layer id
        /// </summary>
        [XmlAttribute] public int id;
        /// <summary>
        /// The layer name
        /// </summary>
        [XmlAttribute] public string name;
        /// <summary>
        /// Total horizontal tiles
        /// </summary>
        [XmlAttribute] public int width;
        /// <summary>
        /// Total vertical tiles
        /// </summary>
        [XmlAttribute] public int height;
        /// <summary>
        /// The layer type. Usually this is "objectgroup" or "tilelayer".
        /// </summary>
        [XmlAttribute] public string type;
        /// <summary>
        /// The tint color set by the user in hex code
        /// </summary>
        [XmlAttribute] public string tintcolor;
        /// <summary>
        /// Defines if the layer is visible in the editor
        /// </summary>
        [XmlAttribute] public int visible;
        /// <summary>
        /// Is true when the layer is locked
        /// </summary>
        [XmlAttribute] public bool locked;
        /// <summary>
        /// The horizontal offset
        /// </summary>
        [XmlAttribute] public int x;
        /// <summary>
        /// The vertical offset
        /// </summary>
        [XmlAttribute] public int y;
        /// <summary>
        /// An int array of gid numbers which define which tile is being used where. The length of the array equals the layer width * the layer height. Is null when the layer is not a tilelayer.
        /// </summary>
        [XmlIgnore] public int[] data;
        
        [XmlElement("data")] [JsonIgnore] public TiledLayerData layerData;
        /// <summary>
        /// A parallel array to data which stores the rotation flags of the tile.
        /// Bit 3 is horizontal flip,
        /// bit 2 is vertical flip, and
        /// bit 1 is (anti) diagonal flip.
        /// Is null when the layer is not a tilelayer.
        /// </summary>
        public byte[] dataRotationFlags;
        /// <summary>
        /// The list of objects in case of an objectgroup layer. Is null when the layer has no objects.
        /// </summary>
        [XmlElement("object")] public TiledObject[] objects;
        /// <summary>
        /// The layer properties if set
        /// </summary>
        [XmlArrayItem(ElementName="property", Type = typeof(TiledProperty))][XmlArray("properties")] public TiledProperty[] properties { get; set; }

        [XmlAttribute] public string image;
        [XmlAttribute] public int opacity;
        [XmlAttribute] public string draworder;
    }

    public class TiledLayerData
    {
        [XmlText]public string data;
        [XmlAttribute] public string encoding = "csv";
    }

    /// <summary>
    /// Represents an tiled object defined in object layers
    /// </summary>
    public class TiledObject
    {
        /// <summary>
        /// The object id
        /// </summary>
        [XmlAttribute] public int id;
        /// <summary>
        /// The object's name
        /// </summary>
        [XmlAttribute] public string name;
        /// <summary>
        /// The object type if defined. Null if none was set.
        /// </summary>
        [XmlAttribute] public string type;
        /// <summary>
        /// The object's x position in pixels
        /// </summary>
        [XmlAttribute] public float x;
        /// <summary>
        /// The object's y position in pixels
        /// </summary>
        [XmlAttribute] public float y;
        /// <summary>
        /// The object's rotation
        /// </summary>
        [XmlAttribute] public int rotation;
        /// <summary>
        /// The object's width in pixels
        /// </summary>
        [XmlAttribute] public float width;
        /// <summary>
        /// The object's height in pixels
        /// </summary>
        [XmlAttribute] public float height;
        /// <summary>
        /// The tileset gid when the object is linked to a tile
        /// </summary>
        [XmlAttribute] public int gid;
        /// <summary>
        /// An array of properties. Is null if none were defined.
        /// </summary>
        [XmlArrayItem(ElementName="property", Type = typeof(TiledProperty))][XmlArray("properties")] public TiledProperty[] properties { get; set; }

        [XmlAttribute] public bool visible;
    }

    [XmlRoot("tile")]
    /// <summary>
    /// Represents a tile within a tileset
    /// </summary>
    /// <remarks>These are not defined for all tiles within a tileset, only the ones with properties, terrains and animations.</remarks>
    public class TiledTile
    {
        /// <summary>
        /// The tile id
        /// </summary>
        [XmlAttribute] public int id;
        /// <summary>
        /// The custom tile type, set by the user
        /// </summary>
        [XmlAttribute] public string type;
        /// <summary>
        /// The terrain definitions as int array. These are indices indicating what part of a terrain and which terrain this tile represents.
        /// </summary>
        /// <remarks>In the map file empty space is used to indicate null or no value. However, since it is an int array I needed something so I decided to replace empty values with -1.</remarks>
        public int[] terrain;
        /// <summary>
        /// An array of properties. Is null if none were defined.
        /// </summary>
        [XmlArrayItem(ElementName="property", Type = typeof(TiledProperty))][XmlArray("properties")] public TiledProperty[] properties { get; set; }
        /// <summary>
        /// An array of tile animations. Is null if none were defined. 
        /// </summary>
        public TiledTileAnimation[] animation;
        /// <summary>
        /// The individual tile image
        /// </summary>
        [XmlIgnore] public string image;
        [XmlIgnore] public int imageheight;
        [XmlIgnore] public int imagewidth;
        [XmlIgnore] public double probability;
        
        [JsonIgnore][XmlElement("image")]
        public TiledImage imageObj;
    }
    
    public class TiledImage
    {
        /// <summary>
        /// The image width
        /// </summary>
        [XmlAttribute] public int width;
        
        /// <summary>
        /// The image height
        /// </summary>
        [XmlAttribute] public int height;
        
        /// <summary>
        /// The image source path
        /// </summary>
        [XmlAttribute] public string source;
    }

    /// <summary>
    /// Represents a tile animation. Tile animations are a group of tiles which act as frames for an animation.
    /// </summary>
    public class TiledTileAnimation
    {
        /// <summary>
        /// The tile id within a tileset
        /// </summary>
        public int tileid;
        /// <summary>
        /// The duration in miliseconds
        /// </summary>
        public int duration;
    }

    /// <summary>
    /// Represents a terrain definition.
    /// </summary>
    public class TiledTerrain
    {
        /// <summary>
        /// The terrain name
        /// </summary>
        public string name;
        /// <summary>
        /// The tile used as icon for the terrain editor
        /// </summary>
        public int tile;
    }

    /// <summary>
    /// Used as data type for the GetSourceRect method. Represents basically a rectangle.
    /// </summary>
    public class TiledSourceRect
    {
        /// <summary>
        /// The x position in pixels from the tile location in the source image
        /// </summary>
        public int x;
        /// <summary>
        /// The y position in pixels from the tile location in the source image
        /// </summary>
        public int y;
        /// <summary>
        /// The width in pixels from the tile in the source image
        /// </summary>
        public int width;
        /// <summary>
        /// The height in pixels from the tile in the source image
        /// </summary>
        public int height;
    }

    /// <summary>
    /// Represents a layer or object group
    /// </summary>
    public class TiledGroup
    {
        /// <summary>
        /// The group's id
        /// </summary>
        public int id;
        /// <summary>
        /// The group's name
        /// </summary>
        public string name;
        /// <summary>
        /// The group's visibility
        /// </summary>
        public bool visible;
        /// <summary>
        /// The group's locked state
        /// </summary>
        public bool locked;
        /// <summary>
        /// The group's user properties
        /// </summary>
        [XmlArrayItem(ElementName="property", Type = typeof(TiledProperty))]
        [XmlArray("properties")] public TiledProperty[] properties { get; set; }
        /// <summary>
        /// The group's layers
        /// </summary>
        public TiledLayer[] layers;
        /// <summary>
        /// The group's objects
        /// </summary>
        public TiledObject[] objects;
        /// <summary>
        /// The group's subgroups
        /// </summary>
        public TiledGroup[] groups;
    }
}