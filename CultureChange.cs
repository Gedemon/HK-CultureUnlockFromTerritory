using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Framework;
using Amplitude.Mercury;
using Amplitude.Mercury.Data.Simulation;
using Amplitude.Mercury.Data.Simulation.Costs;
using Amplitude.Mercury.Interop;
using Amplitude.Mercury.Sandbox;
using Amplitude.Mercury.Simulation;
using Amplitude.Mercury.WorldGenerator;
using Amplitude.Serialization;
using BepInEx.Configuration;
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

			MajorEmpire potentialLiege = majorEmpire.Liege.Entity != null ? majorEmpire.Liege.Entity : majorEmpire;

			Diagnostics.LogWarning($"[Gedemon] potential Liege of the old Empire : {potentialLiege.FactionDefinition.Name}");

			int numMajor = Amplitude.Mercury.Sandbox.Sandbox.MajorEmpires.Length;

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
			
			return oldEmpire != null;
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
		public static bool GetTerritoryChangesOnEvolve(DepartmentOfDevelopment instance, StaticString nextFactionName, out int numCitiesLost, out District potentialCapital, ref IDictionary<Settlement, List<int>> territoryToDetachAndCreate, ref IDictionary<Settlement, List<int>> territoryToDetachAndFree, ref List<Settlement> settlementToLiberate, ref List<Settlement> settlementToFree)
		{

			Diagnostics.LogWarning($"[Gedemon] in GetTerritoryChangesOnEvolve.");

			potentialCapital = null;
			numCitiesLost = 0;

			MajorEmpire majorEmpire = instance.majorEmpire;
			//StaticString nextFactionName = instance.nextFactionName;

			Diagnostics.LogWarning($"[Gedemon] before IsEmpireHumanSlot");
			bool isHuman = TrueCultureLocation.IsEmpireHumanSlot(majorEmpire.Index);
			Diagnostics.LogWarning($"[Gedemon] before KeepOnlyCultureTerritory");
			bool keepOnlyCultureTerritory = TrueCultureLocation.KeepOnlyCultureTerritory();
			bool keepTerritoryAttached = TrueCultureLocation.KeepTerritoryAttached();
			bool capitalChanged = false; // the Capital hasn't been changed yet
			bool needNewCapital = false; // We need a new Capital (and we've not found it yet)

			if ((!isHuman) && TrueCultureLocation.NoTerritoryLossForAI())
			{
				keepOnlyCultureTerritory = false;
			}

			if (majorEmpire.DepartmentOfDevelopment.CurrentEraIndex != 0 && majorEmpire.FactionDefinition.Name != nextFactionName && keepOnlyCultureTerritory)
			{
				Diagnostics.LogWarning($"[Gedemon] before Check for new Capital (exist = {(majorEmpire.Capital.Entity != null)})");
				bool hasCapital = (majorEmpire.Capital.Entity != null);

				if (hasCapital)
				{
					Settlement Capital = majorEmpire.Capital;

					District capitalMainDistrict = Capital.GetMainDistrict();
					Diagnostics.LogWarning($"[Gedemon] before Check Capital Territory ({Capital.SettlementStatus} {Capital.EntityName} is current Capital)");
					needNewCapital = !CultureUnlock.HasTerritory(nextFactionName.ToString(), capitalMainDistrict.Territory.Entity.Index);

					if (needNewCapital && keepTerritoryAttached)
					{
						int count2 = Capital.Region.Entity.Territories.Count;
						for (int k = 0; k < count2; k++)
						{
							Territory territory = Capital.Region.Entity.Territories[k];
							if (CultureUnlock.HasTerritory(nextFactionName.ToString(), territory.Index))
							{
								needNewCapital = false;
								break;
							}
						}
					}

				}
				else
				{
					needNewCapital = true;
				}

				if (needNewCapital)
				{
					// need to find new Capital !
					Diagnostics.LogWarning($"[Gedemon] Searching new Capital in existing Cities first.");

					// check existing settlements first

					foreach (int territoryIndex in CultureUnlock.GetListTerritories(nextFactionName.ToString()))
					{
						int count4 = majorEmpire.Settlements.Count;
						for (int m = 0; m < count4; m++)
						{
							Settlement settlement = majorEmpire.Settlements[m];
							if (settlement.SettlementStatus == SettlementStatuses.City)
							{
								District potentialDistrict = settlement.GetMainDistrict();
								if (territoryIndex == potentialDistrict.Territory.Entity.Index)
								{
									Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : register City for new Capital in territory {potentialDistrict.Territory.Entity.Index}.");
									needNewCapital = false;
									capitalChanged = true;
									potentialCapital = potentialDistrict;
									break;
								}
							}
						}
						if (!needNewCapital)
                        {
							break;
                        }

					}


					if (needNewCapital)
					{
						Diagnostics.LogWarning($"[Gedemon] New Capital not found, now searching in Settlement without cities for a potential Capital position.");

						foreach (int territoryIndex in CultureUnlock.GetListTerritories(nextFactionName.ToString()))
						{

							int count5 = majorEmpire.Settlements.Count;
							for (int n = 0; n < count5; n++)
							{
								Settlement settlement = majorEmpire.Settlements[n];
								if (settlement.SettlementStatus != SettlementStatuses.City)
								{
									District potentialDistrict = settlement.GetMainDistrict();
									if (territoryIndex == potentialDistrict.Territory.Entity.Index)
									{
										Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : register Settlement to Create new Capital in territory {potentialDistrict.Territory.Entity.Index}.");
										needNewCapital = false;
										capitalChanged = true;
										potentialCapital = potentialDistrict;
										break;
									}
								}
							}
							if (!needNewCapital)
							{
								break;
							}
						}
					}
				}

				int count = majorEmpire.Settlements.Count;
				for (int j = 0; j < count; j++)
				{
					Settlement settlement = majorEmpire.Settlements[j];

					bool hasTerritoryFromNewCulture = false;

					int count2 = settlement.Region.Entity.Territories.Count;
					for (int k = 0; k < count2; k++)
					{
						Territory territory = settlement.Region.Entity.Territories[k];
						if (CultureUnlock.HasTerritory(nextFactionName.ToString(), territory.Index))
							hasTerritoryFromNewCulture = true;
					}

					//settlement.PublicOrderCurrent.Value

					bool keepSettlement = hasTerritoryFromNewCulture && keepTerritoryAttached;

					if (!keepSettlement)
					{
						if (settlement.SettlementStatus == SettlementStatuses.City)
						{
							District mainDistrict = settlement.GetMainDistrict();

							bool giveSettlement = !CultureUnlock.HasTerritory(nextFactionName.ToString(), mainDistrict.Territory.Entity.Index);

							Diagnostics.LogWarning($"[Gedemon] Settlement ID#{j}: City {settlement.EntityName} territories, give city = {giveSettlement}");

							for (int k = 0; k < count2; k++)
							{
								Territory territory = settlement.Region.Entity.Territories[(count2 - 1) - k]; // start from last to avoid "CannotDetachConnectorTerritory"
								if (territory.Index != mainDistrict.Territory.Entity.Index)
								{
									if (CultureUnlock.HasTerritory(nextFactionName.ToString(), territory.Index))
									{
										if (giveSettlement)
										{
											FailureFlags flag = majorEmpire.DepartmentOfTheInterior.CanDetachTerritoryFromCity(territory, settlement);
											if (flag == FailureFlags.None || flag == FailureFlags.CannotDetachConnectorTerritory)
											{
												Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : Add to territoryToDetachAndCreate for index = {territory.Index}, is in new Culture Territory but loosing City");

												if (territoryToDetachAndCreate.ContainsKey(settlement))
												{
													territoryToDetachAndCreate[settlement].Add(territory.Index);
												}
												else
												{
													territoryToDetachAndCreate.Add(settlement, new List<int> { territory.Index });
												}
											}
											else
											{
												Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : FAILED to detach and create for territory index = {territory.Index}, {majorEmpire.DepartmentOfTheInterior.CanDetachTerritoryFromCity(territory, settlement)}, is new Culture Territory but loosing city");
											}
										}
										else
										{
											Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : No change for territory index = {territory.Index}, is in new Culture Territory and keeping City");
										}
									}
									else
									{
										if (!giveSettlement)
										{
											FailureFlags flag = majorEmpire.DepartmentOfTheInterior.CanDetachTerritoryFromCity(territory, settlement);
											if (flag == FailureFlags.None || flag == FailureFlags.CannotDetachConnectorTerritory)
											{
												Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : Add to territoryToDetachAndFree for index = {territory.Index}, not in new Culture Territory and keeping city");

												if (territoryToDetachAndFree.ContainsKey(settlement))
												{
													territoryToDetachAndFree[settlement].Add(territory.Index);
												}
												else
												{
													territoryToDetachAndFree.Add(settlement, new List<int> { territory.Index });
												}
											}
											else
											{
												Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : FAILED to detach for territory index = {territory.Index}, {majorEmpire.DepartmentOfTheInterior.CanDetachTerritoryFromCity(territory, settlement)}, not in new Culture Territory and keeping city");
											}
										}
										else
										{
											Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : No change for territory index = {territory.Index}, not in new Culture Territory, as is city");
										}

									}
								}
							}
							if (giveSettlement)
							{
								numCitiesLost++;
								FailureFlags flag = DepartmentOfTheInterior.CanLiberateSettlement(majorEmpire, settlement);
								if (flag == FailureFlags.None || flag == FailureFlags.SettlementIsCapital)
								{
									Diagnostics.LogWarning($"[Gedemon] City {settlement.EntityName} : Add to settlementToLiberate");
									settlementToLiberate.Add(settlement);
								}
								else
								{
									Diagnostics.LogWarning($"[Gedemon] City {settlement.EntityName} : Can't Liberate ({flag}), Add to settlementToFree (after creating new Capital)");
									settlementToFree.Add(settlement);
								}
							}
						}
						else
						{
							Diagnostics.LogWarning($"[Gedemon] Settlement ID#{j}: Check {settlement.SettlementStatus} {settlement.EntityName} territory");

							if (settlement.SettlementStatus != SettlementStatuses.None)
							{
								Territory territory = settlement.Region.Entity.Territories[0];
								if (!CultureUnlock.HasTerritory(nextFactionName.ToString(), territory.Index))
								{
									Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : Add to settlementToFree for index = {territory.Index}, not city, not in new Culture Territory");
									settlementToFree.Add(settlement);
								}
								else
								{
									Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : No change for territory index = {territory.Index}, not city, is in new Culture Territory");
								}
							}
						}
					}
				}
			}

			return needNewCapital || capitalChanged;
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
	}

	class SettlementRefund
    {
		int compensationFactor = TrueCultureLocation.GetCompensationFactor();
		int baseInfluenceRefund;
		public int influenceRefund;
		public FixedPoint productionRefund;
		public FixedPoint moneyRefund;
		public FixedPoint scienceRefund;

		MajorEmpire majorEmpire;

		public SettlementRefund(MajorEmpire empire)
        {
			baseInfluenceRefund = compensationFactor * 10;
			influenceRefund = 0;
			productionRefund = 0;
			moneyRefund = 0;
			scienceRefund = 0;
			majorEmpire = empire;
		}

		public void CompensateFor(Settlement settlement)
        {
			influenceRefund += 30 + (baseInfluenceRefund * majorEmpire.Settlements.Count);
			productionRefund += settlement.ProductionNet.Value * compensationFactor;
			moneyRefund += settlement.MoneyNet.Value * compensationFactor;
			moneyRefund += CultureChange.GetResourcesCompensation(settlement, majorEmpire);
			scienceRefund += settlement.ScienceNet.Value * compensationFactor;

			Diagnostics.LogWarning($"[Gedemon] iterating SettlementImprovements...");
			foreach (SettlementImprovement improvement in settlement.SettlementImprovements.Data)
			{
				Diagnostics.LogWarning($"[Gedemon] Family = {improvement.Family}");
				if (improvement.BuiltImprovements != null)
				{
					foreach (SettlementImprovementDefinition definition in improvement.BuiltImprovements)
					{
						Diagnostics.LogWarning($"[Gedemon] Improvement {definition.Name}");
						FixedPoint cost = definition.ProductionCostDefinition.GetCost(majorEmpire);
						Diagnostics.LogWarning($"[Gedemon] Cost = {cost}");
						productionRefund += cost;
					}

				}
				else
				{
					Diagnostics.LogError($"[Gedemon] improvement.BuiltImprovements is NULL, ignoring");
				}
			}

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
