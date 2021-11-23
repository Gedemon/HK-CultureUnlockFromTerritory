using System.Collections.Generic;
using Amplitude.Mercury.Simulation;
using Amplitude.Mercury.Sandbox;
using Amplitude.Framework;
using Amplitude.Mercury.UI;
using UnityEngine;
using Amplitude;
using Amplitude.Mercury.Data.Simulation;
using Amplitude.Mercury.Interop;
using Amplitude.Mercury.Data.Simulation.Costs;
using Amplitude.Framework.Runtime;
using Amplitude.Framework.Asset;

namespace Gedemon.TrueCultureLocation
{
    class TradeRoute
    {
		public static bool IsRestoreDestroyedTradeRoutes { get; set; } = false;

		public class TradeRouteToRestore
		{
			public MajorEmpire buyer;
			public District resourceExtractorToTrade;
			public FixedPoint setupCost;
		}

		public class ForwardedTradeRouteToRestore
		{
			public MajorEmpire seller;
			public MajorEmpire buyer;
			public FixedPoint SetupCost;
			public SimulationEntityGUID ResourceExtractor;
			public int DepositOwnerIndex;
			public int ResourceDepositIndex;
			public ResourceType TradedResource;
		}

		public static IDictionary<int, TradeRouteToRestore> ListTradeRouteToRestore = new Dictionary<int, TradeRouteToRestore>();
		public static IDictionary<int, ForwardedTradeRouteToRestore> ListFowardedTradeRouteToRestore = new Dictionary<int, ForwardedTradeRouteToRestore>();

		public static void StartListingTradeRouteToRestore()
		{
			Diagnostics.LogWarning($"[Gedemon] Starting to register the Trade Routes To restore...");
			IsRestoreDestroyedTradeRoutes = true;
			ListTradeRouteToRestore.Clear();
			ListFowardedTradeRouteToRestore.Clear();
		}

		public static void AddTradeRouteToRestore(TradeRoadInfo tradeRoadInfo)
        {
			int tradeRoadIndex = tradeRoadInfo.PoolAllocationIndex;
			Diagnostics.LogWarning($"[Gedemon] AddTradeRouteToRestore for ID#{tradeRoadIndex}");
			Diagnostics.LogWarning($"[Gedemon] tradeRoadInfo : DestinationEmpire = {tradeRoadInfo.DestinationEmpireIndex}, DepositOwner = {tradeRoadInfo.DepositOwnerIndex}, ResourceExtractor = {tradeRoadInfo.ResourceExtractor}, Flags = {tradeRoadInfo.Flags}, IsForwardTrade = {tradeRoadInfo.IsForwardTrade}, OriginEmpire = {tradeRoadInfo.OriginEmpireIndex}, Status = {tradeRoadInfo.TradeRoadStatus}");

			if(tradeRoadInfo.IsForwardTrade)
            {
				if (!ListFowardedTradeRouteToRestore.ContainsKey(tradeRoadIndex))
				{
					ForwardedTradeRouteToRestore forwardedTradeRouteToRestore = new ForwardedTradeRouteToRestore
					{
						buyer = Sandbox.MajorEmpires[tradeRoadInfo.DestinationEmpireIndex],
						seller = Sandbox.MajorEmpires[tradeRoadInfo.OriginEmpireIndex],
						ResourceExtractor = tradeRoadInfo.ResourceExtractor,
						SetupCost = tradeRoadInfo.SetupCost,
						DepositOwnerIndex = tradeRoadInfo.DepositOwnerIndex,
						ResourceDepositIndex = tradeRoadInfo.ResourceDepositIndex,
						TradedResource = tradeRoadInfo.TradedResource
					};
					ListFowardedTradeRouteToRestore.Add(tradeRoadIndex, forwardedTradeRouteToRestore);
				}
            }
			else
			{
				if (!ListTradeRouteToRestore.ContainsKey(tradeRoadIndex))
				{
					Sandbox.SimulationEntityRepository.TryGetSimulationEntity(tradeRoadInfo.ResourceExtractor, out District resourceExtractor);
					TradeRouteToRestore tradeRouteToRestore = new TradeRouteToRestore
					{
						buyer = Sandbox.MajorEmpires[tradeRoadInfo.DestinationEmpireIndex],
						resourceExtractorToTrade = resourceExtractor,
						setupCost = tradeRoadInfo.SetupCost
					};
					ListTradeRouteToRestore.Add(tradeRoadIndex, tradeRouteToRestore);
				}
			}

		}

		public static void RestoreTradeRoutes()
		{
			Diagnostics.LogWarning($"[Gedemon] Restoring destroyed trade Routes...");

			IsRestoreDestroyedTradeRoutes = false;

			foreach (KeyValuePair<int, TradeRouteToRestore> tradeRoutes in ListTradeRouteToRestore)
			{
				int tradeRoadIndex = tradeRoutes.Key;
				TradeRouteToRestore tradeRouteToRestore = tradeRoutes.Value;
				if(tradeRouteToRestore.resourceExtractorToTrade.Empire != null)
				{
					Diagnostics.LogWarning($"[Gedemon] Restoring Trade Route for ID#{tradeRoadIndex}, buyer = {tradeRouteToRestore.buyer.FactionDefinition.Name}, resource in {CultureUnlock.GetTerritoryName(tradeRouteToRestore.resourceExtractorToTrade.Territory.Entity.Index)}");
					RestoreTradeRoad(tradeRouteToRestore.buyer, tradeRouteToRestore.resourceExtractorToTrade, TradeRoadPathTypes.Cheapest, out FixedPoint setupCost, out FixedPoint sellingGain);
				}
				else
				{
					Diagnostics.LogWarning($"[Gedemon] Can't restore Trade Route for ID#{tradeRoadIndex}, buyer = {tradeRouteToRestore.buyer.FactionDefinition.Name}, resource in {CultureUnlock.GetTerritoryName(tradeRouteToRestore.resourceExtractorToTrade.Territory.Entity.Index)}, resource is unowned");
				}
			}


			foreach (KeyValuePair<int, ForwardedTradeRouteToRestore> tradeRoutes in ListFowardedTradeRouteToRestore)
			{
				int tradeRoadIndex = tradeRoutes.Key;
				ForwardedTradeRouteToRestore tradeRouteToRestore = tradeRoutes.Value;

				Sandbox.SimulationEntityRepository.TryGetSimulationEntity(tradeRouteToRestore.ResourceExtractor, out District resourceExtractor);

				if (resourceExtractor.Empire != null)
				{
					// add check if seller still has access to resource
					if (tradeRouteToRestore.seller.DepartmentOfResources.availableResources[(int)tradeRouteToRestore.TradedResource])
					{
						Diagnostics.LogWarning($"[Gedemon] Restoring Forward Trade Route for ID#{tradeRoadIndex}, buyer = {tradeRouteToRestore.buyer.FactionDefinition.Name}, seller = {tradeRouteToRestore.seller.FactionDefinition.Name}, resource in {CultureUnlock.GetTerritoryName(resourceExtractor.Territory.Entity.Index)}");
						RestoreForwardTrading(tradeRouteToRestore, TradeRoadPathTypes.Cheapest, out FixedPoint setupCost, out FixedPoint sellingGain);
					}
					else
					{
						Diagnostics.LogWarning($"[Gedemon] Can't restore Trade Route for ID#{tradeRoadIndex}, buyer = {tradeRouteToRestore.buyer.FactionDefinition.Name}, resource in {CultureUnlock.GetTerritoryName(resourceExtractor.Territory.Entity.Index)}, seller doesn't have resource");
					}
				}
				else
				{
					Diagnostics.LogWarning($"[Gedemon] Can't restore Trade Route for ID#{tradeRoadIndex}, buyer = {tradeRouteToRestore.buyer.FactionDefinition.Name}, resource in {CultureUnlock.GetTerritoryName(resourceExtractor.Territory.Entity.Index)}, resource is unowned");
				}
			}
		}

		internal static int RestoreTradeRoad(MajorEmpire buyer, District resourceExtractorToTrade, TradeRoadPathTypes tradeRoadPathType, out FixedPoint setupCost, out FixedPoint sellingGain)
		{
			ref TradeController tradeController = ref Sandbox.TradeController;
			int num = resourceExtractorToTrade.WorldPosition.ToTileIndex();
			int num2 = World.Maps.ResourceDepositMap[num];
			if (!tradeController.TryFindPath(buyer, num, tradeRoadPathType, tradeController.tempTradeRoadPathInfo))
			{
				Diagnostics.LogError($"Path of type '{tradeRoadPathType}' not found.");
				setupCost = 0;
				sellingGain = 0;
				return -1;
			}
			int territoryIndex = tradeController.tempTradeRoadPathInfo.Data[tradeController.tempTradeRoadPathInfo.Length - 1].TerritoryIndex;
			ref TerritoryInfo reference = ref Amplitude.Mercury.Sandbox.Sandbox.World.TerritoryInfo.Data[territoryIndex];
			int territoryIndex2 = Amplitude.Mercury.Sandbox.Sandbox.World.TileInfo.Data[num].TerritoryIndex;
			byte empireIndex = Amplitude.Mercury.Sandbox.Sandbox.World.TerritoryInfo.Data[territoryIndex2].EmpireIndex;
			Empire seller = Amplitude.Mercury.Sandbox.Sandbox.Empires[empireIndex];
			setupCost = tradeController.ComputeCost(buyer, seller, num2, tradeController.tempTradeRoadPathInfo);
			sellingGain = tradeController.ComputeGain(buyer, seller, num2);
			ResourceType resource = Amplitude.Mercury.Sandbox.Sandbox.World.ResourceDepositInfo.GetReferenceAt(num2).Resource;
			int num3 = tradeController.TradeRoadAllocator.Allocate();
			ref TradeRoadInfo referenceAt = ref tradeController.TradeRoadAllocator.GetReferenceAt(num3);
			referenceAt.DestinationCity = reference.SettlementGUID;
			referenceAt.DestinationEmpireIndex = buyer.Index;
			referenceAt.SellerGain = sellingGain;
			referenceAt.RansackValue = 0;
			referenceAt.ResourceExtractor = resourceExtractorToTrade.GUID;
			referenceAt.ResourceTileIndex = num;
			referenceAt.DepositOwnerIndex = empireIndex;
			referenceAt.OriginEmpireIndex = empireIndex;
			referenceAt.TradeRoadStatus = TradeRoadStatus.Active;
			referenceAt.TradedResource = resource;
			referenceAt.TurnOnCreation = SandboxManager.Sandbox.Turn;
			referenceAt.NumberOfTerritoriesCrossed = tradeController.ComputeNumberOfTerritoriesCrossed(tradeController.tempTradeRoadPathInfo);
			referenceAt.SetupCost = setupCost;
			referenceAt.IsForwardTrade = false;
			referenceAt.ResourceDepositIndex = num2;
			referenceAt.Flags = tradeController.ComputeTradeRoadFlags(tradeController.tempTradeRoadPathInfo, resource, isForward: false);
			tradeController.EnforceSteps(num3, tradeController.tempTradeRoadPathInfo);
			referenceAt.PathCount = tradeController.TradeRoadPathByTradeRoadIndex[num3].Count;
			tradeController.TradeRoadAllocator.SetSynchronizationDirty();
			if (tradeController.NumberOfRoadsPerDeposit[num2] == 0)
			{
				resourceExtractorToTrade.AddDescriptor(tradeController.resourceBeingTraded);
			}
			tradeController.NumberOfRoadsPerDeposit[num2]++;
			tradeController.CreateRoundHousesAndJettyForTradeRoadIfNecessary(num3, buyer);
			tradeController.TraverseRoadPathForTradeLinkCount(num3, add: true);
			//tradeController.IncreaseBaseCostFor(num2);
			SimulationEvent_TradeRoadCollectionChanged.Raise(tradeController, num3, TradeRoadChangeAction.Created, TradeRoadStatus.None, buyer.Index);
			return num3;
		}

		internal static int RestoreForwardTrading(ForwardedTradeRouteToRestore forwardedTradeRouteToRestore, TradeRoadPathTypes tradeRoadPathType, out FixedPoint setupCost, out FixedPoint sellingGain)
		{
			MajorEmpire seller = forwardedTradeRouteToRestore.seller;
			MajorEmpire buyer = forwardedTradeRouteToRestore.buyer;

			ref TradeController tradeController = ref Sandbox.TradeController;
			int num = seller.Capital.Entity.WorldPosition.ToTileIndex();
			if (!tradeController.TryFindPath(buyer, num, tradeRoadPathType, tradeController.tempTradeRoadPathInfo))
			{
				Diagnostics.LogError($"Path of type '{tradeRoadPathType}' not found.");
				setupCost = 0;
				sellingGain = 0;
				return -1;
			}
			int territoryIndex = tradeController.tempTradeRoadPathInfo.Data[tradeController.tempTradeRoadPathInfo.Length - 1].TerritoryIndex;
			ref TerritoryInfo reference = ref Amplitude.Mercury.Sandbox.Sandbox.World.TerritoryInfo.Data[territoryIndex];
			setupCost = tradeController.ComputeForwardCost(buyer, seller, forwardedTradeRouteToRestore.SetupCost, forwardedTradeRouteToRestore.ResourceDepositIndex, tradeController.tempTradeRoadPathInfo);
			sellingGain = tradeController.ComputeForwardGain(buyer, seller, forwardedTradeRouteToRestore.SetupCost);
			int num2 = tradeController.TradeRoadAllocator.Allocate();
			ref TradeRoadInfo referenceAt2 = ref tradeController.TradeRoadAllocator.GetReferenceAt(num2);
			referenceAt2.DestinationCity = reference.SettlementGUID;
			referenceAt2.DestinationEmpireIndex = buyer.Index;
			referenceAt2.SellerGain = sellingGain;
			referenceAt2.RansackValue = 0;
			referenceAt2.ResourceExtractor = forwardedTradeRouteToRestore.ResourceExtractor;
			referenceAt2.ResourceTileIndex = num;
			referenceAt2.DepositOwnerIndex = forwardedTradeRouteToRestore.DepositOwnerIndex;
			referenceAt2.OriginEmpireIndex = seller.Index;
			referenceAt2.TradeRoadStatus = TradeRoadStatus.Active;
			referenceAt2.TradedResource = forwardedTradeRouteToRestore.TradedResource;
			referenceAt2.TurnOnCreation = SandboxManager.Sandbox.Turn;
			referenceAt2.PathCount = tradeController.tempTradeRoadPathInfo.Length;
			referenceAt2.NumberOfTerritoriesCrossed = tradeController.ComputeNumberOfTerritoriesCrossed(tradeController.tempTradeRoadPathInfo);
			referenceAt2.SetupCost = setupCost;
			referenceAt2.IsForwardTrade = true;
			referenceAt2.ResourceDepositIndex = forwardedTradeRouteToRestore.ResourceDepositIndex;
			referenceAt2.Flags = tradeController.ComputeTradeRoadFlags(tradeController.tempTradeRoadPathInfo, forwardedTradeRouteToRestore.TradedResource, isForward: true);
			tradeController.EnforceSteps(num2, tradeController.tempTradeRoadPathInfo);
			referenceAt2.PathCount = tradeController.TradeRoadPathByTradeRoadIndex[num2].Count;
			tradeController.TradeRoadAllocator.SetSynchronizationDirty();
			tradeController.CreateRoundHousesAndJettyForTradeRoadIfNecessary(num2, buyer);
			tradeController.TraverseRoadPathForTradeLinkCount(num2, add: true);
			//tradeController.IncreaseForwardBaseCostFor(referenceAt.ResourceDepositIndex);
			SimulationEvent_TradeRoadCollectionChanged.Raise(tradeController, num2, TradeRoadChangeAction.Created, TradeRoadStatus.None, buyer.Index);
			return num2;
		}

	}
}
