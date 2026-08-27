using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace AstraNope.Core
{
    public class SystemScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            var options = builder.RegisterMessagePipe();
            
            
        }
    }
}