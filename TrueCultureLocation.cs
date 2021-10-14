using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Mercury;
using Amplitude.Mercury.Data.Simulation;
using Amplitude.Mercury.Interop;
using Amplitude.Mercury.Sandbox;
using Amplitude.Mercury.Simulation;
using Amplitude.Mercury.WorldGenerator;
using Amplitude.Serialization;
using BepInEx.Configuration;

namespace Gedemon.TrueCultureLocation
{
	public class CultureUnlock
	{

		static readonly IDictionary<string, List<int>> listTerritories = new Dictionary<string, List<int>>  // CivName, list of territory indexes
				{
					{ "Civilization_Era1_Phoenicia",                new List<int>() { 188, 190, 106, 127, 95, 100, 99, 22 } }, // Morocco, Carthage, Liban, Lybia, South Spain
					{ "Civilization_Era1_EgyptianKingdom",          new List<int>() { 128 } }, // Egypt
					{ "Civilization_Era1_HittiteEmpire",            new List<int>() { 123, 118 } }, // East/West Anatiolia
					{ "Civilization_Era1_Babylon",                  new List<int>() { 142 } }, // Iraq
					{ "Civilization_Era1_Assyria",                  new List<int>() { 142, 226, 149, 124, 228, 205, 206 } }, // Iraq + Steppe
					{ "Civilization_Era1_HarappanCivilization",     new List<int>() { 207, 147, 145 } }, // Pakistan, North India
					{ "Civilization_Era1_MycenaeanCivilization",    new List<int>() { 101, 102, 238 } }, // Greece, Balkans, Thrace
					{ "Civilization_Era1_Nubia",                    new List<int>() { 233, 132, 209, 213 } }, // Sudan, Abyssinian, Ethiopia/Somalia, Central African Republic
					{ "Civilization_Era1_ZhouChina",                new List<int>() { 162, 163, 164, 221 } }, // China
					{ "Civilization_Era1_OlmecCivilization",        new List<int>() { 45, 41, 48, 219, 62, 58, 50, 47, 46 } }, // South Mexico/Central America
					{ "Civilization_Era2_RomanEmpire",              new List<int>() { 100, 99, 102, 101, 22, 97, 98 } }, // Italy, Balkans, Greece, South france, East Spain, Sardaigna
					{ "Civilization_Era2_Persia",                   new List<int>() { 243, 144, 146, 207, 142, 123, 118, 235 } }, // Iran, Afghanistan, Pakistan, Iraq, Anatolia, Ballochistan
					{ "Civilization_Era2_MayaCivilization",         new List<int>() { 45, 219, 48 } }, // South Mexico/Central America
					{ "Civilization_Era2_MauryaEmpire",             new List<int>() { 147, 231, 207, 145 } }, // North India, Bengladesh, Pakistan
					{ "Civilization_Era2_Huns",                     new List<int>() { 226, 149, 124, 228, 205, 206  } }, // Volga, Scythia
					{ "Civilization_Era2_Goths",                    new List<int>() { 109, 117, 119 } }, // South Sweden, Poland, Ukraine
					{ "Civilization_Era2_CelticCivilization",       new List<int>() { 24, 19, 20, 21, 22, 96 } }, // France, British islands, NW Spain
					{ "Civilization_Era2_Carthage",                 new List<int>() { 190, 188, 98, 95 } }, // Carthage, Morroco, Sardaigna, South Spain
					{ "Civilization_Era2_AncientGreece",            new List<int>() { 101, 102, 238, 103, 118 } }, // Greece, Balkans, Bulgaria/Romania, Anatolia
					{ "Civilization_Era2_AksumiteEmpire",           new List<int>() { 132, 209, 143, 233 } }, // Abyssinia, Ethiopia/Somalia, Sudan, Yemen
					{ "Civilization_Era3_Vikings",                  new List<int>() { 107, 109, 111, 112, 191, 152, 17, 11 } }, // Norway, Sweden 
					{ "Civilization_Era3_UmayyadCaliphate",         new List<int>() { 106, 123, 142, 143, 227, 144, 243, 146, 207, 128, 127, 190, 188, 95 } }, // Syria, Weast Anatolia, Iraq, Saudi Arabia, Iran, Afghanistan, Pakistan, Egypt, Libya, Carthage, Morroco, South Spain
					{ "Civilization_Era3_MongolEmpire",             new List<int>() { 225, 239, 240, 229, 161, 208, 187, 151, 206, 228, 230, 205, 149 } }, // Mongolia
					{ "Civilization_Era3_MedievalEngland",          new List<int>() { 21, 20 } }, // South England 
					{ "Civilization_Era3_KhmerEmpire",              new List<int>() { 38, 167, 168 } }, // Cambodia, Vietnam, Siam 
					{ "Civilization_Era3_HolyRomanEmpire",          new List<int>() { 105, 99, 23, 117, 104 } }, // Germany
					{ "Civilization_Era3_GhanaEmpire",              new List<int>() { 125, 129, 126, 220, 212 } }, // Mali
					{ "Civilization_Era3_FrankishKingdom",          new List<int>() { 24, 22, 99, 23 } }, // North France
					{ "Civilization_Era3_Byzantium",                new List<int>() { 238, 103, 118, 123, 106, 101, 102, 104, 128 } }, // Bulgaria/Romania, Anatolia, Greece, Balkans, Hungary, Egypt
					{ "Civilization_Era3_AztecEmpire",              new List<int>() { 41, 62, 219 } }, // North Mexico
					{ "Civilization_Era4_VenetianRepublic",         new List<int>() { 99, 102 } }, // North Italy, Balkans
					{ "Civilization_Era4_TokugawaShogunate",        new List<int>() { 29, 30, 165, 27 } }, // Japan
					{ "Civilization_Era4_Spain",                    new List<int>() { 96, 97, 95, 55, 50, 48, 45, 41, 91, 92, 218 } }, // Spain
					{ "Civilization_Era4_PolishKingdom",            new List<int>() { 117, 119, 120, 121 } }, // Poland
					{ "Civilization_Era4_OttomanEmpire",            new List<int>() { 118, 123, 101, 102, 103, 142, 106, 128 } }, // Anatolia, Greece, Balkans, Bulgaria/Romania, 
					{ "Civilization_Era4_MughalEmpire",             new List<int>() { 147, 207, 145, 231, 146, 236, 237 } }, // India, Pakistan, Bengladesh, Afghanistan
					{ "Civilization_Era4_MingChina",                new List<int>() { 164, 162, 163, 221, 229 } }, // China
					{ "Civilization_Era4_JoseonKorea",              new List<int>() { 159 } }, // Korea
					{ "Civilization_Era4_IroquoisConfederacy",      new List<int>() { 60, 63 } }, // NE of North America
					{ "Civilization_Era4_Holland",                  new List<int>() { 23, 57, 50, 136, 141, 170, 175, 178, 176, 177  } }, // Holland, Antilles, Guyana, South Africa, Sri Lanka, Indonesia
					{ "Civilization_Era5_ZuluKingdom",              new List<int>() { 136, 222 } }, // South Africa, Mozambique/Zimbabwe
					{ "Civilization_Era5_Siam",                     new List<int>() { 166, 168, 38 } }, // Thailand, Cambodia
					{ "Civilization_Era5_RussianEmpire",            new List<int>() { 122, 116, 121, 119, 226, 149, 189, 148, 115, 205, 206, 228, 223, 224, 194, 151, 193, 153, 192, 225, 154, 229, 161, 157, 195, 158, 196, 155, 27, 28, 197, 0 } }, // Moscow, St Petersburg, Russia...
					{ "Civilization_Era5_Mexico",                   new List<int>() { 41, 45, 219, 62, 58 } }, // Mexico
					{ "Civilization_Era5_Italy",                    new List<int>() { 100, 99, 127, 132, 209 } }, // Italy, Lybia, Sudan, Ethiopia
					{ "Civilization_Era5_Germany",                  new List<int>() { 105, 104, 117, 120 } }, // Germany
					{ "Civilization_Era5_FrenchRepublic",           new List<int>() { 24, 22, 57, 50, 190, 188, 126, 220, 212, 129, 18, 213, 137, 135, 167, 180, 15, 14, 201, 12, 106} }, // France, Antilles, Guyana, North Africa, Madagascar, Quebec, Canada
					{ "Civilization_Era5_BritishEmpire",            new List<int>() { 21, 20, 19, 14, 201, 10, 60, 61, 63, 12, 5, 9, 8, 6, 199, 200, 128, 132, 131, 215, 222, 210, 136, 142, 227, 207, 145, 147, 231, 31, 32, 33, 34, 35, 36, 37, 38, 39 } }, // England
					{ "Civilization_Era5_AustriaHungary",           new List<int>() { 104, 102 } }, // Austria/hungary
					{ "Civilization_Era5_AfsharidPersia",           new List<int>() { 243, 144, 146, 230, 205, 142, 227, 235 } }, // Iran
					{ "Civilization_Era6_USSR",                     new List<int>() { 122, 116, 121, 119, 226, 149, 189, 148, 115, 205, 206, 228, 223, 224, 194, 151, 193, 153, 192, 225, 154, 161, 157, 195, 158, 196, 155, 27, 28, 197, 0 } }, // Moscow
					{ "Civilization_Era6_USA",                      new List<int>() { 60, 61, 56, 63, 59, 58, 64, 65, 219, 204, 66, 62, 7, 198, 3, 4 } }, // Washington, USA
					{ "Civilization_Era6_Turkey",                   new List<int>() { 118, 123 } }, // Anatolia
					{ "Civilization_Era6_Sweden",                   new List<int>() { 109, 110 } }, // Stockholm
					{ "Civilization_Era6_Japan",                    new List<int>() { 29, 30, 165, 27 } }, // Japan
					{ "Civilization_Era6_India",                    new List<int>() { 147, 145, 207, 231, 236, 237 } }, // India, Pakistan, Bengladesh 
					{ "Civilization_Era6_Egypt",                    new List<int>() { 128, 233 } }, // Egypt
					{ "Civilization_Era6_China",                    new List<int>() { 162, 163, 164, 221, 229, 208, 187, 150, 179, 173, 244 } }, // China
					{ "Civilization_Era6_Brazil",                   new List<int>() { 54, 49, 53, 46 } }, // Brazil
					{ "Civilization_Era6_Australia",                new List<int>() { 35, 31, 32, 33, 34, 36, 37, 38, 39 } }  // Australia
				};

		static readonly IDictionary<string, List<int>> listSlots = new Dictionary<string, List<int>>  // civName, list of player slots (majorEmpire.Index) starting at 0
                {
					{ "Civilization_Era1_Assyria", new List<int>() { 6, 7 } },
				};

		static readonly List<string> firstEraBackup = new List<string> { "Civilization_Era1_Assyria", };

		static readonly List<string> noCapitalTerritory = new List<string> { "Civilization_Era1_Assyria", "Civilization_Era1_HarappanCivilization", "Civilization_Era1_Nubia", "Civilization_Era1_ZhouChina", "Civilization_Era1_MycenaeanCivilization", "Civilization_Era1_HittiteEmpire", "Civilization_Era1_Phoenicia", "Civilization_Era1_OlmecCivilization", "Civilization_Era2_Huns", "Civilization_Era2_Goths", "Civilization_Era2_CelticCivilization", "Civilization_Era3_Vikings", "Civilization_Era3_MongolEmpire" };


		static readonly IDictionary<int, string> continentNames = new Dictionary<int, string>
				{
					{ 0, "Oceans"},
					{ 1, "Americas"},
					{ 2, "Eurasiafrica"},
					{ 3, "Australia"},
					{ 5, "Greenland"},
				};

		static readonly IDictionary<int, string> territoryNames = new Dictionary<int, string>
				{
					{ 0, "Mare Beringianum"},
					{ 1, "Chatanga"},
					{ 2, "Flumen Cromwellianum"},
					{ 3, "Insulae Aleutiae"},
					{ 4, "Alasca"},
					{ 5, "Saskatchewan"}, // South Alberta+South Saskatchewan
					{ 6, "Columbia Britannica"},
					{ 7, "Haida Gwaii"}, // Islands West of British Columbia
					{ 8, "Kitlineq"}, // Victoria Island
					{ 9, "Nunavut"},
					{ 10, "Baffin"}, // North Canada Island
					{ 11, "Groenlandia"},
					{ 12, "Keewatin"},
					{ 13, "Canada"},
					{ 14, "Ungava"},
					{ 15, "Terra Nova"},
					{ 16, "Mare Laboratorium"},
					{ 17, "Islandia"},
					{ 18, "Tzadia"},
					{ 19, "Hibernia"}, // Ireland
					{ 20, "Caledonia"}, // Scottland
					{ 21, "Britannia"}, // England
					{ 22, "Occitania"},
					{ 23, "Batavia"},
					{ 24, "Neustria"}, // Northern France
					{ 25, "Flumen Kuroshio"},
					{ 26, "Flumen Californiense"},
					{ 27, "Esonia"},
					{ 28, "Camtschatca"},
					{ 29, "Iaponia Boreales"},
					{ 30, "Iaponia Australes"},
					{ 31, "Australia Occidentalis"},
					{ 32, "Terra Reginae"},
					{ 33, "Australia Septentrionalis"},
					{ 34, "Australia Australis"},
					{ 35, "Cambria Australis"},
					{ 36, "Te Ika-a-Maui"},
					{ 37, "Te Waipounamu"},
					{ 38, "Cambosia"},
					{ 39, "Tasmania"},
					{ 40, "Magnus Sinus Australianus"},
					{ 41, "Mexicum"},
					{ 42, "Samoa"},
					{ 43, "Oceanus Indicus"},
					{ 44, "Mare Tasmanianum"},
					{ 45, "Guatemala"}, // Central America
					{ 46, "Amazonia"},
					{ 47, "Peruvia"},
					{ 48, "Columbia"},
					{ 49, "Caatinga"}, // North Brazil
					{ 50, "Guiana"},
					{ 51, "Patagonia"}, // South Chile, Argentina
					{ 52, "Atacama"}, // South Peru / North Chile
					{ 53, "Pantanal"},
					{ 54, "Brasilia Australis"},
					{ 55, "Mare Caribaeum"},
					{ 56, "Florida"},
					{ 57, "Antillae"},
					{ 58, "Texia"},
					{ 59, "Mississippia"},
					{ 60, "Massachusetta"},
					{ 61, "Carolinae"},
					{ 62, "Chihuahua"},
					{ 63, "Lacus Magni"},
					{ 64, "Magnae Planities"},
					{ 65, "Dacota"},
					{ 66, "Oregonia"},
					{ 67, "Flumen Aequatoriale Septentrionale"},
					{ 68, "Havaii"},
					{ 69, "Insulae Marianae"},
					{ 70, "Insulae Marsaliensis"},
					{ 71, "Insulae Marchionis"},
					{ 72, "Insula Paschalis"},
					{ 73, "Polynesia Centralis"},
					{ 74, "Insulae Salomanis"},
					{ 75, "Polynesia Australis"},
					{ 76, "Flumen Circumpolare Antarcticum"},
					{ 77, "Insulae Galapagenses"},
					{ 78, "Gyrus Pacifici Australis"},
					{ 79, "Flumen Humboldtianum"},
					{ 80, "Insulae Bonin"},
					{ 81, "Insulae Atlanticae Australis"},
					{ 82, "Mare Argentinum"},
					{ 83, "Fossa Mariana"},
					{ 84, "Polynesia Borealis"},
					{ 85, "Fretum Drakeanum"},
					{ 86, "Oceanus Atlanticus Australis"},
					{ 87, "Flumen Benguelense"},
					{ 88, "Flumen Aequatoriale Australe"},
					{ 89, "Mare Sargassum"},
					{ 90, "Oceanus Atlanticus"},
					{ 91, "Canariae Insulae"},
					{ 92, "Azores"},
					{ 93, "Oceanus Atlanticus Orientalis"},
					{ 94, "Insulae Societalis"},
					{ 95, "Andalusia"},
					{ 96, "Castella"},
					{ 97, "Aragonia"},
					{ 98, "Sardinia"},
					{ 99, "Italia Annonaria"},
					{ 100, "Italia Suburbicaria"},
					{ 101, "Graecia"},
					{ 102, "Illyria"},
					{ 103, "Dacia"},
					{ 104, "Danubia"},
					{ 105, "Germania"},
					{ 106, "Syria"},
					{ 107, "Norvegia"},
					{ 108, "Crasnoiarium"},
					{ 109, "Suecia"}, 
					{ 110, "Permia"},
					{ 111, "Finnia"},
					{ 112, "Kola"},
					{ 113, "Mare Barentsianum"},
					{ 114, "Biarmia"},
					{ 115, "Nova Zembla"},
					{ 116, "Novogardia"},
					{ 117, "Polonia"},
					{ 118, "Anatolia"},
					{ 119, "Ucraina"},
					{ 120, "Baltica"},
					{ 121, "Transcaucasia"},
					{ 122, "Moscovia"},
					{ 123, "Armenia"},
					{ 124, "Ciscaucasia"},
					{ 125, "Mauretania"},
					{ 126, "Sahara"},
					{ 127, "Libya"},
					{ 128, "Aegyptus"},
					{ 129, "Malia"},
					{ 130, "Cammarunia"},
					{ 131, "Kenia"},
					{ 132, "Abyssinia"},
					{ 133, "Socotra"},
					{ 134, "Insulae Mascarene"},
					{ 135, "Madagascaria"},
					{ 136, "Cape"},
					{ 137, "Congo"},
					{ 138, "Flumen Agulhas" },
					{ 139, "Oceanus Indicus Occidentalis"},
					{ 140, "Mare Arabicum"},
					{ 141, "SriLanca"},
					{ 142, "Mesopotamia"},
					{ 143, "Hidiazum"},
					{ 144, "Media"},
					{ 145, "Maharastra"},
					{ 146, "Afgania"},
					{ 147, "Ganges"},
					{ 148, "Nenetsia"},
					{ 149, "Orenburgum"},
					{ 150, "Tibetum "},
					{ 151, "Altai"},
					{ 152, "Lapponia"},
					{ 153, "Tungusca"},
					{ 154, "Lena"},
					{ 155, "Cisbaicalia"},
					{ 156, "Mare Tschukotense"},
					{ 157, "Amur"},
					{ 158, "Magadanum"},
					{ 159, "Corea"},
					{ 160, "Insulae Lineae"},
					{ 161, "Territorium Maritimum"},
					{ 162, "Planum Sinense"},
					{ 163, "Flumen Flavum"},
					{ 164, "Flumen Margaritarum"},
					{ 165, "Formosa"},
					{ 166, "Birmania"},
					{ 167, "Vietnamia"},
					{ 168, "Thailandia"},
					{ 169, "Malaesia"},
					{ 170, "Sumatra"},
					{ 171, "Andamanenses"},
					{ 172, "Sinus Bengalensis"},
					{ 173, "Hainania"},
					{ 174, "Mare Philippinense"},
					{ 175, "Iava"},
					{ 176, "Celebis"},
					{ 177, "Papua"},
					{ 178, "Borneum"},
					{ 179, "Casmiria"},
					{ 180, "Melanesia"},
					{ 181, "Flumen Australianum Occidentale"},
					{ 182, "Gyrus Indici"},
					{ 183, "Archipelagus Crozetense"},
					{ 184, "Oceanus Indicus Australis"},
					{ 185, "Oceanus Atlanticus Septentrionalis"},
					{ 186, "Mare Norvegicum"},
					{ 187, "Tarim"},
					{ 188, "Marocum"},
					{ 189, "Casanum"},
					{ 190, "Numidia"},
					{ 191, "Finmarchia"},
					{ 192, "Taimyr"},
					{ 193, "Tomium"},
					{ 194, "Iamalia"},
					{ 195, "Iana"},
					{ 196, "Colyma"},
					{ 197, "Tschucoticus"},
					{ 198, "Kenai"}, // South Alaska
					{ 199, "Athabasca"}, // North Alberta+North Saskatchewan
					{ 200, "Dene"}, // Nothwest Territories
					{ 201, "Laboratoria"},
					{ 202, "Flumen Argenteum"},
					{ 203, "Terra Ignium"},
					{ 204, "Nivata"},
					{ 205, "Chorasmia"},
					{ 206, "Kazachia"},
					{ 207, "Indus"},
					{ 208, "Desertum Gobium"},
					{ 209, "Somalia"},
					{ 210, "Naimbia"},
					{ 211, "Guinea"},
					{ 212, "Nigritania"},
					{ 213, "Ngbandia "},
					{ 214, "Angolia"},
					{ 215, "Zambia"},
					{ 216, "Capitis Viridis"},
					{ 217, "Tuvalu"},
					{ 218, "Philippinae"},
					{ 219, "Yucatania"},
					{ 220, "Beninum"},
					{ 221, "Sichuan"},
					{ 222, "Zimbabua"},
					{ 223, "Tobolium"},
					{ 224, "Ienisea"},
					{ 225, "Mongolia Ulterior"},
					{ 226, "Voronegia"},
					{ 227, "Arabia"},
					{ 228, "Omium"},
					{ 229, "Manchuria"},
					{ 230, "Parthia"},
					{ 231, "Bengala"},
					{ 232, "California"},
					{ 233, "Sudania"},
					{ 234, "Mozambicum"},
					{ 235, "Gedrosia"},
					{ 236, "Dravidia"},
					{ 237, "Odisa"},
					{ 238, "Thracia"},
					{ 239, "Transbaicalia"},
					{ 240, "Mongolia Citerior"},
					{ 241, "Kiangnanum"},
					{ 242, "Alpes Iaponicae"},
					{ 243, "Persia"},
					{ 244, "Amdo"},
					{ 245, "Transoxiana"},
					{ 246, "Chilia"},
					{ 247, "Venetiola"},
					{ 248, "Paraensis"},
					{ 249, "Flumen Brasiliense"},
					{ 250, "Ultima Thule"},
					//{ 251, ""},
					//{ 252, ""},
					//{ 253, ""},
					//{ 254, ""},
					//{ 255, ""}, // Last Index !
				};

		public static readonly int knowledgeForBackupCiv = 50;
		public static readonly List<int> CompatibleMapHash = new List<int> { 33903987 /*1.1.0*/, };

        public static int CurrentMapHash { get; set; } = 0;

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
		public static int GetCapitalTerritoryIndex(string civilizationName)
		{
			return listTerritories[civilizationName][0];
		}

		public static bool TerritoryHasName(int territoryIndex)
		{
			return territoryNames.ContainsKey(territoryIndex);
		}

		public static string GetTerritoryName(int territoryIndex)
		{
			return territoryNames[territoryIndex];
		}
		 
		public static bool ContinentHasName(int continentIndex)
		{
			return continentNames.ContainsKey(continentIndex);
		}

		public static string GetContinentName(int continentIndex)
		{
			return continentNames[continentIndex];
		}

		public static bool UseTrueCultureLocation()
		{
			if (IsCompatibleMap() && TrueCultureLocation.IsEnabled())
			{
				return true;
			}
			return false;
		}
		public static bool IsCompatibleMap()
		{
			if (CompatibleMapHash.Contains(CurrentMapHash))
			{
				return true;
			}
			return false;
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
					int numTiles = territory.TileIndexes.Length;
					int landTiles = 0;
					for (int j = 0; j < numTiles; j++)
					{
						int num6 = territory.TileIndexes[j];
						ref TileInfo reference = ref Amplitude.Mercury.Sandbox.Sandbox.World.TileInfo.Data[num6];
						if (reference.Elevation > 3)
							landTiles++;
					}

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
