using AstraNope.Gameplay.Player;
using AstraNope.Gameplay.Input;
using AstraNope.Core.World.Water;
using AstraNope.Core.World.Water.Interfaces;
using AstraNope.Data.Messages;
using AstraNope.Data.Messages.Player;
using AstraNope.Data.Items;
using AstraNope.Contracts;
using AstraNope.Services;
using AstraNope.WorldObjects.Items;
using AstraNope.WorldObjects.Structures;
using AstraNope.WorldObjects.Vehicles;
using AstraNope.UI.Panels;
using AstraNope.WorldObjects.Vehicles.Core;
using System;
using AstraNope.Core.World.Entities;
using AstraNope.Core.World.Entities.Bridges;
using AstraNope.Core.World.Entities.Creatures;
using AstraNope.Core.World.Entities.Gateways;
using AstraNope.Core.World.Entities.Interfaces;
using AstraNope.Core.World.Entities.Resources;
using MessagePipe;
using UnityEngine.InputSystem.LowLevel;
using VContainer;
using VContainer.Unity;
using UnityEngine;
using AstraNope.Data.Databases;

namespace AstraNope.Core
{
    /// <summary>
    /// GLifeTimeScope = GM
    /// GM's child is Managers
    /// </summary>
    public class GLifeTimeScope : LifetimeScope
    {
        [SerializeField] private HarvestToolCatalog harvestToolCatalog;
        [SerializeField] private CreatureColorFoodCatalog creatureColorFoodCatalog;

        protected override void Configure(IContainerBuilder builder)
        {
            var options = builder.RegisterMessagePipe();

  
            #region System
            builder.RegisterMessageBroker<GameStateMessage>(options);
            
            builder.RegisterMessageBroker<InventoryRequestMessage>(options);
            builder.RegisterMessageBroker<InventoryChangedMessage>(options);
            builder.RegisterMessageBroker<InventorySwapMessage>(options);
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
            
            builder.RegisterMessageBroker<PlayerUIStateMessage>(options);
            builder.RegisterMessageBroker<PlayerVehicleStateMessage>(options);
            builder.RegisterMessageBroker<VehicleControlAssignedMessage>(options);

            builder.RegisterMessageBroker<PlayerMovementMessage>(options);
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
                .As<IItemCatalog>()
                .As<IEquipmentReader>()
                .As<IInventoryReader>()
                .As<IInventoryWriter>()
                .As<IInventoryActions>()
                .As<IHotbarReader>()
                .As<IHotbarActions>();
            builder.RegisterComponentInHierarchy<BuildingPlacementController>()
                .As<IBuildingPlacementService>()
                .As<IBuildSelectionReader>();
            builder.Register<AstraNope.Services.ScanRewardService>(Lifetime.Singleton).As<IScanRewardService>();
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
                .As<IHotbarInput>()
                .As<IHeldItemInput>();
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

            builder.Register<DotsWorldCreatureGateway>(Lifetime.Singleton)
                .As<IWorldCreatureGateway>()
                .As<ICreatureSpawner>()
                .As<IWorldEntityGateway>();
            builder.RegisterInstance(creatureColorFoodCatalog != null
                ? creatureColorFoodCatalog
                : ScriptableObject.CreateInstance<CreatureColorFoodCatalog>());
            builder.Register<InventoryCreatureToolSelector>(Lifetime.Singleton).As<ICreatureToolSelector>();
            builder.Register<DotsCreatureInteractionService>(Lifetime.Singleton).As<ICreatureInteractionService>();
            builder.RegisterEntryPoint<CreaturePlayerFocusBridge>();
            builder.RegisterEntryPoint<EntityManager>()
                .AsSelf()
                .As<IEntityManager>()
                .As<IEntitySpawnService>()
                .As<IEntityDirectory>()
                .As<IEntityLifecycle>()
                .As<IEntityInteractions>()
                .As<IEntitySettlement>();

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
