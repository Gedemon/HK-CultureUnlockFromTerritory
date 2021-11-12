using Amplitude.Mercury.Simulation;
using HarmonyLib;
using Amplitude.Mercury.Sandbox;
using Amplitude;
using Amplitude.Mercury;
using Amplitude.Mercury.Data.Simulation;
using System.Collections.Generic;
using Amplitude.Mercury.Interop;

namespace Gedemon.TrueCultureLocation
{
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
					availableFactions[i] = __instance.ViolentHumanSpawner.availableMinorFactionDefinitions[i - numAvailablePeacefulFaction].name;
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
					FixedPoint lifeRatio = (FixedPoint)0.5;
					bool hasEnoughLifeTime = (SandboxManager.Sandbox.Turn - minorEmpire.SpawnTurn) > minorEmpire.RemainingLifeTime * lifeRatio;
					PatronageDefinition patronageDefinition = minorEmpire.PatronageDefinition;
					Diagnostics.LogWarning($"[Gedemon] Check Minor Faction {minorEmpire.FactionDefinition.name} ID#{minorEmpire.Index}, Era={minorEmpire.EraIndex}, Status={minorEmpire.MinorFactionStatus}, HomeStatus={minorEmpire.MinorEmpireHomeStatus}, RemainingLife={minorEmpire.RemainingLifeTime}, Spawn={minorEmpire.SpawnTurn}, Life={SandboxManager.Sandbox.Turn - minorEmpire.SpawnTurn}, hasEnoughLifeTime={hasEnoughLifeTime}, lifeRatio = {lifeRatio}, FirstPatron ID#{minorEmpire.RankedMajorEmpireIndexes[Amplitude.Mercury.Sandbox.Sandbox.NumberOfMajorEmpires - 1]}  ");

					//CityFlags.Besieged
					bool canEvolve = true;
					for (int c = 0; c < numCities; c++)
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
						if (settlement.SettlementStatus != SettlementStatuses.City)
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
										if (hasEnoughLifeTime && factionDefinition.EraIndex <= Sandbox.Timeline.GetGlobalEraIndex() - 1 && factionDefinition.EraIndex >= minorEmpire.EraIndex)
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

	/*
	[HarmonyPatch(typeof(BaseHumanMinorFactionSpawner<>))]
	public class TLC_BaseHumanMinorFactionSpawner
	{
		[HarmonyPatch("SetMinorFactionDead")]
		[HarmonyPrefix]
		public static bool SetMinorFactionDead(BaseHumanMinorFactionSpawner<BaseHumanSpawnerDefinition> __instance, MinorEmpire minorEmpire)
		{
			FactionDefinition factionDefinition = minorEmpire.FactionDefinition;
			if (CultureUnlock.UseTrueCultureLocation())
			{

				Diagnostics.LogWarning($"[Gedemon] in BaseHumanMinorFactionSpawner<>, SetMinorFactionDead for {minorEmpire.FactionDefinition.Name}, index = {minorEmpire.Index}");

				BaseHumanSpawnerDefinition spawnerDefinitionForMinorEmpire = __instance.GetSpawnerDefinitionForMinorEmpire(minorEmpire);
				Amplitude.Mercury.Sandbox.Sandbox.MinorFactionManager.ClearMinorEmpirePatronage(minorEmpire);
				if (minorEmpire.SpawnPointIndex >= 0)
				{
					int tileIndex = Amplitude.Mercury.Sandbox.Sandbox.World.SpawnPointInfo.GetReferenceAt(minorEmpire.SpawnPointIndex).TileIndex;
					int territoryIndex = Amplitude.Mercury.Sandbox.Sandbox.World.TileInfo[tileIndex].TerritoryIndex;
					__instance.UnsetMinorEmpireTerritoryIndex(minorEmpire, territoryIndex);
					Amplitude.Mercury.Sandbox.Sandbox.World.FreeSpawnPoint(minorEmpire.SpawnPointIndex);
					minorEmpire.SpawnPointIndex = -1;
				}
				int count = minorEmpire.Settlements.Count;
				if (count > 0)
				{
					for (int num = count - 1; num >= 0; num--)
					{
						Settlement settlement = minorEmpire.Settlements[num];
						//minorEmpire.DepartmentOfTheInterior.DestroyAllDistrictsFromSettlement(settlement, DistrictDestructionSource.MinorDecay);
						minorEmpire.DepartmentOfTheInterior.FreeSettlement(settlement);
					}
				}
				minorEmpire.MinorFactionStatus = MinorFactionStatuses.Dying;
				Amplitude.Mercury.Sandbox.Sandbox.SimulationEntityRepository.SetSynchronizationDirty(minorEmpire);
				FixedPoint defaultGameSpeedMultiplier = Amplitude.Mercury.Sandbox.Sandbox.GameSpeedController.CurrentGameSpeedDefinition.DefaultGameSpeedMultiplier;
				minorEmpire.RemainingLifeTime = ((spawnerDefinitionForMinorEmpire != null) ? ((int)FixedPoint.Ceiling(spawnerDefinitionForMinorEmpire.TimeBeforeFactionRespawn * defaultGameSpeedMultiplier)) : 0);
				__instance.SetMinorEmpireAskingForCamp(minorEmpire, askForCamp: false);
				__instance.AlivedFactionCount--;
				Amplitude.Mercury.Sandbox.Sandbox.MinorFactionManager.TotalAlivedFactionCount--;
				__instance.OnMinorFactionDead(minorEmpire);
				return false;
            }
			return true;
		}
	}
	//*/

	//*
	[HarmonyPatch(typeof(DepartmentOfTheInterior))]
	public class TLC_DepartmentOfTheInterior
	{
		[HarmonyPatch("DestroyAllDistrictsFromSettlement")]
		[HarmonyPrefix]
		public static bool DestroyAllDistrictsFromSettlement(DepartmentOfTheInterior __instance, Settlement settlement, DistrictDestructionSource damageSource)
		{

			Diagnostics.LogWarning($"[Gedemon] in DepartmentOfTheInterior, DestroyAllDistrictsFromSettlement for {settlement.EntityName}, empire index = {__instance.Empire.Index}");

			if (CultureUnlock.UseTrueCultureLocation() && damageSource == DistrictDestructionSource.MinorDecay)
			{
				return false;
            }
			return true;
		}
	}
	//*/
}
