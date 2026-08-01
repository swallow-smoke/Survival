using _001_Scripts.Controller;
using _001_Scripts.Controller.Handler;
using _001_Scripts.Core._000_World._001_Water;
using _001_Scripts.Core._000_World._001_Water.Interface;
using _001_Scripts.Data.Message;
using _001_Scripts.Data.Message.Player;
using _001_Scripts.Data.Item;
using _001_Scripts.Interface;
using _001_Scripts.Managers;
using _001_Scripts.Structure;
using _001_Scripts.Vehicle.Core;
using System;
using MessagePipe;
using UnityEngine.InputSystem.LowLevel;
using VContainer;
using VContainer.Unity;
using UnityEngine;

namespace _001_Scripts.Core
{
    /// <summary>
    /// GLifeTimeScope = GM
    /// GM's child is Managers
    /// </summary>
    public class GLifeTimeScope : LifetimeScope
    {
        [SerializeField] private HarvestToolCatalog harvestToolCatalog;

        protected override void Configure(IContainerBuilder builder)
        {
            var options = builder.RegisterMessagePipe();

  
            #region System
            builder.RegisterMessageBroker<GameStateMessage>(options);
            
            builder.RegisterMessageBroker<InvReqMessage>(options);
            builder.RegisterMessageBroker<InvChangedMessage>(options);
            builder.RegisterMessageBroker<InvSwapMessage>(options);
            
            builder.RegisterMessageBroker<CraftReqMessage>(options);
            builder.RegisterMessageBroker<CraftResultMessage>(options);
            
            builder.RegisterMessageBroker<UIReqMessage>(options);
            builder.RegisterMessageBroker<InteractionUIMessage>(options);
            #endregion

            #region Player
            
            builder.RegisterMessageBroker<PlayerMovementStateMsg>(options);
            builder.RegisterMessageBroker<PlayerUIStateMsg>(options);
            builder.RegisterMessageBroker<PlayerVehicleStateMsg>(options);
            builder.RegisterMessageBroker<VehicleControlAssignedMsg>(options);

            builder.RegisterMessageBroker<PlayerMovementMessage>(options);
            builder.RegisterMessageBroker<PlayerWaterStateMessage>(options);
            builder.RegisterMessageBroker<PlayerStatMessage>(options);

            #endregion

            #region Services

            builder.RegisterComponentInHierarchy<ItemSpawner>();
            builder.RegisterComponentInHierarchy<InventoryController>().As<IInventoryService>();
            builder.RegisterComponentInHierarchy<GameManager>().As<IGameService>().As<IInitializable>();
            builder.RegisterComponentInHierarchy<UIManager>().As<IUIService>().As<IInitializable>();
            builder.RegisterComponentInHierarchy<PlayerContext>().As<IPlayerContext>();
            builder.RegisterComponentInHierarchy<VehicleInjector>();
            builder.RegisterComponentInHierarchy<VehicleSpawner>();
            builder.RegisterComponentInHierarchy<InputHandler>().As<IInputService>();
            builder.RegisterComponentInHierarchy<CraftController>().As<ICraftService>();
            if (harvestToolCatalog == null)
                throw new InvalidOperationException(
                    "HarvestToolCatalog is required on GLifeTimeScope. Run the WorldBuilder resource setup or assign it explicitly.");
            builder.RegisterInstance(harvestToolCatalog);
            builder.Register<DotsWorldResourceGateway>(Lifetime.Singleton).As<IWorldResourceGateway>();
            builder.Register<InventoryHarvestToolSelector>(Lifetime.Singleton).As<IHarvestToolSelector>();
            builder.Register<DotsResourceInteractionService>(Lifetime.Singleton).As<IResourceInteractionService>();
            builder.RegisterEntryPoint<DotsResourceInventoryBridge>();

            #endregion
            
            #region World

            builder.RegisterComponentInHierarchy<WaterQueryService>()
                .As<IWaterQueryService>()
                .As<IWaterQuery>()
                .As<IWaterRegistry>();

            #endregion

        }
    }
}
