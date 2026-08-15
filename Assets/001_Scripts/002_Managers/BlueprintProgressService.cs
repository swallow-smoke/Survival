using System.Collections.Generic;
using _001_Scripts.Data;
using _001_Scripts.Data.Message;
using _001_Scripts.Data.SOJ;
using _001_Scripts.Interface;
using MessagePipe;

namespace _001_Scripts.Managers
{
    public sealed class BlueprintProgressService : IBlueprintProgressService
    {
        private readonly BluePrintDataBase _database;
        private readonly IPublisher<BlueprintProgressChangedMessage> _publisher;

        public BlueprintProgressService(BluePrintDataBase database,
            IPublisher<BlueprintProgressChangedMessage> publisher)
        {
            _database = database;
            _publisher = publisher;
        }

        public IReadOnlyList<BlueprintUnlockStatus> GetAllBlueprints()
        {
            var result = new List<BlueprintUnlockStatus>();
            var blueprints = _database.GetAllBluePrints();
            for (int i = 0; i < blueprints.Count; i++)
            {
                var blueprint = blueprints[i];
                result.Add(ToStatus(blueprint));
            }
            return result;
        }

        public bool TryGetBlueprint(int id, out BlueprintUnlockStatus status)
        {
            var blueprint = _database.GetBluePrint(id);
            if (blueprint == null)
            {
                status = default;
                return false;
            }
            status = ToStatus(blueprint);
            return true;
        }

        public bool AddProgress(int id, int amount = 1)
        {
            if (amount <= 0) return false;
            var blueprint = _database.GetBluePrint(id);
            if (blueprint == null || blueprint.isUnlocked) return false;
            blueprint.unlockProgress = System.Math.Min(blueprint.unlockRequired,
                blueprint.unlockProgress + amount);
            blueprint.isUnlocked = blueprint.unlockProgress >= blueprint.unlockRequired;
            _publisher.Publish(new BlueprintProgressChangedMessage(id));
            return true;
        }

        public bool Unlock(int id)
        {
            var blueprint = _database.GetBluePrint(id);
            if (blueprint == null || blueprint.isUnlocked) return false;
            blueprint.unlockProgress = blueprint.unlockRequired;
            blueprint.isUnlocked = true;
            _publisher.Publish(new BlueprintProgressChangedMessage(id));
            return true;
        }

        private static BlueprintUnlockStatus ToStatus(_001_Scripts.Data.BluePrint.BluePrint blueprint) =>
            new(blueprint.bluePrintId, blueprint.bluePrintName, blueprint.categoryPath, blueprint.iconResource,
                blueprint.isUnlocked, blueprint.unlockProgress, blueprint.unlockRequired);
    }
}
