using UnityEngine;

namespace _001_Scripts.Interface
{
    public interface IPlayerTransformProvider
    {
        Transform PlayerTrs { get; }
    }

    public interface IPlayerContext : IPlayerTransformProvider { }
}
