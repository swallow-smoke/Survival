using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace AstraNope.Core
{
    public class PlayerScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            var options = builder.RegisterMessagePipe();
            
            
        }
    }
}