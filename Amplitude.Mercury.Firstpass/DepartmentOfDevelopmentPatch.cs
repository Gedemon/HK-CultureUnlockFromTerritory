
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
				IDictionary<Settlement, List<int>> territoryToDetachAndCreate = new Dictionary<Settlement, List<int>>();	// Territories attached to a City that we want to detach and keep separated because the city is going to be lost
				IDictionary<Settlement, List<int>> territoryToDetachAndFree = new Dictionary<Settlement, List<int>>();      // Territories attached to a City that we want to detach because they are going to be lost, but not the city
				List<Settlement> settlementToLiberate = new List<Settlement>();												// Full Settlements (including a City or being a single-territory City) that are lost
				List<Settlement> settlementToFree = new List<Settlement>();													// Single Settlement (not a city) that are lost

				MajorEmpire majorEmpire = __instance.majorEmpire;
				MajorEmpire oldEmpire = null;
				StaticString nextFactionName = __instance.nextFactionName;
				Settlement capital = __instance.majorEmpire.Capital;
				Settlement potentialCapital;// = capital;

				bool useMajorEmpire = false;
				bool capitalChanged = CultureChange.GetTerritoryChangesOnEvolve(__instance, out potentialCapital, ref territoryToDetachAndCreate, ref territoryToDetachAndFree, ref settlementToLiberate, ref settlementToFree);

				Diagnostics.LogWarning($"[Gedemon] territoryToDetachAndCreate.Count = {territoryToDetachAndCreate.Count}, territoryToDetachAndFree.Count = {territoryToDetachAndFree.Count}, settlementToLiberate.Count = {settlementToLiberate.Count}, settlementToFree.Count = {settlementToFree.Count}.");
				if (settlementToLiberate.Count > 0)
				{
					useMajorEmpire = CultureChange.TryInitializeFreeMajorEmpireToReplace(majorEmpire, out oldEmpire);
				}

				if (capitalChanged)
                {
					int territoryIndex = potentialCapital.GetMainDistrict().Territory.Entity.Index;
					Diagnostics.LogWarning($"[Gedemon] {potentialCapital.SettlementStatus} {potentialCapital.EntityName} : try to set City as Capital in territory #{territoryIndex} ({CultureUnlock.GetTerritoryName(territoryIndex)}).");
					if (potentialCapital.SettlementStatus != SettlementStatuses.City)
                    {
						majorEmpire.DepartmentOfTheInterior.CreateCityAt(majorEmpire.GUID, potentialCapital.GetMainDistrict().WorldPosition);
					}
					majorEmpire.DepartmentOfTheInterior.SetCapital(potentialCapital, set: true);
					majorEmpire.TurnWhenLastCapitalChanged = SandboxManager.Sandbox.Turn;
					majorEmpire.CapturedCapital.SetEntity(null);
					SimulationEvent_CapitalChanged.Raise(__instance, potentialCapital, capital);
				}

				int influenceRefund = 0;
				MinorEmpire rebelFaction = null;

				foreach (KeyValuePair<Settlement, List<int>> kvp in territoryToDetachAndCreate)
				{
					foreach (int territoryIndex in kvp.Value)
					{
						Diagnostics.LogWarning($"[Gedemon] territoryToDetachAndCreate => #{territoryIndex} ({CultureUnlock.GetTerritoryName(territoryIndex)}).");
						influenceRefund += 30 + (50 * majorEmpire.Settlements.Count);
						DepartmentOfTheInterior.DetachTerritoryFromCity(kvp.Key, territoryIndex, createNewSettlement: true);
					}
				}

				foreach (KeyValuePair<Settlement, List<int>> kvp in territoryToDetachAndFree)
				{
					foreach (int territoryIndex in kvp.Value)
					{
						Diagnostics.LogWarning($"[Gedemon] territoryToDetachAndCreate => #{territoryIndex} ({CultureUnlock.GetTerritoryName(territoryIndex)}).");
						influenceRefund += 30 + (50 * majorEmpire.Settlements.Count);
						Settlement settlement = DepartmentOfTheInterior.DetachTerritoryFromCity(kvp.Key, territoryIndex, createNewSettlement: true);
						if(useMajorEmpire)
                        {
							DepartmentOfDefense.GiveSettlementTo(settlement, oldEmpire);
						}
						else
                        {
							if (rebelFaction == null)
							{
								rebelFaction = CultureChange.DoLiberateSettlement(settlement, majorEmpire);
							}
							else
							{
								DepartmentOfDefense.GiveSettlementTo(settlement, rebelFaction);
							}
						}
					}
				}

				foreach (Settlement settlement in settlementToLiberate)
				{
					Diagnostics.LogWarning($"[Gedemon] settlementToLiberate => #{settlement.Region.Entity.Territories[0].Index} ({CultureUnlock.GetTerritoryName(settlement.Region.Entity.Territories[0].Index)}).");
					influenceRefund += 30 + (50 * majorEmpire.Settlements.Count);
					if (useMajorEmpire)
					{
						DepartmentOfDefense.GiveSettlementTo(settlement, oldEmpire);
					}
					else
					{
						if (rebelFaction == null)
						{
							rebelFaction = CultureChange.DoLiberateSettlement(settlement, majorEmpire);
						}
						else
						{
							DepartmentOfDefense.GiveSettlementTo(settlement, rebelFaction);
						}
					}
				}

				foreach (Settlement settlement in settlementToFree) // districtToVisualupdate
				{
					Diagnostics.LogWarning($"[Gedemon] settlementToFree => #{settlement.Region.Entity.Territories[0].Index} ({CultureUnlock.GetTerritoryName(settlement.Region.Entity.Territories[0].Index)}).");
					influenceRefund += 30 + (50 * majorEmpire.Settlements.Count);
					if (useMajorEmpire)
					{
						DepartmentOfDefense.GiveSettlementTo(settlement, oldEmpire);
					}
					else
					{
						if (rebelFaction == null)
						{
							rebelFaction = CultureChange.DoLiberateSettlement(settlement, majorEmpire);
						}
						else
						{
							DepartmentOfDefense.GiveSettlementTo(settlement, rebelFaction);
						}
					}
				}

				majorEmpire.DepartmentOfCulture.GainInfluence(influenceRefund);

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

				string civilizationName = factionDefinition.Name.ToString();
				if (CultureUnlock.HasTerritory(civilizationName))
				{
					int count = majorEmpire.Settlements.Count;

					for (int i = 0; i < count; i++)
					{
						Settlement settlement = majorEmpire.Settlements[i];

						int count2 = settlement.Region.Entity.Territories.Count;
						for (int k = 0; k < count2; k++)
						{
							Amplitude.Mercury.Simulation.Territory territory = settlement.Region.Entity.Territories[k];
							bool anyTerritory = majorEmpire.DepartmentOfDevelopment.CurrentEraIndex == 0 || CultureUnlock.HasNoCapitalTerritory(civilizationName);
							bool validSettlement = (TrueCultureLocation.GetEraIndexCityRequiredForUnlock() > majorEmpire.DepartmentOfDevelopment.CurrentEraIndex + 1 || settlement.SettlementStatus == SettlementStatuses.City);
							if (CultureUnlock.HasTerritory(civilizationName, territory.Index, anyTerritory))
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
								if (CultureUnlock.HasTerritory(civilizationName, territory.Index))
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
				if (CultureUnlock.IsUnlockedByPlayerSlot(civilizationName, majorEmpire.Index))
				{
					lockedByStartingSlot = false;
					//Diagnostics.Log($"[Gedemon] in ComputeFactionStatus, {majorEmpire.PersonaName} has Starting Slot unlock for {factionDefinition.Name} from majorEmpire.Index = {majorEmpire.Index}");
				}

				if (lockedByTerritory && lockedByStartingSlot)
				{
					// unlock "backup" Culture after some time
					bool backupUnlocked = false;
					if (majorEmpire.KnowledgeStock.Value >= CultureUnlock.knowledgeForBackupCiv && CultureUnlock.IsFirstEraBackupCivilization(civilizationName))
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
