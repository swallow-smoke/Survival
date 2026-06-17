using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace _001_Scripts.Core
{
    public class PlayerScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            var options = builder.RegisterMessagePipe();
            
            
        }
    }
}