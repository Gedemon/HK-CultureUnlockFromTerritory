using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Framework;
using Amplitude.Mercury;
using Amplitude.Mercury.Data.Simulation;
using Amplitude.Mercury.Data.Simulation.Costs;
using Amplitude.Mercury.Interop;
using Amplitude.Mercury.Sandbox;
using Amplitude.Mercury.Simulation;
using FailureFlags = Amplitude.Mercury.Simulation.FailureFlags;

namespace Gedemon.TrueCultureLocation
{
    class CultureChange
    {
		public static MinorEmpire GetMinorFactionFor(FactionDefinition factionDefinition)
        {
			MinorEmpire minorEmpire;

			if (Sandbox.MinorFactionManager.PeacefulHumanSpawner.TryAllocateMinorFaction(out minorEmpire))
			{
				minorEmpire.EraIndex = factionDefinition.EraIndex;
				minorEmpire.EraLevel.Value = minorEmpire.EraIndex;
				minorEmpire.SetFaction(factionDefinition);
				//Sandbox.MinorFactionManager.PeacefulHumanSpawner.SetMinorEmpireHomeStatus(minorEmpire, MinorEmpireHomeStatuses.POI);
				Sandbox.MinorFactionManager.PeacefulHumanSpawner.UpdateMinorFactionAvailableUnits(minorEmpire);
				Sandbox.MinorFactionManager.PeacefulHumanSpawner.UpdateMinorFactionAvailableArmyPatterns(minorEmpire);
				Amplitude.Mercury.Sandbox.Sandbox.MinorFactionManager.InitializeMinorEmpireRelations(minorEmpire);
				Amplitude.Mercury.Sandbox.Sandbox.MinorFactionManager.PickRandomIdeologies(minorEmpire);
				Amplitude.Mercury.Sandbox.Sandbox.MinorFactionManager.PickRandomPatronOrder(minorEmpire);
			}
			
			return minorEmpire;

		}
		public static MinorEmpire DoLiberateSettlement(Settlement settlement, MajorEmpire majorEmpire)
		{
			MinorEmpire minorEmpire = Amplitude.Mercury.Sandbox.Sandbox.MinorFactionManager.PeacefulLiberateHumanSpawner.GetOrAllocateMinorEmpireFor(settlement);
			if (minorEmpire.RankedMajorEmpireIndexes[0] != majorEmpire.Index)
			{
				int num = Array.IndexOf(minorEmpire.RankedMajorEmpireIndexes, majorEmpire.Index);
				minorEmpire.RankedMajorEmpireIndexes[num] = minorEmpire.RankedMajorEmpireIndexes[0];
				minorEmpire.RankedMajorEmpireIndexes[0] = majorEmpire.Index;
			}
			List<int> territoryIndices = settlement.Region.Entity.TerritoryIndices;
			for (int num2 = majorEmpire.Squadrons.Count - 1; num2 >= 0; num2--)
			{
				Squadron squadron = majorEmpire.Squadrons[num2];
				if (squadron.AircraftCarrierArmy.Entity == null && territoryIndices.Contains(squadron.TerritoryIndex))
				{
					DepartmentOfDefense.ReleaseSquadron(squadron);
				}
			}

			DepartmentOfTheInterior.ChangeSettlementOwner(settlement, minorEmpire, keepCaptured: false);
			BaseHumanSpawnerDefinition spawnerDefinitionForMinorEmpire = minorEmpire.Spawner.GetSpawnerDefinitionForMinorEmpire(minorEmpire);
			MinorToMajorRelation minorToMajorRelation = minorEmpire.RelationsToMajor[majorEmpire.Index];
			if (minorToMajorRelation.PatronageStock.Value < 50)
			{
				minorToMajorRelation.PatronageStock.Value = 50;
				MinorFactionManager.RefreshPatronageState(minorToMajorRelation, minorEmpire.PatronageDefinition);
			}
			FixedPoint defaultGameSpeedMultiplier = Amplitude.Mercury.Sandbox.Sandbox.GameSpeedController.CurrentGameSpeedDefinition.DefaultGameSpeedMultiplier;
			minorEmpire.RemainingLifeTime += (int)FixedPoint.Ceiling(spawnerDefinitionForMinorEmpire.AddedLifeTimeInTurnsForNewPatronnage * defaultGameSpeedMultiplier);
			Amplitude.Mercury.Sandbox.Sandbox.VisibilityController.DirtyEmpireVision(minorEmpire);

			return minorEmpire;
		}
		public static MajorEmpire GetFreeMajorEmpire(bool canResurect = false, int maxFame = int.MaxValue)
        {

			MajorEmpire freeEmpire = null;
			int numMajor = Amplitude.Mercury.Sandbox.Sandbox.MajorEmpires.Length;

			if(!TrueCultureLocation.CanRespawnDeadPlayer())
            {
				canResurect = false;
			}

			// check for unused Empire in the pool first
			bool mustResurect = true;
			for (int i = 0; i < numMajor; i++)
			{
				MajorEmpire potentialEmpire = Sandbox.MajorEmpires[i];

				if (potentialEmpire.Armies.Count == 0 && potentialEmpire.Settlements.Count == 0 && potentialEmpire.IsAlive)
				{
					mustResurect = false;
				}
			}

			if (mustResurect && !canResurect)
            {
				return freeEmpire;
			}

			for (int i = 0; i < numMajor; i++)
			{
				MajorEmpire potentialEmpire = Sandbox.MajorEmpires[i];

				Diagnostics.LogWarning($"[Gedemon] potentialEmpire = {potentialEmpire.FactionDefinition.Name}, Armies = {potentialEmpire.Armies.Count}, Settlements = {potentialEmpire.Settlements.Count}, mustResurect = {mustResurect}, isAlive = {potentialEmpire.IsAlive}, fame = {potentialEmpire.FameScore.Value}");

				// we don't want to resurect dead Empire with more fame than us !
				if (!potentialEmpire.IsAlive && potentialEmpire.FameScore.Value >= maxFame)
				{
					Diagnostics.LogWarning($"[Gedemon] aborting, potential resurrected vassal fame > {maxFame}");
					continue;
				}

				if (potentialEmpire.Armies.Count == 0 && potentialEmpire.Settlements.Count == 0 && (potentialEmpire.IsAlive || mustResurect))
				{
					freeEmpire = potentialEmpire;
					return freeEmpire;
				}
			}

			return freeEmpire;

		}
		public static bool TryInitializeFreeMajorEmpireToReplace(MajorEmpire majorEmpire, out MajorEmpire oldEmpire)
		{
			Diagnostics.LogWarning($"[Gedemon] TryInitializeFreeMajorEmpireToReplace for {majorEmpire.FactionDefinition.Name}...");


			oldEmpire = GetFreeMajorEmpire(true, (int)majorEmpire.FameScore.Value);

			if (oldEmpire != null)
			{
				Diagnostics.LogWarning($"[Gedemon] DepartmentOfDevelopment.CurrentEraIndex: {oldEmpire.DepartmentOfDevelopment.CurrentEraIndex} => {majorEmpire.DepartmentOfDevelopment.CurrentEraIndex}");
				oldEmpire.IsAlive = true;
				oldEmpire.DepartmentOfDevelopment.nextFactionName = StaticString.Empty;
				oldEmpire.DepartmentOfDevelopment.isNextFactionConfirmed = false;
				Diagnostics.LogWarning($"[Gedemon] before changes, EraLevel: {oldEmpire.EraLevel.Value}");
				oldEmpire.DepartmentOfDevelopment.CurrentEraIndex = majorEmpire.DepartmentOfDevelopment.CurrentEraIndex;
				//oldEmpire.DepartmentOfScience.CurrentTechnologicalEraIndex = majorEmpire.DepartmentOfScience.CurrentTechnologicalEraIndex;
				oldEmpire.ChangeFaction(majorEmpire.FactionDefinition.Name); // before ApplyStartingEra !
				oldEmpire.DepartmentOfDevelopment.ApplyStartingEra();
				oldEmpire.DepartmentOfDevelopment.PickFirstEraStars();
				//oldEmpire.DepartmentOfDevelopment.ApplyNextEra();

				Diagnostics.LogWarning($"[Gedemon] DepartmentOfScience.CurrentTechnologicalEraIndex = {oldEmpire.DepartmentOfScience.CurrentTechnologicalEraIndex}");
				oldEmpire.DepartmentOfScience.UpdateCurrentTechnologyEraIfNeeded();
				Diagnostics.LogWarning($"[Gedemon] After UpdateCurrentTechnologyEraIfNeeded, CurrentTechnologicalEraIndex = {oldEmpire.DepartmentOfScience.CurrentTechnologicalEraIndex}");
				oldEmpire.DepartmentOfScience.CompleteAllPreviousErasTechnologiesOnStart();
				// Adding Techs
                {
					int length = oldEmpire.DepartmentOfScience.Technologies.Length;
					for (int t = 0; t < length; t++)
					{
						ref Technology reference = ref oldEmpire.DepartmentOfScience.Technologies.Data[t];
						ref Technology reference2 = ref majorEmpire.DepartmentOfScience.Technologies.Data[t];

						//Diagnostics.LogWarning($"[Gedemon] Check {reference.TechnologyDefinition.Name} (replacing Empire = {reference.TechnologyState}, Major Empire {reference2.TechnologyDefinition.Name} = {reference2.TechnologyState})" );

						if (reference2.TechnologyState == TechnologyStates.Completed && reference.TechnologyState != TechnologyStates.Completed)
						{
							reference.InvestedResource = reference.Cost;
							reference.TechnologyState = TechnologyStates.Completed;
							oldEmpire.DepartmentOfScience.TechnologicalEras.Data[reference.EraIndex].ResearchedTechnologyCount++;
							oldEmpire.DepartmentOfScience.ApplyTechnologyEffects(reference.TechnologyDefinition, raiseSimulationEvents: false);
							Amplitude.Mercury.Sandbox.Sandbox.SimulationEntityRepository.SetSynchronizationDirty(oldEmpire);

							Diagnostics.LogWarning($"[Gedemon] Completing know Tech : {reference.TechnologyDefinition.Name} ({reference.TechnologyState})");
						}
					}

					oldEmpire.DepartmentOfScience.Technologies.Frame = Amplitude.Mercury.Sandbox.Sandbox.Frame;
					oldEmpire.DepartmentOfScience.TechnologicalEras.Frame = Amplitude.Mercury.Sandbox.Sandbox.Frame;
					oldEmpire.DepartmentOfScience.UpdateTechnologiesState();
				}

				Diagnostics.LogWarning($"[Gedemon] after changes, EraLevel: {oldEmpire.EraLevel.Value}");
				oldEmpire.SetEmpireSymbol(majorEmpire.EmpireSymbolDefinition.Name);
				SetFactionSymbol(oldEmpire);
			}
			
			return oldEmpire != null;
		}

		public static void FinalizeMajorEmpireSpawning(MajorEmpire majorEmpire)
        {
			majorEmpire.TileInfo.Frame = Amplitude.Mercury.Sandbox.Sandbox.Frame;
			Amplitude.Mercury.Sandbox.Sandbox.World.InitializeMajorEmpireTileInfo(majorEmpire);
			majorEmpire.TransitTileInfo.Frame = Amplitude.Mercury.Sandbox.Sandbox.Frame;
			int length = majorEmpire.TileInfo.Length;
			majorEmpire.TransitTileInfo.Resize(length);
			for (int i = 0; i < length; i++)
			{
				TransitTileInfo value = default(TransitTileInfo);
				value.HistoricalTransitInfoIndex = -1;
				majorEmpire.TransitTileInfo[i] = value;
			}
			majorEmpire.DepartmentOfDevelopment.PickFirstEraStars();
			majorEmpire.lastArmyMaximumSize = majorEmpire.ArmyMaximumSize.Value;
			majorEmpire.lastArmyReinforcementCap = majorEmpire.ArmyReinforcementCap.Value;
			majorEmpire.DepartmentOfTheInterior.ResetSettlementNames();
			majorEmpire.DepartmentOfDefense.TryFindValidUnitFor(MilitiaHelper.MilitiaFamiliyName, out majorEmpire.DepartmentOfDefense.BestMilitiaDefinition);
			majorEmpire.DepartmentOfDefense.TryFindValidUnitFor(SiegeUnitHelper.SiegeUnitFamilyName, out majorEmpire.DepartmentOfDefense.BestSiegeUnitDefinition);
			majorEmpire.DepartmentOfDefense.TryFindValidUnitFor(DepartmentOfDefense.NomadicFamilyName, out majorEmpire.DepartmentOfDefense.BestNomadicUnitDefinition);
			if (majorEmpire.DepartmentOfDevelopment.IsActionAvailable(ActionType.BuyNeighbourSettlementWithMoney))
			{
				majorEmpire.ExoticAbilityFlags |= EmpireExoticAbilityFlags.CanBuyNeighbourTerritoryWithMoney;
			}
			majorEmpire.DepartmentOfCulture.InitializeInvestResourceDepositAction();
			if (majorEmpire.DepartmentOfDevelopment.IsActionAvailable(ActionType.ForwardTrade))
			{
				majorEmpire.ExoticAbilityFlags |= EmpireExoticAbilityFlags.CanForwardTrade;
			}
			WorldPosition worldPosition = new WorldPosition();
			if (majorEmpire.Capital.Entity != null)
            {
				worldPosition = majorEmpire.Capital.Entity.WorldPosition;
			}
			else
            {
				int count = majorEmpire.Settlements.Count;
				for (int j = 0; j < count; j++)
				{
					Settlement settlement = majorEmpire.Settlements[j];
					if (settlement.SettlementStatus == SettlementStatuses.City)
					{
						worldPosition = settlement.WorldPosition;
						break;
					}
				}
			}
			majorEmpire.OriginalContinentIndex = Amplitude.Mercury.Sandbox.Sandbox.World.GetContinentIndex(worldPosition.ToTileIndex());
		}

		public static void SetDiplomaticRelationFromEvolution(MajorEmpire majorEmpire, MajorEmpire oldEmpire)
		{
			MajorEmpire potentialLiege = majorEmpire.Liege.Entity ?? majorEmpire;

			Diagnostics.LogWarning($"[Gedemon] Setting Diplomatic Relation From Evolution, potential Liege of the old Empire : ID#{potentialLiege.Index} {potentialLiege.FactionDefinition.Name}");

			int numMajor = Amplitude.Mercury.Sandbox.Sandbox.MajorEmpires.Length;

			// reset diplomatic relations
			for (int otherIndex = 0; otherIndex < numMajor; otherIndex++)
			{
				{
					DiplomaticRelation relationtoReset = Sandbox.DiplomaticAncillary.GetRelationFor(oldEmpire.Index, otherIndex);
					ResetDiplomaticAmbassy(relationtoReset.LeftAmbassy);
					ResetDiplomaticAmbassy(relationtoReset.RightAmbassy);
					relationtoReset.ApplyState(DiplomaticStateType.Unknown, otherIndex);
				}
			}
			// Set Vassal (to do : depending of stability)
			{
				DiplomaticRelation relationFor = Sandbox.DiplomaticAncillary.GetRelationFor(potentialLiege.Index, oldEmpire.Index);
				DiplomaticStateType state = relationFor.DiplomaticState.State;
				Diagnostics.LogWarning($"[Gedemon] Set Vassal from current DiplomaticState = {state}");
				/*
				if (state == DiplomaticStateType.VassalToLiege)
				{
					relationFor.ApplyState(DiplomaticStateType.War, majorEmpire.Index);
					SimulationEvent_DiplomaticStateChanged.Raise(majorEmpire, majorEmpire.Index, relationFor.DiplomaticState.State, state, oldEmpire.Index, -1);
					state = relationFor.DiplomaticState.State;
				}
				//*/
				relationFor.ApplyState(DiplomaticStateType.VassalToLiege, potentialLiege.Index);
				relationFor.UpdateAbilities(raiseSimulationEvents: true);
				SimulationEvent_DiplomaticStateChanged.Raise(potentialLiege, potentialLiege.Index, relationFor.DiplomaticState.State, state, oldEmpire.Index, -1);
				Diagnostics.LogWarning($"[Gedemon] New  DiplomaticState = {Sandbox.DiplomaticAncillary.GetRelationFor(potentialLiege.Index, oldEmpire.Index).DiplomaticState.State}");
				Sandbox.SimulationEntityRepository.SetSynchronizationDirty(potentialLiege);
				Sandbox.SimulationEntityRepository.SetSynchronizationDirty(oldEmpire);
			}

		}

		private static void ResetDiplomaticAmbassy(DiplomaticAmbassy diplomaticAmbassy)
		{
			diplomaticAmbassy.OnGoingDemands.Clear();
			diplomaticAmbassy.AvailableGrievances.Clear();
			diplomaticAmbassy.EmpireMoral.Empty();
		}
		public static void SetFactionSymbol(MajorEmpire empire)
        {

			//*
			IDatabase<EmpireSymbolDefinition> database = Databases.GetDatabase<EmpireSymbolDefinition>();
			foreach (EmpireSymbolDefinition symbol in database)
			{
				if (empire.FactionDefinition.Name.ToString().Length > 13 && symbol.Name.ToString().Length > 23)
				{
					string factionSuffix = empire.FactionDefinition.Name.ToString().Substring(13); // Civilization_Era2_RomanEmpire
					string symbolSuffix = symbol.Name.ToString().Substring(23); // EmpireSymbolDefinition_Era2_RomanEmpire
					if (factionSuffix == symbolSuffix)
					{
						Diagnostics.LogWarning($"[Gedemon] {symbol.Name} {symbolSuffix} == {empire.FactionDefinition.Name} {factionSuffix}");

						empire.SetEmpireSymbol(symbol.Name);

						ref EmpireNameInfo reference = ref Sandbox.EmpireNamesRepository.EmpireNamePerIndex[empire.Index];

						reference.SymbolIcon = empire.EmpireSymbolDefinition.Symbol;
						reference.SymbolTexture = empire.EmpireSymbolDefinition.Texture;
						reference.EmpireBannerOrnamentTexture = (empire.EmpireBannerOrnamentDefinition?.CircleTexture ?? Amplitude.UI.UITexture.None);

						Sandbox.EmpireNamesRepository.Frame = Amplitude.Mercury.Sandbox.Sandbox.Frame;
					}
				}
			}
			//*/

		}

		public static void UpdateDistrictVisuals(Empire empire)
        {
			int count = empire.Settlements.Count;
			for (int m = 0; m < count; m++)
			{
				Settlement settlement = empire.Settlements[m];

				// 
				int count2 = settlement.Region.Entity.Territories.Count;
				for (int k = 0; k < count2; k++)
				{
					Territory territory = settlement.Region.Entity.Territories[k];
					District district = territory.AdministrativeDistrict;
					if (CultureUnlock.HasTerritory(empire.FactionDefinition.Name.ToString(), territory.Index))
					{
						if (district != null)
						{
							Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : update Administrative District visual in territory {district.Territory.Entity.Index}.");
							district.InitialVisualAffinityName = DepartmentOfTheInterior.GetInitialVisualAffinityFor(empire, district.DistrictDefinition);
						}
						else
						{
							Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : no Administrative District in territory {district.Territory.Entity.Index}.");
						}
					}
					else
					{
						// add instability here ?
						Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : PublicOrderCurrent = {settlement.PublicOrderCurrent.Value}, PublicOrderPositiveTrend = {settlement.PublicOrderPositiveTrend.Value}, PublicOrderNegativeTrend = {settlement.PublicOrderNegativeTrend.Value}, DistanceInTerritoryToCapital = {settlement.DistanceInTerritoryToCapital.Value}.");
						if (district != null)
							Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {district.DistrictDefinition.Name} : PublicOrderProduced = {district.PublicOrderProduced.Value}.");

					}
				}
			}
		}

		public static FixedPoint GetResourcesCompensation(Settlement settlement, MajorEmpire majorEmpire)
		{
			FixedPoint compensation = 0;
			int count2 = settlement.ResourceExtractors.Count;
			for (int k = 0; k < count2; k++)
			{
				District district = settlement.ResourceExtractors[k];
				int num = district.WorldPosition.ToTileIndex();
				ref TileInfo reference = ref Amplitude.Mercury.Sandbox.Sandbox.World.TileInfo.Data[num];
				if (reference.PointOfInterest < World.Tables.PointOfInterestDefinitions.Length)
				{
					Amplitude.Mercury.Data.World.ResourceDepositDefinition resourceDepositDefinition = World.Tables.PointOfInterestDefinitions[reference.PointOfInterest] as Amplitude.Mercury.Data.World.ResourceDepositDefinition;
					if (resourceDepositDefinition != null)
					{
						FixedPoint cost = Sandbox.TradeController.ComputeLicenseCost(majorEmpire, majorEmpire, (int)resourceDepositDefinition.ResourceDefinition.ResourceType);
						compensation += cost;
						Diagnostics.LogWarning($"[Gedemon] Adding Money Compensation for resource {resourceDepositDefinition.ResourceDefinition.Name}, baseCost = {resourceDepositDefinition.ResourceDefinition.TradeBaseLicenceCost}, License Cost = {cost}");
					}
				}
			}
			return compensation;
		}

		public static void RemoveTradeRoutesEnding(Settlement settlement, Empire empire)
		{
			Sandbox.TradeController.SetAvailableTradeRoadDirty();
			int capacity = Sandbox.TradeController.TradeRoadAllocator.Capacity;
			for (int tradeRoadIndex = 0; tradeRoadIndex < capacity; tradeRoadIndex++)
			{
				TradeRoadInfo tradeRoadInfo = Sandbox.TradeController.TradeRoadAllocator.GetReferenceAt(tradeRoadIndex);
				if (tradeRoadInfo.PoolAllocationIndex < 0 || tradeRoadInfo.TradeRoadStatus == TradeRoadStatus.Destroyed || tradeRoadInfo.DestinationEmpireIndex != empire.Index)
				{
					continue;
				}
				if (tradeRoadInfo.DestinationCity == settlement.GUID)
				{
					Diagnostics.LogWarning($"[Gedemon] Destroying route ending in settlement to prevent suspension (ID#{tradeRoadIndex}), originEmpire = {tradeRoadInfo.OriginEmpireIndex}, destinationEmpire = {tradeRoadInfo.DestinationEmpireIndex}, IsRestoreDestroyedTradeRoutes = {TradeRoute.IsRestoreDestroyedTradeRoutes}");
					Sandbox.TradeController.DestroyTradeRoad(tradeRoadIndex, TradeRoadChangeAction.TimedOut);
				}
			}
		}
	}

	class SettlementRefund
    {
		FixedPoint defaultGameSpeedMultiplier = Sandbox.GameSpeedController.CurrentGameSpeedDefinition.DefaultGameSpeedMultiplier;

		CompensationLevel compensationLevel = (CompensationLevel)TrueCultureLocation.GetCompensationLevel();
		int compensationFactor;
		FixedPoint baseInfluenceFactor;
		FixedPoint baseProductionFactor;
		FixedPoint baseMoneyFactor;
		FixedPoint baseScienceFactor;
		FixedPoint influenceRefundFactor;
		FixedPoint productionRefundFactor;
		FixedPoint moneyRefundFactor;
		FixedPoint scienceRefundFactor;
		public FixedPoint influenceRefund;
		public FixedPoint productionRefund;
		public FixedPoint moneyRefund;
		public FixedPoint scienceRefund;

		MajorEmpire majorEmpire;
		// 5- 10 -20
		public SettlementRefund(MajorEmpire empire)
        {
			switch(compensationLevel)
            {
				case CompensationLevel.None:
                {
						compensationFactor = 0;
						baseInfluenceFactor = FixedPoint.Zero;
						baseProductionFactor = FixedPoint.Zero;
						baseMoneyFactor = FixedPoint.Zero;
						baseScienceFactor = FixedPoint.Zero;

						influenceRefundFactor = FixedPoint.Zero;
						productionRefundFactor = FixedPoint.Zero;
						moneyRefundFactor = FixedPoint.Zero;
						scienceRefundFactor = FixedPoint.Zero;

						break;
					}
				case CompensationLevel.Low:
					{
						compensationFactor = 5;
						baseInfluenceFactor = 1;
						baseProductionFactor = (FixedPoint)0.15;
						baseMoneyFactor = (FixedPoint)0.5;
						baseScienceFactor = (FixedPoint)1.5;

						influenceRefundFactor = baseInfluenceFactor;
						productionRefundFactor = compensationFactor * baseProductionFactor;
						moneyRefundFactor = compensationFactor * baseMoneyFactor * defaultGameSpeedMultiplier;
						scienceRefundFactor = compensationFactor * baseScienceFactor * defaultGameSpeedMultiplier;

						break;
					}

				case CompensationLevel.Average:
					{
						compensationFactor = 10;
						baseInfluenceFactor = 2;
						baseProductionFactor = (FixedPoint)0.25;
						baseMoneyFactor = (FixedPoint)0.5;
						baseScienceFactor = (FixedPoint)2;

						break;
					}
				case CompensationLevel.High:
					{
						compensationFactor = 20;
						baseInfluenceFactor = 5;
						baseProductionFactor = (FixedPoint)0.5;
						baseMoneyFactor = (FixedPoint)0.75;
						baseScienceFactor = (FixedPoint)2.5;

						influenceRefundFactor = baseInfluenceFactor;
						productionRefundFactor = compensationFactor * baseProductionFactor;
						moneyRefundFactor = compensationFactor * baseMoneyFactor * defaultGameSpeedMultiplier;
						scienceRefundFactor = compensationFactor * baseScienceFactor * defaultGameSpeedMultiplier;

						break;
					}
				default:
					{
						compensationFactor = 10;
						baseInfluenceFactor = 2;
						baseProductionFactor = (FixedPoint)0.25;
						baseMoneyFactor = (FixedPoint)0.5;
						baseScienceFactor = (FixedPoint)2;

						influenceRefundFactor = baseInfluenceFactor;
						productionRefundFactor = compensationFactor * baseProductionFactor;
						moneyRefundFactor = compensationFactor * baseMoneyFactor * defaultGameSpeedMultiplier;
						scienceRefundFactor = compensationFactor * baseScienceFactor * defaultGameSpeedMultiplier;

						break;
					}
			}

			influenceRefundFactor = baseInfluenceFactor;
			productionRefundFactor = compensationFactor * (1 + baseProductionFactor);
			moneyRefundFactor = compensationFactor * baseMoneyFactor * defaultGameSpeedMultiplier;
			scienceRefundFactor = compensationFactor * baseScienceFactor * defaultGameSpeedMultiplier;

			influenceRefund = 0;
			productionRefund = 0;
			moneyRefund = 0;
			scienceRefund = 0;
			majorEmpire = empire;
		}

		public void CompensateFor(Settlement settlement)
        {
			FixedPoint settleCost = majorEmpire.DepartmentOfTheTreasury.GetSettleCost(settlement.GetMainDistrict().Territory.Entity.Index);

			Diagnostics.LogWarning($"[Gedemon] Calculating refund for settlement (compensationLevel = {compensationLevel})=> settleCost = {settleCost}, ProductionNet = {settlement.ProductionNet.Value}, MoneyNet = {settlement.MoneyNet.Value}, ScienceNet = {settlement.ScienceNet.Value}.");
			Diagnostics.LogWarning($"[Gedemon] - influenceFactor = {influenceRefundFactor}, productionFactor = {productionRefundFactor}, moneyFactor = {moneyRefundFactor}, scienceFactor = {scienceRefundFactor}.");


			FixedPoint settlementInfluenceRefund = influenceRefundFactor * settleCost;
			FixedPoint settlementProductionRefund = settlement.ProductionNet.Value * productionRefundFactor;
			FixedPoint settlementMoneyRefund = settlement.MoneyNet.Value * moneyRefundFactor;
			FixedPoint settlementscienceRefund = settlement.ScienceNet.Value * scienceRefundFactor;

			settlementMoneyRefund += CultureChange.GetResourcesCompensation(settlement, majorEmpire) * baseMoneyFactor;

			Diagnostics.LogWarning($"[Gedemon] iterating SettlementImprovements...");
			foreach (SettlementImprovement improvement in settlement.SettlementImprovements.Data)
			{
				if (improvement.BuiltImprovements != null)
				{
					Diagnostics.LogWarning($"[Gedemon] Family = {improvement.Family}");
					foreach (SettlementImprovementDefinition definition in improvement.BuiltImprovements)
					{
						Diagnostics.LogWarning($"[Gedemon] Improvement {definition.Name}");
						FixedPoint cost = definition.ProductionCostDefinition.GetCost(majorEmpire);
						Diagnostics.LogWarning($"[Gedemon] Cost = {cost}");
						productionRefund += cost * baseProductionFactor;
					}
				}
			}

			Diagnostics.LogWarning($"[Gedemon] - influenceGain   = {settlementInfluenceRefund}, productionGain   = {settlementProductionRefund}, MoneyGain   = {settlementMoneyRefund}, scienceGain   = {settlementscienceRefund}.");

			influenceRefund += FixedPoint.Round(settlementInfluenceRefund);
			productionRefund += FixedPoint.Round(settlementProductionRefund);
			moneyRefund += FixedPoint.Round(settlementMoneyRefund);
			scienceRefund += FixedPoint.Round(settlementscienceRefund);

		}
		public void ApplyCompensation()
        {

			Diagnostics.LogWarning($"[Gedemon] Compensation : influenceRefund = {influenceRefund}, moneyRefund = {moneyRefund}, scienceRefund = {scienceRefund}, productionRefund = {productionRefund} => Capital = {majorEmpire.Capital.Entity.EntityName}");

			Diagnostics.Log($"[Gedemon] Current influence stock {majorEmpire.InfluenceStock.Value}");
			majorEmpire.DepartmentOfCulture.GainInfluence(influenceRefund);
			Diagnostics.Log($"[Gedemon] New influence stock {majorEmpire.InfluenceStock.Value}");
			Diagnostics.Log($"[Gedemon] Current money {majorEmpire.MoneyStock.Value}");
			majorEmpire.DepartmentOfTheTreasury.GainMoney(moneyRefund);
			Diagnostics.Log($"[Gedemon] New money {majorEmpire.MoneyStock.Value}");

			Diagnostics.Log($"[Gedemon] Current research stock {majorEmpire.DepartmentOfScience.TechnologyQueue.CurrentResourceStock}");
			FixedPoint techCostInQueue = FixedPoint.Zero;
			{
				TechnologyQueue techQueue = majorEmpire.DepartmentOfScience.TechnologyQueue;
				int numTechs = techQueue.TechnologyIndices.Count;
				for (int t = 0; t < numTechs; t++)
				{

					int num2 = techQueue.TechnologyIndices[t];
					ref Technology reference = ref majorEmpire.DepartmentOfScience.Technologies.Data[num2];
					if (!(reference.InvestedResource >= reference.Cost) && reference.TechnologyState != TechnologyStates.Completed)
					{

						techCostInQueue += reference.Cost - reference.InvestedResource;
						Diagnostics.Log($"[Gedemon] in TechQueue for {reference.TechnologyDefinition.name}, Cost = {reference.Cost}, Invested = {reference.InvestedResource}, Left = {reference.Cost - reference.InvestedResource}, total cost in Queue = {techCostInQueue}");
					}
				}
			}
			majorEmpire.DepartmentOfScience.GainResearch(scienceRefund, true, false);
			FixedPoint scienceLeft = scienceRefund - techCostInQueue;
			Diagnostics.Log($"[Gedemon] New research stock {majorEmpire.DepartmentOfScience.TechnologyQueue.CurrentResourceStock}, should be {scienceLeft}");
			if (majorEmpire.DepartmentOfScience.TechnologyQueue.CurrentResourceStock < scienceLeft)
			{
				majorEmpire.DepartmentOfScience.TechnologyQueue.CurrentResourceStock = scienceLeft;
			}

			Settlement currentCapital = majorEmpire.Capital;
			Diagnostics.Log($"[Gedemon] Current ConstructionQueue.Entity.CurrentResourceStock {currentCapital.EntityName} =  {currentCapital.ConstructionQueue.Entity.CurrentResourceStock}");
			currentCapital.ConstructionQueue.Entity.CurrentResourceStock += productionRefund;
			Diagnostics.Log($"[Gedemon] after refund: CurrentResourceStock {currentCapital.EntityName} =  {currentCapital.ConstructionQueue.Entity.CurrentResourceStock}");
			FixedPoint productionInQueue = FixedPoint.Zero;
			{
				ConstructionQueue constructionQueue = currentCapital.ConstructionQueue.Entity;
				int numConstruction = constructionQueue.Constructions.Count;
				Diagnostics.Log($"[Gedemon] numConstruction = {numConstruction}");
				for (int c = 0; c < numConstruction; c++)
				{
					Construction construction = constructionQueue.Constructions[c];
					Diagnostics.Log($"[Gedemon] in production queue for {construction.ConstructibleDefinition.Name}, Failures = {construction.FailureFlags}, HasBeenBoughtOut = {construction.HasBeenBoughtOut}, prod. left = {(construction.Cost - construction.InvestedResource)} (cost = {construction.Cost}, invested = {construction.InvestedResource})");

					switch (construction.ConstructibleDefinition.ProductionCostDefinition.Type)
					{
						case ProductionCostType.TurnBased:
							{
								productionInQueue += (construction.Cost - construction.InvestedResource);
								Diagnostics.Log($"[Gedemon] ProductionCostType.TurnBased : new calculated prod. required in queue = {productionInQueue}");
								break;
							}
						case ProductionCostType.Infinite:
							break;
						case ProductionCostType.Production:
							{
								productionInQueue += (construction.Cost - construction.InvestedResource);
								Diagnostics.Log($"[Gedemon] ProductionCostType.Production : new calculated prod. required in queue = {productionInQueue}");
								break;
							}
						case ProductionCostType.Transfert:
							{
								//productionInQueue += (construction.Cost - construction.InvestedResource);
								Diagnostics.Log($"[Gedemon] ProductionCostType.Transfert...");
								break;
							}
						default:
							Diagnostics.LogError("Invalid production cost type.");
							break;
					}
				}
			}
			majorEmpire.DepartmentOfIndustry.InvestProductionFor(currentCapital.ConstructionQueue); // this method change CurrentResourceStock to the minimum value between the current city production and CurrentResourceStock, feature or bug ?
			FixedPoint prodLeft = productionRefund - productionInQueue;
			Diagnostics.Log($"[Gedemon] after InvestProductionFor(ConstructionQueue) : CurrentResourceStock =  {currentCapital.ConstructionQueue.Entity.CurrentResourceStock} (should be {prodLeft})");
			if (currentCapital.ConstructionQueue.Entity.CurrentResourceStock < prodLeft)
			{
				currentCapital.ConstructionQueue.Entity.CurrentResourceStock = prodLeft;
				Amplitude.Mercury.Sandbox.Sandbox.SimulationEntityRepository.SetSynchronizationDirty(currentCapital.ConstructionQueue.Entity);
			}
		}
	}

}
