using _001_Scripts.Interface;
using _001_Scripts.Managers;
using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace _001_Scripts.Core
{
    public class GLifeTimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterMessagePipe();
            
            builder.RegisterComponentInHierarchy<GameManager>().As<IGameService>();
            builder.RegisterComponentInHierarchy<UIManager>().As<IUIService>();
        }
    }
}   