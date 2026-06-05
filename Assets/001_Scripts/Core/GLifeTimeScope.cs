using _001_Scripts.Controller;
using _001_Scripts.Data.Message;
using _001_Scripts.Data.Message.Player;
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

  
            #region System
            builder.RegisterMessageBroker<GameStateMessage>(options);
            
            builder.RegisterMessageBroker<InvReqMessage>(options);
            builder.RegisterMessageBroker<InvChangedMessage>(options);
            
            builder.RegisterMessageBroker<CraftReqMessage>(options);
            builder.RegisterMessageBroker<CraftResultMessage>(options);
            
            builder.RegisterMessageBroker<UIReqMessage>(options);
            #endregion

            #region Player
            
            builder.RegisterMessageBroker<PlayerMovementStateMsg>(options);
            builder.RegisterMessageBroker<PlayerUIStateMsg>(options);
            builder.RegisterMessageBroker<PlayerVehicleStateMsg>(options);
            
            builder.RegisterMessageBroker<PlayerMovementMessage>(options);
            builder.RegisterMessageBroker<PlayerStatMessage>(options);

            #endregion

            #region Services

            builder.RegisterComponentInHierarchy<InventoryController>().As<IInventoryService>();
            builder.RegisterComponentInHierarchy<GameManager>().As<IGameService>().As<IInitializable>();
            builder.RegisterComponentInHierarchy<UIManager>().As<IUIService>().As<IInitializable>();
            // builder.RegisterComponentInHierarchy<CraftController>().As<ICraftService>();
            
            #endregion
     

        }
    }
}   