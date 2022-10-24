using Amplitude.Mercury.Simulation;
using HarmonyLib;
using Amplitude;
using Amplitude.Mercury.Interop;
using Amplitude.Mercury;
using System.Collections.Generic;
using Amplitude.Framework.Simulation;

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
					// destroy main district to also destroy all trade routes we want to restore (else they'll be destroyed too late for restoration, after the district decay) 
					District mainDistrict = settlement.GetMainDistrict();
					ReferenceCollection<District> districtCollection = new ReferenceCollection<District>();
					districtCollection.Add(mainDistrict);
					if (mainDistrict != null)
					{
						__instance.RemoveDistrictsFromSettlement(settlement, districtCollection, DistrictDestructionSource.MinorDecay);
					}

				}
			}
			return true;
		}
		//*/

		//*
		[HarmonyPatch("RemoveDistrictsFromSettlement")]
		[HarmonyPrefix]
		public static bool RemoveDistrictsFromSettlement(DepartmentOfTheInterior __instance, Settlement settlement, ReferenceCollection<District> districts, DistrictDestructionSource damageSource)
		{
			if (districts != null && districts.Count > 0)
			{
				int tileIndex = districts[0].WorldPosition.ToTileIndex();
				if (CurrentGame.Data.HistoricVisualAffinity.TryGetValue(tileIndex, out DistrictVisual visualAffinity))
				{
					//Diagnostics.LogWarning($"[Gedemon] [DepartmentOfTheInterior] RemoveDistrictsFromSettlement called at {districts[0].WorldPosition} from {damageSource}, empire index = {__instance.Empire.Index}, settlement entity name = {settlement.EntityName}, remove Historic Visual = {visualAffinity.VisualAffinity}");
					CurrentGame.Data.HistoricVisualAffinity.Remove(tileIndex);
				}
				return true;
			}
			return false;
		}
		//*

		//*
		[HarmonyPatch("CreateCityAt")]
		[HarmonyPrefix]
		public static bool CreateCityAt(DepartmentOfTheInterior __instance, SimulationEntityGUID initiatorGUID, WorldPosition worldPosition)
		{

			Diagnostics.LogWarning($"[Gedemon] [DepartmentOfTheInterior] CreateCityAt {worldPosition}, empire index = {__instance.Empire.Index}");
			if (CityMap.TryGetCityNameAt(worldPosition, __instance.Empire, out string cityLocalizationKey))
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


		//*
		[HarmonyPatch("ChangeSettlementOwner")]
		[HarmonyPrefix]
		public static bool ChangeSettlementOwner(DepartmentOfTheInterior __instance, Settlement settlement, Empire newEmpireOwner, bool keepCaptured = true)
		{

			//Diagnostics.LogWarning($"[Gedemon] [DepartmentOfTheInterior] ChangeSettlementOwner {settlement.EntityName} at {settlement.WorldPosition}, empire index = {__instance.Empire.Index}, new empire index = {newEmpireOwner.Index}");

			CultureChange.SaveHistoricDistrictVisuals(settlement.Empire.Entity);
			return true;
		}
		//*/
		// private Settlement DetachTerritoryFromCityAndCreateNewSettlement(Settlement city, int territoryIndex)

	}

}
