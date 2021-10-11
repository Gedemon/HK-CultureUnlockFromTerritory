using System.Collections.Generic;
using Amplitude.Mercury.Simulation;
using Amplitude;
using HarmonyLib;
using Amplitude.Mercury.Interop;

namespace Gedemon.CultureUnlock
{
	[HarmonyPatch(typeof(World))]
	public class CultureUnlockWorld
	{
		/*
		[HarmonyPrefix]
		[HarmonyPatch(nameof(SetRandomContinentName))]
		public static bool SetRandomContinentName(World __instance)
		{
			Diagnostics.LogWarning($"[Gedemon] in SetRandomContinentName, continentIndex = {0}");
			if (CultureUnlock.ContinentHasName(x))
            {
				//territory.NameKey = CultureUnlock.GetTerritoryName(territory.TerritoryIndex);
				return false; // don't run original SetRandomContinentName()
			}
			else
				return true; // run original SetRandomContinentName()
		}
		//*/

		[HarmonyPrefix]
		[HarmonyPatch(nameof(SetRandomTerritoryName))]
		public static bool SetRandomTerritoryName(World __instance, ref TerritoryInfo territory, List<string> availableName, Dictionary<string, int> alreadyUsedTerritoryNameOccurence)
		{
			if (CultureUnlock.IsGiantEarthMap() && CultureUnlock.TerritoryHasName(territory.TerritoryIndex))
			{
				territory.NameKey = CultureUnlock.GetTerritoryName(territory.TerritoryIndex);
				return false; // don't run original SetRandomTerritoryName()
			}
			else
				return true; // run original SetRandomTerritoryName()
		}


		[HarmonyPrefix]
		[HarmonyPatch(nameof(LocalizeTerritory))]
		public static bool LocalizeTerritory(World __instance)
		{
			int length = __instance.TerritoryInfo.Length;
			Amplitude.Framework.Localization.ILocalizationService service = Amplitude.Framework.Services.GetService<Amplitude.Framework.Localization.ILocalizationService>();
			for (int i = 0; i < length; i++)
			{
				ref TerritoryInfo reference = ref __instance.TerritoryInfo.Data[i];
				if (CultureUnlock.IsGiantEarthMap() && CultureUnlock.TerritoryHasName(reference.TerritoryIndex))
				{
					reference.LocalizedName = CultureUnlock.GetTerritoryName(reference.TerritoryIndex);
				}
				else
				{
					// debug by displaying idx when there is no real name set
					reference.LocalizedName = reference.TerritoryIndex.ToString();
					/*
					reference.LocalizedName = service.Localize(reference.NameKey);
					if (reference.OccurenceIndex > 1)
					{
						reference.LocalizedName += $" -- {reference.OccurenceIndex}";
					}
					//*/
				}
			}
			return false; // don't run original LocalizeTerritory(), this method fully replaces it
		}

		//*
		[HarmonyPostfix]
		[HarmonyPatch(nameof(LoadTerritories))]
		public static void LoadTerritories(World __instance)
		{
			Diagnostics.Log($"[Gedemon] in World, LoadTerritories");
			Diagnostics.Log($"[Gedemon] Current Map Hash = {CultureUnlock.CurrentMapHash}");

			int num = __instance.Territories.Length;

			string mapString = "";

			for (int i = 0; i < num; i++)
			{
				Territory territory = __instance.Territories[i];
				int numTiles = territory.TileIndexes.Length;
				//Diagnostics.Log($"[Gedemon] Building Map String, territory[{i}] = {numTiles}");
				mapString += numTiles.ToString() + ",";
			}

			CultureUnlock.CurrentMapHash = mapString.GetHashCode();

			Diagnostics.LogError($"[Gedemon] Calculated Current Map Hash = {CultureUnlock.CurrentMapHash}");

			if (!CultureUnlock.IsGiantEarthMap())
				Diagnostics.LogError($"[Gedemon] Unknown Map");
		}
		//*/
	}


}
