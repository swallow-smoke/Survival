using System.Collections.Generic;
using _001_Scripts.Data;

namespace _001_Scripts.Interface
{
    public interface IBlueprintProgressReader
    {
        IReadOnlyList<BlueprintUnlockStatus> GetAllBlueprints();
        bool TryGetBlueprint(int id, out BlueprintUnlockStatus status);
    }

    public interface IBlueprintProgressWriter
    {
        bool AddProgress(int id, int amount = 1);
    }

    public interface IBlueprintProgressService : IBlueprintProgressReader, IBlueprintProgressWriter { }
}
