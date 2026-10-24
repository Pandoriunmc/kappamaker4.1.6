using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace _KappaMarker;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.cichaj.km";
    public string Name { get; init; } = "KappaMarker";
    public string Author { get; init; } = "Cichaj";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new("2.0.0");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; } = "https://github.com/rafalsoja/KappaMarker";
    public bool? IsBundleMod { get; init; } = false;
    public string License { get; init; } = "MIT";
    public bool HasPrepatcher { get; init; } = false;
}

[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class KappaMarker(
    ISptLogger<KappaMarker> logger,
    TemplateTable templateTable,
    LocaleTable localeTable) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            var quests = templateTable.Quests;
            if (quests == null)
            {
                logger.Warning("[KappaMarker] Quests table is null.");
                return Task.CompletedTask;
            }

            string? collectorId = FindCollectorQuestId(quests);
            if (string.IsNullOrWhiteSpace(collectorId))
            {
                logger.Warning("[KappaMarker] Collector quest not found.");
                return Task.CompletedTask;
            }

            var requiredQuests = GetAllRequiredQuests(quests, collectorId);
            if (requiredQuests.Count == 0)
            {
                logger.Warning("[KappaMarker] No required quests found for Collector.");
                return Task.CompletedTask;
            }

            TagKappaQuestLocales(localeTable, requiredQuests);
            logger.Success($"[KappaMarker] {requiredQuests.Count} quests marked.");
        }
        catch (Exception ex)
        {
            logger.Error($"[KappaMarker] Error: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    private static string? FindCollectorQuestId(IDictionary<MongoId, Quest> quests)
    {
        foreach (var (questId, questData) in quests)
        {
            if (questData.QuestName != null &&
                questData.QuestName.Equals("Collector", StringComparison.OrdinalIgnoreCase))
            {
                return questId;
            }
        }
        return null;
    }

    private static HashSet<string> GetAllRequiredQuests(IDictionary<MongoId, Quest> quests, string rootQuestId)
    {
        var visited = new HashSet<string>();

        void Traverse(string questId)
        {
            if (!quests.TryGetValue(questId, out var quest) || visited.Contains(questId))
                return;

            visited.Add(questId);

            var conditions = quest.Conditions?.AvailableForStart;
            if (conditions == null)
                return;

            foreach (var cond in conditions)
            {
                if (cond.ConditionType == "Quest" && cond.Target != null)
                {
                    if (!string.IsNullOrEmpty(cond.Target.Item))
                    {
                        Traverse(cond.Target.Item);
                    }

                    if (cond.Target.List != null)
                    {
                        foreach (var id in cond.Target.List)
                        {
                            Traverse(id);
                        }
                    }
                }
            }
        }

        Traverse(rootQuestId);
        visited.Remove(rootQuestId);

        return visited;
    }

    private static void TagKappaQuestLocales(LocaleTable localeTable, IEnumerable<string> questIds)
    {
        if (localeTable.Global == null)
            return;

        foreach (var questId in questIds)
        {
            string descKey = $"{questId} description";
            string nameKey = $"{questId} name";

            foreach (var lazyLocale in localeTable.Global.Values)
            {
                lazyLocale.AddTransformer(locale =>
                {
                    if (locale == null)
                        return locale;

                    if (locale.TryGetValue(descKey, out var desc) && !desc.StartsWith("Required for Kappa"))
                    {
                        locale[descKey] = "Required for Kappa.\n\n" + desc;
                    }

                    if (locale.TryGetValue(nameKey, out var name) && !name.StartsWith("[K] "))
                    {
                        locale[nameKey] = "[K] " + name;
                    }

                    return locale;
                });
            }
        }
    }
}