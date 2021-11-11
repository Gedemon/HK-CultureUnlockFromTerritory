using Amplitude.Mercury.Simulation;
using HarmonyLib;
using Amplitude.Mercury.Sandbox;
using Amplitude;
using Amplitude.Mercury;
using Amplitude.Mercury.Data.Simulation;
using System;
using Amplitude.Mercury.Interop;
using System.Collections.Generic;

namespace Gedemon.TrueCultureLocation
{


	//*
	[HarmonyPatch(typeof(BaseHumanMinorFactionSpawner<>))]
	public class BaseHumanMinorFactionSpawner_Patch
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
}
