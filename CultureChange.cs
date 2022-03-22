using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Framework;
using Amplitude.Framework.Simulation;
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
		static public IDictionary<int,StaticString> VisualAffinityCache = new Dictionary<int,StaticString>();

		public static void Load()
		{
			Diagnostics.LogWarning($"[Gedemon] [CultureChange] OnLoad: TrueCultureLocation.IsEnabled = {TrueCultureLocation.IsEnabled()}");
			if (!TrueCultureLocation.IsEnabled())
				return;

			SimulationEvent<SimulationEvent_TurnEnd>.Raised += new Action<object, SimulationEvent_TurnEnd>(SimulationEventRaised_TurnEnd);
		}

		public static void Unload()
		{
			Diagnostics.LogWarning($"[Gedemon] [CultureChange] OnUnload: TrueCultureLocation.IsEnabled = {TrueCultureLocation.IsEnabled()}");
			if (!TrueCultureLocation.IsEnabled())
				return;

			SimulationEvent<SimulationEvent_TurnEnd>.Raised -= new Action<object, SimulationEvent_TurnEnd>(SimulationEventRaised_TurnEnd);
		}

		public static bool IsSleepingEmpire(MajorEmpire majorEmpire)
        {
			return majorEmpire.IsAlive && majorEmpire.Armies.Count == 0 && majorEmpire.Settlements.Count == 0 && majorEmpire.OccupiedCityCount.Value == 0;
        }

		private static void SimulationEventRaised_TurnEnd(object sender, SimulationEvent_TurnEnd simulationEventTurnEnd)
        {
			Diagnostics.LogWarning($"[Gedemon] [CultureChange] SimulationEventRaised_TurnEnd");

			if (TrueCultureLocation.CanRespawnDeadPlayer() && TrueCultureLocation.CanEliminateLastEmpires())
			{

				int numAwake = 0;
				int minEra = int.MaxValue;
				int maxEra = 0;
				int numMajor = Amplitude.Mercury.Sandbox.Sandbox.MajorEmpires.Length;
				int maxAwake = numMajor - 2;
				int lastEmpires = 3; // when there is this number of major Empires left in the older era, check if they can be replaced by a Minor Faction

				IDictionary<int, List<MajorEmpire>> EmpiresPerEra = new Dictionary<int, List<MajorEmpire>>();
				for (int empireIndex = 0; empireIndex < numMajor; empireIndex++)
				{
					MajorEmpire majorEmpire = Sandbox.MajorEmpires[empireIndex];
					int empireEraIndex = majorEmpire.DepartmentOfDevelopment.CurrentEraIndex;

					if (!IsSleepingEmpire(majorEmpire))
						numAwake++;
					else
						continue;

					minEra = minEra > empireEraIndex ? empireEraIndex : minEra;
					maxEra = maxEra < empireEraIndex ? empireEraIndex : maxEra;

					if (EmpiresPerEra.TryGetValue(empireEraIndex, out List<MajorEmpire> Empires))
                    {
						Empires.Add(majorEmpire);
						EmpiresPerEra[empireEraIndex] = Empires;
					}
					else
                    {
						EmpiresPerEra.Add(empireEraIndex, new List<MajorEmpire> { majorEmpire });
					}
				}
				Diagnostics.LogWarning($"[Gedemon] [CultureChange] numAwake = {numAwake}, maxAwake = {maxAwake}, maxEra = {maxEra}, minEra = {minEra}, EmpiresPerEra[minEra].Count = {EmpiresPerEra[minEra].Count}, lastEmpires = {lastEmpires}");
				if (numAwake >= maxAwake && maxEra > minEra && EmpiresPerEra[minEra].Count <= lastEmpires)
				{

					FixedPoint defaultGameSpeedMultiplier = Sandbox.GameSpeedController.CurrentGameSpeedDefinition.DefaultGameSpeedMultiplier;
					FixedPoint minLifeSpan = 60 * defaultGameSpeedMultiplier;
					foreach(MajorEmpire majorEmpire in EmpiresPerEra[minEra])
					{
						MajorEmpireExtension empireExtension = MajorEmpireSaveExtension.GetExtension(majorEmpire.Index);
						int LifeSpan = SandboxManager.Sandbox.Turn - empireExtension.SpawnTurn;

						Diagnostics.LogWarning($"[Gedemon] [CultureChange] - potential Fallen Empire : {majorEmpire.FactionDefinition.Name}, ID#{majorEmpire.Index}, LifeSpan = {LifeSpan}, minLife = {minLifeSpan}, EraStar = {majorEmpire.EraStarsCount.Value}, RequiredStar = {majorEmpire.DepartmentOfDevelopment.CurrentEraStarRequirement}");

						// Check before removing...
						if (IsSleepingEmpire(majorEmpire))
							continue;

						if (majorEmpire.OccupiedCityCount.Value > 0)
							continue;

						if (LifeSpan < minLifeSpan)
							continue;

						if(majorEmpire.EraStarsCount.Value == majorEmpire.DepartmentOfDevelopment.CurrentEraStarRequirement)
							continue;

						if (TrueCultureLocation.IsEmpireHumanSlot(majorEmpire.Index))
							continue;

						//CityFlags.Besieged
						bool hasCityUnderSiege = false;
						int numCities = majorEmpire.Cities.Count;
						for (int c = 0; c < numCities; c++)
						{
							if ((majorEmpire.Cities[c].CityFlags & CityFlags.Besieged) != 0)
							{
								hasCityUnderSiege = true;
								break;
							}
						}

						if (hasCityUnderSiege)
							continue;

						bool hasLockedArmy = false;
						int numArmies = majorEmpire.Armies.Count;
						for(int a = 0; a < numArmies; a++)
                        {
							Army army = majorEmpire.Armies[a];
							if (army.IsLocked)
							{
								hasLockedArmy = true;
								break;
							}
						}

						if (hasLockedArmy)
							continue;

						if (Sandbox.MinorFactionManager.minorEmpirePool.Count < 5)
							continue;

						Diagnostics.LogWarning($"[Gedemon] [CultureChange] - all checks passed, removing Major Empire (num Armies = {numArmies})");

						// All check done, destroy all armies
						for (int a = 0; a < numArmies; a++)
						{
							Diagnostics.LogWarning($"[Gedemon] [CultureChange] - army #{a}");
							Army army = majorEmpire.Armies[a];
							Diagnostics.Log($"[Gedemon] [CultureChange] - Calling TryRemoveSquadron()");
							DepartmentOfDefense.TryRemoveSquadron(army, majorEmpire.DepartmentOfDefense);
							Diagnostics.Log($"[Gedemon] [CultureChange] - Calling AddArmyToReleasePool()");
							Amplitude.Mercury.Sandbox.Sandbox.ArmyReleaseController.AddArmyToReleasePool(army, ReleaseType.FailedRetreating);
							Diagnostics.Log($"[Gedemon] [CultureChange] - Check before Calling ReleaseArmy() (army.WorldPosition.ToTileIndex() = {army.WorldPosition.ToTileIndex()})");
							//
							if(army.WorldPosition.ToTileIndex() != -1)
							{
								Diagnostics.Log($"[Gedemon] [CultureChange] - Calling ReleaseArmy()");
								DepartmentOfDefense.ReleaseArmy(army);
							}

							Diagnostics.Log($"[Gedemon] [CultureChange] - Calling SimulationController.RefreshAll()");
							SimulationController.RefreshAll();
						}

						Diagnostics.LogWarning($"[Gedemon] [CultureChange] - Calling DoFactionChange()");
						// Replace by minor factions
						DoFactionChange(majorEmpire, new StaticString("Civilization_Era0_DefaultTribe"), CanReplaceMajor : false);

						Diagnostics.LogWarning($"[Gedemon] [CultureChange] - Calling AddFallenEmpire()");
						// Mark as Fallen Empire (to not be respawned from a Minor faction)
						CurrentGame.Data.AddFallenEmpire(majorEmpire.FactionDefinition.Name);

						break;
					}
				}
			}
		}
		public static void DoFactionChange(MajorEmpire majorEmpire, StaticString nextFactionName, bool CanReplaceMajor = true)
        {

			bool canUseMajorEmpire = false;

			Settlement capital = majorEmpire.Capital;

			IDictionary<StaticString, Empire> listEmpires = new Dictionary<StaticString, Empire>();

			MinorEmpire rebelFaction = null;
			MajorEmpire oldEmpire = null;

			SettlementRefund refund = new SettlementRefund(majorEmpire);

			TerritoryChange territoryChange = new TerritoryChange(majorEmpire, nextFactionName);

			#region 1/ stop battles for lost cities

			if (territoryChange.NumCitiesLost > 0)
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
					Diagnostics.LogWarning($"[Gedemon] {kvp.Key.EntityName} : territoryToDetachAndCreate => #{territoryIndex} ({CultureUnlock.GetTerritoryName(territoryIndex)}).");

					//debug
					bool canDetach = true;
					{
						Territory territory = Amplitude.Mercury.Sandbox.Sandbox.World.Territories[territoryIndex];
						Region region = kvp.Key.Region.Entity;
						if (!(region.TerritoryIndices.Contains(territoryIndex) && region.Territories.Contains(territory)))
						{
							Diagnostics.LogError($"[Gedemon] Failed Check for territory is in region = {region.TerritoryIndices.Contains(territoryIndex)}, territoryIndex in region indices = {region.Territories.Contains(territory)}.");
							canDetach = false;
						}
					}
					if (canDetach)
						DepartmentOfTheInterior.DetachTerritoryFromCity(kvp.Key, territoryIndex, createNewSettlement: true);
				}
			}

			#endregion

			#region 3/ find and set new capital if needed

			Diagnostics.Log($"[Gedemon] before changing Capital (need change = {territoryChange.HasCapitalChanged}, potential exist = {territoryChange.PotentialCapital != null})");
			if (territoryChange.HasCapitalChanged && CanReplaceMajor)
			{

				if (territoryChange.PotentialCapital == null)
				{
					Diagnostics.LogWarning($"[Gedemon] no potential Capital District was passed, try to find one in the territory list for the new faction...");

					int count = majorEmpire.Settlements.Count;
					foreach (int territoryIndex in CultureUnlock.GetListTerritories(nextFactionName.ToString()))
					{
						for (int n = 0; n < count; n++)
						{
							Settlement settlement = majorEmpire.Settlements[n];
							if (settlement.SettlementStatus != SettlementStatuses.City)
							{
								District potentialDistrict = settlement.GetMainDistrict();

								if (potentialDistrict != null)
								{
									if (territoryIndex == potentialDistrict.Territory.Entity.Index)
									{
										Diagnostics.LogWarning($"[Gedemon] found new Capital District in {CultureUnlock.GetTerritoryName(territoryIndex)}");
										territoryChange.PotentialCapital = potentialDistrict;
										goto Found;
									}
								}
								else
								{
									Diagnostics.LogError($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : GetMainDistrict returns null");
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
					SimulationEvent_CapitalChanged.Raise(majorEmpire.DepartmentOfDevelopment, settlement, capital);
				}
				else
				{
					Diagnostics.LogError($"[Gedemon] No new Capital was set...");
				}
			}

			if(!CanReplaceMajor && capital != null)
			{
				majorEmpire.DepartmentOfTheInterior.SetCapital(capital, set: false);
			}
			#endregion

			#region 4/ create new majors and minors

			Diagnostics.Log($"[Gedemon] Check to create new Major Empire for old Empire (Cities = {territoryChange.newMajorSettlements.Count})");

			// Need a major only if there are cities in the old Empire territory.
			if (territoryChange.newMajorSettlements.Count > 0 && CanReplaceMajor)
			{
				canUseMajorEmpire = CultureChange.TryInitializeFreeMajorEmpireToReplace(majorEmpire, out oldEmpire);
				Diagnostics.LogWarning($"[Gedemon] Major Faction for Old Empire created = {canUseMajorEmpire}");
				if (canUseMajorEmpire)
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

				if (!listEmpires.ContainsKey(kvp.Key))
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
						CultureChange.RemoveTradeRoutesEnding(settlement, majorEmpire);

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
					foreach (Settlement city in territoryChange.newMajorSettlements)
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

					if (oldEmpire != null)
					{
						// (re)Set diplomatic relation
						CultureChange.SetDiplomaticRelationFromEvolution(majorEmpire, oldEmpire);
						CultureChange.FinalizeMajorEmpireSpawning(oldEmpire);
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
						CultureChange.RemoveTradeRoutesEnding(settlement, majorEmpire);

						if (newEmpire != oldEmpire)
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
								if (territoryChange.newMinorsTerritories.TryGetValue(territoryIndex, out StaticString territoryFaction))
								{
									if (territoryFaction == cityFaction)
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

			if (territoryChange.newRebelsTerritories.Count > 0)
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

			// Update for removed trade routes
			Sandbox.TradeController.EndTurn_UpdateTradeRoad(SimulationPasses.PassContext.TurnEnd, "");

			Diagnostics.Log($"[Gedemon] Check to re-attach territories to the Evolved Empire (Cities = {majorEmpire.Cities.Count})");
			int numCities = majorEmpire.Cities.Count;
			for (int c = 0; c < numCities; c++)
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
			if(CanReplaceMajor)
				refund.ApplyCompensation();

			#endregion

			#region 9/ finalize Minor Factions

			FixedPoint defaultGameSpeedMultiplier = Sandbox.GameSpeedController.CurrentGameSpeedDefinition.DefaultGameSpeedMultiplier;
			List<MinorEmpire> rebelList = new List<MinorEmpire>();

			if (rebelFaction != null)
			{
				if (rebelFaction.Cities.Count == 0)
				{

					BaseHumanSpawnerDefinition spawnerDefinitionForMinorEmpire = rebelFaction.Spawner.GetSpawnerDefinitionForMinorEmpire(rebelFaction);

					Diagnostics.LogWarning($"[Gedemon] Set Rebels ID#{rebelFaction.Index} ({rebelFaction.FactionDefinition.Name}) to decline from {rebelFaction.MinorFactionStatus}, HomeStatus = {rebelFaction.MinorEmpireHomeStatus}, RemainingLife = {rebelFaction.RemainingLifeTime}, SpawnPointIndex = {rebelFaction.SpawnPointIndex}, , TimeBeforeEvolveToCity = {spawnerDefinitionForMinorEmpire.TimeBeforeEvolveToCity}, ConstructionTurn = {rebelFaction.ConstructionTurn}, speed X = {defaultGameSpeedMultiplier}");
					Sandbox.MinorFactionManager.ViolentHumanSpawner.SetMinorEmpireHomeStatus(rebelFaction, MinorEmpireHomeStatuses.Camp);
					rebelFaction.ConstructionTurn = 100; // test to prevent spawning a city (check is ConstructionTurn++ < TimeBeforeEvolveToCity)
					rebelFaction.IsPeaceful = false;
					rebelFaction.MinorFactionStatus = MinorFactionStatuses.InDecline;
					rebelFaction.RemainingLifeTime = 10 * defaultGameSpeedMultiplier;
					Sandbox.SimulationEntityRepository.SetSynchronizationDirty(rebelFaction);
					Diagnostics.LogWarning($"[Gedemon] Updated {rebelFaction.FactionDefinition.Name} ID#{rebelFaction.Index} RemainingLifeTime =  {rebelFaction.RemainingLifeTime}, {rebelFaction.MinorFactionStatus}, {rebelFaction.MinorEmpireHomeStatus}");

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
						if (rebelFaction.Cities.Count > 1)
						{
							rebelList.Add(rebelFaction);
							int numRebelFaction = System.Math.Min(availableRebelFactions, numRebelsCities);
							int rebelListIndex = 0;

							for (int i = 1; i <= numRebelFaction; i++)
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
								if (rebelListIndex >= rebelList.Count)
								{
									rebelListIndex = 0;
								}
								Settlement city = rebelFaction.Cities[c];

								Diagnostics.LogWarning($"[Gedemon] City for separated Rebels => #{city.GetMainDistrict().Territory.Entity.Index} ({CultureUnlock.GetTerritoryName(city.GetMainDistrict().Territory.Entity.Index)})  Rebels ID#{newRebels.Index} ({newRebels.FactionDefinition.Name})");

								DepartmentOfDefense.GiveSettlementTo(city, newRebels);

								WorldPosition position = city.GetMainDistrict().WorldPosition;

								Diagnostics.LogWarning($"[Gedemon] Try to spawn new rebels army at ({position.Column}, {position.Row}) in {CultureUnlock.GetTerritoryName(city.GetMainDistrict().Territory.Entity.Index)}");
								Sandbox.MinorFactionManager.PeacefulHumanSpawner.SpawnArmy(newRebels, position, isDefender: true);

								Sandbox.MinorFactionManager.PeacefulHumanSpawner.SetMinorEmpireHomeStatus(newRebels, MinorEmpireHomeStatuses.City);
								newRebels.MinorFactionStatus = MinorFactionStatuses.Zenith;
								newRebels.RemainingLifeTime = 20 * defaultGameSpeedMultiplier;
								newRebels.IsPeaceful = true;
								Sandbox.SimulationEntityRepository.SetSynchronizationDirty(newRebels);
								Diagnostics.LogWarning($"[Gedemon] Updated {newRebels.FactionDefinition.Name} ID#{newRebels.Index} RemainingLifeTime =  {newRebels.RemainingLifeTime} {rebelFaction.MinorFactionStatus} {rebelFaction.MinorEmpireHomeStatus}");
							}

						}
						else // initialize the rebel faction with one city
						{
							Sandbox.MinorFactionManager.PeacefulHumanSpawner.SetMinorEmpireHomeStatus(rebelFaction, MinorEmpireHomeStatuses.City);
							rebelFaction.MinorFactionStatus = MinorFactionStatuses.Zenith;
							rebelFaction.RemainingLifeTime = 20 * defaultGameSpeedMultiplier;
							rebelFaction.IsPeaceful = true;
							Sandbox.SimulationEntityRepository.SetSynchronizationDirty(rebelFaction);
							Diagnostics.LogWarning($"[Gedemon] Updated {rebelFaction.FactionDefinition.Name} ID#{rebelFaction.Index} for one city, RemainingLifeTime = {rebelFaction.RemainingLifeTime} {rebelFaction.MinorFactionStatus} {rebelFaction.MinorEmpireHomeStatus}");
						}

						// Try to give non-cities territories of the rebel faction to a new nomad faction (check again if we still have enough faction in the pool)
						if (Sandbox.MinorFactionManager.minorEmpirePool.Count > 0)
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
									Sandbox.MinorFactionManager.ViolentHumanSpawner.SetMinorEmpireHomeStatus(newRebels, MinorEmpireHomeStatuses.Camp);
									newRebels.ConstructionTurn = 100; // test to prevent spawning a city (check is ConstructionTurn++ < TimeBeforeEvolveToCity)
									newRebels.IsPeaceful = false;
									newRebels.MinorFactionStatus = MinorFactionStatuses.InDecline;
									newRebels.RemainingLifeTime = 10 * defaultGameSpeedMultiplier;
									Sandbox.SimulationEntityRepository.SetSynchronizationDirty(newRebels);
									Diagnostics.LogWarning($"[Gedemon] Updated {newRebels.FactionDefinition.Name} ID#{newRebels.Index} RemainingLifeTime = {newRebels.RemainingLifeTime}, {newRebels.MinorFactionStatus}, {newRebels.MinorEmpireHomeStatus}");
								}
								else
								{
									Diagnostics.LogWarning($"[Gedemon] Failed to create minor faction for rebels orphan territories, the first rebel faction keep control of the rebel territories...");
								}
							}
						}
					}
					else
					{
						Diagnostics.LogWarning($"[Gedemon] No available minor faction to split the Rebels faction, use only one faction for cities and territories...");

						Sandbox.MinorFactionManager.ViolentHumanSpawner.SetMinorEmpireHomeStatus(rebelFaction, MinorEmpireHomeStatuses.City);
						rebelFaction.MinorFactionStatus = MinorFactionStatuses.Zenith;
						rebelFaction.RemainingLifeTime = 20 * defaultGameSpeedMultiplier;
						rebelFaction.IsPeaceful = false;
						Sandbox.SimulationEntityRepository.SetSynchronizationDirty(rebelFaction);
						Diagnostics.LogWarning($"[Gedemon] Update {rebelFaction.FactionDefinition.Name} ID#{rebelFaction.Index} RemainingLifeTime = {rebelFaction.RemainingLifeTime}, {rebelFaction.MinorFactionStatus}, {rebelFaction.MinorEmpireHomeStatus}");
					}
				}

			}

			foreach (KeyValuePair<StaticString, Empire> kvp in listEmpires)
			{

				Diagnostics.Log($"[Gedemon] Check if {kvp.Key} ID#{kvp.Value.Index} ({kvp.Value.FactionDefinition.Name}) is rebel ({kvp.Value == rebelFaction || rebelList.Contains(kvp.Value)}) or is oldEmpire ({kvp.Value == oldEmpire}) ");
				if (kvp.Value != rebelFaction && kvp.Value != oldEmpire && !rebelList.Contains(kvp.Value))
				{
					MinorEmpire newMinorFaction = kvp.Value as MinorEmpire;
					if (newMinorFaction != null)
					{
						newMinorFaction.RemainingLifeTime = 25 * defaultGameSpeedMultiplier;
						Diagnostics.LogWarning($"[Gedemon] Updated {newMinorFaction.FactionDefinition.Name} ID#{newMinorFaction.Index} RemainingLifeTime = {newMinorFaction.RemainingLifeTime}, {newMinorFaction.MinorFactionStatus}, {newMinorFaction.MinorEmpireHomeStatus}");

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
			if (rebelFaction != null)
			{
				if (rebelFaction.Cities.Count > 0 && rebelFaction.RemainingLifeTime < 20 * defaultGameSpeedMultiplier)
				{
					// could be here because giving territories lowered the status / remaining life span ?
					// restore them
					rebelFaction.MinorFactionStatus = MinorFactionStatuses.Zenith; //.InDecline; // could use in decline to prevent bribing
					rebelFaction.RemainingLifeTime = 20 * defaultGameSpeedMultiplier;
				}
				Diagnostics.LogWarning($"[Gedemon] Final check {rebelFaction.FactionDefinition.Name} ID#{rebelFaction.Index}, RemainingLifeTime = {rebelFaction.RemainingLifeTime}, {rebelFaction.MinorFactionStatus}, {rebelFaction.MinorEmpireHomeStatus}");
			}
			#endregion
			
		}
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

				if (IsSleepingEmpire(potentialEmpire))
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

				if (potentialEmpire.Armies.Count == 0 && potentialEmpire.Settlements.Count == 0 && potentialEmpire.OccupiedCityCount.Value == 0 && (potentialEmpire.IsAlive || mustResurect))
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

                // Adding civics
                {
					int numCivics = majorEmpire.DepartmentOfDevelopment.Civics.Length;
					for (int c = 0; c < numCivics; c++)
					{
						Civic originalCivic = majorEmpire.DepartmentOfDevelopment.Civics[c];
						if (originalCivic.CivicStatus == CivicStatuses.Enacted || originalCivic.CivicStatus == CivicStatuses.Available)
						{
							int civicIndex = oldEmpire.DepartmentOfDevelopment.GetCivicIndex(originalCivic.CivicDefinition.Name);
							ref Civic newCivic = ref oldEmpire.DepartmentOfDevelopment.Civics.Data[civicIndex];
							int choiceIndex = newCivic.CivicDefinition.GetChoiceIndex(originalCivic.ActiveChoiceName);
							Diagnostics.LogWarning($"[Gedemon] Checking Civic {originalCivic.CivicDefinition.Name}, ActiveChoiceName = {originalCivic.ActiveChoiceName}, Spawned Empire civic status = {newCivic.CivicStatus}, Original Empire civic status = {originalCivic.CivicStatus}");
							if (originalCivic.CivicStatus == CivicStatuses.Enacted && newCivic.CivicStatus != CivicStatuses.Enacted)
							{
								oldEmpire.DepartmentOfDevelopment.ActiveCivic(civicIndex, choiceIndex, raiseSimulationEvents: true);
							}
							if (originalCivic.CivicStatus == CivicStatuses.Available && newCivic.CivicStatus != CivicStatuses.Available)
							{
								oldEmpire.DepartmentOfDevelopment.UnlockCivic(civicIndex, raiseSimulationEvents: true);
							}
						}
					}
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

			MajorEmpireExtension majorExt = MajorEmpireSaveExtension.GetExtension(majorEmpire.Index);
			majorExt.SpawnTurn = SandboxManager.Sandbox.Turn;

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
		public static void UpdateTerritoryLabels(int currentEraIndex)
		{
			ref Amplitude.Mercury.Terrain.TerrainLabel[] terrainLabels = ref Amplitude.Mercury.Presentation.Presentation.PresentationTerritoryHighlightController.territoryLabelsRenderer.terrainLabels;

			int numTerritories = terrainLabels.Length;
			for(int territoryIndex = 0; territoryIndex < numTerritories; territoryIndex++)
            {
				if(CultureUnlock.IsNextEraUnlock(territoryIndex, currentEraIndex))
                {
					//terrainLabels[territoryIndex].Text = "*" + CultureUnlock.GetTerritoryName(territoryIndex) + "*";
					terrainLabels[territoryIndex].OptionalColor = UnityEngine.Color.cyan;
				}
				else
				{
					//terrainLabels[territoryIndex].Text = CultureUnlock.GetTerritoryName(territoryIndex);
					terrainLabels[territoryIndex].OptionalColor = UnityEngine.Color.white;
				}
            }
		}
		public static void UpdateDistrictVisuals(Empire empire)
        {

			if (!TrueCultureLocation.KeepHistoricalDistricts())
				return;

			int count = empire.Settlements.Count;
			for (int m = 0; m < count; m++)
			{
				Settlement settlement = empire.Settlements[m];

				// 
				int count2 = settlement.Region.Entity.Territories.Count;
				for (int k = 0; k < count2; k++)
				{
					Territory territory = settlement.Region.Entity.Territories[k];
					District adminDistrict = territory.AdministrativeDistrict;

					if (CultureUnlock.HasTerritory(empire.FactionDefinition.Name.ToString(), territory.Index))
					{
						if (adminDistrict != null)
						{
							Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : update Administrative District visual in territory {adminDistrict.Territory.Entity.Index}.");
							adminDistrict.InitialVisualAffinityName = DepartmentOfTheInterior.GetInitialVisualAffinityFor(empire, adminDistrict.DistrictDefinition);
						}
						else
						{
							Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : no Administrative District in territory {adminDistrict.Territory.Entity.Index}.");
						}
					}
					else
					{
						// add instability here ?
						Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : PublicOrderCurrent = {settlement.PublicOrderCurrent.Value}, PublicOrderPositiveTrend = {settlement.PublicOrderPositiveTrend.Value}, PublicOrderNegativeTrend = {settlement.PublicOrderNegativeTrend.Value}, DistanceInTerritoryToCapital = {settlement.DistanceInTerritoryToCapital.Value}.");
						if (adminDistrict != null)
						{
							Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {adminDistrict.DistrictDefinition.Name} : PublicOrderProduced = {adminDistrict.PublicOrderProduced.Value}.");
							
						}

					}
				}
			}

		}
		public static void SaveHistoricDistrictVisuals(Empire empire)
		{

			if (!TrueCultureLocation.KeepHistoricalDistricts())
				return;

			int count = empire.Settlements.Count;
			for (int m = 0; m < count; m++)
			{
				Settlement settlement = empire.Settlements[m];
				int count2 = settlement.Region.Entity.Territories.Count;
				for (int k = 0; k < count2; k++)
				{
					Territory territory = settlement.Region.Entity.Territories[k];
					int count3 = territory.Districts.Count;
					for (int l = 0; l < count3; l++)
					{
						District district = territory.Districts[l];
						if(district.DistrictType != DistrictTypes.Exploitation)
						{
							int tileIndex = district.WorldPosition.ToTileIndex();
							if (CurrentGame.Data.HistoricVisualAffinity.TryGetValue(tileIndex, out DistrictVisual districtVisual))
							{
								//CurrentGame.Data.HistoricVisualAffinity[tileIndex] = districtVisual;
							}
							else
							{
								//Diagnostics.LogWarning($"[Gedemon] SaveHistoricDistrictVisuals in {CultureUnlock.GetTerritoryName(territory.Index)} for {district.DistrictDefinition.Name} : district.InitialVisualAffinityName = {district.InitialVisualAffinityName}, EraIndex = {(int)empire.EraLevel.Value}  (tile index = {district.WorldPosition.ToTileIndex()}) at {district.WorldPosition})");
								CurrentGame.Data.HistoricVisualAffinity.Add(tileIndex, new DistrictVisual { VisualAffinity = district.InitialVisualAffinityName, EraIndex = (int)empire.EraLevel.Value });
							}
						}
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
