using System.Collections.Generic;
using Amplitude.Mercury.Simulation;
using Amplitude;
using HarmonyLib;
using Amplitude.Mercury.Interop;
using Amplitude.Mercury.Data.World;

namespace Gedemon.TrueCultureLocation
{
	[HarmonyPatch(typeof(World))]
	public class TCL_World
	{
		//*
		[HarmonyPostfix]
		[HarmonyPatch(nameof(SetRandomContinentName))]
		public static void SetRandomContinentName(World __instance, ref ContinentInfo continentInfo, ref List<ContinentNamingPoolDefinition> possiblePools)
		{
			if (continentInfo.TerritoryIndexes.Length > 0)
            {
				Territory territory = __instance.Territories[continentInfo.TerritoryIndexes[0]];
				int continentIndex = territory.ContinentIndex;
				if (CultureUnlock.ContinentHasName(continentIndex))
				{
					continentInfo.ContinentName = CultureUnlock.GetContinentName(continentIndex);
				}
			}

		}
		//*/

		[HarmonyPrefix]
		[HarmonyPatch(nameof(SetRandomTerritoryName))]
		public static bool SetRandomTerritoryName(World __instance, ref TerritoryInfo territory, List<string> availableName, Dictionary<string, int> alreadyUsedTerritoryNameOccurence)
		{
			if (CultureUnlock.IsCompatibleMap() && CultureUnlock.TerritoryHasName(territory.TerritoryIndex))
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
				if (CultureUnlock.UseTrueCultureLocation() && CultureUnlock.TerritoryHasName(reference.TerritoryIndex))
				{
					reference.LocalizedName = CultureUnlock.GetTerritoryName(reference.TerritoryIndex);
				}
				else
				{
					// debug compatible maps by displaying index where there is no real name set
					if (CultureUnlock.IsCompatibleMap())
					{
						reference.LocalizedName = reference.TerritoryIndex.ToString();
					}
					else
                    {
						//*
						reference.LocalizedName = service.Localize(reference.NameKey);
						if (reference.OccurenceIndex > 1)
						{
							reference.LocalizedName += $" -- {reference.OccurenceIndex}";
						}
						//*/
					}
				}
			}
			return false; // don't run original LocalizeTerritory(), this method fully replaces it
		}

		//*
		[HarmonyPostfix]
		[HarmonyPatch(nameof(LoadTerritories))]
		public static void LoadTerritories(World __instance)
		{
			Diagnostics.Log($"[Gedemon] in World, LoadTerritories, PostFix");
			Diagnostics.Log($"[Gedemon] Current Map Hash = {CultureUnlock.CurrentMapHash}");

			int num = __instance.Territories.Length;
			int maxNum = 256;

			string mapString = "";

			for (int i = 0; i < maxNum; i++)
			{
				int numTiles = 0;
				if (i<num)
                {
					Territory territory = __instance.Territories[i];
					numTiles = territory.TileIndexes.Length;
				}
				
				//Diagnostics.Log($"[Gedemon] Building Map String, territory[{i}] = {numTiles}");
				mapString += (numTiles>999 ? "999," : numTiles < 10 ? "00" + numTiles.ToString() + "," : numTiles < 100 ? "0" + numTiles.ToString() + "," : numTiles.ToString() + ",");
			}

			CultureUnlock.CurrentMapHash = mapString.GetHashCode();

			Diagnostics.LogError($"[Gedemon] Calculated Current Map Hash = {CultureUnlock.CurrentMapHash}");
			//Diagnostics.Log($"[Gedemon] Map string = {mapString}");

			CultureUnlock.InitializeTCL();

		}
		//*/
	}


}
