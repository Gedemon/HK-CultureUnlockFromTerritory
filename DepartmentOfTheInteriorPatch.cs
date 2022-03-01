using Amplitude.Mercury.Simulation;
using HarmonyLib;
using Amplitude;
using Amplitude.Mercury.Interop;
using Amplitude.Mercury;
using System.Collections.Generic;

namespace Gedemon.TrueCultureLocation
{
	[HarmonyPatch(typeof(DepartmentOfTheInterior))]
	public class DepartmentOfTheInteriorPatch
	{
		//*
		[HarmonyPatch("DestroyAllDistrictsFromSettlement")]
		[HarmonyPrefix]
		public static bool DestroyAllDistrictsFromSettlement(DepartmentOfTheInterior __instance, Settlement settlement, ref DistrictDestructionSource damageSource)
		{

			Diagnostics.LogWarning($"[Gedemon] in DepartmentOfTheInterior, DestroyAllDistrictsFromSettlement for {settlement.EntityName}, empire index = {__instance.Empire.Index}");

			if (CultureUnlock.UseTrueCultureLocation() && damageSource == DistrictDestructionSource.MinorDecay)
			{
				// to allow to take back districts by resettling before their destruction
				damageSource = DistrictDestructionSource.None;

				if (TradeRoute.IsRestoreDestroyedTradeRoutes)
				{
					// destroy main district to also destroy all trade routes we want to restore (else they'll be destroyed to late for restoration, after the district decay) 
					District mainDistrict = settlement.GetMainDistrict();
					if (mainDistrict != null)
					{
						__instance.DestroyDistrict(mainDistrict, DistrictDestructionSource.MinorDecay);
					}

				}
			}
			return true;
		}
		//*/

		//*
		[HarmonyPatch("CreateCityAt")]
		[HarmonyPrefix]
		public static bool CreateCityAt(DepartmentOfTheInterior __instance, SimulationEntityGUID initiatorGUID, WorldPosition worldPosition)
		{

			Diagnostics.LogWarning($"[Gedemon] [DepartmentOfTheInterior] CreateCityAt {worldPosition}, empire index = {__instance.Empire.Index}");
			if(CityMap.TryGetCityNameAt(worldPosition, __instance.Empire, out string cityLocalizationKey))
            {
				__instance.availableSettlementNames = new List<string> { cityLocalizationKey };
			}
			return true;
		}
		//*
		
		//*
		[HarmonyPatch("ApplyEvolutionToSettlement_City")]
		[HarmonyPrefix]
		public static bool ApplyEvolutionToSettlement_City(DepartmentOfTheInterior __instance, Settlement settlement)
		{

			Diagnostics.LogWarning($"[Gedemon] [DepartmentOfTheInterior] ApplyEvolutionToSettlement_City {settlement.WorldPosition}, empire index = {__instance.Empire.Index}");
			if (CityMap.TryGetCityNameAt(settlement.WorldPosition, __instance.Empire, out string cityLocalizationKey))
			{
				__instance.availableSettlementNames = new List<string> { cityLocalizationKey };
			}
			return true;
		}
		//*/

	}

}
