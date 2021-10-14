using Amplitude.Mercury.Simulation;
using Amplitude;
using HarmonyLib;
using Amplitude.Framework;
using Amplitude.Framework.Input;
using Amplitude.Mercury.Data.Simulation;

namespace Gedemon.TrueCultureLocation
{
	[HarmonyPatch(typeof(CollectibleManager))]
	public class CultureUnlock_CollectibleManager
	{
		//*
		[HarmonyPostfix]
		[HarmonyPatch(nameof(InitializeOnLoad))]
		public static void InitializeOnLoad(CollectibleManager __instance)
		{
			Diagnostics.LogWarning($"[Gedemon] in CollectibleManager, InitializeOnLoad");
			CultureUnlock.LogTerritoryStats();


			//*
			IDatabase<EmpireStabilityDefinition> database1 = Databases.GetDatabase<EmpireStabilityDefinition>();
			foreach (EmpireStabilityDefinition data in database1)
			{
				Diagnostics.LogWarning($"[Gedemon] PublicOrderEffectDefinition name = {data.name}, Name = {data.Name}, XmlSerializableName = {data.XmlSerializableName}");
				Diagnostics.Log($"[Gedemon] RangeMax = {data.RangeMax}, RangeMaxType = {data.RangeMaxType} ");
				Diagnostics.Log($"[Gedemon] RangeMin = {data.RangeMin}, RangeMinType = {data.RangeMinType} ");
				//data.
			}
			//*/


			//*
			IDatabase<PublicOrderEffectDefinition> database2 = Databases.GetDatabase<PublicOrderEffectDefinition>();
			foreach (PublicOrderEffectDefinition data in database2)
			{
				Diagnostics.LogWarning($"[Gedemon] PublicOrderEffectDefinition name = {data.name}, Name = {data.Name}, XmlSerializableName = {data.XmlSerializableName}");
				Diagnostics.Log($"[Gedemon] RangeMax = {data.PublicOrderRangeMax}, RangeMaxType = {data.RangeMaxType} ");
				Diagnostics.Log($"[Gedemon] RangeMin = {data.PublicOrderRangeMin}, RangeMinType = {data.RangeMinType} ");
				Diagnostics.Log($"[Gedemon] GenerateSettlementCrisis = {data.GenerateSettlementCrisis}");
				//data.
			}
			//*/

			//

		}
		//*/
	}

}
