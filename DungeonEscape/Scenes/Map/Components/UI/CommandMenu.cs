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

        private void ShowStatus(Action done)
        {
            var statusWindow = this.canvas.GetComponent<PartyStatusWindow>();
            statusWindow.Show(this.gameState.Party, done);
        }

        private void ShowHeroStatus(Action done)
        {
            var statusWindow = this.canvas.GetComponent<HeroStatusWindow>();
            if (this.gameState.Party.Members.Count == 1)
            {
                statusWindow.Show(this.gameState.Party.Members.First(), done);
            }
            else
            {
                var selectWindow = this.canvas.GetComponent<SelectHeroWindow>();
                selectWindow.Show( this.gameState.Party.Members, hero =>
                {
                    if (hero == null)
                    {
                        done();
                        return;
                    }
                    
                    statusWindow.Show(hero, done);
                });
            }
        }

        private void CastSpell(Hero caster, Spell spell, Action done)
        {
            var talkWindow = this.canvas.GetComponent<TalkWindow>();
            if (caster.Magic < spell.Cost)
            {
                talkWindow.Show($"{caster.Name}: I do not have enough magic to cast {spell.Name}.", done);
                return;
            }

            if (spell.Type == SpellType.Heal)
            {
                if (this.gameState.Party.Members.Count == 1)
                {
                    var result = Spell.CastHeal(this.gameState.Party.Members.First(), caster, spell);
                    talkWindow.Show(result, done);
                }
                else
                {
                    var selectWindow = this.canvas.GetComponent<SelectHeroWindow>();
                    selectWindow.Show(this.gameState.Party.Members.Where(item => item.Spells.Count != 0), hero =>
                    {
                        if (hero == null)
                        {
                            done();
                        }
                        else
                        {
                            talkWindow.Show(Spell.CastHeal(hero, caster, spell), done);
                        }
                    });
                }
            }
            else if (spell.Type == SpellType.Outside)
            {
                var result = Spell.CastOutside(caster, spell, this.gameState);
                if (string.IsNullOrEmpty(result))
                {
                    done();
                }
                else
                {
                    talkWindow.Show(result, done);
                }
            }
            else
            {
                talkWindow.Show($"{caster.Name} casts {spell.Name} but it did not work", done);
            }
        }
        
        private void ShowSpell(Action done)
        {
            var spellWindow = this.canvas.GetComponent<SpellWindow>();
            if (this.gameState.Party.Members.Count == 1)
            {
                spellWindow.Show(this.gameState.Party.Members.First().Spells.Where(item => item.IsNonEncounterSpell), spell=>
                {
                    if (spell == null)
                    {
                        done();
                        return;
                    }

                    this.CastSpell(this.gameState.Party.Members.First(), spell, done);

                });
            }
            else
            {
                var selectWindow = this.canvas.GetComponent<SelectHeroWindow>();
                selectWindow.Show(this.gameState.Party.Members.Where(item => item.Spells.Count != 0), hero =>
                {
                    if (hero == null)
                    {
                        done();
                        return;
                    }
                    
                    spellWindow.Show(hero.Spells.Where(item => item.IsNonEncounterSpell), spell =>
                    {
                        if (spell == null)
                        {
                            done();
                        }
                        
                        this.CastSpell(hero, spell, done);
                    });
                });
            }
        }

        private void ShowItems(Action done)
        {
            if (this.gameState.Party.Items.Count == 0)
            {
                var talkWindow = this.canvas.GetComponent<TalkWindow>();
                talkWindow.Show("The party has no items", done);
            }
            else
            {
                var inventoryWindow = this.canvas.GetComponent<InventoryWindow>();
                var selectWindow = this.canvas.GetComponent<SelectWindow<string>>();
                inventoryWindow.Show(this.gameState.Party.Items, item =>
                {
                    var menuItems = new List<string>();
                    if (this.gameState.Party.Members.Count(hero => hero.CanUseItem(item)) != 0)
                    {
                        menuItems.Add("Use");
                    }


                    menuItems.Add("Drop");
                    selectWindow.Show(menuItems, action =>
                    {
                        switch (action)
                        {
                            case "Use":
                                var selectHero = this.canvas.GetComponent<SelectHeroWindow>();
                                selectHero.Show(this.gameState.Party.Members.Where(hero => hero.CanUseItem(item)),
                                    hero =>
                                    {
                                        var result = this.UseItem(hero, item, this.gameState.Party);
                                        if (string.IsNullOrEmpty(result))
                                        {
                                            done();
                                        }
                                        else
                                        {
                                            var talkWindow = this.canvas.GetComponent<TalkWindow>();
                                            talkWindow.Show(result, done);
                                        }
                                    });
                                break;
                            case "Drop":
                            {
                                if (item.IsEquipped)
                                {
                                    item.Unequip();
                                }

                                this.gameState.Party.Items.Remove(item);
                                var talkWindow = this.canvas.GetComponent<TalkWindow>();
                                talkWindow.Show($"You dropped {item.Name}", done);
                                break;
                            }
                            default:
                                done();
                                break;
                        }
                    });
                });
            }
        }
        
        

        private string UseItem(Hero hero, ItemInstance item, Party party)
        {
            switch (item.Type)
            {
                case ItemType.OneUse:
                    item.Use(hero);
                    party.Items.Remove(item);
                    return  $"{hero.Name} used {item.Name}";
                case ItemType.Armor:
                case ItemType.Weapon:
                case ItemType.Shield:
                    item.Equip(hero);
                    return  $"{hero.Name} equipped {item.Name}";
                default:
                    return $"{hero.Name} is unable to use {item.Name}";
            }
        }

        public override void OnAddedToEntity()
        {
            base.OnAddedToEntity();
            this.showInput = new VirtualButton();
            this.showInput.Nodes.Add(new VirtualButton.KeyboardKey(Keys.E));
            this.showInput.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.X));
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

            if (this.gameState.Party.Items.Count != 0)
            {
                menuItems.Add("Items");
            }
            
            this.Show(menuItems, result =>
            {
                switch (result)
                {
                    case "Status":
                        this.ShowStatus(this.UnPause);
                        break;
                    case "Spells":
                        this.ShowSpell(this.UnPause);
                        break;
                    case "Stats":
                        this.ShowHeroStatus(this.UnPause);
                        break;
                    case "Items":
                        this.ShowItems(this.UnPause);
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