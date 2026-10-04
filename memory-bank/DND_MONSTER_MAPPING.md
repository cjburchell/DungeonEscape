# D&D Monster Mapping

This is the working map from Dungeon Escape's current monster roster to D&D-style equivalents for the next monster balance pass.

Use this as design guidance before changing `DungeonEscape.Unity/Assets/DungeonEscape/Data/allmonsters.json`. Prefer SRD/public reference monsters when a close equivalent exists. Keep distinct Dragon Quest-style monsters as custom stat blocks inspired by nearby D&D creatures rather than forcing exact renames.

## Rename/Stat Pass Status

The first implementation pass has renamed the active monster roster toward D&D/SRD-style names, populated D&D identity fields, traits, and actions, and updated map encounter references. The original art/image IDs, biomes, group sizes, rarity, and rough world placement were preserved.

Monster-side legacy JRPG combat fields `Attack`, `Defence`, `MagicDefence`, and `Agility` were removed from `allmonsters.json` and `Monster`. `MonsterInstance` now derives compatibility values for older skill/effect paths from D&D fields:

- `Attack` from damage dice and damage bonus.
- `Defence` from Armor Class.
- `MagicDefence` from Wisdom modifier plus proficiency.
- `Agility` from Dexterity.

Hero-side legacy combat fields remain for now because equipment, buffs/debuffs, skills, and menu display still use them.

## Mapping Approach

- **Direct**: close enough to use the D&D monster as the first balance target.
- **Close**: use the D&D monster's role, CR range, defenses, and attack shape, but keep Dungeon Escape identity.
- **Custom**: no clean D&D equivalent; build a custom stat block using the listed inspiration.

## Current Roster Mapping

| Monster | Current CR | Suggested D&D Anchor | Fit | Balance Notes |
| --- | ---: | --- | --- | --- |
| Killerpillar | 1 | Giant Centipede / Giant Poisonous Snake | Close | Keep as low-tier venomous vermin; tune around poison bite rather than raw damage. |
| Bodkin Archer | 1/8 | Goblin / Scout | Close | Small ranged humanoid. Use shortbow-style ranged attack and low HP. |
| Hell hornet | 1 | Giant Wasp | Direct | Flying poisonous insect; good direct anchor. |
| Scissor beetle | 1/8 | Giant Fire Beetle | Close | Small armored insect. Could trade light trait for pincer flavor. |
| Ram raider | 3 | Giant Goat / Minotaur | Close | Charge-based bruiser. Current CR 3 suggests beefier than goat, lighter than minotaur. |
| Bodkin bowyer | 1 | Goblin Boss / Scout | Close | Elite archer version of Bodkin Archer. |
| Venom wasp | 3 | Giant Wasp | Close | Stronger poisonous flyer. Scale HP/damage above direct giant wasp. |
| Slime | 1/8 | Gray Ooze / Ochre Jelly | Close | Iconic custom slime; use ooze traits lightly, but much weaker than most D&D oozes. |
| Army ant | 1/8 | Swarm of Insects / Giant Fire Beetle | Close | Low-tier group insect. Consider swarm-style identity if group encounters matter. |
| Sabrecat | 3 | Tiger / Saber-Toothed Tiger | Close | Fast pouncer. Current CR 3 points closer to saber-toothed tiger role. |
| Metal Slime | 1 | Custom evasive ooze | Custom | Keep very high AC, tiny HP, high XP identity; not a normal D&D monster pattern. |
| Blue Dragon | 10 | Young Blue Dragon | Direct | Current CR 10 is close to young blue dragon territory. Add breath/save pass later. |
| Eveel | 5 | Giant Constrictor Snake / Giant Poisonous Snake | Close | Aquatic eel-serpent. Needs bite/constrict or poison identity. |
| Thriller wave | 3 | Water Weird | Direct | Aquatic animated water threat; strong thematic fit. |
| Poison eveel | 1/2 | Giant Poisonous Snake | Close | Lower-tier poisonous serpent/eel. Current AC/HP may be high for CR 1/2. |
| Sea fortress | 5 | Giant Shark / Dragon Turtle inspiration | Custom | Shell/fortress identity needs custom aquatic brute, not full dragon turtle power. |
| Flython | 5 | Flying Snake / Wyvern inspiration | Custom | Flying serpent. Use snake chassis plus flight; avoid full wyvern unless late-game. |
| Ocker | 3 | Ochre Jelly | Direct | Name/theme strongly suggests ochre jelly. Current CR is higher than SRD ochre jelly, so scale deliberately. |
| Eyelasher | 3 | Spectator / eye-monster inspiration | Custom | Avoid copying non-SRD beholder-style identity; custom eye stalk/controller role. |
| Tentacular | 8 | Giant Octopus / Kraken inspiration | Custom | Large tentacled aquatic boss. Use grapple/reach; far below true kraken. |
| Ethereal serpent | 3 | Phase Spider / Ghost inspiration | Custom | Ethereal mobility or resistance gimmick. Needs custom action/trait support. |
| King squid | 5 | Giant Octopus / Kraken inspiration | Custom | Mid-tier tentacle monster; likely grapple and ink/escape traits later. |
| Plated goretoise | 8 | Ankylosaurus / Triceratops | Close | Armored charging beast. High HP and shell AC are core identity. |
| Drackal | 5 | Wyvern / Young Dragon inspiration | Custom | Draconic beast below true dragon. Keep bite/claw/flight if sprite supports it. |
| Cobra | 1/8 | Poisonous Snake | Direct | Small venomous snake. |
| Cobra kaiser | 3 | Giant Poisonous Snake | Close | Elite snake; current CR 3 needs scaled HP/damage. |
| Shadow | 3 | Shadow | Direct | Strong direct undead anchor. Strength drain can become a later special action. |
| Armoured scorpion | 1 | Giant Scorpion | Close | Current CR lower than SRD giant scorpion; use smaller armored scorpion variant. |
| Fuddlestick | 1/4 | Twig Blight / Animated Shrub | Close | Confusing plant/stick enemy. Add minor control/confusion later if desired. |
| Hades condor | 3 | Giant Eagle / Vrock inspiration | Custom | Dark flying predator. Current CR 3 is stronger than giant eagle, far below vrock. |
| Troll | 4 | Troll | Direct | Direct anchor. Regeneration is the key future trait. |
| Dreadful drackal | 6 | Wyvern / Young Dragon inspiration | Custom | Stronger drackal. Candidate for breath/poison special once monster actions exist. |
| Manticore | 6 | Manticore | Direct | Direct anchor, but current CR is higher than SRD manticore; scale as elite. |
| Franticore | 10 | Chimera / Manticore inspiration | Custom | Boss-tier manticore variant. Strong multiattack/ranged spike candidate. |
| Estark the Destroyer | 10 | Fire Giant / Horned Devil / Balor inspiration | Custom | Major boss. Use D&D math for CR 10, not the full high-CR fiend stat block. |
| Jackanape | 1 | Ape / Baboon | Close | Monkey enemy; CR 1 suggests ape-like bruiser. |
| Badboon | 1 | Baboon / Ape | Close | Variant monkey enemy; can be weaker/more numerous than Jackanape. |
| Ursa minor | 3 | Brown Bear / Polar Bear | Close | Bear bruiser. CR 3 suggests boosted bear. |
| Great troll | 6 | Troll | Close | Elite troll. Regeneration and heavy melee when monster traits exist. |
| Ratscal | 1/8 | Giant Rat | Direct | Direct low-tier rat anchor. |
| Walking stick | 1/8 | Twig Blight / Animated Shrub | Close | Weak stick/insect ambusher. |
| Bullmustiff | 1 | Mastiff / Dire Wolf | Close | Dog/wolf bruiser. Use pack tactics later if group behavior is added. |
| Garuda | 3 | Giant Eagle / Griffon | Close | Mythic bird attacker. Griffon-like math may fit better than eagle. |
| Ursa major | 5 | Polar Bear / Owlbear inspiration | Close | Bigger bear. If avoiding non-SRD owlbear stats, use boosted polar bear math. |
| Striking sabrecat | 3 | Saber-Toothed Tiger | Direct | Direct pouncing big-cat anchor. |
| Green Dragon | 4 | Green Dragon Wyrmling / Young Green Dragon inspiration | Close | Current CR 4 sits between wyrmling and young green dragon. |
| GrayBear | 10 | Giant Ape / Tyrannosaurus Rex inspiration | Custom | Boss bear. Needs high HP, heavy melee, possibly multiattack. |
| Crabid | 1/4 | Giant Crab | Direct | Direct crab anchor. |
| Shell Slime | 1/8 | Custom shelled ooze / Giant Crab armor math | Custom | Keep slime identity with high AC shell. |
| Sea Slime | 1 | Gray Ooze / Water Weird inspiration | Close | Aquatic slime. Use ooze durability with water flavor. |

## Recommended Balance Order

1. Add optional monster metadata fields for `DndMonster`, `MonsterType`, `Size`, `Speed`, and `Alignment`.
2. Add monster action data, starting with one default action per monster: name, attack bonus, reach/range, damage dice, damage type, and optional save DC.
3. Rebalance direct matches first: Cobra, Ratscal, Crabid, Hell hornet, Shadow, Troll, Manticore, Blue Dragon.
4. Rebalance close matches by family: insects, snakes, cats, bears, flyers, aquatic monsters.
5. Build custom stat blocks last for Metal Slime, Eyelasher, Tentacular, Ethereal serpent, Sea fortress, Drackal variants, Franticore, Estark, GrayBear, Shell Slime.

## Open Design Decisions

- Decide whether to use D&D monster names internally as data anchors, while keeping Dungeon Escape display names unchanged.
- Decide whether boss monsters should match official CR math strictly or stay intentionally JRPG-boss-sized with inflated HP.
- Decide whether poison, regeneration, grapples, pounce, breath weapons, and strength drain wait for a full monster-actions system or get lightweight first-pass fields.
