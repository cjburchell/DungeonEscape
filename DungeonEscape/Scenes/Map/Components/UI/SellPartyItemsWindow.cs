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
        private readonly List<Hero> _heroes;

        public SellPartyItemsWindow(UiSystem ui, List<Hero> heroes) : base(ui, null, new Point(20, 20), 700, 48)
        {
            _heroes = heroes;
        }

        protected override Button CreateButton(ItemInstance item)
        {
            var table = new Table();
            var image = new Image(item.Image).SetAlignment(Align.Left);
            var equipSymbol = string.Empty;
            if (item.IsEquipped)
            {
                var equippedHero = this._heroes.FirstOrDefault(hero => hero.Id == item.EquippedTo);
                if (equippedHero != null)
                {
                    equipSymbol = $"(E-{equippedHero.Name})";
                }
            }
            
            var style = item.Rarity switch
            {
                Rarity.Uncommon => "uncommon_label",
                Rarity.Rare => "rare_label",
                Rarity.Epic => "epic_label",
                Rarity.Common => "common_label",
                _ => null
            };
            
            var equip = new Label(equipSymbol, Skin).SetAlignment(Align.Left);
            var itemName = new Label(item.Name, Skin, style).SetAlignment(Align.Left);
            var cost = new Label($"{item.Gold * 3 / 4}", Skin).SetAlignment(Align.Right);
            table.Add(image).Width(32);
            table.Add(itemName).Width(500);
            table.Add(equip).Width(100);
            table.Add(cost).Width(32);

            var button = new Button(Skin, "no_border");
            button.Add(table);
            return button;
        }
    }
}