using System.Collections.Generic;
using Amplitude.Mercury.Simulation;
using Amplitude;
using HarmonyLib;
using Amplitude.Mercury.Interop;
using Amplitude.Mercury.Data.Simulation;
using Amplitude.Mercury.Sandbox;
using FailureFlags = Amplitude.Mercury.Simulation.FailureFlags;
using HumankindModTool;
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

			MajorEmpire majorEmpire = __instance.majorEmpire;
			StaticString nextFactionName = __instance.nextFactionName;

			if (majorEmpire.DepartmentOfDevelopment.CurrentEraIndex != 0)
			{
				CultureChange.SaveHistoricDistrictVisuals(majorEmpire);

				if (CultureUnlock.UseTrueCultureLocation())
				{
					TradeRoute.StartListingTradeRouteToRestore();

					if (majorEmpire.FactionDefinition.Name != nextFactionName)
					{
						CultureChange.DoFactionChange(majorEmpire, nextFactionName);
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

				CultureChange.UpdateDistrictVisuals(majorEmpire);
				CultureChange.SetFactionSymbol(majorEmpire);

				TradeRoute.RestoreTradeRoutes();
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

						if (settlement.SettlementStatus == SettlementStatuses.BeingSettled)
						{
							continue;
						}

						if ((settlement.CityFlags & CityFlags.Captured) == 0)
						{
							int count2 = settlement.Region.Entity.Territories.Count;
							for (int k = 0; k < count2; k++)
							{
								Amplitude.Mercury.Simulation.Territory territory = settlement.Region.Entity.Territories[k];
								bool anyTerritory = majorEmpire.DepartmentOfDevelopment.CurrentEraIndex == 0 || CultureUnlock.HasNoCapitalTerritory(factionName);
								bool validSettlement = (TrueCultureLocation.GetEraIndexCityRequiredForUnlock() > majorEmpire.DepartmentOfDevelopment.CurrentEraIndex + 1 || settlement.SettlementStatus == SettlementStatuses.City || CultureUnlock.IsNomadCulture(majorEmpire.FactionDefinition.name));
								if (CultureUnlock.HasCoreTerritory(factionName, territory.Index, anyTerritory))
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
								if (CultureUnlock.HasCoreTerritory(factionName, territory.Index))
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
					FixedPoint defaultGameSpeedMultiplier = Sandbox.GameSpeedController.CurrentGameSpeedDefinition.DefaultGameSpeedMultiplier;
					FixedPoint knowledgeUnlock = CultureUnlock.knowledgeForBackupCiv * defaultGameSpeedMultiplier;
					if (majorEmpire.KnowledgeStock.Value >= knowledgeUnlock && CultureUnlock.IsFirstEraBackupCivilization(factionName))
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
