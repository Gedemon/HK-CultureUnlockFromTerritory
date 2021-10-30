using Amplitude.Mercury.Simulation;
using Amplitude;
using HarmonyLib;
using Amplitude.Framework;
using Amplitude.Framework.Input;
using Amplitude.Mercury.Data.Simulation;
using Amplitude.Mercury.Data.World;
using System.Linq;
using System.Collections.Generic;
using Amplitude.Mercury.Options;
using Amplitude.Mercury.Data.GameOptions;
using HumankindModTool;
using Amplitude.Mercury.Sandbox;

namespace Gedemon.TrueCultureLocation
{
	[HarmonyPatch(typeof(CollectibleManager))]
	public class CultureUnlock_CollectibleManager
	{
		[HarmonyPostfix]
		[HarmonyPatch(nameof(InitializeOnLoad))]
		public static void InitializeOnLoad(CollectibleManager __instance)
		{
			Diagnostics.LogWarning($"[Gedemon] in CollectibleManager, InitializeOnLoad");
			CultureUnlock.LogTerritoryStats();
			CultureUnlock.logEmpiresTerritories();

			// stability level	= PublicOrderEffectDefinition, EmpireStabilityDefinition
			// units			= PresentationUnitDefinition
			// cultures			= FactionDefinition
			// eras				= EraDefinition
			// 
			// BuildingVisualAffinityDefinition
			// UnitVisualAffinityDefinition
			/* 
			IDatabase<BuildingVisualAffinityDefinition> database1 = Databases.GetDatabase<BuildingVisualAffinityDefinition>();
			foreach (BuildingVisualAffinityDefinition data in database1)
			{
				Diagnostics.LogWarning($"[Gedemon] BuildingVisualAffinityDefinition name = {data.name}");//, Name = {data.Name}");

				foreach (var prop in data.GetType().GetProperties())
				{
					//Diagnostics.Log($"[Gedemon] {prop.Name} = {prop.GetValue(data, null)}");
				}
			}

			IDatabase<UnitVisualAffinityDefinition> database2 = Databases.GetDatabase<UnitVisualAffinityDefinition>();
			foreach (UnitVisualAffinityDefinition data in database2)
			{
				Diagnostics.LogWarning($"[Gedemon] UnitVisualAffinityDefinition name = {data.name}");//, Name = {data.Name}");

				foreach (var prop in data.GetType().GetProperties())
				{
					//Diagnostics.Log($"[Gedemon] {prop.Name} = {prop.GetValue(data, null)}");
				}
			}
			//*/

			var gameOptionDefinitions = Databases.GetDatabase<GameOptionDefinition>();
			foreach (var option in gameOptionDefinitions)
			{
                IGameOptionsService gameOptions = Services.GetService<IGameOptionsService>();
                Diagnostics.LogWarning($"[Gedemon] gameOptions {option.name} = { gameOptions.GetOption(option.Name).CurrentValue}");
			}


			// temporary fix on load for saves made before this is done on start
			int numSettlingEmpires = TrueCultureLocation.GetSettlingEmpireSlots();
			if (CultureUnlock.UseTrueCultureLocation() && numSettlingEmpires < Sandbox.NumberOfMajorEmpires)
			{
				Diagnostics.LogWarning($"[Gedemon] in CollectibleManager, InitializeOnLoad for Timeline, reseting globalEraThresholds for {numSettlingEmpires} Settling Empires / {Sandbox.NumberOfMajorEmpires} Major Empires");

				Sandbox.Timeline.globalEraThresholds[Sandbox.Timeline.StartingEraIndex] = Sandbox.Timeline.eraDefinitions[Sandbox.Timeline.StartingEraIndex].BaseGlobalEraThreshold * numSettlingEmpires;
				for (int l = Sandbox.Timeline.StartingEraIndex + 1; l <= Sandbox.Timeline.EndingEraIndex; l++)
				{
					Sandbox.Timeline.globalEraThresholds[l] = Sandbox.Timeline.globalEraThresholds[l - 1] + Sandbox.Timeline.eraDefinitions[l].BaseGlobalEraThreshold * numSettlingEmpires;
				}
			}
		}
	}

}
