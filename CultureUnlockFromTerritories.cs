using BepInEx;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amplitude.Mercury.Simulation;
using Amplitude;
using HarmonyLib;
using System.Reflection;
using Amplitude.Mercury.Options;
using Amplitude.Mercury.Interop;
using Amplitude.Mercury.Data.Simulation;
using Amplitude.Mercury.Sandbox;
using FailureFlags = Amplitude.Mercury.Simulation.FailureFlags;
using Amplitude.Framework;

namespace CultureUnlockFromTerritories
{
    [BepInPlugin(pluginGuid, "Culture Unlock From Territories", "1.0.0.2")]
    public class CultureUnlockFromTerritories : BaseUnityPlugin
    {
		public const string pluginGuid = "gedemon.humankind.cultureunlockfromterritories";

		private static ConfigEntry<bool> keepOnlyCultureTerritory;
		private static ConfigEntry<bool> keepTerritoryAttached;
		private static ConfigEntry<bool> outpostCanUnlockTerritory;
		private static ConfigEntry<bool> limitDecisionForAI;

		// Awake is called once when both the game and the plug-in are loaded
		void Awake()
        {
			UnityEngine.Debug.Log("Starting initialization !");
			CultureUnlock.LogElevationHash();

			keepOnlyCultureTerritory = Config.Bind("General",
									"KeepOnlyCultureTerritory",
									true,
									"Toggle to set if Empires will keep only the territories of a new Culture and liberate the other Territories");

			keepTerritoryAttached = Config.Bind("General",
									"KeepTerritoryAttached",
									true,
									"Toggle to set if Territories that are attached to a Settlement that has at least one territory belonging to the new Culture will not be detached and kept in the Empire, even when KeepOnlyCultureTerritory is active");

			outpostCanUnlockTerritory = Config.Bind("General",
									"OutpostCanUnlockTerritory",
									true,
									"Toggle to set if an outpost is enough to unlock a Culture, or if the new Culture Capital Territory needs to be a City (or attached to a City) of that Empire");

			limitDecisionForAI = Config.Bind("General",
									"LimitDecisionForAI",
									true,
									"Toggle to limit AI Empires to Culture choices that doesn't result in a big territory loss");

			Harmony harmony = new Harmony(pluginGuid);
            Instance = this;
            harmony.PatchAll();
        }
        public static CultureUnlockFromTerritories Instance;

		public static bool KeepOnlyCultureTerritory()
        {
			return keepOnlyCultureTerritory.Value;
		}
		public static bool KeepTerritoryAttached()
		{
			return keepTerritoryAttached.Value;
		}
		public static bool OutpostCanUnlockTerritory()
		{
			return outpostCanUnlockTerritory.Value;
		}
		public static bool LimitDecisionForAI()
		{
			return limitDecisionForAI.Value;
		}
	}
    [HarmonyPatch(typeof(CivilizationsManager))]
    public class UnlockCultureFromTerritories_CivilizationsManager
    {
        [HarmonyPatch(nameof(IsLockedBy))]
        [HarmonyPrefix]
        public static bool IsLockedBy(CivilizationsManager __instance, ref int __result, StaticString factionName)
        {
            if (CultureUnlock.IsGiantEarthMap() && CultureUnlock.HasNoCapitalTerritory(factionName.ToString()))
            {
                __result = -1;
                return false;
            }
            else
            {
                return true;
            }
        }

        [HarmonyPatch(nameof(LockFaction))]
        [HarmonyPrefix]
        public static bool LockFaction(CivilizationsManager __instance, StaticString factionName, int lockingEmpireIndex)
        {
            if (CultureUnlock.IsGiantEarthMap() && CultureUnlock.HasNoCapitalTerritory(factionName.ToString()))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
    [HarmonyPatch(typeof(DepartmentOfDevelopment))]
    public class UnlockCultureFromTerritories_DepartmentOfDevelopment
    {
		[HarmonyPrefix]
		[HarmonyPatch(nameof(ApplyFactionChange))]
		public static void ApplyFactionChange(DepartmentOfDevelopment __instance)
		{
			if (CultureUnlock.IsGiantEarthMap())
			{ 
				IDictionary<Settlement, List<int>> territoryToDetachAndCreate = new Dictionary<Settlement, List<int>>();
				IDictionary<Settlement, List<int>> territoryToDetachAndFree = new Dictionary<Settlement, List<int>>();
				List<Settlement> settlementToLiberate = new List<Settlement>();
				List<Settlement> settlementToFree = new List<Settlement>();

				MajorEmpire majorEmpire = __instance.majorEmpire;
				StaticString nextFactionName = __instance.nextFactionName;

				bool keepOnlyCultureTerritory = CultureUnlockFromTerritories.KeepOnlyCultureTerritory();
				bool keepTerritoryAttached = CultureUnlockFromTerritories.KeepTerritoryAttached();

				// Territory check on Culture Change after Neolithic
				if (majorEmpire.DepartmentOfDevelopment.CurrentEraIndex != 0 && majorEmpire.FactionDefinition.Name != nextFactionName && keepOnlyCultureTerritory)
				{
					Diagnostics.LogWarning($"[Gedemon] in ApplyFactionChange, {majorEmpire.PersonaName} is changing faction from  {majorEmpire.FactionDefinition.Name} to {nextFactionName}");

					// relocate capital first, if needed
					Settlement Capital = majorEmpire.Capital;
					District capitalMainDistrict = Capital.GetMainDistrict();
					bool needNewCapital = !CultureUnlock.HasTerritory(nextFactionName.ToString(), capitalMainDistrict.Territory.Entity.Index);

					if (needNewCapital)
					{
						// need to find new Capital !
						Diagnostics.LogWarning($"[Gedemon] {Capital.SettlementStatus} {Capital.EntityName} : Is Capital, need to find new Capital.");

						// check existing settlements first
						int count4 = majorEmpire.Settlements.Count;
						for (int m = 0; m < count4; m++)
						{
							Settlement potentialCapital = majorEmpire.Settlements[m];
							if (potentialCapital.SettlementStatus == SettlementStatuses.City)
							{
								District potentialDistrict = potentialCapital.GetMainDistrict();
								if (CultureUnlock.HasTerritory(nextFactionName.ToString(), potentialDistrict.Territory.Entity.Index))
								{
									Diagnostics.LogWarning($"[Gedemon] {potentialCapital.SettlementStatus} {potentialCapital.EntityName} : check to set new Capital in territory {potentialDistrict.Territory.Entity.Index}.");
									majorEmpire.DepartmentOfTheInterior.SetCapital(potentialCapital, set: true);
									majorEmpire.TurnWhenLastCapitalChanged = SandboxManager.Sandbox.Turn;
									majorEmpire.CapturedCapital.SetEntity(null);
									SimulationEvent_CapitalChanged.Raise(__instance, potentialCapital, Capital);
									needNewCapital = false;
								}
							}
						}

						if (needNewCapital)
						{
							int count5 = majorEmpire.Settlements.Count;
							for (int n = 0; n < count5; n++)
							{
								Settlement potentialCapital = majorEmpire.Settlements[n];
								if (potentialCapital.SettlementStatus != SettlementStatuses.City)
								{
									District potentialDistrict = potentialCapital.GetMainDistrict();
									if (CultureUnlock.HasTerritory(nextFactionName.ToString(), potentialDistrict.Territory.Entity.Index))
									{
										Diagnostics.LogWarning($"[Gedemon] {potentialCapital.SettlementStatus} {potentialCapital.EntityName} : check to set new Capital in territory {potentialDistrict.Territory.Entity.Index}.");

										Settlement newCapital = majorEmpire.DepartmentOfTheInterior.CreateCityAt(majorEmpire.GUID, potentialDistrict.WorldPosition);

										majorEmpire.DepartmentOfTheInterior.SetCapital(newCapital, set: true);
										majorEmpire.TurnWhenLastCapitalChanged = SandboxManager.Sandbox.Turn;
										majorEmpire.CapturedCapital.SetEntity(null);
										SimulationEvent_CapitalChanged.Raise(__instance, newCapital, Capital);
										needNewCapital = false;
									}
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
													Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : Try to detach and create for territory index = {territory.Index}, is in new Culture Territory but loosing City");

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
													Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : Try to detach for territory index = {territory.Index}, not in new Culture Territory and keeping city");

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
									if (DepartmentOfTheInterior.CanLiberateSettlement(majorEmpire, settlement) == FailureFlags.None)
									{
										Diagnostics.LogWarning($"[Gedemon] City {settlement.EntityName} : Try to Liberate");
										settlementToLiberate.Add(settlement);
									}
									else
									{
										Diagnostics.LogWarning($"[Gedemon] City {settlement.EntityName} : Can't Liberate ({DepartmentOfTheInterior.CanLiberateSettlement(majorEmpire, settlement)}), try to Free if new capital was found (need capital = {needNewCapital})");
										if (!needNewCapital)
										{
											settlementToFree.Add(settlement);
										}
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
										Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : Try to free for territory index = {territory.Index}, not city, not in new Culture Territory");
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

				int influenceRefund = 0;

				foreach (KeyValuePair<Settlement, List<int>> kvp in territoryToDetachAndCreate)
				{
					foreach (int territoryIndex in kvp.Value)
					{
						influenceRefund += 30 + (50 * majorEmpire.Settlements.Count);
						DepartmentOfTheInterior.DetachTerritoryFromCity(kvp.Key, territoryIndex, createNewSettlement: true);
					}
				}

				foreach (KeyValuePair<Settlement, List<int>> kvp in territoryToDetachAndFree)
				{
					foreach (int territoryIndex in kvp.Value)
					{
						influenceRefund += 30 + (50 * majorEmpire.Settlements.Count);
						DepartmentOfTheInterior.DetachTerritoryFromCity(kvp.Key, territoryIndex, createNewSettlement: false);
					}
				}

				foreach (Settlement settlement in settlementToLiberate)
				{
					influenceRefund += 30 + (50 * majorEmpire.Settlements.Count);
					CultureUnlock.DoLiberateSettlement(settlement, majorEmpire);
				}

				foreach (Settlement settlement in settlementToFree) // districtToVisualupdate
				{
					influenceRefund += 30 + (50 * majorEmpire.Settlements.Count);
					majorEmpire.DepartmentOfTheInterior.FreeSettlement(settlement);
				}

				majorEmpire.DepartmentOfCulture.GainInfluence(influenceRefund);
		}
		}

		[HarmonyPostfix]
		[HarmonyPatch(nameof(ApplyFactionChange))]
		public static void ApplyFactionChangePost(DepartmentOfDevelopment __instance)
		{
			MajorEmpire majorEmpire = __instance.majorEmpire;

			int count = majorEmpire.Settlements.Count;
			for (int m = 0; m < count; m++)
			{
				Settlement settlement = majorEmpire.Settlements[m];
				if (settlement.SettlementStatus == SettlementStatuses.City)
				{
					District district = settlement.GetMainDistrict();
					if (CultureUnlock.HasTerritory(majorEmpire.FactionDefinition.Name.ToString(), district.Territory.Entity.Index))
					{
						Diagnostics.LogWarning($"[Gedemon] {settlement.SettlementStatus} {settlement.EntityName} : update city center visual in territory {district.Territory.Entity.Index}.");
						district.InitialVisualAffinityName = DepartmentOfTheInterior.GetInitialVisualAffinityFor(majorEmpire, district.DistrictDefinition);
					}
				}
			}

			IDatabase<EmpireSymbolDefinition> database = Databases.GetDatabase<EmpireSymbolDefinition>();
			foreach (EmpireSymbolDefinition symbol in database)
			{
				//Diagnostics.LogWarning($"[Gedemon] {symbol.Name} {symbol.Name.ToString().Length} == {majorEmpire.FactionDefinition.Name} {majorEmpire.FactionDefinition.Name.ToString().Length} ??");
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

			/*
			IDatabase<FactionDefinition> database2 = Databases.GetDatabase<FactionDefinition>();
			foreach (FactionDefinition faction in database2)
			{
				Diagnostics.LogWarning($"[Gedemon] FactionDefinition {faction.name} {faction.Name} ");
				foreach (string settlementName in faction.LocalizedSettlementNames)
				{
					Diagnostics.LogWarning($"[Gedemon] settlementName {settlementName}");
				}
			}
			*/

			//IDatabase<MinorFactionEraDefinition> database = Databases.GetDatabase<MinorFactionEraDefinition>(instantiateNewDatabaseUponFailure: true);

			Diagnostics.LogWarning($"[Gedemon] Symbol == {majorEmpire.EmpireSymbolDefinition.Name}");

			Amplitude.Mercury.Sandbox.Sandbox.SimulationEntityRepository.SetSynchronizationDirty(__instance.Empire);
		}

		[HarmonyPrefix]
		[HarmonyPatch(nameof(ComputeFactionStatus))]
		public static bool ComputeFactionStatus(DepartmentOfDevelopment __instance, ref FactionStatus __result, FactionDefinition factionDefinition)
		{
			MajorEmpire majorEmpire = __instance.majorEmpire;
			StaticString nextFactionName = __instance.nextFactionName;

			FactionStatus factionStatus = FactionStatus.Unlocked;

			/* Gedemon <<<<< */
			bool IsGiantEarth = CultureUnlock.IsGiantEarthMap();

			if (IsGiantEarth)
			{
				bool lockedByTerritory = true;
				bool lockedByStartingSlot = true;

				Diagnostics.Log($"[Gedemon] in ComputeFactionStatus, {majorEmpire.PersonaName} (ID={majorEmpire.Index}, EraStars ={majorEmpire.EraStarsCount.Value}/{majorEmpire.DepartmentOfDevelopment.CurrentEraStarRequirement}, knw={majorEmpire.KnowledgeStock.Value}, pop={majorEmpire.SumOfPopulationAndUnits.Value}) from {majorEmpire.FactionDefinition.Name} check to unlock {factionDefinition.Name}");

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
							bool validSettlement = (CultureUnlockFromTerritories.OutpostCanUnlockTerritory() || settlement.SettlementStatus == SettlementStatuses.City);
							if (CultureUnlock.HasTerritory(civilizationName, territory.Index, anyTerritory) && validSettlement)
							{
								Diagnostics.Log($"[Gedemon] in ComputeFactionStatus, {majorEmpire.PersonaName} has Territory unlock for {factionDefinition.Name} from Territory ID = {territory.Index}");
								lockedByTerritory = false;
								break;
							}
						}
					}


					// Check for AI Decision control
					if ((!majorEmpire.IsControlledByHuman) && CultureUnlockFromTerritories.LimitDecisionForAI() && !lockedByTerritory)
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

							bool keepSettlement = hasTerritoryFromNewCulture && CultureUnlockFromTerritories.KeepTerritoryAttached();

							if (!keepSettlement)
							{
								territoriesLost += territoriesRemovedFromSettlement;
							}
						}

						if (territoriesLost > territoriesCount * 0.5)
						{
							Diagnostics.Log($"[Gedemon] in ComputeFactionStatus, AI limitation from territory loss = {territoriesLost} / {territoriesCount}");
							lockedByTerritory = true;
						}
					}

				}
				if (CultureUnlock.IsUnlockedByPlayerSlot(civilizationName, majorEmpire.Index))
				{
					lockedByStartingSlot = false;
					Diagnostics.Log($"[Gedemon] in ComputeFactionStatus, {majorEmpire.PersonaName} has Starting Slot unlock for {factionDefinition.Name} from majorEmpire.Index = {majorEmpire.Index}");
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
				if (CultureUnlock.IsGiantEarthMap() && CultureUnlock.HasNoCapitalTerritory(nextFactionName.ToString()))
				{
					Diagnostics.Log($"[Gedemon] in LockedByYou check, nextFactionName = {nextFactionName}, factionDefinition.Name = {factionDefinition.Name}, factionStatus = {factionStatus}");
				}
				else
				{
					factionStatus = ((!(factionDefinition.Name == nextFactionName)) ? (factionStatus | FactionStatus.LockedByEra) : (factionStatus | FactionStatus.LockedByYou));
					Diagnostics.Log($"[Gedemon] in LockedByYou check, nextFactionName = {nextFactionName}, factionDefinition.Name = {factionDefinition.Name}, factionStatus = {factionStatus}");
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
			Diagnostics.Log($"[Gedemon] in ComputeFactionStatus, {majorEmpire.PersonaName} faction status for {factionDefinition.Name} is {factionStatus}");
			/* Gedemon >>>>> */
			__result = factionStatus;
			return false;
		}
	}

}
