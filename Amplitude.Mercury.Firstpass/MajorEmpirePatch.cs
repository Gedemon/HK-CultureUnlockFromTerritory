using Amplitude.Mercury.Simulation;
using HarmonyLib;
using Amplitude.Mercury.Sandbox;
using Amplitude;
using Amplitude.Mercury;
using Amplitude.Mercury.Data.Simulation;
using Amplitude.Framework.Session;
using Amplitude.Framework;
using Amplitude.Mercury.Session;
using System;
using Amplitude.Mercury.Interop;
using System.Collections.Generic;

namespace Gedemon.TrueCultureLocation
{

	[HarmonyPatch(typeof(MajorEmpire))]
	public class TCL_MajorEmpire
	{

		//*
		[HarmonyPostfix]
		[HarmonyPatch(nameof(InitializeOnStart))]
		public static void InitializeOnStart(MajorEmpire __instance)
		{
			bool isHuman = TrueCultureLocation.IsEmpireHumanSlot(__instance.Index);

			Diagnostics.LogWarning($"[Gedemon] in MajorEmpire, InitializeOnStart for {__instance.majorEmpireDescriptorName}, index = {__instance.Index}, IsControlledByHuman = {isHuman}"); // IsEmpireHumanSlot(int empireIndex)
			WorldPosition worldPosition = new WorldPosition(World.Tables.SpawnLocations[__instance.Index]);

			bool isImmediate = true;
			FixedPoint bonusProduction = FixedPoint.Zero;
			SimulationEntityGUID GUID = SimulationEntityGUID.Zero;

			if (TrueCultureLocation.HasStartingOutpost(__instance.Index, isHuman)) // 
			{
				//__instance.DepartmentOfTheInterior.CreateCampAt(__instance.Armies[0].GUID, worldPosition, FixedPoint.Zero, isImmediate);
				//*
				Settlement settlement = __instance.DepartmentOfTheInterior.CreateSettlement(GUID, worldPosition);
				settlement.AddDescriptor(DepartmentOfTheInterior.campDescriptorName);

				__instance.DepartmentOfIndustry.AttachQueueTo(settlement);
				settlement.SetAvailableConstructionsDirty(AvailableConstructionsDirtyStatuses.Full);
				DepartmentOfTheInterior.AttachSettlementToEmpire(settlement, __instance);


				//Diagnostics.LogWarning($"[Gedemon] before AttachSettlementToEmpire");
				//__instance.DepartmentOfTheInterior.CreateRegion(settlement);
				//
				//Diagnostics.LogWarning($"[Gedemon] beforeSimulationController.AllocateSimulationEntity<Region>");
				Region region = Amplitude.Framework.Simulation.SimulationController.AllocateSimulationEntity<Region>();
				region.SurroundingTerritories.Count = 0;
				region.Settlement.SetEntity(settlement);
				//Diagnostics.LogWarning($"[Gedemon] before Regions.Add(region)");
				Amplitude.Mercury.Sandbox.Sandbox.World.Regions.Add(region);
				settlement.Region.SetEntity(region);
				//
				int num = settlement.WorldPosition.ToTileIndex();
				int territoryIndex = Amplitude.Mercury.Sandbox.Sandbox.World.TileInfo.Data[num].TerritoryIndex;
				////

				//Diagnostics.LogWarning($"[Gedemon] before AttachTerritoryToSettlementRegion");
				//__instance.DepartmentOfTheInterior.AttachTerritoryToSettlementRegion(settlement, territoryIndex);
				//
				settlement.TerritoryCount.Value++;
				Amplitude.Mercury.Simulation.Territory territory = Amplitude.Mercury.Sandbox.Sandbox.World.Territories[territoryIndex];
				Region entity = settlement.Region.Entity;
				entity.TerritoryIndices.Add(territoryIndex);
				entity.Territories.Add(territory);
				int[] adjacentTerritories = territory.AdjacentTerritories;
				int num3 = adjacentTerritories.Length;
				for (int i = 0; i < num3; i++)
				{
					int num2 = adjacentTerritories[i];
					ref Amplitude.Mercury.Interop.TerritoryInfo reference2 = ref Amplitude.Mercury.Sandbox.Sandbox.World.TerritoryInfo.Data[num2];
					SimulationEntityGUID simulationEntityGUID = reference2.RegionGUID;
					if (reference2.Claimed && simulationEntityGUID == entity.GUID)
					{
						entity.SurroundingTerritories.Remove((short)territoryIndex);
					}
					else
					{
						entity.SurroundingTerritories.Add((short)num2);
					}
				}
				////
				//Diagnostics.LogWarning($"[Gedemon] before ClaimTerritory");
				//Amplitude.Mercury.Sandbox.Sandbox.World.ClaimTerritory(territoryIndex, settlement);
				{
					byte b = (byte)settlement.Empire.Entity.Index;
					Region entity2 = settlement.Region.Entity;
					//Diagnostics.LogWarning($"[Gedemon] before TerritoryInfo");
					ref Amplitude.Mercury.Interop.TerritoryInfo reference3 = ref Amplitude.Mercury.Sandbox.Sandbox.World.TerritoryInfo.Data[territoryIndex];
					int num4 = (reference3.Claimed ? reference3.EmpireIndex : (-1));
					reference3.SettlementGUID = settlement.GUID;
					reference3.SettlementIndex = settlement.PoolAllocationIndex;
					//Diagnostics.LogWarning($"[Gedemon] before PrimaryColor");
					reference3.EmpireColor = Amplitude.Mercury.Sandbox.Sandbox.EmpireNamesRepository.GetEmpirePrimaryColor(b);
					reference3.EmpireIndex = b;
					reference3.RegionGUID = entity2.GUID;
					reference3.IsOwnedByCity = settlement.SettlementStatus == SettlementStatuses.City;
					reference3.Claimed = true;
					//Diagnostics.LogWarning($"[Gedemon] before Frame");
					Sandbox.World.TerritoryInfo.Frame = Amplitude.Mercury.Sandbox.Sandbox.Frame;
					//Diagnostics.LogWarning($"[Gedemon] before World.Territories");
					Amplitude.Mercury.Simulation.Territory territory2 = Sandbox.World.Territories[territoryIndex];
					//Diagnostics.LogWarning($"[Gedemon] before SetEntity");
					territory2.Region.SetEntity(entity2);
					if (b != num4)
					{
						//Diagnostics.LogWarning($"[Gedemon] before CultureManager.OnTerritoryOwnerChanged");
						Amplitude.Mercury.Sandbox.Sandbox.CultureManager.OnTerritoryOwnerChanged(territoryIndex, num4, b);
						//Diagnostics.LogWarning($"[Gedemon] before ReligionManager.OnTerritoryOwnerChanged");
						Amplitude.Mercury.Sandbox.Sandbox.ReligionManager.OnTerritoryOwnerChanged(territoryIndex, num4, b);
						//Diagnostics.LogWarning($"[Gedemon] before SimulationEvent_TerritoryOwnerChanged Sandbox.World = {Sandbox.World},  territoryIndex = {territoryIndex} num4 = {num4} b = {b}");
						//SimulationEvent_TerritoryOwnerChanged.Raise(Sandbox.World, territoryIndex, num4, b);
						//ControlAreaManager.cs
						{
							int num5 = b;
							if (num4 >= Amplitude.Mercury.Sandbox.Sandbox.NumberOfMajorEmpires)
							{
								num4 = -1;
							}
							if (b >= Amplitude.Mercury.Sandbox.Sandbox.NumberOfMajorEmpires)
							{
								num5 = -1;
							}
							if (num4 >= 0 || num5 >= 0)
							{
								//Diagnostics.LogWarning($"[Gedemon] before Sandbox.ControlAreaManager.areaIndicesPerTerritory {Sandbox.ControlAreaManager}, {Sandbox.ControlAreaManager.areaIndicesPerTerritory}");
								//
								if (Sandbox.ControlAreaManager.areaIndicesPerTerritory != null)
								{

									//Diagnostics.LogWarning($"[Gedemon] not null !");
									ControlAreaManager.AreaIndexWithProvider[] array = Sandbox.ControlAreaManager.areaIndicesPerTerritory[territoryIndex];
									int num6 = array.Length;
									for (int i = 0; i < num6; i++)
									{
										ControlAreaManager.AreaIndexWithProvider areaIndexWithProvider = array[i];
										int areaIndex = areaIndexWithProvider.AreaIndex;
										//Diagnostics.LogWarning($"[Gedemon] before ControlAreaInfo.GetReferenceAt");
										ref ControlAreaInfo referenceAt = ref Sandbox.ControlAreaManager.ControlAreaInfo.GetReferenceAt(areaIndex);
										if (num4 >= 0)
										{
											if (referenceAt.ClaimedTerritoryCountPerEmpire[num4] == referenceAt.TerritoryCount)
											{
												referenceAt.OwnerIndex = -1;
												areaIndexWithProvider.Provider.AreaLostBy(areaIndex, referenceAt.AssociatedInfoIndex, num4);
											}
											referenceAt.ClaimedTerritoryCountPerEmpire[num4]--;
										}
										if (num5 >= 0)
										{
											referenceAt.ClaimedTerritoryCountPerEmpire[num5]++;
											if (referenceAt.ClaimedTerritoryCountPerEmpire[num5] == referenceAt.TerritoryCount)
											{
												referenceAt.OwnerIndex = num5;
												areaIndexWithProvider.Provider.AreaOwnedBy(areaIndex, referenceAt.AssociatedInfoIndex, num5);
											}
										}
										//Diagnostics.LogWarning($"[Gedemon] before ControlAreaInfo.SetSynchronizationDirty");
										Sandbox.ControlAreaManager.ControlAreaInfo.SetSynchronizationDirty();
									}
								}
								///
							}

						}
						////
						//LandmarkManager.cs
						{
							//int num = 0;
							//if (e.NewOwnerEmpireIndex >= 0 && e.NewOwnerEmpireIndex < Amplitude.Mercury.Sandbox.Sandbox.NumberOfMajorEmpires)
							//{
							//	num = Amplitude.Mercury.Sandbox.Sandbox.MajorEmpires[e.NewOwnerEmpireIndex].DepartmentOfDevelopment.CurrentEraIndex;
							//}
							//Territory territory = Amplitude.Mercury.Sandbox.Sandbox.World.Territories[e.TerritoryIndex];
							//int count = territory.LandmarkParts.Count;
							//for (int i = 0; i < count; i++)
							//{
							//	territory.LandmarkParts[i].OwnerEraIndex.Value = num;
							//}
						}
						//
						if (Sandbox.World.IsContinentOwnedByEmpire(territory2.ContinentIndex, b))
						{
							//Diagnostics.LogWarning($"[Gedemon] before SimulationEvent_ContinentConquered");
							SimulationEvent_ContinentConquered.Raise(Sandbox.World, territory2.ContinentIndex, b);
						}
					}
				}
				//Diagnostics.LogWarning($"[Gedemon] after ClaimTerritory");


				//int num = worldPosition.ToTileIndex();
				//int territoryIndex = Amplitude.Mercury.Sandbox.Sandbox.World.TileInfo.Data[num].TerritoryIndex;
				ref Amplitude.Mercury.Interop.TerritoryInfo reference = ref Amplitude.Mercury.Sandbox.Sandbox.World.TerritoryInfo.Data[territoryIndex];
				settlement.EntityName.LocalizationKey = reference.LocalizedName;
				//Diagnostics.LogWarning($"[Gedemon] before GatherAdjacentSettlements");
				DepartmentOfTheInterior.GatherAdjacentSettlements(settlement);
				//Diagnostics.LogWarning($"[Gedemon] before GatherTerritoryDistricts");
				__instance.DepartmentOfTheInterior.GatherTerritoryDistricts(settlement);
				if (isImmediate)
				{
					__instance.DepartmentOfTheInterior.ApplyEvolutionToSettlement_Camp(settlement);
				}
				else
				{
					District district = settlement.GetDistrictAt(worldPosition);
					if (district != null)
					{
						__instance.DepartmentOfTheInterior.UpgradeDistrict(district, DepartmentOfTheInterior.BeforeCampDefinition);
					}
					else
					{
						district = __instance.DepartmentOfTheInterior.CreateExtensionDistrictInSettlement(settlement, worldPosition, DepartmentOfTheInterior.BeforeCampDefinition);
					}
					Amplitude.Mercury.Data.Simulation.ExploitationRuleDefinition exploitationRuleDefinition = DepartmentOfTheInterior.BeforeCampDefinition.ExploitationRuleDefinition;
					__instance.DepartmentOfTheInterior.GenerateExploitationsAround(district, exploitationRuleDefinition);
					DepartmentOfTheInterior.AddExploitationRuleDescriptorsFor(district, exploitationRuleDefinition);
					__instance.DepartmentOfIndustry.AddConstructionToSettlement(settlement, DepartmentOfTheInterior.evolutionCampDefinition, EnqueuePosition.AtBegin, worldPosition);
					settlement.ConstructionQueue.Entity.CurrentResourceStock += bonusProduction;
					__instance.DepartmentOfIndustry.InvestProductionFor(settlement.ConstructionQueue);
					isImmediate = settlement.SettlementStatus == SettlementStatuses.Camp;
					__instance.DepartmentOfTheInterior.UpdateAdministrativeCenter(district);
					DepartmentOfTheInterior.UpdateDistrictsAfterSettlementCreation(settlement);
				}
				//Diagnostics.LogWarning($"[Gedemon] before InitializeArmiesCollection");
				DepartmentOfTheInterior.InitializeFriendlyHostileArmiesSettlement(settlement);
				if (isImmediate)
				{
					SimulationEvent_CampFounded.Raise(__instance.DepartmentOfTheInterior, settlement);
					SimulationEvent_SettlementNameChanged.Raise(__instance.DepartmentOfTheInterior, settlement);
				}
				if (__instance != null)
				{
					__instance.DepartmentOfTheInterior.UpdateSettlementDistanceToCapital(settlement);
					if ((__instance.MiscFlags & EmpireMiscFlags.HasAlreadyFoundACamp) == 0)
					{
						__instance.SetMiscFlags(EmpireMiscFlags.HasAlreadyFoundACamp, set: true);
					}
				}
				//Diagnostics.LogWarning($"[Gedemon] before SimulationEvent_SettlementOwnerChanged");
				SimulationEvent_SettlementOwnerChanged.Raise(__instance.DepartmentOfTheInterior, settlement, -1);
				//Diagnostics.LogWarning($"[Gedemon] after SimulationEvent_SettlementOwnerChanged");
				if (__instance != null)
				{
					__instance.DepartmentOfCommunication.Notify(new Amplitude.Mercury.Interop.NewSettlementCreatedNotificationData
					{
						InitiatorGUID = GUID,
						SettlementGUID = settlement.GUID
					});
				}
				//*/

				//Amplitude.Mercury.Sandbox.Sandbox.Empires[__instance.Index].DepartmentOfTheInterior.CreateCampAt(SimulationEntityGUID.Zero, worldPosition, FixedPoint.Zero, true);
				//__instance.DepartmentOfTheInterior.CreateCampAt(SimulationEntityGUID.Zero, worldPosition, FixedPoint.Zero, false);
			}

		}
		//*/
	}

	//*
	[HarmonyPatch(typeof(HumanMinorFactionSpawner))]
	public class TCL_HumanMinorFactionSpawner
	{
		[HarmonyPatch("SpawnMinorFactionAt")]
		[HarmonyPrefix]
		public static bool SpawnMinorFactionAt(HumanMinorFactionSpawner __instance, int tileIndex)
		{
			if (CultureUnlock.UseTrueCultureLocation())
            {
				int territoryIndex = Amplitude.Mercury.Sandbox.Sandbox.World.TileInfo.Data[tileIndex].TerritoryIndex;
				Territory territory = Amplitude.Mercury.Sandbox.Sandbox.World.Territories[territoryIndex];
				int numAvailableFaction = __instance.availableMinorFactionDefinitions.Count;
				int indexOverride = 0;
				bool foundFaction = false;

				Diagnostics.LogWarning($"[Gedemon] in HumanMinorFactionSpawner, SpawnMinorFactionAt for tileIndex = {tileIndex}, territoryIndex = {territoryIndex}, Territory Name = {CultureUnlock.GetTerritoryName(territoryIndex)}, numAvailableFaction = {numAvailableFaction}");

				for (int i = 0; i < numAvailableFaction; i++)
				{
					FactionDefinition potentialFaction = __instance.availableMinorFactionDefinitions[i];
					Diagnostics.Log($"[Gedemon] testing potential faction : {potentialFaction.name}, IsMinorFactionPosition = {CultureUnlock.IsMinorFactionPosition(territoryIndex, potentialFaction.name)}");
					if (CultureUnlock.IsMinorFactionPosition(territoryIndex, potentialFaction.name))
					{
						indexOverride = i;
						foundFaction = true;
						Diagnostics.Log($"[Gedemon] return faction index (matching territory)");
						break; // found fitting Faction
					}
					else
					{
						if (TrueCultureLocation.UseLargerSpawnAreaForMinorFaction())
                        {

							Amplitude.Mercury.Interop.AI.Entities.Territory.Adjacency[] adjacencies = Amplitude.Mercury.Interop.AI.Snapshots.World.Territories[territory.Index].Adjacencies;
							for (int j = 0; j < territory.AdjacentTerritories.Length; j++)
							{
								int adjacentTerritoryIndex = territory.AdjacentTerritories[j];
								if (CultureUnlock.IsMinorFactionPosition(adjacentTerritoryIndex, potentialFaction.name) && ((adjacencies[j].Transition & Amplitude.Mercury.Data.AI.TerritoryTransition.Open) != 0))
								{
									indexOverride = i; // potential faction, but continue to loop in case there is a better choice
									foundFaction = true;
									Diagnostics.Log($"[Gedemon] mark faction index (adjacent territory, may find a better match)");
								}
							}
						}
					}
				}
				if (!foundFaction)
				{
					Diagnostics.Log($"[Gedemon] no faction found, aborting...");
					//__instance.RemoveMinorEmpire(minorEmpire);
					return false;
				}

				// original method
				{
					__instance.TryAllocateMinorFaction(out var minorEmpire);
					BaseHumanSpawnerDefinition spawnerDefinitionForMinorEmpire = __instance.GetSpawnerDefinitionForMinorEmpire(minorEmpire);
					int index = RandomHelper.Next(SandboxManager.Sandbox.Turn + Amplitude.Mercury.Sandbox.Sandbox.WorldSeed + (int)(ulong)minorEmpire.GUID, 0, __instance.availableMinorFactionDefinitions.Count);
					// Gedemon <<<<<
					if (foundFaction)
                    {
						index = indexOverride;
                    }
					// Gedemon >>>>>
					FactionDefinition factionDefinition = __instance.availableMinorFactionDefinitions[index];
					__instance.availableMinorFactionDefinitions.RemoveAt(index);
					minorEmpire.SetFaction(factionDefinition);
					StaticString buildingVisualAffinityFor = factionDefinition.GetBuildingVisualAffinityFor(__instance.eraIndex);
					MinorSpawnPoint minorSpawnPoint = Amplitude.Mercury.Sandbox.Sandbox.World.CreateSpawnPointAt(tileIndex, (byte)minorEmpire.Index, spawnerDefinitionForMinorEmpire.VisualAffinity.ElementName, buildingVisualAffinityFor, factionDefinition.Name);
					minorEmpire.SpawnPointIndex = minorSpawnPoint.PoolAllocationIndex;
					minorEmpire.DepartmentOfTheInterior.ResetSettlementNames();
					//int territoryIndex = Amplitude.Mercury.Sandbox.Sandbox.World.TileInfo.Data[tileIndex].TerritoryIndex;
					__instance.SetMinorEmpireTerritoryIndex(minorEmpire, territoryIndex);
					__instance.SetMinorEmpireHomeStatus(minorEmpire, MinorEmpireHomeStatuses.POI);
					__instance.UpdateMinorFactionAvailableUnits(minorEmpire);
					__instance.UpdateMinorFactionAvailableArmyPatterns(minorEmpire);
					if (spawnerDefinitionForMinorEmpire.CanAskForCamp)
					{
						// Gedemon <<<<<
						if (TrueCultureLocation.UseStartingOutpostForMinorFaction() && __instance.IsTerritoryValidForSettle(territory))
						{
							WorldPosition startPosition = minorSpawnPoint.WorldPosition;
							minorEmpire.DepartmentOfTheInterior.CreateCampAt(SimulationEntityGUID.Zero, startPosition, FixedPoint.Zero, false);
						}
						else
						// Gedemon >>>>>
							__instance.SetMinorEmpireAskingForCamp(minorEmpire, askForCamp: true);
					}
					__instance.SpawnArmyAtHomeFor(minorEmpire, isDefender: false);
					Amplitude.Mercury.Sandbox.Sandbox.MinorFactionManager.InitializeMinorEmpireRelations(minorEmpire);
					Amplitude.Mercury.Sandbox.Sandbox.MinorFactionManager.PickRandomIdeologies(minorEmpire);
					Amplitude.Mercury.Sandbox.Sandbox.MinorFactionManager.PickRandomPatronOrder(minorEmpire);
					Amplitude.Mercury.Sandbox.Sandbox.MinorFactionManager.UpdateDistanceToMinorFactionMap();

					Diagnostics.LogError($"[Gedemon] Spawned {factionDefinition.name} in {CultureUnlock.GetTerritoryName(territoryIndex)}, CanAskForCamp = {spawnerDefinitionForMinorEmpire.CanAskForCamp}, UseStartingOutpost = {TrueCultureLocation.UseStartingOutpostForMinorFaction()}, HasAlreadyFoundASettlement = {minorEmpire.HasAlreadyFoundASettlement}, Armies.Count = {minorEmpire.Armies.Count}, RemainingLifeTime = {minorEmpire.RemainingLifeTime}");
				}

				return false;
            }
			return true;
		}
	}
	//*/

	//*
	[HarmonyPatch(typeof(MinorFactionManager))]
	public class TCL_MinorFactionManager
	{

		//[HarmonyPatch("FillValidTileIndexesInTerritory")]
		//[HarmonyPrefix]
		public static bool FillValidTileIndexesInTerritory(MinorFactionManager __instance, List<int> validTileIndexes, int territoryIndex)
		{
			if (CultureUnlock.UseTrueCultureLocation())
			{
				Territory territory = Amplitude.Mercury.Sandbox.Sandbox.World.Territories[territoryIndex];
				int num = territory.TileIndexes.Length;
				for (int i = 0; i < num; i++)
				{
					int num2 = territory.TileIndexes[i];

					//ref TileInfo reference = ref Amplitude.Mercury.Sandbox.Sandbox.World.TileInfo.Data[num2];
					ref Amplitude.Mercury.Interop.AI.Data.Tile tile = ref Amplitude.Mercury.Interop.AI.Snapshots.World.Tiles[num2];

					if (__instance.IsWorldPositionValidForSpawnFaction(num2) && tile.BorderDistance > 2) // cause out of range index in GetRandomValidTileIndexInTerritory ?
					{
						validTileIndexes.Add(num2);
					}
				}
				return false;
			}
			return true;
		}

		[HarmonyPatch("IsTerritoryValidForSpawnFaction")]
		[HarmonyPrefix]
		public static bool IsTerritoryValidForSpawnFaction(MinorFactionManager __instance, ref bool __result, Territory territory)
		{
			if (CultureUnlock.UseTrueCultureLocation())
			{

				//Diagnostics.Log($"[Gedemon] in MinorFactionManager, IsTerritoryValidForSpawnFaction for {CultureUnlock.GetTerritoryName(territory.Index)}, MinorFactionManager Era = ({__instance.CurrentMinorFactionEraDefinition.EraIndex}) ");

				if (__instance.CurrentMinorFactionEraDefinition.EraIndex == 0)
                {
					return true; // no limits for neolithic spawn (animals)
                }

				int numAvailablePeacefulFaction = __instance.PeacefulHumanSpawner.availableMinorFactionDefinitions.Count;
				int numAvailableViolentFaction = __instance.ViolentHumanSpawner.availableMinorFactionDefinitions.Count;
				int numAvailableFactions = numAvailablePeacefulFaction + numAvailableViolentFaction;
				string[] availableFactions = new string[numAvailableFactions];


				//Diagnostics.LogWarning($"[Gedemon] in MinorFactionManager, IsTerritoryValidForSpawnFaction for Territory Name = {CultureUnlock.GetTerritoryName(territory.Index)}, HasAnyMinorFactionPosition = {CultureUnlock.HasAnyMinorFactionPosition(territory.Index)}, numAvailableViolentFaction = {numAvailableViolentFaction}, numAvailablePeacefulFaction = {numAvailablePeacefulFaction} ");

				for (int i = 0; i < numAvailablePeacefulFaction; i++)
				{
					//Diagnostics.Log($"[Gedemon] adding {__instance.PeacefulHumanSpawner.availableMinorFactionDefinitions[i].name} from Peaceful list at index {i}");
					availableFactions[i] = __instance.PeacefulHumanSpawner.availableMinorFactionDefinitions[i].name;
				}

				for (int i = numAvailablePeacefulFaction; i < numAvailableFactions; i++)
				{
					//Diagnostics.Log($"[Gedemon] adding {__instance.ViolentHumanSpawner.availableMinorFactionDefinitions[i- numAvailablePeacefulFaction].name} from Violent list at index {i}");
					availableFactions[i] = __instance.ViolentHumanSpawner.availableMinorFactionDefinitions[i- numAvailablePeacefulFaction].name;
				}


				for (int i = 0; i < numAvailableFactions; i++)
				{
					string factionName = availableFactions[i];
					//Diagnostics.Log($"[Gedemon] IsMinorFactionPosition for {factionName} in territory = {CultureUnlock.IsMinorFactionPosition(territory.Index, factionName)}");
					if (CultureUnlock.IsMinorFactionPosition(territory.Index, factionName))
					{
						Diagnostics.LogWarning($"[Gedemon] in MinorFactionManager, IsTerritoryValidForSpawnFaction for {CultureUnlock.GetTerritoryName(territory.Index)}, Found ({factionName}) !");
						return true;
					}
					else
					{
						if (TrueCultureLocation.UseLargerSpawnAreaForMinorFaction())
                        {
							Amplitude.Mercury.Interop.AI.Entities.Territory.Adjacency[] adjacencies = Amplitude.Mercury.Interop.AI.Snapshots.World.Territories[territory.Index].Adjacencies;
							for (int j = 0; j < territory.AdjacentTerritories.Length; j++) //foreach (int adjacentTerritoryIndex in territory.AdjacentTerritories)
							{
								int adjacentTerritoryIndex = territory.AdjacentTerritories[j];
								//Diagnostics.Log($"[Gedemon] IsMinorFactionPosition in adjacent territory ({CultureUnlock.GetTerritoryName(adjacentTerritoryIndex)}) = {CultureUnlock.IsMinorFactionPosition(adjacentTerritoryIndex, factionName)}");
								if (CultureUnlock.IsMinorFactionPosition(adjacentTerritoryIndex, factionName))
								{
									Diagnostics.LogWarning($"[Gedemon] in MinorFactionManager, IsTerritoryValidForSpawnFaction for {CultureUnlock.GetTerritoryName(territory.Index)}, Found ({factionName}) on {CultureUnlock.GetTerritoryName(adjacentTerritoryIndex)}, check open adjacency = {(adjacencies[j].Transition & Amplitude.Mercury.Data.AI.TerritoryTransition.Open)}");
									if ((adjacencies[j].Transition & Amplitude.Mercury.Data.AI.TerritoryTransition.Open) != 0)
									{
										return true;
									}
								}
							}
						}
					}
				}

				__result = false;
				return false;
            }
			return true;
		}

		[HarmonyPatch("NewTurnBeginPass_UpdateMinorFactionsSpawning")]
		[HarmonyPrefix]
		public static void NewTurnBeginPass_UpdateMinorFactionsSpawning(MinorFactionManager __instance, SimulationPasses.PassContext context, string name)
		{
			Diagnostics.LogWarning($"[Gedemon] in NewTurnBeginPass_UpdateMinorFactionsSpawning, (PostFix) for {context}, GlobalEra index = {Sandbox.Timeline.GetGlobalEraIndex()}, Game Turn = {SandboxManager.Sandbox.Turn}, EmpireCanSpawnFromMinorFactions = {TrueCultureLocation.EmpireCanSpawnFromMinorFactions()}");

			if (CultureUnlock.UseTrueCultureLocation() && TrueCultureLocation.EmpireCanSpawnFromMinorFactions())
			{
				int numberOfMinorEmpires = Amplitude.Mercury.Sandbox.Sandbox.NumberOfMinorEmpires;
				for (int i = 0; i < numberOfMinorEmpires; i++)
				{
					MinorEmpire minorEmpire = Amplitude.Mercury.Sandbox.Sandbox.MinorEmpires[i];
					int numCities = minorEmpire.Cities.Count;

					if (!minorEmpire.IsAlive || numCities == 0)
					{
						continue;
					}
					//*
					int count = minorEmpire.RelationsToMajor.Count;
					bool halfLifePassed = (SandboxManager.Sandbox.Turn - minorEmpire.SpawnTurn) > minorEmpire.RemainingLifeTime;
					PatronageDefinition patronageDefinition = minorEmpire.PatronageDefinition;
					Diagnostics.LogWarning($"[Gedemon] Check Minor Faction {minorEmpire.FactionDefinition.name} ID#{minorEmpire.Index}, Era={minorEmpire.EraIndex}, Status={minorEmpire.MinorFactionStatus}, HomeStatus={minorEmpire.MinorEmpireHomeStatus}, RemainingLife={minorEmpire.RemainingLifeTime}, Spawn={minorEmpire.SpawnTurn}, Life={SandboxManager.Sandbox.Turn - minorEmpire.SpawnTurn}, halfLifePassed={halfLifePassed}, FirstPatron ID#{minorEmpire.RankedMajorEmpireIndexes[Amplitude.Mercury.Sandbox.Sandbox.NumberOfMajorEmpires - 1]}  ");

					//CityFlags.Besieged
					bool canEvolve = true;
					for(int c = 0; c < numCities; c++)
                    {
						if ((minorEmpire.Cities[c].CityFlags & CityFlags.Besieged) != 0)
                        {
							Diagnostics.LogWarning($"[Gedemon] - {minorEmpire.Cities[c].EntityName} is under siege, can't Evolve now...");
							canEvolve = false;
							break;
						}
                    }
					
					if (!canEvolve)
                    {
						continue;
                    }


					FactionDefinition newfaction = null;
					int numSettlment = minorEmpire.Settlements.Count;
					for (int s = 0; s < numSettlment; s++)
					{
						Settlement settlement = minorEmpire.Settlements[s];
                        if(settlement.SettlementStatus != SettlementStatuses.City)
						{
							continue;
                        }
						int count2 = settlement.Region.Entity.Territories.Count;
						for (int k = 0; k < count2; k++)
						{
							Territory territory = settlement.Region.Entity.Territories[k];
							int territoryIndex = territory.Index;
							if (CultureUnlock.HasAnyMajorEmpirePosition(territoryIndex))
							{
								Diagnostics.LogWarning($"[Gedemon] Minor Faction own territory {CultureUnlock.GetTerritoryName(territoryIndex)}, a potential Major Empire location");
								List<string> majorEmpireNames = CultureUnlock.GetListMajorEmpiresForTerritory(territoryIndex);
								foreach (string empireName in majorEmpireNames)
								{
									StaticString factionName = new StaticString(empireName);
									FactionDefinition factionDefinition = Utils.GameUtils.GetFactionDefinition(factionName);
									if (factionDefinition != null)
									{
										Diagnostics.LogWarning($"[Gedemon] - {empireName}, Era Index = {factionDefinition.EraIndex}");
										if (halfLifePassed && factionDefinition.EraIndex <= Sandbox.Timeline.GetGlobalEraIndex() - 1 && factionDefinition.EraIndex >= minorEmpire.EraIndex)
										{
											newfaction = factionDefinition;
											goto FoundNewFaction;
										}
									}
									else
									{
										Diagnostics.LogError($"[Gedemon] - Can't find FactionDefinition for {empireName}");
									}
								}
							}
						}
					}
				FoundNewFaction:;
					if (newfaction != null)
					{

						MajorEmpire newEmpire = CultureChange.GetFreeMajorEmpire();
						if (newEmpire != null)
						{
							Diagnostics.LogError($"[Gedemon] Spawning Major Empire {newfaction.name} to replace Minor Faction {minorEmpire.FactionDefinition.name}");

							newEmpire.ChangeFaction(newfaction.Name);
							newEmpire.DepartmentOfDevelopment.ApplyStartingEra();
							newEmpire.DepartmentOfDevelopment.PickFirstEraStars();
							newEmpire.DepartmentOfScience.UpdateCurrentTechnologyEraIfNeeded();
							newEmpire.DepartmentOfScience.CompleteAllPreviousErasTechnologiesOnStart();

							for (int j = 0; j < count; j++)
							{
								MinorToMajorRelation minorToMajorRelation = minorEmpire.RelationsToMajor[j];
								MajorEmpire otherEmpire = minorToMajorRelation.MajorEmpire.Entity;
								DiplomaticRelation majorToMajorRelation = Sandbox.DiplomaticAncillary.GetRelationFor(newEmpire.Index, otherEmpire.Index);
								DiplomaticStateType state;
								if (minorToMajorRelation.PatronageStock.Value > 0)
								{
									MinorPatronageGaugeLevel level = MinorFactionManager.GetPatronageLevelWithMajor(minorEmpire, otherEmpire);
									Diagnostics.LogWarning($"[Gedemon] Patronage Stock = {minorToMajorRelation.PatronageStock.Value} for {otherEmpire.FactionDefinition.name} ID#{otherEmpire.Index}, Permissions = {level.Permission}");
									
									if (minorToMajorRelation.PatronageStock.Value == patronageDefinition.MaximumPatronGauge)
									{
										Diagnostics.LogWarning($"[Gedemon] - Set as Ally");
										state = DiplomaticStateType.Alliance;
										majorToMajorRelation.ApplyState(state, newEmpire.Index);
										majorToMajorRelation.UpdateAbilities(raiseSimulationEvents: true);
									}
									else
									{
										state = DiplomaticStateType.Peace;
									}

									SimulationEvent_DiplomaticStateChanged.Raise(newEmpire, newEmpire.Index, majorToMajorRelation.DiplomaticState.State, state, otherEmpire.Index, -1);

									if ((level.Permission & MinorPatronageGaugeLevel.MinorGaugePermission.NonAgression) != 0)
									{
										//DiplomaticRelationHelper.ExecuteAction(newEmpire.Index, otherEmpire.Index, DiplomaticAction.ProposeMilitaryAgreement);
										Diagnostics.LogWarning($"[Gedemon] - Raise MilitaryAgreementLevel from {majorToMajorRelation.CurrentAgreements.MilitaryAgreementLevel}");
										majorToMajorRelation.CurrentAgreements.MilitaryAgreementLevel = MilitaryAgreements.NonAggression;
										Diagnostics.LogWarning($"[Gedemon] - New MilitaryAgreementLevel = {majorToMajorRelation.CurrentAgreements.MilitaryAgreementLevel}");

									}

									if ((level.Permission & MinorPatronageGaugeLevel.MinorGaugePermission.TradeRessources) != 0)
									{
										//DiplomaticRelationHelper.ExecuteAction(newEmpire.Index, otherEmpire.Index, DiplomaticAction.ProposeEconomicalAgreement);
										Diagnostics.LogWarning($"[Gedemon] - Raise EconomicalAgreementLevel from {majorToMajorRelation.CurrentAgreements.EconomicalAgreementLevel}");
										majorToMajorRelation.CurrentAgreements.EconomicalAgreementLevel = EconomicalAgreements.AllResourceTrade;
										Diagnostics.LogWarning($"[Gedemon] - New EconomicalAgreementLevel = {majorToMajorRelation.CurrentAgreements.EconomicalAgreementLevel}");
									}

									Sandbox.SimulationEntityRepository.SetSynchronizationDirty(newEmpire);
									Sandbox.SimulationEntityRepository.SetSynchronizationDirty(otherEmpire);

								}
							}

							newEmpire.DepartmentOfForeignAffairs.AssimilateMinorEmpire(minorEmpire);

							CultureChange.UpdateDistrictVisuals(newEmpire);
							CultureChange.SetFactionSymbol(newEmpire);
						}
					}
				}
			}
		}
	}
	//*/


	[HarmonyPatch(typeof(ArmyActionHelper))]
	public class TCL_ArmyActionHelper
	{
		[HarmonyPatch("FillCreateCampFailures")]
		[HarmonyPatch(new Type[] { typeof(Army), typeof(ArmyActionFailureFlags), typeof(FixedPoint), typeof(FixedPoint), typeof(FixedPoint), typeof(bool) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Normal })]
		[HarmonyPrefix]
		public static bool FillCreateCampFailures(Army army, ref ArmyActionFailureFlags failureFlags, ref FixedPoint minimumMoneyCost, ref FixedPoint minimumInfluenceCost, ref FixedPoint instantUnitCost, bool computeMinimumCost)
		{
			Empire empire = army.Empire.Entity;
			int empireIndex = empire.Index;
			bool isHuman = TrueCultureLocation.IsEmpireHumanSlot(empireIndex);
			if (!TrueCultureLocation.IsSettlingEmpire(empireIndex, isHuman))
			{
				failureFlags |= ArmyActionFailureFlags_Part1.IsLesserEmpire;
				return false;
			}
			return true;
		}

	}

}
