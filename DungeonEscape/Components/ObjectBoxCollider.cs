﻿using Microsoft.Xna.Framework;
using Nez;
using Nez.Tiled;

namespace DungeonEscape.Components
{
    public class ObjectBoxCollider : BoxCollider
    {
        public TmxObject Object { get; private set; }

        public ObjectBoxCollider(TmxObject tmxObject, Rectangle rectangle) : base(rectangle)
        {
            this.Object = tmxObject;
        }
    }
}