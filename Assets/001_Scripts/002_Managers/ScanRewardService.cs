using System;
using System.Collections.Generic;
using _001_Scripts.Data;
using _001_Scripts.Data.Message;
using _001_Scripts.Entities;
using _001_Scripts.Interface;
using _001_Scripts.Structure;

namespace _001_Scripts.Managers
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
                notifications?.Show("스캔 데이터 오류", $"로그 ID를 찾을 수 없습니다: {id}", "!",
                    NotificationKind.Warning, 4f);
                return;
            }
            if (logs?.Add(entry) == true)
                notifications?.Show("로그 해금", entry.title, "◇", NotificationKind.Info, 3f);
        }

        private void GrantBlueprint(int id, int amount, bool unlockImmediately, HashSet<int> granted)
        {
            if (!granted.Add(id) || blueprintReader == null || blueprintWriter == null) return;
            if (!blueprintReader.TryGetBlueprint(id, out BlueprintUnlockStatus before))
            {
                notifications?.Show("스캔 데이터 오류", $"청사진 ID를 찾을 수 없습니다: {id}", "!",
                    NotificationKind.Warning, 4f);
                return;
            }

            bool changed = unlockImmediately ? blueprintWriter.Unlock(id) : blueprintWriter.AddProgress(id, amount);
            if (!changed || !blueprintReader.TryGetBlueprint(id, out BlueprintUnlockStatus after)) return;

            string body = after.IsUnlocked
                ? after.Name
                : $"{after.Name}  ( {after.Progress} / {after.Required} )";
            notifications?.Show(after.IsUnlocked ? "청사진 해금" : "청사진 분석", body, "⌬",
                NotificationKind.Info, 3.5f);
        }

        private void GrantWorldItemLog(WorldItem worldItem)
        {
            if (!worldItem || itemCatalog == null || !itemCatalog.TryGetItem(worldItem.ItemId, out var item)) return;
            string description = string.IsNullOrWhiteSpace(item.itemDesc)
                ? "아직 상세 분석 기록이 없는 아이템이다."
                : item.itemDesc.Trim();
            var entry = new LogEntry
            {
                id = $"item:{item.itemId}",
                title = $"아이템 · {item.itemName}",
                body = $"{description}\n\n분류: {item.Role}\n무게: {item.weight:0.##}",
                imageResource = string.Empty
            };
            if (logs?.Add(entry) == true)
                notifications?.Show("아이템 도감 등록", item.itemName, "▣", NotificationKind.Info, 3f);
        }
    }
}
