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
			//*/

			var gameOptionDefinitions = Databases.GetDatabase<GameOptionDefinition>();
			foreach (var option in gameOptionDefinitions)
			{
                IGameOptionsService gameOptions = Services.GetService<IGameOptionsService>();
                Diagnostics.LogWarning($"[Gedemon] gameOptions {option.name} = { gameOptions.GetOption(option.Name).CurrentValue}");
			}

		}
	}

}
