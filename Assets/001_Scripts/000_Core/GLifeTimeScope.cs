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
using _001_Scripts.UI;
using _001_Scripts.Vehicle.Core;
using System;
using MessagePipe;
using UnityEngine.InputSystem.LowLevel;
using VContainer;
using VContainer.Unity;
using UnityEngine;
using _001_Scripts.Data.SOJ;

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
            builder.RegisterMessageBroker<HotbarSelectionMessage>(options);
            
            builder.RegisterMessageBroker<CraftReqMessage>(options);
            builder.RegisterMessageBroker<CraftResultMessage>(options);
            
            builder.RegisterMessageBroker<UIReqMessage>(options);
            builder.RegisterMessageBroker<InteractionUIMessage>(options);
            builder.RegisterMessageBroker<NotificationMessage>(options);
            builder.RegisterMessageBroker<LogCollectionChangedMessage>(options);
            builder.RegisterMessageBroker<BlueprintProgressChangedMessage>(options);
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

            builder.Register<NotificationService>(Lifetime.Singleton).As<INotificationService>();
            builder.Register<LogCollectionService>(Lifetime.Singleton)
                .As<ILogCollectionService>()
                .As<ILogCollectionReader>()
                .As<ILogCollectionWriter>();
            builder.Register<JsonLogCatalog>(Lifetime.Singleton).As<ILogCatalog>();
            var blueprintDatabase = ScriptableObject.CreateInstance<BluePrintDataBase>();
            blueprintDatabase.Reload();
            builder.RegisterInstance(blueprintDatabase);
            builder.Register<BlueprintProgressService>(Lifetime.Singleton)
                .As<IBlueprintProgressService>()
                .As<IBlueprintProgressReader>()
                .As<IBlueprintProgressWriter>();
            builder.RegisterComponentInHierarchy<ItemSpawner>()
                .AsSelf()
                .As<IPickupSpawner>();
            builder.RegisterComponentInHierarchy<InventoryController>()
                .As<IInventoryService>()
                .As<IInventoryReader>()
                .As<IInventoryWriter>()
                .As<IInventoryActions>()
                .As<IHotbarReader>()
                .As<IHotbarActions>();
            builder.RegisterComponentInHierarchy<GameManager>().As<IGameService>().As<IInitializable>();
            builder.RegisterComponentInHierarchy<UIManager>()
                .As<IUIService>()
                .As<IUIPanelNavigator>()
                .As<IInitializable>();
            builder.RegisterComponentInHierarchy<PlayerContext>()
                .As<IPlayerContext>()
                .As<IPlayerTransformProvider>();
            builder.RegisterComponentInHierarchy<VehicleInjector>();
            builder.RegisterComponentInHierarchy<VehicleSpawner>()
                .AsSelf()
                .As<IVehicleSpawner>();
            builder.RegisterComponentInHierarchy<InputHandler>()
                .As<IInputService>()
                .As<IMovementInput>()
                .As<IInteractionInput>()
                .As<IVehicleInput>()
                .As<IUIInput>()
                .As<IHotbarInput>();
            builder.RegisterComponentInHierarchy<CraftController>()
                .As<ICraftService>()
                .As<ICraftingService>();
            builder.RegisterComponentInHierarchy<WorkbenchPanel>();
            builder.RegisterComponentInHierarchy<SubmarineFabricatorPanel>();
            builder.RegisterComponentInHierarchy<BlueprintPanel>();
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
