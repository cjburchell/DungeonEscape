// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Redpoint.DungeonEscape.State
{
    using System.Collections.Generic;
    using System.Linq;
    using Nez.Sprites;
    using Nez.Textures;

    public class MonsterInstance : Fighter
    {
        private readonly Monster _info;
        
        public MonsterInstance(Monster info, IGame gameState)
        {
            this._info = info;
            this.Health = Dice.Roll(info.HealthRandom,info.HealthTimes, info.HealthConst);
            this.MaxHealth = this.Health;
            this.Magic = Dice.Roll(info.MagicRandom, info.MagicTimes, info.MagicConst);
            this.MaxMagic = this.Magic;
            this.Attack = info.Attack;
            this.Defence = info.Defence;
            this.MagicDefence = info.MagicDefence;
            this.Agility = info.Agility;
            this.Name = info.Name;
            this.Level = info.MinLevel;
            this.Xp = info.Xp;
            this.Gold = info.Gold;
            
            
            foreach (var item in info.Items.Select(gameState.GetCustomItem).Where(item => item != null))
            {
                this.Items.Add(new ItemInstance(item));
            }
            
            var spriteImage = new Sprite(this._info.Image);
            var spriteFlash = new Sprite(this._info.Flash);
            this.Image.SetSprite(spriteImage);
            this.Animator = new SpriteAnimator(spriteImage)
            {
                Speed = 1.0f
            };
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

        public Rarity Rarity => _info.Rarity;

        public int Gold { get; set; }

        public override IEnumerable<Spell> GetSpells(IEnumerable<Spell> availableSpells)
        {
            return this._info.SpellList.Select(spellId => availableSpells.FirstOrDefault(item => item.Name == spellId))
                .Where(spell => spell != null).ToList();
        }
        
        public override IEnumerable<Skill> GetSkills(IEnumerable<Skill> availableSkills)
        {
            return this._info.SkillList.Select(id => availableSkills.FirstOrDefault(item => item.Name == id))
                .Where(skill => skill != null).ToList();
        }
    }
}