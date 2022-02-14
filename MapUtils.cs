using System.Collections.Generic;
using Amplitude;
using Amplitude.Mercury;
using Amplitude.Mercury.Interop;
using Amplitude.Mercury.Simulation;

namespace Gedemon.TrueCultureLocation
{
    public class MapUtils
    {
		public static bool AreSameLandContinent(Territory territory, CultureUnlock.TerritoryData territoryData)
        {
			bool useDirectRelativePosition = TrueCultureLocation.UseReferenceCoordinates();

			bool bothContinent = territory.ContinentIndex > 0 && territoryData.Continent > 0;

			if (useDirectRelativePosition)
				return bothContinent;

			if (!bothContinent)
				return false;

			if (CultureUnlock.AreSameContinentByReference(territory, territoryData))
				return true;
			else
				return false;
        }
		public static bool AreSameContinentType(Territory territory, CultureUnlock.TerritoryData territoryData, World currentWorld)
		{
			bool bothOcean = territory.ContinentIndex == 0 && territoryData.Continent == 0;

			if(bothOcean)
			{
				int territoryLandTiles = GetNumLandTiles(territory, currentWorld);
				bool bothIsland = territoryLandTiles > 0 && territoryData.LandSize > 0;
				bool bothWater = territoryLandTiles == 0 && territoryData.LandSize == 0;

				return bothIsland || bothWater;
			}


			bool bothContinent = AreSameLandContinent(territory, territoryData);

			return bothContinent;
		}
		public static bool AreSameContinentType(Territory territory, CultureUnlock.TerritoryData territoryData, World currentWorld, bool IgnoreIsland)
		{
			if (!IgnoreIsland)
				return AreSameContinentType(territory, territoryData, currentWorld);

			bool bothOcean = territory.ContinentIndex == 0 && territoryData.Continent == 0;
			bool bothContinent = AreSameLandContinent(territory, territoryData);

			return bothContinent || bothOcean;
		}

		public static int GetNumLandTiles(Territory territory)
        {
			int numTiles = territory.TileIndexes.Length;
			int landTiles = 0;
			for (int j = 0; j < numTiles; j++)
			{
				int tileIndex = territory.TileIndexes[j];
				ref TileInfo reference = ref Amplitude.Mercury.Sandbox.Sandbox.World.TileInfo.Data[tileIndex];
				if (reference.Elevation > 3)
					landTiles++;
			}
			return landTiles;
		}

		public static int GetNumLandTiles(Territory territory, World currentWorld) // During map validation Sandbox is not initialized yet, and we pass the world instance
		{
			int numTiles = territory.TileIndexes.Length;
			int landTiles = 0;
			for (int j = 0; j < numTiles; j++)
			{
				int tileIndex = territory.TileIndexes[j];
				TileInfo tileInfo = currentWorld.TileInfo.Data[tileIndex];
				if (tileInfo.Elevation > 3)
					landTiles++;
			}
			return landTiles;
		}

		public static void LogTerritoryStats()
		{
			Diagnostics.LogError($"[Gedemon] Logging Territory Stats");

			int num = Amplitude.Mercury.Sandbox.Sandbox.World.Territories.Length;
			int numLandTiles = 0;
			int numContinentTerritories = 0;
			int numlargeTerritories = 0;
			int numSmallTerritories = 0;
			for (int i = 0; i < num; i++)
			{
				Territory territory = Amplitude.Mercury.Sandbox.Sandbox.World.Territories[i];

				if (!territory.IsOcean)
				{

					ref TerritoryInfo info = ref Amplitude.Mercury.Sandbox.Sandbox.World.TerritoryInfo.Data[i];

					numContinentTerritories++;

					int landTiles = GetNumLandTiles(territory);

					numLandTiles += landTiles;
					//*
					if (landTiles < 25)
					{
						Diagnostics.LogWarning($"[Gedemon] #{landTiles} tiles for index  #{territory.Index} - {info.LocalizedName} (Small)");
						numSmallTerritories++;
					}
					else
					{
						if (landTiles > 75)
						{
							Diagnostics.LogWarning($"[Gedemon] #{landTiles} tiles for index  #{territory.Index} - {info.LocalizedName} (Large)");
							numlargeTerritories++;
						}
						else
						{
							Diagnostics.Log($"[Gedemon] #{landTiles} tiles for index  #{territory.Index} - {info.LocalizedName}");
						}
					}

					//*/
				}

			}
			int average = numLandTiles / numContinentTerritories;
			Diagnostics.LogError($"[Gedemon] Total territories = {num}, average land tiles per Continent territory = {average} ({numLandTiles}/{numContinentTerritories}), Small territories (<25 tiles) = {numSmallTerritories}, Large territories (>75 tiles) = {numlargeTerritories}");

		}

		public static void LogTerritoryData()
		{

			Diagnostics.LogError($"[Gedemon] Logging Continent Datas");

			int numContinents = Amplitude.Mercury.Sandbox.Sandbox.World.ContinentInfo.Length;

			for (int i = 0; i < numContinents; i++)
			{
				ContinentInfo continentInfo = Amplitude.Mercury.Sandbox.Sandbox.World.ContinentInfo[i];
				WorldPosition visualCenter = new WorldPosition(continentInfo.VisualCenterTileIndex);
				Diagnostics.LogWarning($"[Gedemon] [LogTerritoryData] {{ new ContinentReference(\"{continentInfo.ContinentName}\", {i}, {continentInfo.TerritoryIndexes.Length}, {visualCenter.Column}, {visualCenter.Row}) }},");
			}

			Diagnostics.LogError($"[Gedemon] Logging Territory Datas");

			int num = Amplitude.Mercury.Sandbox.Sandbox.World.Territories.Length;
			for (int i = 0; i < num; i++)
			{
				Territory territory = Amplitude.Mercury.Sandbox.Sandbox.World.Territories[i];
				TerritoryInfo info = Amplitude.Mercury.Sandbox.Sandbox.World.TerritoryInfo.Data[i];

				int numTiles = territory.TileIndexes.Length;
				int landTiles = GetNumLandTiles(territory);

				Diagnostics.LogWarning($"[Gedemon] [LogTerritoryData] {{ \"{info.LocalizedName}\", new TerritoryReference({territory.Index}, {territory.Biome}, {territory.VisualCenter.Row}, {territory.VisualCenter.Column}, {territory.ContinentIndex}, {territory.TileIndexes.Length}, {landTiles}) }},");
				//ReferenceGEM referenceGEM = new ReferenceGEM(territory.Index, territory.Biome, new WorldPosition(territory.VisualCenter.Column, territory.VisualCenter.Row), territory.IsOcean, territory.TileIndexes.Length);
				//	{ "Mare Beringianum", new TerritoryReference(0, 0, new WorldPosition(109, 77), true, 35)},
			}

		}
	}
}
