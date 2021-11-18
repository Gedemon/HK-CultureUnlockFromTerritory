using System.Collections.Generic;
using Amplitude;
using Amplitude.Mercury;
using Amplitude.Mercury.Data.Simulation;
using Amplitude.Mercury.Sandbox;
using Amplitude.Mercury.Simulation;
using FailureFlags = Amplitude.Mercury.Simulation.FailureFlags;

namespace Gedemon.TrueCultureLocation
{
	class TerritoryChange
	{
		public List<Settlement> settlementLost = new List<Settlement>();									// City Settlements that are lost
		public IDictionary<Settlement, List<int>> citiesInitialTerritories = new Dictionary<Settlement, List<int>>();	// Cities initial list of attached territories
		IDictionary<int, List<int>> citiesFinalTerritories = new Dictionary<int, List<int>>();      // Cities final list of attached territories

		public List<int> newRebelsTerritories = new List<int>();                // list of Territory indexes for a rebel faction

		public List<int> newMajorTerritories = new List<int>();                // list of Territory indexes for a new major
		public List<Settlement> newMajorSettlements = new List<Settlement>();  // list of City Settlements for a new major

		public IDictionary<StaticString, List<Settlement>> newMinorsSettlements = new Dictionary<StaticString, List<Settlement>>();    // list City Settlement for each new minor factions
		public IDictionary<int, StaticString> newMinorsTerritories = new Dictionary<int, StaticString>();								// new minor factions for each territory

		public List<int> territoriesLost = new List<int>();
		public List<int> territoriesKept = new List<int>();
		public List<int> territoriesKeptFromLostCities = new List<int>();

		public List<StaticString> listNewFactions = new List<StaticString>(); // list all required new factions (rebels separated ?)

		public StaticString NextFactionName { get; set; } = StaticString.Empty;
		public StaticString OldFactionName { get; set; } = StaticString.Empty;
		public MajorEmpire MajorEmpire { get; set; } = null;
		public District PotentialCapital { get; set; } = null;
		public int NumCitiesLost { get; set; } = 0;

		public bool HasCapitalChanged { get; set; } = false;

		public TerritoryChange(MajorEmpire empire, StaticString nextFactionName)
		{
			Diagnostics.LogWarning($"[Gedemon] initialize TerritoryChanges");

			MajorEmpire = empire;
			NextFactionName = nextFactionName;
			OldFactionName = MajorEmpire.FactionDefinition.Name;

			listNewFactions.Add(OldFactionName);

			bool isHuman = TrueCultureLocation.IsEmpireHumanSlot(MajorEmpire.Index);
			bool keepOnlyCultureTerritory = TrueCultureLocation.KeepOnlyCultureTerritory();
			bool keepTerritoryAttached = TrueCultureLocation.KeepTerritoryAttached();
			bool capitalChanged = false; // the Capital hasn't been changed yet
			bool needNewCapital = false; // We need a new Capital (and we've not found it yet)

			if ((!isHuman) && TrueCultureLocation.NoTerritoryLossForAI())
			{
				keepOnlyCultureTerritory = false;
			}

			if (MajorEmpire.FactionDefinition.Name != nextFactionName)
			{
				if (MajorEmpire.DepartmentOfDevelopment.CurrentEraIndex != 0 && keepOnlyCultureTerritory)
				{
					#region Check Capital

					Diagnostics.Log($"[Gedemon] before Check for new Capital (exist = {(MajorEmpire.Capital.Entity != null)})");
					bool hasCapital = (MajorEmpire.Capital.Entity != null);

					if (hasCapital)
					{
						Settlement Capital = MajorEmpire.Capital;

						District capitalMainDistrict = Capital.GetMainDistrict();
						Diagnostics.LogWarning($"[Gedemon] before Check Capital Territory ({Capital.SettlementStatus} {Capital.EntityName} ({CultureUnlock.GetTerritoryName(capitalMainDistrict.Territory.Entity.Index)}) is current Capital)");
						needNewCapital = !CultureUnlock.HasCoreTerritory(nextFactionName.ToString(), capitalMainDistrict.Territory.Entity.Index);

						if (needNewCapital && keepTerritoryAttached)
						{
							int count2 = Capital.Region.Entity.Territories.Count;
							for (int k = 0; k < count2; k++)
							{
								Territory territory = Capital.Region.Entity.Territories[k];
								if (CultureUnlock.HasCoreTerritory(nextFactionName.ToString(), territory.Index))
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

						int numSettlement = MajorEmpire.Settlements.Count;

						Diagnostics.LogWarning($"[Gedemon] Searching new Capital in existing Cities first.");
						foreach (int territoryIndex in CultureUnlock.GetListTerritories(nextFactionName.ToString()))
						{
							for (int m = 0; m < numSettlement; m++)
							{
								Settlement settlement = MajorEmpire.Settlements[m];
								if (settlement.SettlementStatus == SettlementStatuses.City && (settlement.CityFlags & CityFlags.Captured) == 0)
								{
									District potentialDistrict = settlement.GetMainDistrict();
									if (territoryIndex == potentialDistrict.Territory.Entity.Index)
									{
										Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : register City for new Capital in territory {potentialDistrict.Territory.Entity.Index}.");
										needNewCapital = false;
										capitalChanged = true;
										PotentialCapital = potentialDistrict;
										goto FoundCapital;
									}
								}
							}
						}

						Diagnostics.LogWarning($"[Gedemon] New Capital not found, now searching in Settlement without cities for a potential Capital position.");
						foreach (int territoryIndex in CultureUnlock.GetListTerritories(nextFactionName.ToString()))
						{
							//Diagnostics.LogWarning($"[Gedemon] - check territory #{territoryIndex} ({CultureUnlock.GetTerritoryName(territoryIndex)})");
							for (int n = 0; n < numSettlement; n++)
							{
								Settlement settlement = MajorEmpire.Settlements[n];
								//Diagnostics.LogWarning($"[Gedemon] - check settlement #{n} (exist = {settlement != null})");
								if (settlement.SettlementStatus != SettlementStatuses.City)
								{
									District potentialDistrict = settlement.GetMainDistrict();
									//Diagnostics.LogWarning($"[Gedemon] - check main District (exist = {potentialDistrict != null})");
									if(potentialDistrict != null)
									{
										if (territoryIndex == potentialDistrict.Territory.Entity.Index)
										{
											Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : register Settlement to Create new Capital in territory {potentialDistrict.Territory.Entity.Index}.");
											needNewCapital = false;
											capitalChanged = true;
											PotentialCapital = potentialDistrict;
											goto FoundCapital;
										}
									}
									else
                                    {
										Diagnostics.LogError($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : GetMainDistrict returns null");
									}
								}
							}
						}
					FoundCapital:;
					}
					#endregion

					#region Sort Territories

					Diagnostics.Log($"[Gedemon] Sorting Territories");

					int count = MajorEmpire.Settlements.Count;
					for (int j = 0; j < count; j++)
					{
						Settlement settlement = MajorEmpire.Settlements[j];

						bool hasTerritoryFromNewCulture = false;

						int count2 = settlement.Region.Entity.Territories.Count;
						for (int k = 0; k < count2; k++)
						{
							Territory territory = settlement.Region.Entity.Territories[k];
							if (CultureUnlock.HasCoreTerritory(nextFactionName.ToString(), territory.Index))
								hasTerritoryFromNewCulture = true;
						}

						//settlement.PublicOrderCurrent.Value

						bool keepSettlement = (hasTerritoryFromNewCulture && keepTerritoryAttached) || ((settlement.CityFlags & CityFlags.Captured) != 0) ;

						if (!keepSettlement)
						{
							if (settlement.SettlementStatus == SettlementStatuses.City)
							{
								District mainDistrict = settlement.GetMainDistrict();
								int mainTerritoryIndex = mainDistrict.Territory.Entity.Index;

								bool giveCity = !CultureUnlock.HasCoreTerritory(nextFactionName.ToString(), mainTerritoryIndex);

								Diagnostics.LogWarning($"[Gedemon] Settlement ID#{j}: City {settlement.EntityName} of {CultureUnlock.GetTerritoryName(mainTerritoryIndex)}, checking territories, give city = {giveCity}");

								for (int k = 0; k < count2; k++)
								{
									Territory territory = settlement.Region.Entity.Territories[(count2 - 1) - k]; // start from last to avoid "CannotDetachConnectorTerritory"

									if (territory.Index != mainTerritoryIndex)
									{
										// add to initial territories per city list
										if (citiesInitialTerritories.ContainsKey(settlement))
										{
											citiesInitialTerritories[settlement].Add(territory.Index);
										}
										else
										{
											citiesInitialTerritories.Add(settlement, new List<int> { territory.Index });
										}

										if (CultureUnlock.HasCoreTerritory(nextFactionName.ToString(), territory.Index))
										{
											territoriesKept.Add(territory.Index);

											if (giveCity)
											{

												territoriesKeptFromLostCities.Add(territory.Index);
												FailureFlags flag = MajorEmpire.DepartmentOfTheInterior.CanDetachTerritoryFromCity(territory, settlement);
												if (flag == FailureFlags.None || flag == FailureFlags.CannotDetachConnectorTerritory)
												{
													Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : Add to territoriesKept and territoriesKeptFromLostCities for index = {territory.Index} ({CultureUnlock.GetTerritoryName(territory.Index)}), is in new Culture Territory but loosing City");

												}
												else
												{
													Diagnostics.LogError($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : Add to territoriesKept and territoriesKeptFromLostCities but FAILED on check detach for territory index = {territory.Index} ({CultureUnlock.GetTerritoryName(territory.Index)}), {MajorEmpire.DepartmentOfTheInterior.CanDetachTerritoryFromCity(territory, settlement)}, is new Culture Territory but loosing city");
												}
											}
											else
											{
												Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : Add to territoriesKept for index = {territory.Index} ({CultureUnlock.GetTerritoryName(territory.Index)}), is in new Culture Territory and keeping City");
											}
										}
										else
										{
											territoriesLost.Add(territory.Index);

											if (!giveCity)
											{
												FailureFlags flag = MajorEmpire.DepartmentOfTheInterior.CanDetachTerritoryFromCity(territory, settlement);
												if (flag == FailureFlags.None || flag == FailureFlags.CannotDetachConnectorTerritory)
												{
													Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : Add to territoriesLost for index = {territory.Index} ({CultureUnlock.GetTerritoryName(territory.Index)}), not in new Culture Territory and keeping city");

												}
												else
												{
													Diagnostics.LogError($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : Add to territoriesLost but check FAILED to detach for territory index = {territory.Index} ({CultureUnlock.GetTerritoryName(territory.Index)}), {MajorEmpire.DepartmentOfTheInterior.CanDetachTerritoryFromCity(territory, settlement)}, not in new Culture Territory and keeping city");
												}
											}
											else
											{
												Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : Add to territoriesLost for territory index = {territory.Index} ({CultureUnlock.GetTerritoryName(territory.Index)}), not in new Culture Territory, as is city");
											}
										}
									}
								}
								if (giveCity)
								{
									NumCitiesLost++;
									settlementLost.Add(settlement);
									FailureFlags flag = DepartmentOfTheInterior.CanLiberateSettlement(MajorEmpire, settlement);
									if (flag == FailureFlags.None || flag == FailureFlags.SettlementIsCapital)
									{
										Diagnostics.LogWarning($"[Gedemon] City {settlement.EntityName} of {CultureUnlock.GetTerritoryName(mainTerritoryIndex)} : Add to settlementLost");
									}
									else
									{
										Diagnostics.LogError($"[Gedemon] City {settlement.EntityName} of {CultureUnlock.GetTerritoryName(mainTerritoryIndex)} : Add to settlementLost but FAILED for CanLiberate check ({flag})");
									}
								}
							}
							else
							{
								Diagnostics.LogWarning($"[Gedemon] Settlement ID#{j}: Check {settlement.SettlementStatus} {settlement.EntityName} territory");

								if (settlement.SettlementStatus != SettlementStatuses.None)
								{
									Territory territory = settlement.Region.Entity.Territories[0];
									if (!CultureUnlock.HasCoreTerritory(nextFactionName.ToString(), territory.Index))
									{
										Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : Add to territoriesLost for index = {territory.Index} ({CultureUnlock.GetTerritoryName(territory.Index)}), not city, not in new Culture Territory");
										territoriesLost.Add(territory.Index);
									}
									else
									{
										Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : Add to territoriesKept for index = {territory.Index} ({CultureUnlock.GetTerritoryName(territory.Index)}), not city, is in new Culture Territory");
										territoriesKept.Add(territory.Index);
									}
								}
							}
						}
					}
					#endregion

					#region Cities distribution

					Diagnostics.Log($"[Gedemon] Cities distribution");

					foreach (Settlement settlement in settlementLost)
					{
						District mainDistrict = settlement.GetMainDistrict();
						int territoryIndex = mainDistrict.Territory.Entity.Index;

						Diagnostics.LogWarning($"[Gedemon] Check if the city territory is part of the Old Empire...");
						// Check if the city territory is part of the Old Empire
						if (CultureUnlock.HasTerritory(OldFactionName, territoryIndex))
						{
							Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} {CultureUnlock.GetTerritoryName(territoryIndex)}: Add to newMajorSettlements for replacing Empire");
							newMajorSettlements.Add(settlement);
							continue;
						}

						Diagnostics.LogWarning($"[Gedemon] Check for MinorFaction...");
						// Check for MinorFaction
						if (CultureUnlock.HasAnyMinorFactionPosition(territoryIndex))
						{
							foreach (string minorFactionName in CultureUnlock.GetListMinorFactionsForTerritory(territoryIndex))
							{
								Diagnostics.LogWarning($"[Gedemon] - check {minorFactionName}");
								StaticString factionName = new StaticString(minorFactionName);
								FactionDefinition factionDefinition = Utils.GameUtils.GetFactionDefinition(factionName);
								if (factionDefinition!=null)
								{
									int minEraIndex = MajorEmpire.DepartmentOfDevelopment.CurrentEraIndex;
									int maxEraIndex = Sandbox.Timeline.GetGlobalEraIndex();
									if (minEraIndex > maxEraIndex)
									{
										minEraIndex = maxEraIndex;
									}
									if (factionDefinition.EraIndex >= minEraIndex && factionDefinition.EraIndex <= maxEraIndex && !newMinorsSettlements.ContainsKey(factionName))
									{
										Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} {CultureUnlock.GetTerritoryName(territoryIndex)}: Add to newMinorsSettlements for {factionName}");
										newMinorsSettlements.Add(factionName, new List<Settlement> { settlement });
										listNewFactions.Add(factionName);
										goto FoundFactionForCity;
									}
								}
								else
								{
									Diagnostics.LogWarning($"[Gedemon] - factionDefinition is null...");

								}
							}
						}

						Diagnostics.LogWarning($"[Gedemon] Check for MajorFactionDefinition (to use with a Minor)...");
						// Check for MajorFactionDefinition (to use with a Minor)
						if (CultureUnlock.HasAnyMajorEmpirePosition(territoryIndex))
						{
							foreach (string majorFactionName in CultureUnlock.GetListMajorEmpiresForTerritory(territoryIndex))
							{
								Diagnostics.LogWarning($"[Gedemon] - check {majorFactionName}");
								StaticString factionName = new StaticString(majorFactionName);
								FactionDefinition factionDefinition = Utils.GameUtils.GetFactionDefinition(factionName);
								if (factionDefinition != null)
								{
									if (factionDefinition.EraIndex >= MajorEmpire.DepartmentOfDevelopment.CurrentEraIndex && factionDefinition.EraIndex <= Sandbox.Timeline.GetGlobalEraIndex())
									{
										Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : Add to newMinorsSettlements for {factionName}");
										if (newMinorsSettlements.ContainsKey(factionName))
										{
											newMinorsSettlements[factionName].Add(settlement);
										}
										else
										{
											newMinorsSettlements.Add(factionName, new List<Settlement> { settlement });
										}
										if (!listNewFactions.Contains(factionName))
										{
											listNewFactions.Add(factionName);
										}
										goto FoundFactionForCity;
									}

								}
								else
								{
									Diagnostics.LogWarning($"[Gedemon] - factionDefinition is null...");

								}
							}
						}

						Diagnostics.LogWarning($"[Gedemon] Add to the rebels faction...");
						// Add to the rebels faction
						Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : Add to newMinorsSettlements for {OldFactionName}");
						if (newMinorsSettlements.ContainsKey(OldFactionName))
						{
							newMinorsSettlements[OldFactionName].Add(settlement);
						}
						else
						{
							newMinorsSettlements.Add(OldFactionName, new List<Settlement> { settlement });
						}
					FoundFactionForCity:;
					}
					#endregion

					#region Territories Distribution

					Diagnostics.Log($"[Gedemon] Territories distribution");

					foreach (int territoryIndex in territoriesLost)
                    {
						// check if the territory is part of the Old Empire
						if (CultureUnlock.HasTerritory(OldFactionName, territoryIndex))
                        {
							Diagnostics.LogWarning($"[Gedemon] {CultureUnlock.GetTerritoryName(territoryIndex)} (ID #{territoryIndex}): Add to newMajorTerritories for replacing Empire");
							newMajorTerritories.Add(territoryIndex);
							continue;
						}

						// check if the territory is part of the new minor factions
						foreach(StaticString factionName in listNewFactions)
						{
							if (CultureUnlock.HasTerritory(factionName, territoryIndex))
							{
								Diagnostics.LogWarning($"[Gedemon] {CultureUnlock.GetTerritoryName(territoryIndex)} (ID #{territoryIndex}): Add to newMinorsTerritories for {factionName}");
								newMinorsTerritories.Add(territoryIndex, factionName);
								goto FoundFaction;
							}
						}

						// add to rebels list
						Diagnostics.LogWarning($"[Gedemon] {CultureUnlock.GetTerritoryName(territoryIndex)} (ID #{territoryIndex}): Add to newRebelsTerritories");
						newRebelsTerritories.Add(territoryIndex);

					FoundFaction:;
					}

                    #endregion
                }
            }

            HasCapitalChanged = needNewCapital || capitalChanged;
		}
	}
}
