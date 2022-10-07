using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Nez;
using Redpoint.DungeonEscape.Scenes.Common.Components.UI;
using Redpoint.DungeonEscape.Scenes.Map.Components;
using Redpoint.DungeonEscape.Scenes.Map.Components.Objects;
using Redpoint.DungeonEscape.Scenes.Map.Components.UI;
using Redpoint.DungeonEscape.State;

namespace Redpoint.DungeonEscape.Scenes.Map
{
    public class MapInput
    {
        private readonly VirtualButton _showCommandWindowInput;
        private readonly VirtualButton _showInventoryWindowInput;
        private readonly VirtualButton _showSpellWindowInput;
        private readonly VirtualButton _showExitWindowInput;

        public MapInput()
        {
            this._showCommandWindowInput = new VirtualButton();
            this._showCommandWindowInput.Nodes.Add(new VirtualButton.KeyboardKey(Keys.E));
            this._showCommandWindowInput.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.X));
            
            this._showInventoryWindowInput = new VirtualButton();
            this._showInventoryWindowInput.Nodes.Add(new VirtualButton.KeyboardKey(Keys.I));
            this._showInventoryWindowInput.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.Y));
            
            this._showSpellWindowInput = new VirtualButton();
            this._showSpellWindowInput.Nodes.Add(new VirtualButton.KeyboardKey(Keys.P));
            this._showSpellWindowInput.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.RightShoulder));
            
            this._showExitWindowInput = new VirtualButton();
            this._showExitWindowInput.Nodes.Add(new VirtualButton.KeyboardKey(Keys.Escape));
            this._showExitWindowInput.Nodes.Add(new VirtualButton.GamePadButton(0, Buttons.Start));
        }

        public void HandleInput(UiSystem ui, IPlayer player, IGame gameState
        )
        {
            void UnPause()
            {
                gameState.IsPaused = false;
            }

            if (this._showCommandWindowInput.IsReleased)
            {
                ShowCommandWindow(player, gameState, ui,  UnPause);
            }
            else if (_showInventoryWindowInput.IsReleased)
            {
                if (gameState.Party.AliveMembers.Any(member => member.Items.Count != 0))
                {
                    gameState.IsPaused = true;
                    ShowItems(ui, gameState, player,  UnPause );
                }
            }
            else if (_showSpellWindowInput.IsReleased)
            {
                if (gameState.Party.AliveMembers.Any(member => member.GetSpells(gameState.Spells).Any()))
                {
                    gameState.IsPaused = true;
                    ShowSpell(ui, gameState, player, UnPause);
                }
            }
            else if (this._showExitWindowInput.IsReleased)
            {
                ui.Input.HandledHide = true;
                gameState.IsPaused = true;
                var commandMenu = new SelectWindow<string>(ui, null, new Point(20, 20), 200);
                var options = new List<string> { "New Game" };
                if (gameState.LoadableGameSaves.Any(item => !item.IsEmpty))
                {
                    options.Add("Load Game");
                }

                options.Add("Settings");
                options.Add("Quit");

                commandMenu.Show(options, result =>
                {
                    switch (result)
                    {
                        case "New Game":
                            gameState.ShowNewQuest();
                            break;
                        case "Load Game":
                            gameState.ShowLoadQuest();
                            break;
                        case "Settings":
                            gameState.ShowSettings();
                            break;
                        case "Quit":
                            Core.Exit();
                            break;
                        default:
                            UnPause();
                            break;
                    }
                });
            }
        }

        private static void ShowCommandWindow(IPlayer player, IGame gameState, UiSystem ui, Action done)
        {
            var menuItems = new List<string>();
            if (player.CurrentlyOverObjects.Any(i => i.State.Type == SpriteType.Npc && i.CanDoAction()))
            {
                menuItems.Add("Talk");
            }

            if (player.CurrentlyOverObjects.Any(i => i.State.Type is SpriteType.Door && i.CanDoAction()))
            {
                menuItems.Add("Open");
            }

            if (player.CurrentlyOverObjects.Any(i =>
                    i.State.Type is SpriteType.HiddenItem or SpriteType.Chest && i.CanDoAction()))
            {
                menuItems.Add("Search");
            }

            menuItems.Add("Status");
            if (gameState.Party.AliveMembers.Any(member => member.GetSpells(gameState.Spells).Any()))
            {
                menuItems.Add("Spells");
            }

            if (gameState.Party.AliveMembers.Any(member => member.Items.Count != 0))
            {
                menuItems.Add("Items");
            }

            if (gameState.Party.Members.Count > 1 && gameState.Party.CurrentMapIsOverWorld)
            {
                menuItems.Add("Party");
            }

            if (gameState.Party.ActiveQuests.Any())
            {
                menuItems.Add("Quests");
            }

            gameState.IsPaused = true;
            var commandMenu = new CommandMenu(ui);
            commandMenu.Show(menuItems, result =>
            {
                switch (result)
                {
                    case "Spells":
                        ShowSpell(ui, gameState, player, done);
                        break;
                    case "Status":
                        ShowHeroStatus(ui, gameState, done);
                        break;
                    case "Items":
                        ShowItems(ui, gameState, player, done);
                        break;
                    case "Quests":
                        ShowQuests( ui, gameState, done);
                        break;
                    case "Party":
                        ShowParty(ui, gameState, done);
                        break;
                    case "Talk":
                        DoTalk(player, done);
                        break;
                    case "Search":
                        DoSearch(player, done);
                        break;
                    case "Open":
                        DoOpen(player, done);
                        break;
                    default:
                        done();
                        break;
                }
            });
        }
        
        private static void DoOpen(IPlayer player, Action done)
        {
            var doors = player.CurrentlyOverObjects.Where(i => i.State.Type == SpriteType.Door && i.CanDoAction()).ToList();
            if (!doors.Any())
            {
                done();
                return;
            }

            if (doors.Count != 1)
            {
                Debug.Warn("found more than one door");
            }

            doors.First().OnAction(done);
        }

        private static void DoSearch(IPlayer player, Action done)
        {
            var chests = player.CurrentlyOverObjects.Where(i => i.State.Type is SpriteType.Chest or SpriteType.HiddenItem && i.CanDoAction()).ToList();
            if (!chests.Any())
            {
                done();
                return;
            }

            if (chests.Count != 1)
            {
                Debug.Warn("found more than one chest");
            }

            chests.First().OnAction(done);
        }

        private static void DoTalk(IPlayer player, Action done)
        {
            var npc = player.CurrentlyOverObjects.Where(i => i.State.Type == SpriteType.Npc && i.CanDoAction()).ToList();
            if (!npc.Any())
            {
                done();
                return;
            }

            if (npc.Count != 1)
            {
                Debug.Warn("found more than one npc");
            }

            npc.First().OnAction(done);
        }

        private static void ShowQuests(UiSystem ui, IGame gameState, Action done)
        {
            new QuestWindow(ui).Show(gameState.Party.ActiveQuests, gameState.Quests, done);
        }
        
        private static void ShowParty(UiSystem ui, IGame gameState, Action done)
        {
            new PartyWindow(ui).Show(gameState, done);
        }

        private static void ShowHeroStatus(UiSystem ui, IGame gameState, Action done)
        {
            if (gameState.Party.ActiveMembers.Count() == 1)
            {
                var statusWindow = new HeroStatusWindow(ui);
                var hero = gameState.Party.ActiveMembers.First();
                statusWindow.Show(hero, done);
            }
            else
            {
                var selectWindow = new SelectHeroWindow(ui);
                selectWindow.Show( gameState.Party.ActiveMembers, hero =>
                {
                    if (hero == null)
                    {
                        done();
                        return;
                    }
                    var statusWindow = new HeroStatusWindow(ui);
                    statusWindow.Show(hero, done);
                });
            }
        }

        private static void CastSpell(IFighter caster, Spell spell, IGame gameState, UiSystem ui, IPlayer player, Action done)
        {
            switch (spell.Targets)
            {
                case Target.Group:
                {
                    var result = spell.Cast(gameState.Party.ActiveMembers,null, caster, gameState);
                    if (string.IsNullOrEmpty(result))
                    {
                        done();
                        return;
                    }
                
                    new TalkWindow(ui).Show(result, done);
                    return;
                }
                case Target.Object:
                {
                    var result = spell.Cast(null, player.CurrentlyOverObjects.Select(i => i.State) , caster, gameState);
                    if (string.IsNullOrEmpty(result))
                    {
                        done();
                        return;
                    }
                
                    new TalkWindow(ui).Show(result, done);
                    break;
                }
                case Target.Single:
                {
                    Func<Hero, bool> filter = hero => !hero.IsDead;
                    if (spell.Type == SkillType.Revive)
                    {
                        filter = hero => hero.IsDead;
                    }

                    if (gameState.Party.ActiveMembers.Count(filter) == 1 && spell.Type != SkillType.Revive)
                    {
                        var result = spell.Cast(gameState.Party.ActiveMembers.Where(filter), null, caster, gameState);
                        new TalkWindow(ui).Show(result, done);
                        return;
                    }

                    new SelectHeroWindow(ui).Show(gameState.Party.ActiveMembers.Where(filter), target =>
                    {
                        if (target == null)
                        {
                            done();
                            return;
                        }

                        new TalkWindow(ui).Show(spell.Cast(new[] { target }, null, caster, gameState), done);
                    });
                    return;
                }
                case Target.None:
                {
                    var result = spell.Cast(null,null, caster, gameState);
                    if (string.IsNullOrEmpty(result))
                    {
                        done();
                        return;
                    }

                    new TalkWindow(ui).Show(result, done);
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void ShowSpell(UiSystem ui, IGame gameState, IPlayer player, Action done)
        {
            if (gameState.Party.AliveMembers.Count(member => member.GetSpells(gameState.Spells).Any()) == 1)
            {
                var hero = gameState.Party.AliveMembers.First(member => member.GetSpells(gameState.Spells).Any());
                var spellWindow = new SpellWindow(ui, hero);
                spellWindow.Show(hero.GetSpells(gameState.Spells).Where(item => item.IsNonEncounterSpell), spell=>
                {
                    if (spell == null)
                    {
                        done();
                        return;
                    }

                    CastSpell(hero, spell, gameState, ui, player,  done);

                });
            }
            else
            {
                var selectWindow = new SelectHeroWindow(ui);
                selectWindow.Show(gameState.Party.AliveMembers.Where(item => item.GetSpells(gameState.Spells).Any(spell => spell.IsNonEncounterSpell)), hero =>
                {
                    if (hero == null)
                    {
                        done();
                        return;
                    }
                    
                    var spellWindow = new SpellWindow(ui, hero);
                    spellWindow.Show(hero.GetSpells(gameState.Spells).Where(item => item.IsNonEncounterSpell), spell =>
                    {
                        if (spell == null)
                        {
                            done();
                            return;
                        }
                        
                        CastSpell(hero, spell, gameState, ui, player, done);
                    });
                });
            }
        }

        private static void ShowItems(UiSystem ui, IGame gameState, IPlayer player, Action done)
        {
            if (gameState.Party.AliveMembers.All(i => i.Items.Count == 0))
            {
                new TalkWindow(ui).Show("The party has no items", done);
            }
            else
            {
                void SelectItems(Hero selectedHero)
                {
                    if (selectedHero == null)
                    {
                        done();
                        return;
                    }

                    var inventoryWindow = new InventoryWindow(ui, selectedHero);
                    inventoryWindow.Show(selectedHero.Items, item =>
                    {
                        if (item == null)
                        {
                            done();
                            return;
                        }

                        var menuItems = new List<string>();
                        if (selectedHero.CanUseItem(item))
                        {
                            menuItems.Add("Use");
                        }

                        if (selectedHero.CanEquipItem(item))
                        {
                            menuItems.Add("Equip");
                        }

                        if (gameState.Party.AliveMembers.Count() != 1 &&
                            gameState.Party.AliveMembers.Any(hero =>
                                hero.Items.Count < Party.MaxItems && hero != selectedHero))
                        {
                            menuItems.Add("Transfer");
                        }

                        if (item.Type != ItemType.Quest)
                        {
                            menuItems.Add("Drop");
                        }

                        if (!menuItems.Any())
                        {
                            done();
                            return;
                        }


                        var selectWindow = new SelectWindow<string>(ui, null, new Point(20, 20));
                        selectWindow.Show(menuItems, action =>
                        {
                            switch (action)
                            {
                                case "Transfer":
                                {
                                    var selectHero = new SelectHeroWindow(ui);
                                    selectHero.Show(
                                        gameState.Party.AliveMembers.Where(hero =>
                                            hero.Items.Count < Party.MaxItems && hero != selectedHero),
                                        hero =>
                                        {
                                            if (hero == null)
                                            {
                                                done();
                                                return;
                                            }

                                            if (item.IsEquipped)
                                            {
                                                item.UnEquip(gameState.Party.ActiveMembers);
                                            }

                                            selectedHero.Items.Remove(item);
                                            hero.Items.Add(item);

                                            new TalkWindow(ui).Show(
                                                $"{selectedHero.Name} gave {item.Name} {hero.Name}", done);

                                        });
                                    break;
                                }
                                case "Equip":
                                {
                                    var (message, _) = UseItem(gameState, selectedHero, selectedHero, null, item);
                                    if (string.IsNullOrEmpty(message))
                                    {
                                        done();
                                    }
                                    else
                                    {
                                        new TalkWindow(ui).Show(message, done);
                                    }

                                    break;
                                }
                                case "Use":
                                    switch (item.Target)
                                    {
                                        case Target.Single:
                                            if (gameState.Party.AliveMembers.Count() == 1)
                                            {
                                                var (message, _) = UseItem(gameState, selectedHero,
                                                    gameState.Party.AliveMembers.First(), null, item);
                                                if (string.IsNullOrEmpty(message))
                                                {
                                                    done();
                                                }
                                                else
                                                {
                                                    new TalkWindow(ui).Show(message, done);
                                                }
                                            }
                                            else
                                            {
                                                var selectHero = new SelectHeroWindow(ui);
                                                selectHero.Show(
                                                    gameState.Party.AliveMembers.Where(hero =>
                                                        hero.CanUseItem(item)),
                                                    hero =>
                                                    {
                                                        if (hero == null)
                                                        {
                                                            done();
                                                            return;
                                                        }

                                                        var (message, _) = UseItem(gameState, selectedHero, hero, null,
                                                            item);
                                                        if (string.IsNullOrEmpty(message))
                                                        {
                                                            done();
                                                        }
                                                        else
                                                        {
                                                            new TalkWindow(ui).Show(message, done);
                                                        }
                                                    });
                                            }

                                            break;
                                        case Target.Group:
                                        {
                                            var firstTime = true;
                                            var message = "";
                                            foreach (var member in gameState.Party.AliveMembers.Cast<IFighter>())
                                            {
                                                var (resultMessage, result) = UseItem(gameState, selectedHero, member
                                                    , null, item, firstTime);
                                                if (result)
                                                {
                                                    firstTime = false;
                                                }
                                                
                                                message += $"{resultMessage}\n";
                                            }

                                            if (string.IsNullOrEmpty(message))
                                            {
                                                done();
                                            }
                                            else
                                            {
                                                new TalkWindow(ui).Show(message, done);
                                            }

                                        }
                                            break;
                                        case Target.Object:
                                        {
                                            var firstTime = true;
                                            var message = "";
                                            foreach (var overObject in player.CurrentlyOverObjects)
                                            {
                                                var (resultMessage, result) = UseItem(gameState, selectedHero, null
                                                    , overObject.State, item, firstTime);

                                                if (result)
                                                {
                                                    firstTime = false;
                                                }
                                                
                                                message += $"{resultMessage}\n";
                                            }

                                            if (string.IsNullOrEmpty(message))
                                            {
                                                done();
                                            }
                                            else
                                            {
                                                new TalkWindow(ui).Show(message, done);
                                            }

                                            break;
                                        }
                                        case Target.None:
                                        {
                                            var (message,_) = UseItem(gameState, selectedHero,
                                                null, null, item);
                                            if (string.IsNullOrEmpty(message))
                                            {
                                                done();
                                            }
                                            else
                                            {
                                                new TalkWindow(ui).Show(message, done);
                                            }

                                            break;
                                        }
                                        default:
                                            throw new ArgumentOutOfRangeException();
                                    }
                                    break;
                                case "Drop":
                                {
                                    if (item.IsEquipped)
                                    {
                                        item.UnEquip(gameState.Party.ActiveMembers);
                                    }

                                    selectedHero.Items.Remove(item);
                                    new TalkWindow(ui).Show($"{selectedHero.Name} dropped {item.Name}", done);
                                    break;
                                }
                                default:
                                    done();
                                    break;
                            }
                        });
                    });
                }

                if (gameState.Party.AliveMembers.Count() == 1)
                {
                    var hero = gameState.Party.AliveMembers.First();
                    SelectItems(hero);
                }
                else
                {
                    var selectHero = new SelectHeroWindow(ui);
                    selectHero.Show(gameState.Party.AliveMembers.Where(hero => hero.Items.Count != 0),
                        SelectItems
                    );
                }
            }
        }

        public static (string, bool) UseItem(IGame game, IFighter source, IFighter target, BaseState targetObject, ItemInstance item, bool ignoreCharges = false)
        {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (item.Type)
            {
                case ItemType.OneUse:
                {
                    var (message ,result) = item.Use(source, target, targetObject, game, 0);
                    if (result)
                    {
                        source.Items.Remove(item);
                    }
                    
                    return (message, result);
                }
                case ItemType.RepeatableUse:
                {
                    var (message, result) = item.Use(source, target, targetObject, game, 0, ignoreCharges);
                    if (!item.HasCharges)
                    {
                        source.Items.Remove(item);
                        message += " and has been destroyed.";
                    }

                    return (message, result);
                }
                case ItemType.Armor:
                case ItemType.Weapon:
                    var oldItems = target.Items.Where(i => target.GetEquipmentId(item.Slots).Contains(i.Id)).ToList();
                    var oldStats = target.Stats.ToList();
                    foreach (var oldItem in oldItems)
                    {
                        oldItem.UnEquip(game.Party.ActiveMembers);
                    }
                    
                    item.UnEquip(game.Party.ActiveMembers);
                    target.Equip(item);
                    var results = "";
                    foreach (var newStat in target.Stats.ToList())
                    {
                        var oldStat = oldStats.FirstOrDefault(i => i.Type == newStat.Type);
                        if (oldStat == null || oldStat.Value == newStat.Value)
                        {
                            continue;
                        }
                        
                        var value = newStat.Value - oldStat.Value;
                        var direction = value > 0 ? "Increased" : "Decreased";
                        results += $"\n{oldStat.Type} {direction} by {Math.Abs(value)}";
                    }
                    
                    if (oldItems.Count == 0)
                    {
                        return ($"{target.Name} put on the {item.Name}{results}", true);
                    }

                    var itemList = oldItems.Aggregate("", (current, oldItem) => current + (string.IsNullOrEmpty(current) ? $" {oldItem.Name}" : $" and {oldItem.Name}"));
                    return  ($"{target.Name} took off{itemList}, and put on the {item.Name}{results}", true);
                    
                default:
                    return ($"{target.Name} is unable to use {item.Name}", false);
            }
        }
    }
}