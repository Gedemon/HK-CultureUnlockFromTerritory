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
			//MapUtils.LogTerritoryStats();
			CultureUnlock.logEmpiresTerritories();
			MapUtils.LogTerritoryData();

			// stability level	= PublicOrderEffectDefinition, EmpireStabilityDefinition
			// units			= PresentationUnitDefinition
			// cultures			= FactionDefinition
			// eras				= EraDefinition
			// 
			// BuildingVisualAffinityDefinition
			// UnitVisualAffinityDefinition
			// EmpireSymbolDefinition
			// LocalizedStringElement
			/* 
			var database1 = Databases.GetDatabase<Amplitude.Mercury.Data.Simulation.EmpireSymbolDefinition>();
			foreach (Amplitude.Mercury.Data.Simulation.EmpireSymbolDefinition data in database1)
			{
				//Diagnostics.LogWarning($"[Gedemon] EmpireSymbolDefinition name = {data.name}");//, Name = {data.Name}");

				foreach (var prop in data.GetType().GetProperties())
				{
					Diagnostics.Log($"[Gedemon] {prop.Name} = {prop.GetValue(data, null)}");
				}
			}
			//*/

			/*
			var definition = Databases.GetDatabase<Amplitude.Framework.Localization.LocalizedStringElement>();
			foreach (Amplitude.Framework.Localization.LocalizedStringElement data in definition)
			{
				Diagnostics.LogWarning($"[Gedemon] Localization {data.name} = {data.CompactedNodes[0].TextValue}");
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
