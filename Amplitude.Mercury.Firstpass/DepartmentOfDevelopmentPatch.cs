
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
using Amplitude.Mercury;
using System.Linq;

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

					bool canUseMajorEmpire = false;

					Settlement capital = majorEmpire.Capital;

					IDictionary<StaticString, Empire> listEmpires = new Dictionary<StaticString, Empire>();

					MinorEmpire rebelFaction = null;
					MajorEmpire oldEmpire = null;

					SettlementRefund refund = new SettlementRefund(majorEmpire);

					TerritoryChange territoryChange = new TerritoryChange(majorEmpire, nextFactionName);

                    #region 1/ stop battles for lost cities

					if(territoryChange.NumCitiesLost > 0)
                    {

						Diagnostics.Log($"[Gedemon] Check to stop battles for lost cities");
						foreach (Settlement settlement in territoryChange.settlementLost)
						{
							if ((settlement.CityFlags & CityFlags.Besieged) != 0)
							{
								Diagnostics.LogWarning($"[Gedemon] - {settlement.EntityName} is under siege, cancelling battle...");
								Sandbox.BattleRepository.TryGetBattleAt(settlement.WorldPosition, out Battle battle);
								battle.StateMachine.ChangeState(BattleState.Cancelled);
							}
						}
					}

					#endregion

					#region 2/ detache and create for all territories that are not cities

					Diagnostics.Log($"[Gedemon] Detaching from cities and creating new settlement");
					foreach (KeyValuePair<Settlement, List<int>> kvp in territoryChange.citiesInitialTerritories)
					{
						foreach (int territoryIndex in kvp.Value)
						{
							Diagnostics.LogWarning($"[Gedemon] territoryToDetachAndCreate => #{territoryIndex} ({CultureUnlock.GetTerritoryName(territoryIndex)}).");
							DepartmentOfTheInterior.DetachTerritoryFromCity(kvp.Key, territoryIndex, createNewSettlement: true);
						}
					}

					#endregion

					#region 3/ find and set new capital if needed

					Diagnostics.Log($"[Gedemon] before changing Capital (need change = {territoryChange.HasCapitalChanged}, potential exist = {territoryChange.PotentialCapital != null})");
					if (territoryChange.HasCapitalChanged)
					{

						if (territoryChange.PotentialCapital == null)
						{
							Diagnostics.LogWarning($"[Gedemon] no potential Capital District was passed, try to find one in the territory list for the new faction...");

							int count = majorEmpire.Settlements.Count;
							foreach (int territoryIndex in CultureUnlock.GetListTerritories(nextFactionName))
							{
								for (int n = 0; n < count; n++)
								{
									Settlement settlement = majorEmpire.Settlements[n];
									if (settlement.SettlementStatus != SettlementStatuses.City)
									{
										District potentialDistrict = settlement.GetMainDistrict();
										if (territoryIndex == potentialDistrict.Territory.Entity.Index)
										{
											Diagnostics.LogWarning($"[Gedemon] found new Capital District in {CultureUnlock.GetTerritoryName(territoryIndex)}");
											territoryChange.PotentialCapital = potentialDistrict;
											goto Found;
										}
									}
								}
							}
						Found:;
						}

						if (territoryChange.PotentialCapital != null)
						{
							int territoryIndex = territoryChange.PotentialCapital.Territory.Entity.Index;
							Settlement settlement = territoryChange.PotentialCapital.Settlement;
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
					#endregion

					#region 4/ create new majors and minors

					Diagnostics.Log($"[Gedemon] Check to create new Major Empire for old Empire (Cities = {territoryChange.newMajorSettlements.Count})");

					// Need a major only if there are cities in the old Empire territory.
					if (territoryChange.newMajorSettlements.Count > 0)
                    {
						canUseMajorEmpire = CultureChange.TryInitializeFreeMajorEmpireToReplace(majorEmpire, out oldEmpire);
						Diagnostics.LogWarning($"[Gedemon] Major Faction for Old Empire created = {canUseMajorEmpire}");
						if(canUseMajorEmpire)
						{

							Diagnostics.LogWarning($"[Gedemon] - Old Empire ID#{oldEmpire.Index}, add in listEmpire in case we can't create a minor Rebel..."); 
							listEmpires.Add(territoryChange.OldFactionName, oldEmpire);
						}
					}

					int availableMinorFactions = Sandbox.MinorFactionManager.minorEmpirePool.Count;
					Diagnostics.LogWarning($"[Gedemon] Trying to create new minor factions, Available Minors = {availableMinorFactions}, Cities for minor = {territoryChange.newMinorsSettlements.Count}, territories for rebels = {territoryChange.newRebelsTerritories.Count}");

					// Set a rebel faction if needed (there are rebel territories, or we need an old empire but no major was available, or there are orphan territories in the old empire core list)
					if (territoryChange.newRebelsTerritories.Count > 0 || (oldEmpire == null && territoryChange.newMajorTerritories.Count > 0))
					{
						rebelFaction = CultureChange.GetMinorFactionFor(majorEmpire.FactionDefinition);
						Diagnostics.LogWarning($"[Gedemon] RebelFaction created = {rebelFaction != null}");

						if (rebelFaction != null)
						{
							Diagnostics.LogWarning($"[Gedemon] - Rebel Faction ID#{rebelFaction.Index}");
							if (oldEmpire == null)
							{
								Diagnostics.LogWarning($"[Gedemon] - Rebels Minors used as primary replacement for the Old Empire (no Major replacement found)");
								listEmpires.Add(territoryChange.OldFactionName, rebelFaction);
							}
							else
							{
								Diagnostics.LogWarning($"[Gedemon] - Rebels Minors used as secondary replacement for the Old Empire (Major replacement exists)");
								listEmpires[territoryChange.OldFactionName] = rebelFaction;
							}
						}
					}

					foreach (KeyValuePair<StaticString, List<Settlement>> kvp in territoryChange.newMinorsSettlements)
					{
						FactionDefinition factionDefinition = Utils.GameUtils.GetFactionDefinition(kvp.Key);

						if(!listEmpires.ContainsKey(kvp.Key))
                        {

							MinorEmpire minorFaction = CultureChange.GetMinorFactionFor(factionDefinition);

							Diagnostics.LogWarning($"[Gedemon] new Minor Faction created = {minorFaction != null} for {kvp.Key}");

							if (minorFaction == null)
							{
								Diagnostics.LogWarning($"[Gedemon] - No minor faction available, try to use rebel faction instead");
								minorFaction = rebelFaction; // no need to try to create a new rebel faction if it's null, as the result of CultureChange.GetMinorFactionFor would be also null at this point
							}
							if (minorFaction != null)
							{
								Diagnostics.LogWarning($"[Gedemon] - new Faction ID#{minorFaction.Index}");
								listEmpires.Add(kvp.Key, minorFaction);
							}
							else if (canUseMajorEmpire)
							{
								Diagnostics.LogWarning($"[Gedemon] - No minor available, use Old Empire instead");
								listEmpires.Add(kvp.Key, oldEmpire);
							}
							else
							{
								Diagnostics.LogError($"[Gedemon] - FAILED to assign a required new faction");
							}
						}

					}

					#endregion

					#region 5/ handle new major (or rebels)

					Diagnostics.Log($"[Gedemon] Trying to assign old Empire (Cities = {territoryChange.newMajorSettlements.Count})");

					if (territoryChange.newMajorSettlements.Count > 0 || territoryChange.newMajorTerritories.Count > 0)
					{
						Empire newEmpire = oldEmpire != null ? oldEmpire : listEmpires.ContainsKey(territoryChange.OldFactionName) ? listEmpires[territoryChange.OldFactionName] : null; // as MajorEmpire;

						if (newEmpire != null)
                        {
							Diagnostics.LogWarning($"[Gedemon] - replacing Empire ID#{newEmpire.Index}");
							foreach (Settlement settlement in territoryChange.newMajorSettlements)
                            {
								Diagnostics.LogWarning($"[Gedemon] city ({settlement.EntityName}) for Old Empire (isMajor={canUseMajorEmpire}) => #{settlement.Region.Entity.Territories[0].Index} ({CultureUnlock.GetTerritoryName(settlement.Region.Entity.Territories[0].Index)}).");

								refund.CompensateFor(settlement);
								DepartmentOfDefense.GiveSettlementTo(settlement, newEmpire);

								if (canUseMajorEmpire)
								{
									if (settlement == capital)
									{
										Diagnostics.LogWarning($"[Gedemon] Was Capital, set as Capital for new spawned Empire...");
										newEmpire.DepartmentOfTheInterior.SetCapital(settlement, true);
									}
								}
							}

							foreach (int territoryIndex in territoryChange.newMajorTerritories)
							{
								Territory territory = Sandbox.World.Territories[territoryIndex];
								Settlement settlement = territory.AdministrativeDistrict.Entity.Settlement;
								Diagnostics.LogWarning($"[Gedemon] settlement for Old Empire (isMajor={canUseMajorEmpire}) => #{settlement.Region.Entity.Territories[0].Index} ({CultureUnlock.GetTerritoryName(settlement.Region.Entity.Territories[0].Index)}).");

								refund.CompensateFor(settlement);
								DepartmentOfDefense.GiveSettlementTo(settlement, newEmpire);
							}

							// re-attach to cities
							foreach(Settlement city in territoryChange.newMajorSettlements)
                            {

								if (territoryChange.citiesInitialTerritories.ContainsKey(city))
								{
									foreach (int territoryIndex in territoryChange.citiesInitialTerritories[city].AsEnumerable().Reverse())
									{
										if (territoryChange.newMajorTerritories.Contains(territoryIndex))
										{

											Territory territory = Sandbox.World.Territories[territoryIndex];
											Settlement territorySettlement = territory.AdministrativeDistrict.Entity.Settlement;
											FixedPoint cost = new FixedPoint();
											FailureFlags flag = newEmpire.DepartmentOfTheInterior.CanTerritoryBeAttachedToCity(territorySettlement, city, ref cost);
											Diagnostics.LogWarning($"[Gedemon] Try to re-attach {CultureUnlock.GetTerritoryName(territoryIndex)} to {city.EntityName} for new spawned Empire (flag = {flag})");
											if (flag == FailureFlags.None || flag == FailureFlags.NotAMajorEmpire || flag == FailureFlags.NotEnoughInfluence)
                                            {
												newEmpire.DepartmentOfTheInterior.MergeSettlementIntoCity(territorySettlement, city);
											}
											else
                                            {
												Diagnostics.LogError($"[Gedemon] FAILED to re-attach ({flag})");
											}
										}
									}
								}
							}

							if(oldEmpire != null)
							{
								// (re)Set diplomatic relation
								CultureChange.SetDiplomaticRelationFromEvolution(majorEmpire, oldEmpire);
							}
						}
						else
						{
							Diagnostics.LogError($"[Gedemon] No Empire found for newMajorSettlements / newMajorTerritories");
						}
					}
					#endregion

					#region 6/ handle new minors

					Diagnostics.Log($"[Gedemon] Trying to assign new minor factions (Cities = {territoryChange.newMinorsSettlements.Count})");

					foreach (KeyValuePair<StaticString, List<Settlement>> kvp in territoryChange.newMinorsSettlements)
					{
						StaticString cityFaction = kvp.Key;
						List<Settlement> cityList = kvp.Value;
						if (listEmpires.TryGetValue(cityFaction, out Empire newEmpire))
						{
							Diagnostics.LogWarning($"[Gedemon] Cities for replacing Faction ID#{newEmpire.Index} ({cityFaction})");
							// Give Cities
							foreach (Settlement settlement in cityList)
							{
								Diagnostics.LogWarning($"[Gedemon]City ( {settlement.EntityName}) => #{settlement.Region.Entity.Territories[0].Index} ({CultureUnlock.GetTerritoryName(settlement.Region.Entity.Territories[0].Index)})");

								refund.CompensateFor(settlement);
								DepartmentOfDefense.GiveSettlementTo(settlement, newEmpire);


								if(newEmpire != oldEmpire)
								{
									Diagnostics.LogWarning($"[Gedemon] Try to spawn defending army at ({settlement.WorldPosition.Column}, {settlement.WorldPosition.Row}) in {CultureUnlock.GetTerritoryName(settlement.GetMainDistrict().Territory.Entity.Index)}");
									Sandbox.MinorFactionManager.PeacefulHumanSpawner.SpawnArmy(newEmpire as MinorEmpire, settlement.WorldPosition, isDefender: true);
								}
                                else
								{
									Diagnostics.LogWarning($"[Gedemon] newMinorsSettlements contains an entry for Major OldEmpire Faction (ID#{newEmpire.Index})");
								}

							}

							// Give territories
							foreach (KeyValuePair<int, StaticString> territories in territoryChange.newMinorsTerritories)
                            {
								int territoryIndex = territories.Key;
								StaticString territoryFaction = territories.Value;
								if (territoryFaction == cityFaction)
                                {
									Territory territory = Sandbox.World.Territories[territoryIndex];
									Settlement territorySettlement = territory.AdministrativeDistrict.Entity.Settlement;

									Diagnostics.LogWarning($"[Gedemon] Settlement for Minor {cityFaction} => #{territoryIndex} ({CultureUnlock.GetTerritoryName(territoryIndex)}).");

									refund.CompensateFor(territorySettlement);
									DepartmentOfDefense.GiveSettlementTo(territorySettlement, newEmpire);
								}
                            }

							// Re-attach territories to cities when possible
							foreach (Settlement city in cityList)
							{
								if (territoryChange.citiesInitialTerritories.ContainsKey(city))
								{
									foreach (int territoryIndex in territoryChange.citiesInitialTerritories[city].AsEnumerable().Reverse())
									{
										if (territoryChange.newMinorsTerritories.TryGetValue(territoryIndex, out StaticString territoryFaction) )
                                        {
											if(territoryFaction == cityFaction)
                                            {
												Territory territory = Sandbox.World.Territories[territoryIndex];
												Settlement territorySettlement = territory.AdministrativeDistrict.Entity.Settlement;
												FixedPoint cost = new FixedPoint();
												FailureFlags flag = newEmpire.DepartmentOfTheInterior.CanTerritoryBeAttachedToCity(territorySettlement, city, ref cost);
												Diagnostics.LogWarning($"[Gedemon] Try to re-attach {CultureUnlock.GetTerritoryName(territoryIndex)} to {city.EntityName} for minor faction (flag = {flag})");
												if (flag == FailureFlags.None || flag == FailureFlags.NotAMajorEmpire || flag == FailureFlags.NotEnoughInfluence)
												{
													newEmpire.DepartmentOfTheInterior.MergeSettlementIntoCity(territorySettlement, city);
												}
												else
												{
													Diagnostics.LogError($"[Gedemon] FAILED to re-attach ({flag})");
												}
											}
                                        }
									}
								}
							}

							CultureChange.UpdateDistrictVisuals(newEmpire);
						}
						else
                        {
							Diagnostics.LogError($"[Gedemon] No new faction assigned to {kvp.Key}");
						}

					}

					#endregion

					#region 7/ give orphan territories to rebel faction

					Diagnostics.Log($"[Gedemon] Check to give orphan territories to Nomad rebels (orphans = {territoryChange.newRebelsTerritories.Count}, minor rebel faction exist = {rebelFaction != null})");

					if(territoryChange.newRebelsTerritories.Count > 0)
                    {
						Empire nomadRebels = null;
						if (rebelFaction != null)
						{
							nomadRebels = rebelFaction;
						}
						else
						{
							nomadRebels = oldEmpire;
						}

						if (nomadRebels != null)
						{
							Diagnostics.LogWarning($"[Gedemon] - nomad rebels ID#{nomadRebels.Index} {nomadRebels.FactionDefinition.Name}");
							foreach (int territoryIndex in territoryChange.newRebelsTerritories)
                            {
								Territory territory = Sandbox.World.Territories[territoryIndex];
								Settlement territorySettlement = territory.AdministrativeDistrict.Entity.Settlement;

								Diagnostics.LogWarning($"[Gedemon] Settlement for Rebels => #{territoryIndex} ({CultureUnlock.GetTerritoryName(territoryIndex)}).");

								refund.CompensateFor(territorySettlement);
								DepartmentOfDefense.GiveSettlementTo(territorySettlement, nomadRebels);
							}

						}
						else
						{
							Diagnostics.LogError($"[Gedemon] - FAILED to assign nomad rebels");
						}
					}

					#endregion

					#region 8/ finalize evolving Empire

					Diagnostics.Log($"[Gedemon] Check to re-attach territories to the Evolved Empire (Cities = {majorEmpire.Cities.Count})");
					int numCities = majorEmpire.Cities.Count;
					for(int c = 0; c < numCities; c++)
					{
						Settlement city = majorEmpire.Cities[c];
						if (territoryChange.citiesInitialTerritories.ContainsKey(city))
						{
							foreach (int territoryIndex in territoryChange.citiesInitialTerritories[city].AsEnumerable().Reverse())
							{
								if (territoryChange.territoriesKept.Contains(territoryIndex))
								{
									Territory territory = Sandbox.World.Territories[territoryIndex];
									Settlement territorySettlement = territory.AdministrativeDistrict.Entity.Settlement;
									FixedPoint cost = new FixedPoint();
									FailureFlags flag = majorEmpire.DepartmentOfTheInterior.CanTerritoryBeAttachedToCity(territorySettlement, city, ref cost);
									Diagnostics.LogWarning($"[Gedemon] Try to re-attach {CultureUnlock.GetTerritoryName(territoryIndex)} to {city.EntityName} for the Evolved Empire (flag = {flag})");
									if (flag == FailureFlags.None)
									{
										majorEmpire.DepartmentOfTheInterior.MergeSettlementIntoCity(territorySettlement, city);
									}
									else
									{
										Diagnostics.LogError($"[Gedemon] FAILED to re-attach ({flag})");
									}
								}
							}
						}
					}

					// Give compensation from lost territories
					refund.ApplyCompensation();

					#endregion

					#region 9/ finalize Minor Factions

					FixedPoint defaultGameSpeedMultiplier = Sandbox.GameSpeedController.CurrentGameSpeedDefinition.DefaultGameSpeedMultiplier;

					if (rebelFaction != null)
					{
						if (rebelFaction.Cities.Count == 0)
						{

							BaseHumanSpawnerDefinition spawnerDefinitionForMinorEmpire = rebelFaction.Spawner.GetSpawnerDefinitionForMinorEmpire(rebelFaction);

							Diagnostics.LogWarning($"[Gedemon] Set Rebels ID#{rebelFaction.Index} ({rebelFaction.FactionDefinition.Name}) to decline from {rebelFaction.MinorFactionStatus}, HomeStatus = {rebelFaction.MinorEmpireHomeStatus}, RemainingLife = {rebelFaction.RemainingLifeTime}, SpawnPointIndex = {rebelFaction.SpawnPointIndex}, , TimeBeforeEvolveToCity = {spawnerDefinitionForMinorEmpire.TimeBeforeEvolveToCity}, ConstructionTurn = {rebelFaction.ConstructionTurn}, speed X = {defaultGameSpeedMultiplier}");
							Sandbox.MinorFactionManager.PeacefulHumanSpawner.SetMinorEmpireHomeStatus(rebelFaction, MinorEmpireHomeStatuses.Camp);
							rebelFaction.ConstructionTurn = 100; // test to prevent spawning a city (check is ConstructionTurn++ < TimeBeforeEvolveToCity)
							rebelFaction.IsPeaceful = false;
							rebelFaction.MinorFactionStatus = MinorFactionStatuses.InDecline;
							rebelFaction.RemainingLifeTime = 10 * defaultGameSpeedMultiplier;
							Sandbox.SimulationEntityRepository.SetSynchronizationDirty(rebelFaction);

							int numSettlement = rebelFaction.Settlements.Count;
							for (int s = 0; s < numSettlement; s++)
							{
								Settlement settlement = rebelFaction.Settlements[s];
								WorldPosition position = settlement.GetMainDistrict().WorldPosition;

								Diagnostics.LogWarning($"[Gedemon] Try to spawn rebel army at ({position.Column}, {position.Row}) in {CultureUnlock.GetTerritoryName(settlement.GetMainDistrict().Territory.Entity.Index)}");
								Sandbox.MinorFactionManager.ViolentHumanSpawner.SpawnArmy(rebelFaction, position, isDefender: false);
							}

						}
                        else
						{
							int numRebelsCities = rebelFaction.Cities.Count;
							int availableRebelFactions = Sandbox.MinorFactionManager.minorEmpirePool.Count;
							Diagnostics.Log($"[Gedemon] Check to split rebel faction (Cities = {numRebelsCities}, available minors = {availableRebelFactions} )");
							if (availableRebelFactions > 0)
							{
								List<MinorEmpire> rebelList = new List<MinorEmpire>();
								rebelList.Add(rebelFaction);
								int numRebelFaction = System.Math.Min(availableRebelFactions, numRebelsCities);
								int rebelListIndex = 0;

								for(int i = 1; i <= numRebelFaction; i++)
                                {
									MinorEmpire newRebels = CultureChange.GetMinorFactionFor(majorEmpire.FactionDefinition);
									Diagnostics.LogWarning($"[Gedemon] Additional Rebel Faction #{i} created = {newRebels != null}");
									if (newRebels != null)
                                    {
										rebelList.Add(newRebels);
									}
									else
                                    {
										break;
                                    }
								}

								for (int c = 0; c < numRebelsCities; c++)
								{
									MinorEmpire newRebels = rebelList[rebelListIndex]; // there is at least rebelFaction at [0]
									rebelListIndex++;
									if(rebelListIndex >= rebelList.Count)
                                    {
										rebelListIndex = 0;
									}
									Settlement city = rebelFaction.Cities[c];

									Diagnostics.LogWarning($"[Gedemon] City for separated Rebels => #{city.GetMainDistrict().Territory.Entity.Index} ({CultureUnlock.GetTerritoryName(city.GetMainDistrict().Territory.Entity.Index)})  Rebels ID#{newRebels.Index} ({newRebels.FactionDefinition.Name})");

									DepartmentOfDefense.GiveSettlementTo(city, newRebels);

									WorldPosition position = city.GetMainDistrict().WorldPosition;

									Diagnostics.LogWarning($"[Gedemon] Try to spawn new rebels army at ({position.Column}, {position.Row}) in {CultureUnlock.GetTerritoryName(city.GetMainDistrict().Territory.Entity.Index)}");
									Sandbox.MinorFactionManager.ViolentHumanSpawner.SpawnArmy(newRebels, position, isDefender: true);

									Sandbox.MinorFactionManager.PeacefulHumanSpawner.SetMinorEmpireHomeStatus(newRebels, MinorEmpireHomeStatuses.City);
									newRebels.RemainingLifeTime = 20 * defaultGameSpeedMultiplier;
									newRebels.IsPeaceful = true;
									newRebels.MinorFactionStatus = MinorFactionStatuses.InDecline;
									Sandbox.SimulationEntityRepository.SetSynchronizationDirty(newRebels);
								}

								// Try to give non-cities territories of the rebel faction to a new nomad faction
								if(Sandbox.MinorFactionManager.minorEmpirePool.Count > 0)
								{
									List<Settlement> finalOrphanTerritories = new List<Settlement>();
									int numRebelsTerritories = rebelFaction.Settlements.Count;
									for (int s = 0; s < numRebelsTerritories; s++)
									{
										Settlement settlement = rebelFaction.Settlements[s];
										if (settlement.SettlementStatus != SettlementStatuses.City)
										{
											finalOrphanTerritories.Add(settlement);
										}
									}
									if (finalOrphanTerritories.Count > 0)
									{
										MinorEmpire newRebels = CultureChange.GetMinorFactionFor(majorEmpire.FactionDefinition);
										if (newRebels != null)
										{
											Diagnostics.LogWarning($"[Gedemon] Created nomad Rebels for orphan territorie => Rebels ID#{newRebels.Index} ({newRebels.FactionDefinition.Name})");
											foreach (Settlement settlement in finalOrphanTerritories)
											{
												DepartmentOfDefense.GiveSettlementTo(settlement, newRebels);

												WorldPosition position = settlement.WorldPosition;
												Diagnostics.LogWarning($"[Gedemon] Try to spawn new rebels army at ({position.Column}, {position.Row}) in {CultureUnlock.GetTerritoryName(settlement.GetMainDistrict().Territory.Entity.Index)}");
												Sandbox.MinorFactionManager.ViolentHumanSpawner.SpawnArmy(newRebels, position, isDefender: false);
											}
											Sandbox.MinorFactionManager.PeacefulHumanSpawner.SetMinorEmpireHomeStatus(rebelFaction, MinorEmpireHomeStatuses.Camp);
											rebelFaction.ConstructionTurn = 100; // test to prevent spawning a city (check is ConstructionTurn++ < TimeBeforeEvolveToCity)
											rebelFaction.IsPeaceful = false;
											rebelFaction.MinorFactionStatus = MinorFactionStatuses.InDecline;
											rebelFaction.RemainingLifeTime = 10 * defaultGameSpeedMultiplier;
											Sandbox.SimulationEntityRepository.SetSynchronizationDirty(rebelFaction);
										}
									}
								}

							}
                            else
                            {

								Sandbox.MinorFactionManager.PeacefulHumanSpawner.SetMinorEmpireHomeStatus(rebelFaction, MinorEmpireHomeStatuses.City);
								rebelFaction.RemainingLifeTime = 20 * defaultGameSpeedMultiplier;
								rebelFaction.IsPeaceful = false;
								rebelFaction.MinorFactionStatus = MinorFactionStatuses.InDecline;
								Sandbox.SimulationEntityRepository.SetSynchronizationDirty(rebelFaction);
							}
						}

					}

					foreach (KeyValuePair<StaticString, Empire> kvp in listEmpires)
                    {

						Diagnostics.Log($"[Gedemon] Check if {kvp.Key} ID#{kvp.Value.Index} ({kvp.Value.FactionDefinition.Name}) is rebel ({kvp.Value == rebelFaction}) or is oldEmpire ({kvp.Value == oldEmpire}) ");
						if (kvp.Value != rebelFaction && kvp.Value != oldEmpire)
                        {
							MinorEmpire newMinorFaction = kvp.Value as MinorEmpire;
							if (newMinorFaction != null)
							{
								Diagnostics.LogWarning($"[Gedemon] Update {newMinorFaction.FactionDefinition.Name} RemainingLifeTime =  {newMinorFaction.RemainingLifeTime}");
								newMinorFaction.RemainingLifeTime = 25 * defaultGameSpeedMultiplier;
								Diagnostics.Log($"[Gedemon] - changed to RemainingLifeTime =  {newMinorFaction.RemainingLifeTime}");

								Diagnostics.Log($"[Gedemon] - Add patronnage stock...");
								MinorToMajorRelation minorToMajorRelation = newMinorFaction.RelationsToMajor[majorEmpire.Index];
								if (minorToMajorRelation.PatronageStock.Value < 25)
								{
									minorToMajorRelation.PatronageStock.Value = 25;
									MinorFactionManager.RefreshPatronageState(minorToMajorRelation, newMinorFaction.PatronageDefinition);
								}
							}
                        }
                    }
					#endregion
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

				/*
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
				//*/
				CultureChange.UpdateDistrictVisuals(majorEmpire);
				CultureChange.SetFactionSymbol(majorEmpire);
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
				if (CultureUnlock.HasMajorTerritories(factionName))
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
	}
}
