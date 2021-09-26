using System;
using System.Collections.Generic;
using Amplitude.Mercury.Data.Simulation;
using Amplitude.Mercury.WorldGenerator;
using BepInEx.Configuration;

namespace Amplitude.Mercury.Simulation
{
	public class CultureUnlock
	{

		static readonly IDictionary<string, List<int>> listTerritories = new Dictionary<string, List<int>>  // CivName, list of territory indexes
				{
					{ "Civilization_Era1_Phoenicia",                new List<int>() { 188, 190, 106, 157, 95 } }, // Morocco, Carthage, Liban, Lybia, South Spain
					{ "Civilization_Era1_EgyptianKingdom",          new List<int>() { 128 } }, // Egypt
					{ "Civilization_Era1_HittiteEmpire",            new List<int>() { 123, 118 } }, // East/West Anatiolia
					{ "Civilization_Era1_Babylon",                  new List<int>() { 142 } }, // Iraq
					{ "Civilization_Era1_Assyria",                  new List<int>() { 142 } }, // Iraq
					{ "Civilization_Era1_HarappanCivilization",     new List<int>() { 146, 207, 147 } }, // Afghanistan, Pakistan, North India
					{ "Civilization_Era1_MycenaeanCivilization",    new List<int>() { 101, 102, 103 } }, // Greece, Balkans, Bulgaria/Romania
					{ "Civilization_Era1_Nubia",                    new List<int>() { 132, 209, 213 } }, // Sudan, Ethiopia/Somalia, Central African Republic
					{ "Civilization_Era1_ZhouChina",                new List<int>() { 162, 163, 164, 221 } }, // China
					{ "Civilization_Era1_OlmecCivilization",        new List<int>() { 45 } }, // South Mexico/Central America
					{ "Civilization_Era2_RomanEmpire",              new List<int>() { 100, 99, 102, 101, 22, 97, 98 } }, // Italy, Balkans, Greece, South france, East Spain, Sardaigna
					{ "Civilization_Era2_Persia",                   new List<int>() { 144, 146, 207, 142, 123, 118 } }, // Iran, Afghanistan, Pakistan, Iraq, Anatolia
					{ "Civilization_Era2_MayaCivilization",         new List<int>() { 45 } }, // South Mexico/Central America
					{ "Civilization_Era2_MauryaEmpire",             new List<int>() { 147, 231, 207, 145 } }, // North India, Bengladesh, Pakistan
					{ "Civilization_Era2_Huns",                     new List<int>() { 226, 149 } }, // Volga, Scythia
					{ "Civilization_Era2_Goths",                    new List<int>() { 109, 117, 119 } }, // South Sweden, Poland, Ukraine
					{ "Civilization_Era2_CelticCivilization",       new List<int>() { 24, 19, 20, 21, 22, 96 } }, // France, British islands, NW Spain
					{ "Civilization_Era2_Carthage",                 new List<int>() { 190, 188, 98, 95 } }, // Carthage, Morroco, Sardaigna, South Spain
					{ "Civilization_Era2_AncientGreece",            new List<int>() { 101, 102, 103, 118, 100 } }, // Greece, Balkans, Bulgaria/Romania, West Anatolia, South Italy
					{ "Civilization_Era2_AksumiteEmpire",           new List<int>() { 209, 132, 143 } }, // Ethiopia/Somalia, Sudan, Yemen
					{ "Civilization_Era3_Vikings",                  new List<int>() { 107, 108, 109, 110, 191, 152 } }, // Norway, Sweden 
					{ "Civilization_Era3_UmayyadCaliphate",         new List<int>() { 106, 123, 142, 143, 227, 144, 146, 207, 128, 127, 190, 188, 95 } }, // Syria, Weast Anatolia, Iraq, Saudi Arabia, Iran, Afghanistan, Pakistan, Egypt, Libya, Carthage, Morroco, South Spain
					{ "Civilization_Era3_MongolEmpire",             new List<int>() { 225, 229, 161, 208, 187, 151, 206, 228, 230, 205, 149 } }, // Mongolia
					{ "Civilization_Era3_MedievalEngland",          new List<int>() { 21, 20 } }, // South England 
					{ "Civilization_Era3_KhmerEmpire",              new List<int>() { 168, 167 } }, // Cambodia
					{ "Civilization_Era3_HolyRomanEmpire",          new List<int>() { 105, 99, 23, 117, 104 } }, // Germany
					{ "Civilization_Era3_GhanaEmpire",              new List<int>() { 129, 125, 126, 220, 212 } }, // Mali
					{ "Civilization_Era3_FrankishKingdom",          new List<int>() { 24, 22, 99, 23 } }, // North France
					{ "Civilization_Era3_Byzantium",                new List<int>() { 103, 118, 123, 106, 101, 102, 104, 128 } }, // Bulgaria/Romania, Anatolia, Greece, Balkans, Hungary, Egypt
					{ "Civilization_Era3_AztecEmpire",              new List<int>() { 41 } }, // North Mexico
					{ "Civilization_Era4_VenetianRepublic",         new List<int>() { 99, 102 } }, // North Italy, Balkans
					{ "Civilization_Era4_TokugawaShogunate",        new List<int>() { 29, 30, 165, 27 } }, // Japan
					{ "Civilization_Era4_Spain",                    new List<int>() { 96, 97, 95, 55, 50, 48, 45, 41, 91, 92, 218 } }, // Spain
					{ "Civilization_Era4_PolishKingdom",            new List<int>() { 117, 119, 120, 121 } }, // Poland
					{ "Civilization_Era4_OttomanEmpire",            new List<int>() { 118, 123, 101, 102, 103, 142, 106, 128 } }, // Anatolia, Greece, Balkans, Bulgaria/Romania, 
					{ "Civilization_Era4_MughalEmpire",             new List<int>() { 147, 207, 145, 231, 146 } }, // India, Pakistan, Bengladesh, Afghanistan
					{ "Civilization_Era4_MingChina",                new List<int>() { 164, 162, 163, 221, 229 } }, // China
					{ "Civilization_Era4_JoseonKorea",              new List<int>() { 159 } }, // Korea
					{ "Civilization_Era4_IroquoisConfederacy",      new List<int>() { 60, 63 } }, // NE of North America
					{ "Civilization_Era4_Holland",                  new List<int>() { 23, 57, 50, 136, 141, 170, 175, 178, 176, 177  } }, // Holland, Antilles, Guyana, South Africa, Sri Lanka, Indonesia
					{ "Civilization_Era5_ZuluKingdom",              new List<int>() { 136, 222 } }, // South Africa, Mozambique/Zimbabwe
					{ "Civilization_Era5_Siam",                     new List<int>() { 166, 168 } }, // Thailand
					{ "Civilization_Era5_RussianEmpire",            new List<int>() { 122, 116, 121, 119, 226, 149, 189, 148, 115, 205, 206, 228, 223, 224, 194, 151, 193, 153, 192, 225, 154, 229, 161, 157, 195, 158, 196, 155, 27, 28, 197, 0 } }, // Moscow, St Petersburg, Russia...
					{ "Civilization_Era5_Mexico",                   new List<int>() { 41, 45, 219, 62, 58 } }, // Mexico
					{ "Civilization_Era5_Italy",                    new List<int>() { 100, 99, 127, 132, 209 } }, // Italy, Lybia, Sudan, Ethiopia
					{ "Civilization_Era5_Germany",                  new List<int>() { 105, 104, 117, 120 } }, // Germany
					{ "Civilization_Era5_FrenchRepublic",           new List<int>() { 24, 22, 57, 50, 190, 188, 126, 220, 212, 129, 18, 213, 137, 135, 167, 180, 15, 14, 201, 12, 106} }, // France, Antilles, Guyana, North Africa, Madagascar, Quebec, Canada
					{ "Civilization_Era5_BritishEmpire",            new List<int>() { 21, 20, 19, 14, 201, 10, 60, 61, 63, 12, 5, 9, 8, 6, 199, 200, 128, 132, 131, 215, 222, 210, 136, 142, 227, 207, 145, 147, 231, 31, 32, 33, 34, 35, 36, 37, 38, 39 } }, // England
					{ "Civilization_Era5_AustriaHungary",           new List<int>() { 104, 102 } }, // Austria/hungary
					{ "Civilization_Era5_AfsharidPersia",           new List<int>() { 144, 146, 230, 205, 142, 227 } }, // Iran
					{ "Civilization_Era6_USSR",                     new List<int>() { 122, 116, 121, 119, 226, 149, 189, 148, 115, 205, 206, 228, 223, 224, 194, 151, 193, 153, 192, 225, 154, 161, 157, 195, 158, 196, 155, 27, 28, 197, 0 } }, // Moscow
					{ "Civilization_Era6_USA",                      new List<int>() { 60, 61, 56, 63, 59, 58, 64, 65, 219, 204, 66, 62, 7, 198, 3, 4 } }, // Washington, USA
					{ "Civilization_Era6_Turkey",                   new List<int>() { 118, 123 } }, // Anatolia
					{ "Civilization_Era6_Sweden",                   new List<int>() { 109, 110 } }, // Stockholm
					{ "Civilization_Era6_Japan",                    new List<int>() { 29, 30, 165, 27 } }, // Japan
					{ "Civilization_Era6_India",                    new List<int>() { 147, 145, 207, 231 } }, // India, Pakistan, Bengladesh 
					{ "Civilization_Era6_Egypt",                    new List<int>() { 128 } }, // Egypt
					{ "Civilization_Era6_China",                    new List<int>() { 162, 163, 164, 221, 229, 208, 187, 150, 179, 173 } }, // China
					{ "Civilization_Era6_Brazil",                   new List<int>() { 54, 49, 53, 46 } }, // Brazil
					{ "Civilization_Era6_Australia",                new List<int>() { 35, 31, 32, 33, 34, 36, 37, 38, 39 } }  // Australia
				};

		static readonly IDictionary<string, List<int>> listSlots = new Dictionary<string, List<int>>  // civName, list of player slots (majorEmpire.Index) starting at 0
                {
					{ "Civilization_Era1_Assyria", new List<int>() { 6, 7 } },
					{ "Civilization_Era1_OlmecCivilization", new List<int>() { 8, 9 } }
				};

		static readonly List<string> firstEraBackup = new List<string> { "Civilization_Era1_Assyria", };

		static readonly List<string> noCapitalTerritory = new List<string> { "Civilization_Era1_Assyria", "Civilization_Era1_Phoenicia", "Civilization_Era2_Goths", "Civilization_Era2_CelticCivilization", "Civilization_Era3_Vikings" };

		public static readonly int knowledgeForBackupCiv = 50;

		public static bool HasTerritory(string civilizationName)
		{
			return listTerritories.ContainsKey(civilizationName);
		}

		public static bool HasTerritory(string civilizationName, int territoryIndex)
		{
			return listTerritories[civilizationName].Contains(territoryIndex);
		}

		public static bool HasTerritory(string civilizationName, int territoryIndex, bool any)
		{
			if (any)
            {
				return listTerritories[civilizationName].Contains(territoryIndex);
			}
			else
            {
				return listTerritories[civilizationName][0] == territoryIndex;
			}
		}

		public static bool HasFirstTerritory(string civilizationName, int territoryIndex)
		{
			return listTerritories[civilizationName][0] == territoryIndex;
		}

		public static bool IsUnlockedByPlayerSlot(string civilizationName, int empireIndex)
		{
			return listSlots.ContainsKey(civilizationName) && listSlots[civilizationName].Contains(empireIndex);
		}

		public static bool IsFirstEraBackupCivilization(string civilizationName)
		{
			return firstEraBackup.Contains(civilizationName);
		}

		public static bool HasNoCapitalTerritory(string civilizationName) // can be picked by multiple players
		{
			return noCapitalTerritory.Contains(civilizationName);
		}

		public static List<int> GetListTerritories(string civilizationName)
        {
			return listTerritories[civilizationName];
		}

		public static bool IsGiantEarthMap()
		{
			if (WorldPosition.WorldHeight.Equals(94) && WorldPosition.WorldWidth.Equals(180))
			{
				int ElevationHash = WorldGeneratorOutput.Maps.Elevation.GetHashCode();
				//Diagnostics.LogWarning($"[Gedemon] in ComputeFactionStatus, ElevationHash = {ElevationHash}");
				if (ElevationHash == 168143359)
				{
					return true;
				}
			}
			return false;
		}
		public static void LogElevationHash()
		{

			int ElevationHash = WorldGeneratorOutput.Maps.Elevation.GetHashCode();
			Diagnostics.LogWarning($"[Gedemon] Current Map Elevation Hash = {ElevationHash}");

		}
		public static void DoLiberateSettlement(Settlement settlement, MajorEmpire majorEmpire)
		{
			MinorEmpire orAllocateMinorEmpireFor = Amplitude.Mercury.Sandbox.Sandbox.MinorFactionManager.PeacefulLiberateHumanSpawner.GetOrAllocateMinorEmpireFor(settlement);
			if (orAllocateMinorEmpireFor.RankedMajorEmpireIndexes[0] != majorEmpire.Index)
			{
				int num = Array.IndexOf(orAllocateMinorEmpireFor.RankedMajorEmpireIndexes, majorEmpire.Index);
				orAllocateMinorEmpireFor.RankedMajorEmpireIndexes[num] = orAllocateMinorEmpireFor.RankedMajorEmpireIndexes[0];
				orAllocateMinorEmpireFor.RankedMajorEmpireIndexes[0] = majorEmpire.Index;
			}
			List<int> territoryIndices = settlement.Region.Entity.TerritoryIndices;
			for (int num2 = majorEmpire.Squadrons.Count - 1; num2 >= 0; num2--)
			{
				Squadron squadron = majorEmpire.Squadrons[num2];
				if (squadron.AircraftCarrierArmy.Entity == null && territoryIndices.Contains(squadron.TerritoryIndex))
				{
					DepartmentOfDefense.ReleaseSquadron(squadron);
				}
			}

			DepartmentOfTheInterior.ChangeSettlementOwner(settlement, orAllocateMinorEmpireFor, keepCaptured: false);
			BaseHumanSpawnerDefinition spawnerDefinitionForMinorEmpire = orAllocateMinorEmpireFor.Spawner.GetSpawnerDefinitionForMinorEmpire(orAllocateMinorEmpireFor);
			MinorToMajorRelation minorToMajorRelation = orAllocateMinorEmpireFor.RelationsToMajor[majorEmpire.Index];
			if (minorToMajorRelation.PatronageStock.Value < 50)
			{
				minorToMajorRelation.PatronageStock.Value = 50;
				MinorFactionManager.RefreshPatronageState(minorToMajorRelation, orAllocateMinorEmpireFor.PatronageDefinition);
			}
			FixedPoint defaultGameSpeedMultiplier = Amplitude.Mercury.Sandbox.Sandbox.GameSpeedController.CurrentGameSpeedDefinition.DefaultGameSpeedMultiplier;
			orAllocateMinorEmpireFor.RemainingLifeTime += (int)FixedPoint.Ceiling(spawnerDefinitionForMinorEmpire.AddedLifeTimeInTurnsForNewPatronnage * defaultGameSpeedMultiplier);
			Amplitude.Mercury.Sandbox.Sandbox.VisibilityController.DirtyEmpireVision(orAllocateMinorEmpireFor);
		}
	}
}
