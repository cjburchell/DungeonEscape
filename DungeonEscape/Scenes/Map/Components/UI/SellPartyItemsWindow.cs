using System.Collections.Generic;
using System.Linq;

namespace Redpoint.DungeonEscape.Scenes.Map.Components.UI
{
    using Common.Components.UI;
    using Microsoft.Xna.Framework;
    using Nez.UI;
    using State;

    public class SellPartyItemsWindow : SelectWindow<ItemInstance>
    {
        private readonly IEnumerable<Hero> _heroes;

        public SellPartyItemsWindow(UiSystem ui, IEnumerable<Hero> heroes) : base(ui, null, new Point(20, 20), 700, 48)
        {
            _heroes = heroes;
        }

        protected override Button CreateButton(ItemInstance item)
        {
            var table = new Table();
            var image = new Image(item.Image);
            var equipSymbol = string.Empty;
            if (item.IsEquipped)
            {
                if (_heroes.Any(hero => hero.Name == item.EquippedTo))
                {
                    equipSymbol = "(E)";
                }
            }
            
            var style = item.Rarity switch
            {
                Rarity.Uncommon => "uncommon_label",
                Rarity.Rare => "rare_label",
                Rarity.Epic => "epic_label",
                Rarity.Common => "common_label",
                Rarity.Legendary => "legendary_label",
                _ => null
            };

            var equip = new Label(equipSymbol, Skin);
            var itemName = new Label(item.NameWithStats, Skin, style);
            var cost = new Label($"{item.Gold * 3 / 4}", Skin);
            table.Add(image).Width(48).Left();
            table.Add(itemName).Width(500).Left();
            table.Add(equip).Width(100).Left();
            table.Add(cost).Width(32).Right();

            var button = new Button(Skin, "no_border");
            button.Add(table).Left();
            return button;
        }
    }
}