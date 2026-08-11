using System;

namespace _001_Scripts.Data.Structure.Interface
{
    public interface IDestroyable
    {
        void Destroy();
    }

    [Obsolete("Use IDestroyable. This interface is kept for source compatibility.")]
    public interface IDestructable : IDestroyable { }
}
