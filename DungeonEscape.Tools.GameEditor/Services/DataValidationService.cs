using Redpoint.DungeonEscape.Data;
using Redpoint.DungeonEscape.State;

namespace DungeonEscape.Tools.GameEditor.Services;

public enum DataValidationSeverity
{
    Error,
    Warning
}

public sealed record DataValidationIssue(DataValidationSeverity Severity, string Location, string Message);

public sealed class DataValidationService
{
    private const string OverworldMapClass = "Overworld";

    private static readonly StatType[] RequiredClassStats =
    {
        StatType.Health,
        StatType.Attack,
        StatType.Defence,
        StatType.MagicDefence,
        StatType.Agility,
        StatType.Magic
    };

    private static readonly StatType[] RequiredStatNameTypes =
    {
        StatType.Agility,
        StatType.Defence,
        StatType.Health,
        StatType.Attack,
        StatType.Magic,
        StatType.MagicDefence
    };

    private readonly DataFolderService data;
    private readonly MonsterImageCatalog monsterImages;
    private readonly ItemImageCatalog itemImages;
    private readonly SpellImageCatalog spellImages;
    private readonly HeroImageCatalog heroImages;

    public DataValidationService(
        DataFolderService data,
        MonsterImageCatalog monsterImages,
        ItemImageCatalog itemImages,
        SpellImageCatalog spellImages,
        HeroImageCatalog heroImages)
    {
        this.data = data;
        this.monsterImages = monsterImages;
        this.itemImages = itemImages;
        this.spellImages = spellImages;
        this.heroImages = heroImages;
    }

    public IReadOnlyList<DataValidationIssue> Validate()
    {
        if (!data.HasDocument)
        {
            return Array.Empty<DataValidationIssue>();
        }

        var issues = new List<DataValidationIssue>();
        var skillNames = NameSet(data.Skills.Select(skill => skill.Name));
        var spellNames = NameSet(data.Spells.Select(spell => spell.Name));
        var itemRefs = NameSet(data.Items.Select(item => item.Name)
            .Concat(data.Items.Select(item => item.Id))
            .Concat(data.ItemDefinitions.SelectMany(definition => definition.Names ?? new List<ItemName>()).Select(name => name.Name))
            .Append(DataSourceCatalog.RandomItemId));
        var questIds = NameSet(data.Quests.Select(quest => quest.Id));
        var dialogIds = NameSet(data.Dialogs.Select(dialog => dialog.Id));
        var monsterNames = NameSet(data.Monsters.Select(monster => monster.Name));
        var classValues = NameSet(data.ClassLevels.Select(classStats => classStats.Class));
        var mapIds = NameSet(data.Maps.Select(map => map.Id).Concat(data.Maps.Select(map => "maps/" + map.Id)));

        ValidateRequiredNames(issues, "Monster", data.Monsters.Select((item, index) => (index, Value: (string?)item.Name)));
        ValidateRequiredNames(issues, "Spell", data.Spells.Select((item, index) => (index, Value: (string?)item.Name)));
        ValidateRequiredNames(issues, "Skill", data.Skills.Select((item, index) => (index, Value: (string?)item.Name)));
        ValidateRequiredNames(issues, "Item", data.Items.Select((item, index) => (index, Value: (string?)item.Name)));
        ValidateRequiredNames(issues, "Quest", data.Quests.Select((item, index) => (index, Value: (string?)item.Id)));
        ValidateRequiredNames(issues, "Dialog", data.Dialogs.Select((item, index) => (index, Value: (string?)item.Id)));
        ValidateRequiredNames(issues, "Class level", data.ClassLevels.Select((item, index) => (index, Value: (string?)item.Class)));

        ValidateDuplicates(issues, "Monster name", data.Monsters.Select(monster => monster.Name));
        ValidateDuplicates(issues, "Spell name", data.Spells.Select(spell => spell.Name));
        ValidateDuplicates(issues, "Skill name", data.Skills.Select(skill => skill.Name));
        ValidateDuplicates(issues, "Item name", data.Items.Select(item => item.Name));
        ValidateDuplicates(issues, "Item id", data.Items.Select(item => item.Id));
        ValidateDuplicates(issues, "Quest id", data.Quests.Select(quest => quest.Id));
        ValidateDuplicates(issues, "Dialog id", data.Dialogs.Select(dialog => dialog.Id));
        ValidateDuplicates(issues, "Stat name type", data.StatNames.Select(statName => statName.Type.ToString()));
        ValidateDuplicates(issues, "Class level class", data.ClassLevels.Select(classStats => classStats.Class));

        ValidateMonsters(issues, skillNames, spellNames, itemRefs);
        ValidateSpells(issues, skillNames, classValues);
        ValidateItems(issues, skillNames, questIds, classValues);
        ValidateItemDefinitions(issues, classValues);
        ValidateQuests(issues, itemRefs);
        ValidateDialogs(issues, questIds, itemRefs, monsterNames);
        ValidateStatNames(issues);
        ValidateClassLevels(issues, skillNames);
        ValidateMaps(issues, itemRefs, dialogIds, monsterNames, mapIds, classValues);

        return issues
            .OrderBy(issue => issue.Severity)
            .ThenBy(issue => issue.Location, StringComparer.OrdinalIgnoreCase)
            .ThenBy(issue => issue.Message, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void ValidateMonsters(
        List<DataValidationIssue> issues,
        HashSet<string> skillNames,
        HashSet<string> spellNames,
        HashSet<string> itemRefs)
    {
        for (var i = 0; i < data.Monsters.Count; i++)
        {
            var monster = data.Monsters[i];
            var location = Label("Monster", monster.Name, i);
            if (monsterImages.Entries.Count > 0 && !monsterImages.TryGet(monster.ImageId, out _))
            {
                Warning(issues, location, $"Image #{monster.ImageId} was not found in allmonsters.tsx.");
            }

            ValidateReferences(issues, location, "spell", monster.SpellList, spellNames);
            ValidateReferences(issues, location, "skill", monster.SkillList, skillNames);
            ValidateReferences(issues, location, "item", monster.Items, itemRefs);
        }
    }

    private void ValidateSpells(List<DataValidationIssue> issues, HashSet<string> skillNames, HashSet<string> classValues)
    {
        for (var i = 0; i < data.Spells.Count; i++)
        {
            var spell = data.Spells[i];
            var location = Label("Spell", spell.Name, i);
            if (spellImages.Catalog.Entries.Count > 0 && !spellImages.Catalog.TryGet(spell.ImageId, out _))
            {
                Warning(issues, location, $"Image #{spell.ImageId} was not found in items.tsx.");
            }

            ValidateReference(issues, location, "skill", spell.SkillId, skillNames);
            ValidateClasses(issues, location, spell.Classes, classValues);
        }
    }

    private void ValidateItems(List<DataValidationIssue> issues, HashSet<string> skillNames, HashSet<string> questIds, HashSet<string> classValues)
    {
        for (var i = 0; i < data.Items.Count; i++)
        {
            var item = data.Items[i];
            var location = Label("Item", item.Name, i);
            if (itemImages.Catalog.Entries.Count > 0 && !itemImages.Catalog.TryGet(item.ImageId, out _))
            {
                Warning(issues, location, $"Image #{item.ImageId} was not found in items2.tsx.");
            }

            ValidateReference(issues, location, "skill", item.SkillId, skillNames);
            ValidateReference(issues, location, "quest", item.QuestId, questIds);
            ValidateClasses(issues, location, item.Classes, classValues);
        }
    }

    private void ValidateItemDefinitions(List<DataValidationIssue> issues, HashSet<string> classValues)
    {
        for (var i = 0; i < data.ItemDefinitions.Count; i++)
        {
            var definition = data.ItemDefinitions[i];
            ValidateClasses(issues, Label("Item definition", definition.Type.ToString(), i), definition.Classes, classValues);
        }
    }

    private void ValidateQuests(List<DataValidationIssue> issues, HashSet<string> itemRefs)
    {
        for (var i = 0; i < data.Quests.Count; i++)
        {
            var quest = data.Quests[i];
            var location = Label("Quest", string.IsNullOrEmpty(quest.Name) ? quest.Id : quest.Name, i);
            ValidateReferences(issues, location, "reward item", quest.Items, itemRefs);

            var stages = quest.Stages ?? new List<QuestStage>();
            var stageNumbers = stages.Select(stage => stage.Number).ToList();
            if (stageNumbers.Count != stageNumbers.Distinct().Count())
            {
                Error(issues, location, "Quest stage numbers must be unique.");
            }
        }
    }

    private void ValidateClassLevels(List<DataValidationIssue> issues, HashSet<string> skillNames)
    {
        for (var i = 0; i < data.ClassLevels.Count; i++)
        {
            var classStats = data.ClassLevels[i];
            var location = Label("Class level", classStats.Class, i);
            if (heroImages.Entries.Count > 0 && !heroImages.TryGet(classStats.DefaultImage, out _))
            {
                Warning(issues, location, $"Default image #{classStats.DefaultImage} was not found in hero.png.");
            }

            if (classStats.FirstLevel == 0)
            {
                Warning(issues, location, "First level is 0.");
            }

            var stats = classStats.Stats ?? new List<Stats>();
            var statTypes = stats.Select(stat => stat.Type).ToList();
            foreach (var requiredStat in RequiredClassStats)
            {
                if (!statTypes.Contains(requiredStat))
                {
                    Error(issues, location, $"Missing required {requiredStat} stat row.");
                }
            }

            ValidateDuplicates(issues, location + " stat type", stats.Select(stat => stat.Type.ToString()));
            foreach (var stat in stats)
            {
                if (!RequiredClassStats.Contains(stat.Type))
                {
                    Error(issues, location, $"Unexpected {stat.Type} stat row.");
                }

                if (stat.RollTimes <= 0)
                {
                    Error(issues, location, $"{stat.Type} roll times must be greater than 0.");
                }
            }

            ValidateReferences(issues, location, "skill", classStats.Skills, skillNames);
        }
    }

    private void ValidateStatNames(List<DataValidationIssue> issues)
    {
        var statNameTypes = data.StatNames.Select(statName => statName.Type).ToList();
        foreach (var requiredType in RequiredStatNameTypes)
        {
            if (!statNameTypes.Contains(requiredType))
            {
                Error(issues, "Stat Names", $"Missing required {requiredType} row.");
            }
        }

        foreach (var statName in data.StatNames)
        {
            if (!RequiredStatNameTypes.Contains(statName.Type))
            {
                Error(issues, "Stat Names", $"Unexpected {statName.Type} row.");
            }
        }
    }

    private void ValidateDialogs(
        List<DataValidationIssue> issues,
        HashSet<string> questIds,
        HashSet<string> itemRefs,
        HashSet<string> monsterNames)
    {
        for (var i = 0; i < data.Dialogs.Count; i++)
        {
            var dialog = data.Dialogs[i];
            var location = Label("Dialog", dialog.Id, i);
            if (dialog.Dialogs == null || dialog.Dialogs.Count == 0)
            {
                Warning(issues, location, "Dialog has no heads.");
                continue;
            }

            for (var headIndex = 0; headIndex < dialog.Dialogs.Count; headIndex++)
            {
                var head = dialog.Dialogs[headIndex];
                var headLocation = $"{location} head {headIndex + 1}";
                if (string.IsNullOrWhiteSpace(head.Text))
                {
                    Warning(issues, headLocation, "Dialog head text is empty.");
                }

                ValidateReference(issues, headLocation, "quest", head.Quest, questIds);
                ValidateDialogStages(issues, headLocation, head);
                ValidateChoices(issues, headLocation, head.Choices, questIds, itemRefs, monsterNames, head.Quest);
            }
        }
    }

    private static void ValidateDialogStages(List<DataValidationIssue> issues, string location, DialogHead head)
    {
        var stages = head.QuestStage ?? head.ForQuestStage;
        if (stages == null)
        {
            return;
        }

        foreach (var stage in stages)
        {
            if (stage < 0)
            {
                Error(issues, location, $"Quest stage {stage} is invalid.");
            }
        }
    }

    private void ValidateChoices(
        List<DataValidationIssue> issues,
        string location,
        IReadOnlyList<Choice>? choices,
        HashSet<string> questIds,
        HashSet<string> itemRefs,
        HashSet<string> monsterNames,
        string? parentQuestId)
    {
        if (choices == null)
        {
            return;
        }

        for (var i = 0; i < choices.Count; i++)
        {
            var choice = choices[i];
            var choiceLocation = $"{location} choice {i + 1}";
            if (string.IsNullOrWhiteSpace(choice.Text))
            {
                Warning(issues, choiceLocation, "Choice text is empty.");
            }

            ValidateReference(issues, choiceLocation, "quest", choice.Quest, questIds);
            ValidateReference(issues, choiceLocation, "item", choice.ItemId, itemRefs);
            ValidateReferences(issues, choiceLocation, "item", choice.Items, itemRefs);
            ValidateReference(issues, choiceLocation, "monster", choice.Monster, monsterNames);

            var effectiveQuestId = string.IsNullOrWhiteSpace(choice.Quest) ? parentQuestId : choice.Quest;
            if (choice.NextQuestStage.HasValue && choice.NextQuestStage.Value != 0 && string.IsNullOrWhiteSpace(effectiveQuestId))
            {
                Error(issues, choiceLocation, "NextQuestStage requires a quest.");
            }
            else if (choice.NextQuestStage.HasValue && choice.NextQuestStage.Value < 0)
            {
                Error(issues, choiceLocation, $"Next quest stage {choice.NextQuestStage.Value} is invalid.");
            }
            else if (choice.NextQuestStage.HasValue && choice.NextQuestStage.Value != 0 &&
                     !GetQuestStageNumbers(effectiveQuestId).Contains(choice.NextQuestStage.Value))
            {
                Error(issues, choiceLocation, $"Quest '{effectiveQuestId}' does not have stage {choice.NextQuestStage.Value}.");
            }

            if (choice.Dialog != null)
            {
                if (string.IsNullOrWhiteSpace(choice.Dialog.Text))
                {
                    Warning(issues, choiceLocation, "Nested dialog text is empty.");
                }

                ValidateChoices(issues, choiceLocation + " nested dialog", choice.Dialog.Choices, questIds, itemRefs, monsterNames, effectiveQuestId);
            }
        }
    }

    private IReadOnlyList<int> GetQuestStageNumbers(string? questId)
    {
        if (string.IsNullOrWhiteSpace(questId))
        {
            return Array.Empty<int>();
        }

        var stages = data.Quests
            .FirstOrDefault(quest => string.Equals(quest.Id, questId, StringComparison.OrdinalIgnoreCase))
            ?.Stages?
            .Select(stage => stage.Number)
            .Distinct()
            .ToList();

        return stages ?? (IReadOnlyList<int>)Array.Empty<int>();
    }

    private void ValidateMaps(
        List<DataValidationIssue> issues,
        HashSet<string> itemRefs,
        HashSet<string> dialogIds,
        HashSet<string> monsterNames,
        HashSet<string> mapIds,
        HashSet<string> classValues)
    {
        foreach (var map in data.Maps)
        {
            var location = $"Map '{map.Id}'";
            ValidateMapClass(issues, location, map.Class);
            ValidateDuplicateMapObjectIds(issues, location, map.Objects);

            foreach (var randomMonster in map.RandomMonsters)
            {
                ValidateReference(issues, location + " random monsters", "monster", randomMonster.Name, monsterNames);
            }

            foreach (var mapObject in map.Objects)
            {
                ValidateMapObject(issues, mapObject, itemRefs, dialogIds, mapIds, classValues);
            }
        }
    }

    private static void ValidateMapClass(List<DataValidationIssue> issues, string location, string? mapClass)
    {
        if (string.IsNullOrWhiteSpace(mapClass) ||
            string.Equals(mapClass, OverworldMapClass, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Error(issues, location, $"Map class '{mapClass}' is not supported. Use no map class or '{OverworldMapClass}'.");
    }

    private static void ValidateMapObject(
        List<DataValidationIssue> issues,
        MapObjectDocument mapObject,
        HashSet<string> itemRefs,
        HashSet<string> dialogIds,
        HashSet<string> mapIds,
        HashSet<string> classValues)
    {
        var location = $"Map '{mapObject.MapId}' object #{mapObject.Id} '{mapObject.DisplayName}'";
        ValidateReference(issues, location, "dialog", GetProperty(mapObject, "Dialog"), dialogIds);
        ValidateReference(issues, location, "item", GetProperty(mapObject, "ItemId"), itemRefs);
        ValidateReference(issues, location, "key item", GetProperty(mapObject, "KeyId"), itemRefs);
        ValidateReference(issues, location, "key item", GetProperty(mapObject, "KeyItemId"), itemRefs);
        ValidateReference(issues, location, "warp map", GetProperty(mapObject, "WarpMap"), mapIds);

        if (string.Equals(mapObject.Class, "Chest", StringComparison.OrdinalIgnoreCase) &&
            !mapObject.Properties.ContainsKey("Locked"))
        {
            Warning(issues, location, "Chest should explicitly set Locked.");
        }

        if (string.Equals(mapObject.Class, "Door", StringComparison.OrdinalIgnoreCase) &&
            !mapObject.Properties.ContainsKey("Locked"))
        {
            Warning(issues, location, "Door should explicitly set Locked.");
        }

        if (string.Equals(mapObject.Class, "NpcPartyMember", StringComparison.OrdinalIgnoreCase))
        {
            ValidateReference(issues, location, "class", GetProperty(mapObject, "Class"), classValues);
        }
    }

    private static string? GetProperty(MapObjectDocument mapObject, string name)
    {
        return mapObject.Properties.TryGetValue(name, out var value) ? value : null;
    }

    private static void ValidateDuplicateMapObjectIds(
        List<DataValidationIssue> issues,
        string location,
        IEnumerable<MapObjectDocument> mapObjects)
    {
        var duplicateGroups = mapObjects
            .Where(mapObject => mapObject.Id > 0)
            .GroupBy(mapObject => mapObject.GroupName, StringComparer.OrdinalIgnoreCase)
            .SelectMany(layerGroup => layerGroup
                .GroupBy(mapObject => mapObject.Id)
                .Where(idGroup => idGroup.Count() > 1)
                .Select(idGroup => new
                {
                    Layer = layerGroup.Key,
                    Id = idGroup.Key,
                    Count = idGroup.Count()
                }));

        foreach (var group in duplicateGroups)
        {
            var layerName = string.IsNullOrWhiteSpace(group.Layer) ? "unnamed object layer" : $"object layer '{group.Layer}'";
            Warning(issues, location, $"Object id {group.Id} is used by {group.Count} objects in {layerName}.");
        }
    }

    private static void ValidateReferences(
        List<DataValidationIssue> issues,
        string location,
        string label,
        IEnumerable<string>? references,
        HashSet<string> knownValues)
    {
        if (references == null)
        {
            return;
        }

        foreach (var reference in references)
        {
            ValidateReference(issues, location, label, reference, knownValues);
        }
    }

    private static void ValidateReference(
        List<DataValidationIssue> issues,
        string location,
        string label,
        string? reference,
        HashSet<string> knownValues)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return;
        }

        if (!knownValues.Contains(reference))
        {
            Error(issues, location, $"Unknown {label} reference '{reference}'.");
        }
    }

    private static void ValidateClasses(
        List<DataValidationIssue> issues,
        string location,
        IEnumerable<string>? classes,
        HashSet<string> classValues)
    {
        if (classes == null || classValues.Count == 0)
        {
            return;
        }

        foreach (var itemClass in classes.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            if (!classValues.Contains(itemClass))
            {
                Error(issues, location, $"Class '{itemClass}' is not defined in classlevels.json.");
            }
        }
    }

    private static void ValidateRequiredNames(
        List<DataValidationIssue> issues,
        string label,
        IEnumerable<(int Index, string? Value)> values)
    {
        foreach (var (index, value) in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Error(issues, $"{label} #{index + 1}", $"{label} requires a name or id.");
            }
        }
    }

    private static void ValidateDuplicates(List<DataValidationIssue> issues, string label, IEnumerable<string?> values)
    {
        foreach (var group in values
                     .Where(value => !string.IsNullOrWhiteSpace(value))
                     .GroupBy(value => value!, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1))
        {
            Error(issues, label, $"Duplicate value '{group.Key}'.");
        }
    }

    private static HashSet<string> NameSet(IEnumerable<string?> source) =>
        source
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static string Label(string kind, string? name, int index) =>
        string.IsNullOrWhiteSpace(name) ? $"{kind} #{index + 1}" : $"{kind} '{name}'";

    private static void Error(List<DataValidationIssue> issues, string location, string message) =>
        issues.Add(new DataValidationIssue(DataValidationSeverity.Error, location, message));

    private static void Warning(List<DataValidationIssue> issues, string location, string message) =>
        issues.Add(new DataValidationIssue(DataValidationSeverity.Warning, location, message));
}
