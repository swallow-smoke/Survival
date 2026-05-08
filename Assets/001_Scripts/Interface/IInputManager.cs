using System;

namespace _001_Scripts.Interface
{
    public interface IInputManager : IManager
    {
        void Subscribe(ref Action target, Action handler);
        void UnSubscribe(ref Action target, Action handler);
    }
}