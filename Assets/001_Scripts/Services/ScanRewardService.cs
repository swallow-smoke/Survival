using System;
using System.Collections.Generic;
using AstraNope.Data;
using AstraNope.Data.Messages;
using AstraNope.WorldObjects.Entities;
using AstraNope.Contracts;
using AstraNope.WorldObjects.Items;
using AstraNope.WorldObjects.Structures;
using AstraNope.WorldObjects.Vehicles;

using AstraNope.Localization;
namespace AstraNope.Services
{
    public sealed class ScanRewardService : IScanRewardService
    {
        private readonly ILogCollectionWriter logs;
        private readonly ILogCatalog logCatalog;
        private readonly IBlueprintProgressReader blueprintReader;
        private readonly IBlueprintProgressWriter blueprintWriter;
        private readonly IItemCatalog itemCatalog;
        private readonly INotificationService notifications;

        public ScanRewardService(ILogCollectionWriter logs, ILogCatalog logCatalog,
            IBlueprintProgressReader blueprintReader, IBlueprintProgressWriter blueprintWriter,
            IItemCatalog itemCatalog, INotificationService notifications)
        {
            this.logs = logs;
            this.logCatalog = logCatalog;
            this.blueprintReader = blueprintReader;
            this.blueprintWriter = blueprintWriter;
            this.itemCatalog = itemCatalog;
            this.notifications = notifications;
        }

        public void Grant(ScannableTarget target)
        {
            if (!target) return;
            var grantedLogs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var grantedBlueprints = new HashSet<int>();

            if (!string.IsNullOrWhiteSpace(target.UnlockLogId))
                GrantLog(target.UnlockLogId, grantedLogs);

            IReadOnlyList<ScanReward> rewards = target.Rewards;
            if (rewards != null)
            {
                foreach (ScanReward reward in rewards)
                {
                    if (reward == null) continue;
                    switch (reward.type)
                    {
                        case ScanRewardType.Log:
                            GrantLog(reward.logId, grantedLogs);
                            break;
                        case ScanRewardType.BlueprintProgress:
                            GrantBlueprint(reward.blueprintId, Math.Max(1, reward.amount), false,
                                grantedBlueprints);
                            break;
                        case ScanRewardType.BlueprintUnlock:
                            GrantBlueprint(reward.blueprintId, 0, true, grantedBlueprints);
                            break;
                    }
                }
            }

            if (target.IncludeWorldItemLog)
                GrantWorldItemLog(target.GetComponentInParent<WorldItem>());
        }

        private void GrantLog(string id, HashSet<string> granted)
        {
            if (string.IsNullOrWhiteSpace(id) || !granted.Add(id)) return;
            LogEntry entry = logCatalog?.Get(id);
            if (entry == null)
            {
                notifications?.Show(L10n.T("k_484efd0fca"), L10n.F("k_282d31b47d", id), "!",
                    NotificationKind.Warning, 4f);
                return;
            }
            if (logs?.Add(entry) == true)
                notifications?.Show(L10n.T("k_bec38964ea"), entry.title, "◇", NotificationKind.Info, 3f);
        }

        private void GrantBlueprint(int id, int amount, bool unlockImmediately, HashSet<int> granted)
        {
            if (!granted.Add(id) || blueprintReader == null || blueprintWriter == null) return;
            if (!blueprintReader.TryGetBlueprint(id, out BlueprintUnlockStatus before))
            {
                notifications?.Show(L10n.T("k_484efd0fca"), L10n.F("k_80b85992f1", id), "!",
                    NotificationKind.Warning, 4f);
                return;
            }

            bool changed = unlockImmediately ? blueprintWriter.Unlock(id) : blueprintWriter.AddProgress(id, amount);
            if (!changed || !blueprintReader.TryGetBlueprint(id, out BlueprintUnlockStatus after)) return;

            string body = after.IsUnlocked
                ? after.Name
                : $"{after.Name}  ( {after.Progress} / {after.Required} )";
            notifications?.Show(after.IsUnlocked ? L10n.T("k_93d9800537") : L10n.T("k_e49026e528"), body, "⌬",
                NotificationKind.Info, 3.5f);
        }

        private void GrantWorldItemLog(WorldItem worldItem)
        {
            if (!worldItem || itemCatalog == null || !itemCatalog.TryGetItem(worldItem.ItemId, out var item)) return;
            string description = string.IsNullOrWhiteSpace(item.itemDesc)
                ? L10n.T("k_c6fadddccc")
                : item.itemDesc.Trim();
            string roleLabel = item.Role.ToString();
            string weightLabel = item.weight.ToString("0.##");
            var entry = new LogEntry
            {
                id = $"item:{item.itemId}",
                title = L10n.F("k_09532e1bb3", item.itemName),
                body = L10n.F("k_d221ca1aef", description, roleLabel, weightLabel),
                imageResource = string.Empty
            };
            if (logs?.Add(entry) == true)
                notifications?.Show(L10n.T("k_2f8f3e015f"), item.itemName, "▣", NotificationKind.Info, 3f);
        }
    }
}
