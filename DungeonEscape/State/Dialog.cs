﻿// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Redpoint.DungeonEscape.State
{
    using System.Collections.Generic;

    public class Dialog
    {

        public int? Id { get; set; }
        public int? Quest { get; set; }
        public List<DialogText> Dialogs { get; set; }
    }
}