using System.Collections.Generic;
using AstraNope.Data;

namespace AstraNope.Contracts
{
    public interface IBlueprintProgressReader
    {
        IReadOnlyList<BlueprintUnlockStatus> GetAllBlueprints();
        bool TryGetBlueprint(int id, out BlueprintUnlockStatus status);
    }

    public interface IBlueprintProgressWriter
    {
        bool AddProgress(int id, int amount = 1);
        bool Unlock(int id);
    }

    public interface IBlueprintProgressService : IBlueprintProgressReader, IBlueprintProgressWriter { }
}
