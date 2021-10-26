using System;
using System.Collections.Generic;
using System.Linq;
using DungeonEscape.Scenes.Common.Components.UI;
using DungeonEscape.State;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Nez;

namespace DungeonEscape.Scenes.Map.Components.UI
{
    public class CommandMenu: SelectWindow<string>, IUpdatable
    {
        private readonly IGame gameState;

        public CommandMenu(UICanvas canvas, WindowInput input, IGame gameState) : base(canvas, input,"Command", new Point(30,30),100)
        {
            this.gameState = gameState;
        }
        
        private VirtualButton showInput;

        private void UnPause()
        {
            this.gameState.IsPaused = false;
        }

        private void ShowStatus()
        {
            var statusWindow = this.canvas.GetComponent<PartyStatusWindow>();
            statusWindow.Show(this.gameState.Party, this.UnPause);
        }

        private void ShowHeroStatus()
        {
            var statusWindow = this.canvas.GetComponent<HeroStatusWindow>();
            if (this.gameState.Party.Members.Count == 1)
            {
                statusWindow.Show(this.gameState.Party.Members.First(), this.UnPause);
            }
            else
            {
                var selectWindow = this.canvas.GetComponent<SelectHeroWindow>();
                selectWindow.Show( this.gameState.Party.Members, hero =>
                {
                    if (hero == null)
                    {
                        this.UnPause();
                        return;
                    }
                    
                    statusWindow.Show(hero, this.UnPause);
                });
            }
        }

        private void ShowSpell()
        {
            var action = new Action<Hero,Spell>((caster, spell) =>
            {
                if (spell == null)
                {
                    this.UnPause();
                    return;
                }
                
                var talkWindow = this.canvas.GetComponent<TalkWindow>();
                if (caster.Magic < spell.Cost)
                {
                    talkWindow.Show($"${caster.Name}: I do not have enough magic to cast {spell.Name}.", () => this.UnPause());
                    return;
                }

                if (spell?.Type == SpellType.Heal)
                {
                    if (this.gameState.Party.Members.Count == 1)
                    {
                        talkWindow.Show($"${caster.Name} I am unable to cast", () => this.UnPause());
                    }
                    else
                    {
                        var selectWindow = this.canvas.GetComponent<SelectHeroWindow>();
                        selectWindow.Show(this.gameState.Party.Members.Where(item => item.Spells.Count != 0), hero =>
                        {
                            if (hero == null)
                            {
                                this.UnPause();
                                return;
                            }
                            
                            //TODO: heal this hero
                        });
                    }
                }
                else
                {
                    talkWindow.Show($"${caster.Name}: I am unable to cast {spell.Name}", () => this.UnPause());
                }
            });
            var spellWindow = this.canvas.GetComponent<SpellWindow>();
            if (this.gameState.Party.Members.Count == 1)
            {
                spellWindow.Show(this.gameState.Party.Members.First().Spells.Where(item => item.IsNonEncounterSpell), spell=> action(this.gameState.Party.Members.First(), spell));
            }
            else
            {
                var selectWindow = this.canvas.GetComponent<SelectHeroWindow>();
                selectWindow.Show(this.gameState.Party.Members.Where(item => item.Spells.Count != 0), hero =>
                {
                    if (hero == null)
                    {
                        this.UnPause();
                        return;
                    }
                    
                    spellWindow.Show(hero.Spells.Where(item => item.IsNonEncounterSpell), spell => action(hero, spell));
                });
            }
        }

        private void ShowItems()
        {
            if (this.gameState.Party.Items.Count == 0)
            {
                var talkWindow = this.canvas.GetComponent<TalkWindow>();
                talkWindow.Show("The party has no items", this.UnPause);
            }
            else
            {
                var inventoryWindow = this.canvas.GetComponent<InventoryWindow>();
                inventoryWindow.Show(this.gameState.Party.Items,_ => this.UnPause() );
            }
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();
            this.showInput = new VirtualButton();
            this.showInput.Nodes.Add(new VirtualButton.KeyboardKey(Keys.E));
            this.showInput.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.X));
        }

        private void ShowEquip()
        {
            var equipWindow = this.canvas.GetComponent<EquipWindow>();
            if (this.gameState.Party.Members.Count == 1)
            {
                equipWindow.Show(this.gameState.Party.Members.First(), this.UnPause);
            }
            else
            {
                var selectWindow = this.canvas.GetComponent<SelectHeroWindow>();
                selectWindow.Show( this.gameState.Party.Members, hero =>
                {
                    if (hero == null)
                    {
                        this.UnPause();
                        return;
                    }
                    
                    equipWindow.Show(hero, this.UnPause);
                });
            }
        }

        public override void OnRemovedFromEntity()
        {
            this.showInput.Deregister();
        }

        public void Update()
        {
            if (this.gameState.IsPaused || this.IsFocused || !this.showInput.IsReleased)
            {
                return;
            }

            var menuItems = new List<string> {"Status", "Stats"};
            if (this.gameState.Party.Members.Count(member => member.Spells.Count != 0) != 0)
            {
                menuItems.Add("Spells");
            }
            menuItems.Add("Items");
            menuItems.Add("Equip");

            this.Show(menuItems, result =>
            {
                switch (result)
                {
                    case "Status":
                        this.ShowStatus();
                        break;
                    case "Spells":
                        this.ShowSpell();
                        break;
                    case "Stats":
                        this.ShowHeroStatus();
                        break;
                    case "Items":
                        this.ShowItems();
                        break;
                    case "Equip":
                        this.ShowEquip();
                        break;
                    default:
                        this.UnPause();
                        break;
                }   
            });
            this.gameState.IsPaused = true;
        }
    }
}