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
        
        public MonsterInstance(Monster info)
        {
            this._info = info;
            this.Health = Dice.Roll(8,info.Health, info.HealthConst);
            this.MaxHealth = this.Health;
            this.Magic = Dice.Roll(8, info.Magic, info.MagicConst);
            this.MaxMagic = this.Magic;
            this.Attack = info.Attack;
            this.Defence = info.Defence;
            this.Agility = info.Agility;
            this.Name = info.Name;
            this.Level = info.MinLevel;
            this.Xp = info.Xp;
            this.Gold = info.Gold;
            
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

        public int Gold { get; }

        public override IEnumerable<Spell> GetSpells(IEnumerable<Spell> availableSpells)
        {
            return this._info.SpellList.Select(spellId => availableSpells.FirstOrDefault(item => item.Id == spellId))
                .Where(spell => spell != null).ToList();
        }
    }
}