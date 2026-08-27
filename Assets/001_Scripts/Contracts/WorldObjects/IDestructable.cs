using System;

namespace AstraNope.Contracts.WorldObjects
{
    public interface IDestroyable
    {
        void Destroy();
    }

    [Obsolete("Use IDestroyable. This interface is kept for source compatibility.")]
    public interface IDestructable : IDestroyable { }
}
