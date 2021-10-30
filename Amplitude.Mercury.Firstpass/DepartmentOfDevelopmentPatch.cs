
using System.Collections.Generic;
using Amplitude.Mercury.Simulation;
using Amplitude;
using HarmonyLib;
using Amplitude.Mercury.Interop;
using Amplitude.Mercury.Data.Simulation;
using Amplitude.Mercury.Sandbox;
using FailureFlags = Amplitude.Mercury.Simulation.FailureFlags;
using Amplitude.Framework;
using HumankindModTool;
using Amplitude.Mercury.Data.Simulation.Costs;

namespace Gedemon.TrueCultureLocation
{

	[HarmonyPatch(typeof(DepartmentOfDevelopment))]
	public class TCL_DepartmentOfDevelopment
	{
		[HarmonyPrefix]
		[HarmonyPatch(nameof(ApplyFactionChange))]
		public static void ApplyFactionChange(DepartmentOfDevelopment __instance)
		{
			Diagnostics.LogError($"[Gedemon] in ApplyFactionChange, {__instance.majorEmpire.PersonaName} is changing faction from  {__instance.majorEmpire.FactionDefinition.Name} to {__instance.nextFactionName}");
			Diagnostics.Log($"[Gedemon] UseTrueCultureLocation() = {CultureUnlock.UseTrueCultureLocation()}, KeepOnlyCultureTerritory =  {TrueCultureLocation.KeepOnlyCultureTerritory()},  KeepTerritoryAttached = {TrueCultureLocation.KeepTerritoryAttached()}, NoTerritoryLossForAI = {TrueCultureLocation.NoTerritoryLossForAI()}, TerritoryLossOption = {GameOptionHelper.GetGameOption(TrueCultureLocation.TerritoryLossOption)}");

			if (CultureUnlock.UseTrueCultureLocation())
			{
				MajorEmpire majorEmpire = __instance.majorEmpire;
				StaticString nextFactionName = __instance.nextFactionName;

				if (majorEmpire.DepartmentOfDevelopment.CurrentEraIndex != 0 && majorEmpire.FactionDefinition.Name != nextFactionName)
				{
					IDictionary<Settlement, List<int>> territoryToDetachAndCreate = new Dictionary<Settlement, List<int>>();    // Territories attached to a City that we want to detach and keep separated because the city is going to be lost
					IDictionary<Settlement, List<int>> territoryToDetachAndFree = new Dictionary<Settlement, List<int>>();      // Territories attached to a City that we want to detach because they are going to be lost, but not the city
					List<Settlement> settlementToLiberate = new List<Settlement>();                                             // Full Settlements (including a City or being a single-territory City) that are lost
					List<Settlement> settlementToFree = new List<Settlement>();                                                 // Single Settlement (not a city) that are lost

					MajorEmpire oldEmpire = null;
					Settlement capital = __instance.majorEmpire.Capital;
					District potentialCapital;// = capital;
					int numCitiesLost;

					bool useMajorEmpire = false;
					bool needNewCapital = CultureChange.GetTerritoryChangesOnEvolve(__instance, nextFactionName, out numCitiesLost, out potentialCapital, ref territoryToDetachAndCreate, ref territoryToDetachAndFree, ref settlementToLiberate, ref settlementToFree);

					Diagnostics.LogWarning($"[Gedemon] numCitiesLost = {numCitiesLost}, territoryToDetachAndCreate = {territoryToDetachAndCreate.Count}, territoryToDetachAndFree = {territoryToDetachAndFree.Count}, settlementToLiberate = {settlementToLiberate.Count}, settlementToFree = {settlementToFree.Count}, compensationFactor = {TrueCultureLocation.GetCompensationFactor()}.");
					if (numCitiesLost > 0)
					{
						useMajorEmpire = CultureChange.TryInitializeFreeMajorEmpireToReplace(majorEmpire, out oldEmpire);
					}

					int compensationFactor = TrueCultureLocation.GetCompensationFactor();
					int baseInfluenceRefund = compensationFactor * 10;
					int influenceRefund = 0;
					FixedPoint productionRefund = 0;
					FixedPoint moneyRefund = 0;
					FixedPoint scienceRefund = 0;
					MinorEmpire rebelFaction = null;

					Diagnostics.LogWarning($"[Gedemon] before territoryToDetachAndCreate...");
					foreach (KeyValuePair<Settlement, List<int>> kvp in territoryToDetachAndCreate)
					{
						foreach (int territoryIndex in kvp.Value)
						{
							Diagnostics.LogWarning($"[Gedemon] territoryToDetachAndCreate => #{territoryIndex} ({CultureUnlock.GetTerritoryName(territoryIndex)}).");
							influenceRefund += 30 + (baseInfluenceRefund * majorEmpire.Settlements.Count);
							DepartmentOfTheInterior.DetachTerritoryFromCity(kvp.Key, territoryIndex, createNewSettlement: true);
						}
					}

					Diagnostics.LogWarning($"[Gedemon] before territoryToDetachAndFree...");
					foreach (KeyValuePair<Settlement, List<int>> kvp in territoryToDetachAndFree)
					{
						foreach (int territoryIndex in kvp.Value)
						{
							Settlement settlement = DepartmentOfTheInterior.DetachTerritoryFromCity(kvp.Key, territoryIndex, createNewSettlement: true);

							Diagnostics.LogWarning($"[Gedemon] territoryToDetachAndFree => #{territoryIndex} ({CultureUnlock.GetTerritoryName(territoryIndex)}), ProductionNet = {settlement.ProductionNet.Value}, MoneyNet = {settlement.MoneyNet.Value}, ScienceNet = {settlement.ScienceNet.Value}.");
							influenceRefund += 30 + (baseInfluenceRefund * majorEmpire.Settlements.Count);
							productionRefund += settlement.ProductionNet.Value * compensationFactor;
							moneyRefund += settlement.MoneyNet.Value * compensationFactor;
							moneyRefund += GetResourcesCompensation(settlement, majorEmpire);
							scienceRefund += settlement.ScienceNet.Value * compensationFactor;
							if (useMajorEmpire)
							{
								Diagnostics.LogWarning($"[Gedemon] Calling GiveSettlementTo(settlement, oldEmpire)...");
								DepartmentOfDefense.GiveSettlementTo(settlement, oldEmpire);
							}
							else
							{
								if (rebelFaction == null)
								{
									Diagnostics.LogWarning($"[Gedemon] Calling DoLiberateSettlement(settlement, majorEmpire)...");
									rebelFaction = CultureChange.DoLiberateSettlement(settlement, majorEmpire);
								}
								else
								{
									Diagnostics.LogWarning($"[Gedemon] Calling GiveSettlementTo(settlement, rebelFaction)...");
									DepartmentOfDefense.GiveSettlementTo(settlement, rebelFaction);
								}
							}
						}
					}

					Diagnostics.LogWarning($"[Gedemon] before settlementToLiberate...");
					foreach (Settlement settlement in settlementToLiberate)
					{
						Diagnostics.LogWarning($"[Gedemon] settlementToLiberate => #{settlement.Region.Entity.Territories[0].Index} ({CultureUnlock.GetTerritoryName(settlement.Region.Entity.Territories[0].Index)}), ProductionNet = {settlement.ProductionNet.Value}, MoneyNet = {settlement.MoneyNet.Value}, ScienceNet = {settlement.ScienceNet.Value}.");
						influenceRefund += 30 + (baseInfluenceRefund * majorEmpire.Settlements.Count);
						productionRefund += settlement.ProductionNet.Value * compensationFactor;
						moneyRefund += settlement.MoneyNet.Value * compensationFactor;
						moneyRefund += GetResourcesCompensation(settlement, majorEmpire);
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

						Diagnostics.LogWarning($"[Gedemon]IsCapital ?");
						bool wasCapital = settlement.IsCapital;
						if (wasCapital)
						{
							Diagnostics.LogWarning($"[Gedemon] Was Capital, unset...");
							majorEmpire.DepartmentOfTheInterior.SetCapital(settlement, false);

						}

						Diagnostics.LogWarning($"[Gedemon] useMajorEmpire = {useMajorEmpire}");
						if (useMajorEmpire)
						{
							DepartmentOfDefense.GiveSettlementTo(settlement, oldEmpire);
							if (wasCapital)
							{
								Diagnostics.LogWarning($"[Gedemon] Was Capital, set as Capital for new spawned Empire...");
								majorEmpire.DepartmentOfTheInterior.SetCapital(settlement, true);
							}
						}
						else
						{
							if (rebelFaction == null)
							{
								Diagnostics.LogWarning($"[Gedemon] Calling DoLiberateSettlement(settlement, majorEmpire)...");
								rebelFaction = CultureChange.DoLiberateSettlement(settlement, majorEmpire);
							}
							else
							{
								Diagnostics.LogWarning($"[Gedemon] Calling GiveSettlementTo(settlement, rebelFaction)...");
								DepartmentOfDefense.GiveSettlementTo(settlement, rebelFaction);
							}
						}
					}

					Diagnostics.LogWarning($"[Gedemon] before changing Capital (need change = {needNewCapital}, potential exist = {potentialCapital != null})"); // before iterating settlementToFree (can't liberate those if you don't have a capital)
					if (needNewCapital)
					{

						if (potentialCapital == null)
						{
							Diagnostics.LogWarning($"[Gedemon] no potential Capital District was passed, try to find one in the territory list for the new faction...");
							foreach (int territoryIndex in CultureUnlock.GetListTerritories(nextFactionName.ToString()))
							{
								int count = majorEmpire.Settlements.Count;
								for (int n = 0; n < count; n++)
								{
									Settlement settlement = majorEmpire.Settlements[n];
									if (settlement.SettlementStatus != SettlementStatuses.City)
									{
										District potentialDistrict = settlement.GetMainDistrict();
										if (territoryIndex == potentialDistrict.Territory.Entity.Index)
										{
											Diagnostics.LogWarning($"[Gedemon] found new Capital District in {CultureUnlock.GetTerritoryName(territoryIndex)}");
											potentialCapital = potentialDistrict;
											break;
										}
									}
								}
							}

						}

						if (potentialCapital != null)
						{
							int territoryIndex = potentialCapital.Territory.Entity.Index;
							Settlement settlement = potentialCapital.Settlement;
							Diagnostics.LogWarning($"[Gedemon] try to set City as Capital in territory #{territoryIndex} ({CultureUnlock.GetTerritoryName(territoryIndex)}).");
							if (settlement.SettlementStatus != SettlementStatuses.City)
							{
								Diagnostics.LogWarning($"[Gedemon] try to create City for Capital...");
								majorEmpire.DepartmentOfTheInterior.ApplyEvolutionToSettlement(settlement, DepartmentOfTheInterior.EvolutionCityDefinition);
							}
							Diagnostics.LogWarning($"[Gedemon] Calling SetCapital...");
							majorEmpire.DepartmentOfTheInterior.SetCapital(settlement, set: true);
							majorEmpire.TurnWhenLastCapitalChanged = SandboxManager.Sandbox.Turn;
							majorEmpire.CapturedCapital.SetEntity(null);
							SimulationEvent_CapitalChanged.Raise(__instance, settlement, capital);
						}
						else
						{
							Diagnostics.LogError($"[Gedemon] No new Capital was set...");
						}
					}

					Diagnostics.LogWarning($"[Gedemon] before settlementToFree...");
					foreach (Settlement settlement in settlementToFree)
					{
						Diagnostics.LogWarning($"[Gedemon] settlementToFree => #{settlement.Region.Entity.Territories[0].Index} ({CultureUnlock.GetTerritoryName(settlement.Region.Entity.Territories[0].Index)}), ProductionNet = {settlement.ProductionNet.Value}, MoneyNet = {settlement.MoneyNet.Value}, ScienceNet = {settlement.ScienceNet.Value}.");
						influenceRefund += 30 + (baseInfluenceRefund * majorEmpire.Settlements.Count);
						productionRefund += settlement.ProductionNet.Value * compensationFactor;
						moneyRefund += settlement.MoneyNet.Value * compensationFactor;
						moneyRefund += GetResourcesCompensation(settlement, majorEmpire);
						scienceRefund += settlement.ScienceNet.Value * compensationFactor;
						if (useMajorEmpire)
						{
							Diagnostics.LogWarning($"[Gedemon] Calling GiveSettlementTo(settlement, oldEmpire)...");
							DepartmentOfDefense.GiveSettlementTo(settlement, oldEmpire);
						}
						else
						{
							if (rebelFaction == null)
							{
								Diagnostics.LogWarning($"[Gedemon] Calling DoLiberateSettlement(settlement, majorEmpire)...");
								rebelFaction = CultureChange.DoLiberateSettlement(settlement, majorEmpire);
							}
							else
							{
								Diagnostics.LogWarning($"[Gedemon] Calling GiveSettlementTo(settlement, rebelFaction)...");
								DepartmentOfDefense.GiveSettlementTo(settlement, rebelFaction);
							}
						}

					}

					Diagnostics.LogWarning($"[Gedemon] Compensation : influenceRefund = {influenceRefund}, moneyRefund = {moneyRefund}, scienceRefund = {scienceRefund}, productionRefund = {productionRefund} => Capital = {__instance.majorEmpire.Capital.Entity.EntityName}");

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
						for (int t=0; t < numTechs; t++)
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
					if(majorEmpire.DepartmentOfScience.TechnologyQueue.CurrentResourceStock < scienceLeft)
					{
						majorEmpire.DepartmentOfScience.TechnologyQueue.CurrentResourceStock = scienceLeft;
					}

					Settlement currentCapital = __instance.majorEmpire.Capital;
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

		[HarmonyPostfix]
		[HarmonyPatch(nameof(ApplyFactionChange))]
		public static void ApplyFactionChangePost(DepartmentOfDevelopment __instance)
		{

			if (CultureUnlock.UseTrueCultureLocation())
			{
				MajorEmpire majorEmpire = __instance.majorEmpire;

				Diagnostics.LogWarning($"[Gedemon] in ApplyFactionChangePost.");

				int count = majorEmpire.Settlements.Count;
				for (int m = 0; m < count; m++)
				{
					Settlement settlement = majorEmpire.Settlements[m];

					// 
					int count2 = settlement.Region.Entity.Territories.Count;
					for (int k = 0; k < count2; k++)
					{
						Territory territory = settlement.Region.Entity.Territories[k];
						District district = territory.AdministrativeDistrict;
						if (CultureUnlock.HasTerritory(majorEmpire.FactionDefinition.Name.ToString(), territory.Index))
						{
							if (district != null)
							{
								Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : update Administrative District visual in territory {district.Territory.Entity.Index}.");
								district.InitialVisualAffinityName = DepartmentOfTheInterior.GetInitialVisualAffinityFor(majorEmpire, district.DistrictDefinition);
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

				//*
				IDatabase<EmpireSymbolDefinition> database = Databases.GetDatabase<EmpireSymbolDefinition>();
				foreach (EmpireSymbolDefinition symbol in database)
				{
					if (majorEmpire.FactionDefinition.Name.ToString().Length > 13 && symbol.Name.ToString().Length > 23)
					{
						string factionSuffix = majorEmpire.FactionDefinition.Name.ToString().Substring(13); // Civilization_Era2_RomanEmpire
						string symbolSuffix = symbol.Name.ToString().Substring(23); // EmpireSymbolDefinition_Era2_RomanEmpire
						if (factionSuffix == symbolSuffix)
						{
							Diagnostics.LogWarning($"[Gedemon] {symbol.Name} {symbolSuffix} == {majorEmpire.FactionDefinition.Name} {factionSuffix}");

							majorEmpire.SetEmpireSymbol(symbol.Name);

						}
					}
				}
				//*/

				// hack to refresh culture symbol in UI
				Sandbox.EmpireNamesRepository.SandboxStarted_InitializeEmpireNames(SimulationPasses.PassContext.OrderProcessed, "EmpireNamesRepository_RefreshEmpireNames");
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(nameof(ComputeFactionStatus))]
		public static bool ComputeFactionStatus(DepartmentOfDevelopment __instance, ref FactionStatus __result, FactionDefinition factionDefinition)
		{
			MajorEmpire majorEmpire = __instance.majorEmpire;
			StaticString nextFactionName = __instance.nextFactionName;

			FactionStatus factionStatus = FactionStatus.Unlocked;

			/* Gedemon <<<<< */

			if (CultureUnlock.UseTrueCultureLocation())
			{
				bool lockedByTerritory = true;
				bool lockedByStartingSlot = true;

				//Diagnostics.Log($"[Gedemon] in ComputeFactionStatus, {majorEmpire.PersonaName} (ID={majorEmpire.Index}, EraStars ={majorEmpire.EraStarsCount.Value}/{majorEmpire.DepartmentOfDevelopment.CurrentEraStarRequirement}, knw={majorEmpire.KnowledgeStock.Value}, pop={majorEmpire.SumOfPopulationAndUnits.Value}) from {majorEmpire.FactionDefinition.Name} check to unlock {factionDefinition.Name}");

				string factionName = factionDefinition.Name.ToString();
				if (CultureUnlock.HasTerritory(factionName))
				{
					int count = majorEmpire.Settlements.Count;

					for (int i = 0; i < count; i++)
					{
						Settlement settlement = majorEmpire.Settlements[i];

						int count2 = settlement.Region.Entity.Territories.Count;
						for (int k = 0; k < count2; k++)
						{
							Amplitude.Mercury.Simulation.Territory territory = settlement.Region.Entity.Territories[k];
							bool anyTerritory = majorEmpire.DepartmentOfDevelopment.CurrentEraIndex == 0 || CultureUnlock.HasNoCapitalTerritory(factionName);
							bool validSettlement = (TrueCultureLocation.GetEraIndexCityRequiredForUnlock() > majorEmpire.DepartmentOfDevelopment.CurrentEraIndex + 1 || settlement.SettlementStatus == SettlementStatuses.City || CultureUnlock.IsNomadCulture(majorEmpire.FactionDefinition.name));
							if (CultureUnlock.HasTerritory(factionName, territory.Index, anyTerritory))
							{
								if (validSettlement)
								{
									//Diagnostics.Log($"[Gedemon] in ComputeFactionStatus, {majorEmpire.PersonaName} has Territory unlock for {factionDefinition.Name} from Territory ID = {territory.Index}");
									lockedByTerritory = false;
									break;
								}
								else
								{
									//Diagnostics.Log($"[Gedemon] in ComputeFactionStatus, {majorEmpire.PersonaName} has Territory for {factionDefinition.Name} from Territory ID = {territory.Index}, but is invalid ({settlement.SettlementStatus}, nextEraID = {majorEmpire.DepartmentOfDevelopment.CurrentEraIndex + 1}, cityRequiredAtEraID = {TrueCultureLocation.GetEraIndexCityRequiredForUnlock()}) ");
								}
							}
						}
					}

					// Check for AI Decision control
					if ((!majorEmpire.IsControlledByHuman) && TrueCultureLocation.UseLimitDecisionForAI() && (!lockedByTerritory) && (!TrueCultureLocation.NoTerritoryLossForAI()))
					{
						int territoriesLost = 0;
						int territoriesCount = 0;

						for (int j = 0; j < count; j++)
						{
							Settlement settlement = majorEmpire.Settlements[j];

							bool hasTerritoryFromNewCulture = false;
							int territoriesRemovedFromSettlement = 0;

							int count2 = settlement.Region.Entity.Territories.Count;
							territoriesCount += count2;

							for (int k = 0; k < count2; k++)
							{
								Territory territory = settlement.Region.Entity.Territories[k];
								if (CultureUnlock.HasTerritory(factionName, territory.Index))
									hasTerritoryFromNewCulture = true;
								else
									territoriesRemovedFromSettlement += 1;
							}

							bool keepSettlement = hasTerritoryFromNewCulture && TrueCultureLocation.KeepTerritoryAttached();

							if (!keepSettlement)
							{
								territoriesLost += territoriesRemovedFromSettlement;
							}
						}

						if (territoriesLost > territoriesCount * 0.5)
						{
							//Diagnostics.Log($"[Gedemon] in ComputeFactionStatus, AI limitation from territory loss = {territoriesLost} / {territoriesCount}");
							lockedByTerritory = true;
						}
					}

				}
				if (CultureUnlock.IsUnlockedByPlayerSlot(factionName, majorEmpire.Index))
				{
					lockedByStartingSlot = false;
					//Diagnostics.Log($"[Gedemon] in ComputeFactionStatus, {majorEmpire.PersonaName} has Starting Slot unlock for {factionDefinition.Name} from majorEmpire.Index = {majorEmpire.Index}");
				}

				if (lockedByTerritory && lockedByStartingSlot)
				{
					// unlock "backup" Culture after some time
					bool backupUnlocked = false;
					if (majorEmpire.KnowledgeStock.Value >= CultureUnlock.knowledgeForBackupCiv && CultureUnlock.IsFirstEraBackupCivilization(factionName))
					{
						backupUnlocked = true;
					}

					if (!backupUnlocked)
					{
						// the AI doesn't check LockedByEmpireMiscFlags and LockedByOthers without an actual empire index break the UI for the Human
						if (majorEmpire.IsControlledByHuman)
						{
							factionStatus |= FactionStatus.LockedByEmpireMiscFlags;
						}
						else
						{
							factionStatus |= FactionStatus.LockedByOthers;
							factionStatus |= FactionStatus.LockedByEmpireMiscFlags;
						}
					}
				}
			}
			/* Gedemon >>>>> */

			if (!StaticString.IsNullOrEmpty(nextFactionName))
			{
				if (CultureUnlock.UseTrueCultureLocation() && CultureUnlock.HasNoCapitalTerritory(nextFactionName.ToString()))
				{
					//Diagnostics.Log($"[Gedemon] in LockedByYou check, nextFactionName = {nextFactionName}, factionDefinition.Name = {factionDefinition.Name}, factionStatus = {factionStatus}");
				}
				else
				{
					factionStatus = ((!(factionDefinition.Name == nextFactionName)) ? (factionStatus | FactionStatus.LockedByEra) : (factionStatus | FactionStatus.LockedByYou));
					//Diagnostics.Log($"[Gedemon] in LockedByYou check, nextFactionName = {nextFactionName}, factionDefinition.Name = {factionDefinition.Name}, factionStatus = {factionStatus}");
				}
			}
			else
			{
				if ((UnityEngine.Object)(object)factionDefinition != (UnityEngine.Object)(object)majorEmpire.FactionDefinition && Amplitude.Mercury.Sandbox.Sandbox.CivilizationsManager.IsLockedBy(factionDefinition.Name) != -1)
				{
					factionStatus |= FactionStatus.LockedByOthers;
				}
				EraDefinition currentEraDefinition = __instance.GetCurrentEraDefinition();
				if (currentEraDefinition != null)
				{
					if (majorEmpire.EraStarsCount.Value < __instance.CurrentEraStarRequirement)
					{
						factionStatus |= FactionStatus.LockedByEraStars;
					}
					int num = ((currentEraDefinition.EvolutionBaseRequirementMiscEmpireFlags != null) ? currentEraDefinition.EvolutionBaseRequirementMiscEmpireFlags.Length : 0);
					if (num > 0)
					{
						bool flag = false;
						for (int i = 0; i < num; i++)
						{
							EmpireMiscFlags empireMiscFlags = currentEraDefinition.EvolutionBaseRequirementMiscEmpireFlags[i];
							if ((empireMiscFlags & majorEmpire.MiscFlags) == empireMiscFlags)
							{
								flag = true;
								break;
							}
						}
						if (!flag)
						{
							factionStatus |= FactionStatus.LockedByEmpireMiscFlags;
						}
					}
				}
				if ((UnityEngine.Object)(object)factionDefinition != (UnityEngine.Object)(object)majorEmpire.FactionDefinition)
				{
					if (Amplitude.Mercury.Sandbox.Sandbox.ScenarioController.IsEraTransitionEnabled())
					{
						EraDefinition eraDefinition = Amplitude.Mercury.Sandbox.Sandbox.Timeline.GetEraDefinition(__instance.CurrentEraIndex + 1);
						if (eraDefinition == null || factionDefinition.EraIndex != eraDefinition.EraIndex)
						{
							factionStatus |= FactionStatus.LockedByEra;
						}
					}
					else
					{
						factionStatus |= FactionStatus.LockedByEra;
					}
				}
			}
			/* Gedemon <<<<< */
			//Diagnostics.Log($"[Gedemon] in ComputeFactionStatus, {majorEmpire.PersonaName} faction status for {factionDefinition.Name} is {factionStatus}");
			/* Gedemon >>>>> */
			__result = factionStatus;
			return false;
		}

		private static FixedPoint GetResourcesCompensation(Settlement settlement, MajorEmpire majorEmpire)
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

}
