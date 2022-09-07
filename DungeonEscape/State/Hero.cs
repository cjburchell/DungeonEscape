// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Global
namespace Redpoint.DungeonEscape.State
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Xna.Framework.Graphics;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Nez.Sprites;

    public class Hero : Fighter
    {

        [JsonConverter(typeof(StringEnumConverter))]
        public Class Class { get; set; }
        
        [JsonConverter(typeof(StringEnumConverter))]
        public Gender Gender { get; set; }
        
        public ulong NextLevel { get; set; }

        public override IEnumerable<Spell> GetSpells(IEnumerable<Spell> availableSpells)
        {
            return availableSpells.Where(spell => spell.MinLevel <= this.Level && spell.Classes.Contains(this.Class));
        }
        
        public override IEnumerable<Skill> GetSkills(IEnumerable<Skill> availableSkills)
        {
            return this.Skills.Select(id => availableSkills.FirstOrDefault(item => item.Name == id))
                .Where(skill => skill != null).ToList();
        }

        public Dictionary<Slot, string> Slots = new();
        
        public string Id { get; set; }

        public Hero()
        {
            this.Id = Guid.NewGuid().ToString();
        }

        public void SetupImage(Texture2D heroTexture)
        {
            const int heroHeight = 48;
            const int heroWidth = Scenes.Map.MapScene.DefaultTileSize;
            var flashTexture = Monster.CreateFlashImage(heroTexture);
            var sprites = Nez.Textures.Sprite.SpritesFromAtlas(heroTexture, heroWidth, heroHeight);
            var flashSprites = Nez.Textures.Sprite.SpritesFromAtlas(flashTexture, heroWidth, heroHeight);
            var animationBaseIndex = (int) this.Class * 16 + (int) this.Gender * 8;
            var spriteImage = sprites[animationBaseIndex + 4];
            var spriteFlash = flashSprites[animationBaseIndex + 4];
            this.Image.SetSprite(spriteImage);
            this.Animator = new SpriteAnimator(spriteImage) {Speed = 1.0f};
            this.Animator.AddAnimation("Damage", new[]
            {
                spriteImage,
                spriteFlash,
                spriteImage,
                spriteFlash,
                spriteImage,
                spriteFlash,
                spriteImage,
                spriteFlash
            });
        }

        public void Setup(IGame game, int level = 1, bool generateItems = true)
        {
            this.Level = 1;
            var classStatList = game.ClassLevelStats.ToList();
            var classStats = classStatList.First(stats => stats.Class == this.Class);
            this.Xp = 0;
            this.NextLevel = classStats.FirstLevel;

            // Roll starting stats
            this.MaxHealth = classStats.Stats.First(item => item.Type == StatType.Health).RollStartValue();
            this.Attack = classStats.Stats.First(item => item.Type == StatType.Attack).RollStartValue();
            this.Defence = classStats.Stats.First(item => item.Type == StatType.Defence).RollStartValue();
            this.MagicDefence = classStats.Stats.First(item => item.Type == StatType.MagicDefence).RollStartValue();
            this.MaxMagic = classStats.Stats.First(item => item.Type == StatType.Magic).RollStartValue();
            this.Agility = classStats.Stats.First(item => item.Type == StatType.Agility).RollStartValue();
            this.Skills = classStats.Skills.ToList();

            this.Health = this.MaxHealth;
            this.Magic = this.MaxMagic;
            while (this.Level < level)
            {
                this.Xp = this.NextLevel;
                this.CheckLevelUp(classStatList, null, out _);
            }

            if (!generateItems) return;

            this.Items = new List<ItemInstance>();

            var armor = game.CreateRandomEquipment(this.Level, Math.Max(this.Level - 5, 1), Rarity.Common,
                ItemType.Armor,
                this.Class, Slot.Chest);
            if (armor != null)
            {
                var item = new ItemInstance(armor);
                this.Items.Add(item);
                this.Equip(item);
            }

            var weapon = game.CreateRandomEquipment(this.Level, Math.Max(this.Level - 5, 1), Rarity.Common,
                ItemType.Weapon,
                this.Class);
            if (weapon != null)
            {
                var item = new ItemInstance(weapon);
                this.Items.Add(item);
                this.Equip(item);
            }
        }

        public List<string> Skills { get; set; }

        public bool CheckLevelUp(IEnumerable<ClassStats> classLevels, IEnumerable<Spell> availableSpells, out string levelUpMessage)
        {
            if (this.Xp < this.NextLevel)
            {
                levelUpMessage = null;
                return false;
            }
            
            var classStats = classLevels.First(stats => stats.Class == this.Class);
            
            var oldLevel = this.Level;
            this.Level++;
            this.NextLevel = CalculateNextLevel(oldLevel, this.NextLevel);

            levelUpMessage = $"{this.Name} has advanced to level {this.Level}\n";
            
            var health = classStats.Stats.First( item=> item.Type == StatType.Health).RollNextValue();
            if (health != 0)
            {
                levelUpMessage += $"Health +{health}\n";
            }
            var attack = classStats.Stats.First( item=> item.Type == StatType.Attack).RollNextValue();
            if (attack != 0)
            {
                levelUpMessage += $"Attack +{attack}\n";
            }
            var defence = classStats.Stats.First(item => item.Type == StatType.Defence).RollNextValue();
            if (defence != 0)
            {
                levelUpMessage += $"Defence +{defence}\n";
            }
            
            var magicDefence = classStats.Stats.First(item => item.Type == StatType.MagicDefence).RollNextValue();
            if (magicDefence != 0)
            {
                levelUpMessage += $"Defence +{magicDefence}\n";
            }
            
            var magic = classStats.Stats.First( item=> item.Type == StatType.Magic).RollNextValue();
            if (magic != 0)
            {
                levelUpMessage += $"Magic +{magic}\n";
            }
            var agility = classStats.Stats.First( item=> item.Type == StatType.Agility).RollNextValue();
            if (agility != 0)
            {
                levelUpMessage += $"Agility +{agility}\n";
            }
            
            this.MaxHealth += health;
            this.Attack += attack;
            this.Defence += defence;
            this.MagicDefence += magicDefence;
            this.MaxMagic += magic;
            this.Agility += agility;

            if (availableSpells != null)
            {
                levelUpMessage = availableSpells.Where(spell => spell.MinLevel <= this.Level && spell.MinLevel > oldLevel && spell.Classes.Contains(this.Class)).Aggregate(levelUpMessage, (current, spell) => current + $"Has learned the {spell.Name} Spell\n");
            }
            
            levelUpMessage += $"Next Level is {this.NextLevel} XP\n";
            
            this.Magic = this.MaxMagic;
            this.Health = this.MaxHealth;
            return true;
        }

        private static ulong CalculateNextLevel(int oldLevel, ulong currentLevel)
        {
            var factors = new Dictionary<int, double>
            {
                {1, 3.0},
                {2, 2.0},
                {3, 1.75},
                {4, 1.65},
                {5, 1.5},
                {10, 1.35},
                {15, 1.2},
                {20, 1.1},
                {45, 1}
            };

            var factor = 1.0;
            foreach (var (key, value) in factors)
            {
                if (oldLevel < key)
                {
                    break;
                }

                factor = value;
            }

            const double randomFactor = 0.05;

            var nextLevel = (ulong) (currentLevel * factor) +
                             (ulong) Nez.Random.NextInt((int) (Math.Min(currentLevel, int.MaxValue) * randomFactor));
            
            Console.WriteLine($"{oldLevel}: {factor} {nextLevel} {nextLevel - currentLevel}");
            return nextLevel;
        }

        public bool CanUseItem(ItemInstance item)
        {
            return !this.IsDead && !item.IsEquipped && (item.Type is ItemType.OneUse or ItemType.RepeatableUse || item.IsEquippable) &&
                   (!item.IsEquippable || item.Classes == null || item.Classes.Contains(this.Class));
        }
        
        public void UnEquip(ItemInstance item)
        {
            if (!item.IsEquipped)
            {
                return;
            }
            
            if (!item.Slots.Any(slot => this.Slots.ContainsKey(slot) && this.Slots[slot] == item.Id))
            {
                return;
            }
            
            item.EquippedTo = null;
            item.IsEquipped = false;
            foreach (var slot in item.Slots)
            {
                this.Slots[slot] = null;
            }
            
            this.Agility -= item.Agility;
            this.Attack -= item.Attack;
            this.Defence -= item.Defence;
            this.MagicDefence -= item.MagicDefence;
            this.MaxHealth -= item.Health;

            if (this.Health > this.MaxHealth)
            {
                this.Health = this.MaxHealth;
            }
        }

        public override List<string> GetEquipmentId(IEnumerable<Slot> slots)
        {
            return (from slot in slots where this.Slots.ContainsKey(slot) select this.Slots[slot]).ToList();
        }

        public override void Equip(ItemInstance item)
        {
            foreach (var slot in item.Slots)
            {
                this.Slots[slot] = item.Id;
            }
            item.IsEquipped = true;
            item.EquippedTo = this.Id;
            this.Agility += item.Agility;
            this.Attack += item.Attack;
            this.Defence += item.Defence;
            this.MagicDefence += item.MagicDefence;
            this.MaxHealth += item.Health;
        }
    }
}