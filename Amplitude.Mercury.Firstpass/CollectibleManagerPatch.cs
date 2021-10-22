using Amplitude.Mercury.Simulation;
using Amplitude;
using HarmonyLib;
using Amplitude.Framework;
using Amplitude.Framework.Input;
using Amplitude.Mercury.Data.Simulation;
using Amplitude.Mercury.Data.World;
using System.Linq;
using System.Collections.Generic;

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

			// stability level	= PublicOrderEffectDefinition, EmpireStabilityDefinition
			// units			= PresentationUnitDefinition
			// cultures			= FactionDefinition
			// eras				= EraDefinition
			/* 
			IDatabase<FactionDefinition> database1 = Databases.GetDatabase<FactionDefinition>();
			foreach (FactionDefinition data in database1)
			{
				Diagnostics.LogWarning($"[Gedemon] FactionDefinition name = {data.name}");//, Name = {data.Name}");

				foreach (var prop in data.GetType().GetProperties())
				{
					Diagnostics.Log($"[Gedemon] {prop.Name} = {prop.GetValue(data, null)}");
				}
			}
			//*/
		}
	}

}
