using Amplitude.Mercury.Simulation;
using Amplitude;
using HarmonyLib;
using Amplitude.Framework;
using Amplitude.Mercury.Options;
using Amplitude.Mercury.Data.GameOptions;

namespace Gedemon.TrueCultureLocation
{
	[HarmonyPatch(typeof(CollectibleManager))]
	public class TCL_CollectibleManager
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
			var database1 = Databases.GetDatabase<Amplitude.Mercury.Data.Simulation.FactionDefinition>();
			foreach (Amplitude.Mercury.Data.Simulation.FactionDefinition data in database1)
			{
				//Diagnostics.LogWarning($"[Gedemon] FactionDefinition name = {data.name}");//, Name = {data.Name}");

				foreach (var prop in data.GetType().GetProperties())
				{
					Diagnostics.Log($"[Gedemon] {prop.Name} = {prop.GetValue(data, null)}");
				}
			}
			//*/

			// Log all factions
			var factionDefinitions = Databases.GetDatabase<Amplitude.Mercury.Data.Simulation.FactionDefinition>();
			foreach (Amplitude.Mercury.Data.Simulation.FactionDefinition data in factionDefinitions)
			{
				Diagnostics.LogWarning($"[Gedemon] FactionDefinition name = {data.name}, era = {data.EraIndex}");//, Name = {data.Name}");
			}

			// Log all options
			var gameOptionDefinitions = Databases.GetDatabase<GameOptionDefinition>();
			foreach (var option in gameOptionDefinitions)
			{
                IGameOptionsService gameOptions = Services.GetService<IGameOptionsService>();
                Diagnostics.LogWarning($"[Gedemon] gameOptions {option.name} = { gameOptions.GetOption(option.Name).CurrentValue}");
			}

		}
	}

}
