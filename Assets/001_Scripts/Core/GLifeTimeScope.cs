using _001_Scripts.Controller;
using _001_Scripts.Data.Message;
using _001_Scripts.Interface;
using _001_Scripts.Managers;
using MessagePipe;
using VContainer;
using VContainer.Unity;

namespace _001_Scripts.Core
{
    /// <summary>
    /// GLifeTimeScope = GM
    /// GM's child is Managers
    /// </summary>
    public class GLifeTimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            var options = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<GameStateMessage>(options);
            builder.RegisterMessageBroker<InvMessage>(options);
            builder.RegisterMessageBroker<CraftReqMessage>(options);
            builder.RegisterMessageBroker<CraftResultMessage>(options);
            builder.RegisterMessageBroker<UIReqMessage>(options);
            builder.RegisterMessageBroker<PlayerMovementMessage>(options);
            builder.RegisterMessageBroker<PlayerStatMessage>(options);
            builder.RegisterMessageBroker<StateMessage>(options);
                
            builder.RegisterComponentInHierarchy<InventoryController>().As<IInventoryService>();
            builder.RegisterComponentInHierarchy<GameManager>().As<IGameService>();
            builder.RegisterComponentInHierarchy<UIManager>().As<IUIService>();
            builder.RegisterComponentInHierarchy<CraftController>().As<ICraftService>();
        }
    }
}   