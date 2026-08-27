using UnityEngine;

namespace AstraNope.Contracts
{
    public interface IPlayerTransformProvider
    {
        Transform PlayerTrs { get; }
    }

    public interface IPlayerContext : IPlayerTransformProvider { }
}
