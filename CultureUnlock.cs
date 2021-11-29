using System.Collections.Generic;
using Amplitude;
using Amplitude.Mercury.Interop;
using Amplitude.Mercury.Simulation;

namespace Gedemon.TrueCultureLocation
{
	public class CultureUnlock
	{
        #region Giant Earth Map default settings

        static readonly IDictionary<int, Hexagon.OffsetCoords> ExtraPositionsGiantEarthMap = new Dictionary<int, Hexagon.OffsetCoords> // EmpireIndex (player slots)
				{
					{ 0, new Hexagon.OffsetCoords(32, 41)}, // Nubia 
					{ 1, new Hexagon.OffsetCoords(40, 51)}, // Mesopotamia 
					{ 2, new Hexagon.OffsetCoords(56, 55)}, // India 
					{ 3, new Hexagon.OffsetCoords(17, 52)}, // Carthage
					{ 4, new Hexagon.OffsetCoords(79, 60)}, // China
					{ 5, new Hexagon.OffsetCoords(28, 58)}, // Greece
					{ 6, new Hexagon.OffsetCoords(31, 47)}, // Egypt
					{ 7, new Hexagon.OffsetCoords(42, 57)}, // Assyria
					{ 8, new Hexagon.OffsetCoords(38, 58)}, // Cappadocia
					{ 9, new Hexagon.OffsetCoords(21, 59)}, // Rome
					{ 10, new Hexagon.OffsetCoords(48, 72)}, // Urals
					{ 11, new Hexagon.OffsetCoords(23, 82)}, // Sweden
					{ 12, new Hexagon.OffsetCoords(76, 73)}, // Mongolia
					{ 13, new Hexagon.OffsetCoords(7, 58)}, // Spain
					{ 14, new Hexagon.OffsetCoords(28, 15)}, // South Africa
					{ 15, new Hexagon.OffsetCoords(78, 40)}, // South-East Asia
					{ 16, new Hexagon.OffsetCoords(28, 16)}, // 
					{ 17, new Hexagon.OffsetCoords(78, 41)}, // 
					{ 18, new Hexagon.OffsetCoords(28, 17)}, // 
					{ 19, new Hexagon.OffsetCoords(78, 42)}, // 
					{ 20, new Hexagon.OffsetCoords(28, 18)}, // 
					{ 21, new Hexagon.OffsetCoords(78, 43)}, // 
					{ 22, new Hexagon.OffsetCoords(28, 19)}, // 
					{ 23, new Hexagon.OffsetCoords(78, 44)}, // 
					{ 24, new Hexagon.OffsetCoords(28, 20)}, // 
					{ 25, new Hexagon.OffsetCoords(78, 45)}, // 
					{ 26, new Hexagon.OffsetCoords(28, 21)}, // 
					{ 27, new Hexagon.OffsetCoords(78, 46)}, // 
					{ 28, new Hexagon.OffsetCoords(28, 22)}, // 
					{ 29, new Hexagon.OffsetCoords(78, 47)}, // 
					{ 30, new Hexagon.OffsetCoords(28, 23)}, // 
					{ 31, new Hexagon.OffsetCoords(78, 48)}, // 
				};

		static readonly IDictionary<int, Hexagon.OffsetCoords> ExtraPositionsNewWorldGiantEarthMap = new Dictionary<int, Hexagon.OffsetCoords> // EmpireIndex (player slots)
				{
					{ 0, new Hexagon.OffsetCoords(32, 41)}, // Nubia 
					{ 1, new Hexagon.OffsetCoords(40, 51)}, // Mesopotamia 
					{ 2, new Hexagon.OffsetCoords(56, 55)}, // India 
					{ 3, new Hexagon.OffsetCoords(17, 52)}, // Carthage
					{ 4, new Hexagon.OffsetCoords(79, 60)}, // China
					{ 5, new Hexagon.OffsetCoords(28, 58)}, // Greece
					{ 6, new Hexagon.OffsetCoords(31, 47)}, // Egypt
					{ 7, new Hexagon.OffsetCoords(42, 57)}, // Assyria
					{ 8, new Hexagon.OffsetCoords(38, 58)}, // Cappadocia
					{ 9, new Hexagon.OffsetCoords(146, 48)}, // Central America
					{ 10, new Hexagon.OffsetCoords(140, 57)}, // Texas
					{ 11, new Hexagon.OffsetCoords(23, 80)}, // Sweden
					{ 12, new Hexagon.OffsetCoords(76, 73)}, // Mongolia
					{ 13, new Hexagon.OffsetCoords(154,38)}, // Columbia
					{ 14, new Hexagon.OffsetCoords(28, 15)}, // South Africa
					{ 15, new Hexagon.OffsetCoords(78, 40)}, // South-East Asia
					{ 16, new Hexagon.OffsetCoords(28, 16)}, // 
					{ 17, new Hexagon.OffsetCoords(78, 41)}, // 
					{ 18, new Hexagon.OffsetCoords(28, 17)}, // 
					{ 19, new Hexagon.OffsetCoords(78, 42)}, // 
					{ 20, new Hexagon.OffsetCoords(28, 18)}, // 
					{ 21, new Hexagon.OffsetCoords(78, 43)}, // 
					{ 22, new Hexagon.OffsetCoords(28, 19)}, // 
					{ 23, new Hexagon.OffsetCoords(78, 44)}, // 
					{ 24, new Hexagon.OffsetCoords(28, 20)}, // 
					{ 25, new Hexagon.OffsetCoords(78, 45)}, // 
					{ 26, new Hexagon.OffsetCoords(28, 21)}, // 
					{ 27, new Hexagon.OffsetCoords(78, 46)}, // 
					{ 28, new Hexagon.OffsetCoords(28, 22)}, // 
					{ 29, new Hexagon.OffsetCoords(78, 47)}, // 
					{ 30, new Hexagon.OffsetCoords(28, 23)}, // 
					{ 31, new Hexagon.OffsetCoords(78, 48)}, // 
				};

		static readonly IDictionary<string, List<int>> listMinorFactionTerritoriesGiantEarthMap = new Dictionary<string, List<int>>  // CivName, list of territory indexes
				{
					{ "IndependentPeople_Era1_Peaceful_Akkadians",      new List<int>() { 142, 123 } }, // Mesopotamia, Assyria
					{ "IndependentPeople_Era1_Peaceful_Elamites",       new List<int>() { 243 } }, // Persia
					{ "IndependentPeople_Era1_Peaceful_Noks",           new List<int>() { 220 } }, // Beninum
					{ "IndependentPeople_Era1_Peaceful_NorteChicoPeople",   new List<int>() { 47 } }, // Peruvia
					{ "IndependentPeople_Era1_Violent_Hurrians",        new List<int>() { 123 } }, // Assyria
					{ "IndependentPeople_Era1_Violent_Hyksos",          new List<int>() { 128 } }, // Aegyptus
					{ "IndependentPeople_Era1_Violent_Kassites",        new List<int>() { 142 } }, // Mesopotami
					{ "IndependentPeople_Era1_Violent_Lullubis",        new List<int>() { 142 } }, // Mesopotami
					{ "IndependentPeople_Era1_Violent_Medes",           new List<int>() { 144, 243 } }, // Media, Persia
					{ "IndependentPeople_Era1_Violent_Minoans",         new List<int>() { 101 } }, // Graecia
					{ "IndependentPeople_Era1_Violent_Mittanians",      new List<int>() { 123 } }, // Assyria
					{ "IndependentPeople_Era2_Peaceful_Etruscans",      new List<int>() { 99, 100 } }, // Italia Annonaria, Italia Suburbicaria
					{ "IndependentPeople_Era2_Peaceful_Garamantes",     new List<int>() { 127 } }, // Libya
					{ "IndependentPeople_Era2_Peaceful_Mochicas",       new List<int>() { 47 } }, // Peruvia
					{ "IndependentPeople_Era2_Peaceful_Nazcans",        new List<int>() { 47, 52 } }, // Peruvia, Atacama
					{ "IndependentPeople_Era2_Peaceful_Parthians",      new List<int>() { 230 } }, // Parthia
					{ "IndependentPeople_Era2_Peaceful_Scythians",      new List<int>() { 205, 149, 206, 228, 124, 226 } }, // Chorasmia, Orenburgum, Kazachia, Omium, Ciscaucasia, Voronegia
					{ "IndependentPeople_Era2_Peaceful_Zapotecs",       new List<int>() { 219, 41 } }, // Yucatania, Mexicum
					{ "IndependentPeople_Era2_Violent_Arverni",         new List<int>() { 22 } }, // Occitania
					{ "IndependentPeople_Era2_Violent_Burgundians",     new List<int>() { 117, 105, 99, 22 } }, // Polonia, Germania, Italia Annonaria, Occitania
					{ "IndependentPeople_Era2_Violent_Cantabris",       new List<int>() { 96, 97 } }, // Castella, Aragonia
					{ "IndependentPeople_Era2_Violent_Cherusci",        new List<int>() { 105 } }, // Germania
					{ "IndependentPeople_Era2_Violent_Dacians",         new List<int>() { 103 } }, // Dacia
					{ "IndependentPeople_Era2_Violent_Molossians",      new List<int>() { 101, 102 } }, // Graecia, Illyria
					{ "IndependentPeople_Era2_Violent_Numidians",       new List<int>() { 190 } }, // Numidia
					{ "IndependentPeople_Era2_Violent_Picts",           new List<int>() { 20 } }, // Caledonia
					{ "IndependentPeople_Era2_Violent_Sabines",         new List<int>() { 100, 99 } }, // Italia Suburbicaria, Italia Annonaria
					{ "IndependentPeople_Era2_Violent_Vandals",         new List<int>() { 117, 120, 190, 98, 105, 22, 95 } }, // Polonia, Baltica, Numidia?, Sardinia?, Germania, Occitania, Andalusia
					{ "IndependentPeople_Era3_Peaceful_Armenians",      new List<int>() { 121, 118, 123 } }, // Transcaucasia, Cappadocia, Assyria
					{ "IndependentPeople_Era3_Peaceful_Icelanders",     new List<int>() { 17, 11 } }, // Islandia, Groenlandia
					{ "IndependentPeople_Era3_Peaceful_Khazars",        new List<int>() { 124, 226, 121, 119, 205 } }, // Ciscaucasia, Voronegia, Transcaucasia, Ucraina, Chorasmia
					{ "IndependentPeople_Era3_Peaceful_Malinke",        new List<int>() { 129, 211 } }, // Malia, Guinea
					{ "IndependentPeople_Era3_Peaceful_Toltecs",        new List<int>() { 219, 41 } }, // Yucatania, Mexicum
					{ "IndependentPeople_Era3_Violent_Avars",           new List<int>() { 124, 104, 102, 103 } }, // Ciscaucasia, Danubia, Illyria, Dacia
					{ "IndependentPeople_Era3_Violent_Berbers",         new List<int>() { 188, 190, 126, 127, 125 } }, // Marocum, Numidia, Sahara, Libya, Mauretania
					{ "IndependentPeople_Era3_Violent_Bretons",         new List<int>() { 24 } }, // Neustria
					{ "IndependentPeople_Era3_Violent_Magyars",         new List<int>() { 119, 103, 104 } }, // Ucraina, Dacia, Danubia
					{ "IndependentPeople_Era3_Violent_Normans",         new List<int>() { 24, 21 } }, // Neustria, Anglia
					{ "IndependentPeople_Era3_Violent_Ostrogoths",      new List<int>() { 109, 99, 119, 104 } }, // Suecia, Italia Annonaria, Ucraina, Danubia
					{ "IndependentPeople_Era3_Violent_Scots",           new List<int>() { 20 } }, // Caledonia
					{ "IndependentPeople_Era3_Violent_Somalis",         new List<int>() { 209 } }, // Somalia
					{ "IndependentPeople_Era3_Violent_Thai",            new List<int>() { 168, 169 } }, // Thailandia, Malaesia
					{ "IndependentPeople_Era3_Violent_Wisigoths",       new List<int>() { 103, 109, 22, 95, 97, 96 } }, // Dacia, Suecia, Occitania, Andalusia, Aragonia, Castella
					{ "IndependentPeople_Era4_Peaceful_Inca",           new List<int>() { 47, 52, 246 } }, // Peruvia, Atacama, Chilia
					{ "IndependentPeople_Era4_Peaceful_Malays",         new List<int>() { 169, 170, 178 } }, // Malaesia, Sumatra, Borneum
					{ "IndependentPeople_Era4_Peaceful_Tamils",         new List<int>() { 236, 141 } }, // Dravidia, Sri Lanca
					{ "IndependentPeople_Era4_Peaceful_Tarascans",      new List<int>() { 41 } }, // Mexicum
					{ "IndependentPeople_Era4_Violent_Kongo",           new List<int>() { 214, 137 } }, // Angolia, Congo
					{ "IndependentPeople_Era4_Violent_Mississipians",   new List<int>() { 59, 63, 61, 56 } }, // Mississippia, Lacus Magni, Carolinae, Florida
					{ "IndependentPeople_Era4_Violent_Tlaxcaltecs",     new List<int>() { 41 } }, // Mexicum
				};

		static readonly IDictionary<string, List<int>> listMajorEmpireTerritoriesGiantEarthMap = new Dictionary<string, List<int>>  // CivName, list of territory indexes
				{
					{ "Civilization_Era1_Phoenicia",                new List<int>() { 188, 190, 106, 127, 95, 100, 99, 22 } }, // Morocco, Carthage, Liban, Lybia, South Spain
					{ "Civilization_Era1_EgyptianKingdom",          new List<int>() { 128 } }, // Egypt
					{ "Civilization_Era1_HittiteEmpire",            new List<int>() { 118 } }, // East/West Anatiolia
					{ "Civilization_Era1_Babylon",                  new List<int>() { 142 } }, // Iraq
					{ "Civilization_Era1_Assyria",                  new List<int>() { 123 } }, // Iraq + Steppe
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
					{ "Civilization_Era2_Goths",                    new List<int>() { 109, 117, 119, 120 } }, // South Sweden, Poland, Ukraine, Baltica
					{ "Civilization_Era2_CelticCivilization",       new List<int>() { 24, 19, 20, 21, 22, 96 } }, // France, British islands, NW Spain
					{ "Civilization_Era2_Carthage",                 new List<int>() { 190, 188, 98, 95 } }, // Carthage, Morroco, Sardaigna, South Spain
					{ "Civilization_Era2_AncientGreece",            new List<int>() { 101, 102, 238, 103, 118 } }, // Greece, Balkans, Bulgaria/Romania, Anatolia
					{ "Civilization_Era2_AksumiteEmpire",           new List<int>() { 132, 209, 143, 233 } }, // Abyssinia, Ethiopia/Somalia, Sudan, Yemen
					{ "Civilization_Era3_Vikings",                  new List<int>() { 107, 109, 111, 112, 191, 152, 17, 11 } }, // Norway, Sweden 
					{ "Civilization_Era3_UmayyadCaliphate",         new List<int>() { 106, 123, 142, 143, 227, 144, 243, 146, 207, 128, 127, 190, 188, 95 } }, // Syria, West Anatolia, Iraq, Saudi Arabia, Iran, Afghanistan, Pakistan, Egypt, Libya, Carthage, Morroco, South Spain
					{ "Civilization_Era3_MongolEmpire",             new List<int>() { 225, 239, 240, 229, 161, 208, 187, 151, 206, 228, 230, 205, 149, 193 } }, // Mongolia
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
					{ "Civilization_Era4_OttomanEmpire",            new List<int>() { 118, 123, 101, 102, 103, 142, 106, 128, 238, 250 } }, // Anatolia, Greece, Balkans, Bulgaria/Romania, 
					{ "Civilization_Era4_MughalEmpire",             new List<int>() { 147, 207, 145, 231, 146, 236, 237 } }, // India, Pakistan, Bengladesh, Afghanistan
					{ "Civilization_Era4_MingChina",                new List<int>() { 164, 162, 163, 221, 229, 241 } }, // China
					{ "Civilization_Era4_JoseonKorea",              new List<int>() { 159 } }, // Korea
					{ "Civilization_Era4_IroquoisConfederacy",      new List<int>() { 60, 63 } }, // NE of North America
					{ "Civilization_Era4_Holland",                  new List<int>() { 23, 57, 50, 136, 141, 170, 175, 178, 176, 177  } }, // Holland, Antilles, Guyana, South Africa, Sri Lanka, Indonesia
					{ "Civilization_Era5_ZuluKingdom",              new List<int>() { 136, 222 } }, // South Africa, Mozambique/Zimbabwe
					{ "Civilization_Era5_Siam",                     new List<int>() { 166, 168, 38 } }, // Thailand, Cambodia
					{ "Civilization_Era5_RussianEmpire",            new List<int>() { 122, 116, 121, 119, 226, 149, 189, 148, 115, 205, 206, 228, 223, 224, 194, 151, 193, 153, 192, 225, 154, 229, 161, 157, 195, 158, 196, 155, 27, 28, 197, 0, 1, 110, 239, 114, 108, 124 } }, // Moscow, St Petersburg, Russia...
					{ "Civilization_Era5_Mexico",                   new List<int>() { 41, 45, 219, 62, 58 } }, // Mexico
					{ "Civilization_Era5_Italy",                    new List<int>() { 100, 99, 127, 132, 209 } }, // Italy, Lybia, Sudan, Ethiopia
					{ "Civilization_Era5_Germany",                  new List<int>() { 105, 104, 117, 120 } }, // Germany
					{ "Civilization_Era5_FrenchRepublic",           new List<int>() { 24, 22, 57, 50, 190, 188, 126, 220, 212, 129, 18, 213, 137, 135, 167, 180, 15, 14, 201, 12, 106} }, // France, Antilles, Guyana, North Africa, Madagascar, Quebec, Canada
					{ "Civilization_Era5_BritishEmpire",            new List<int>() { 21, 20, 19, 14, 201, 10, 60, 61, 63, 12, 5, 9, 8, 6, 199, 200, 128, 132, 131, 215, 222, 210, 136, 142, 227, 207, 145, 147, 231, 31, 32, 33, 34, 35, 36, 37, 38, 39 } }, // England
					{ "Civilization_Era5_AustriaHungary",           new List<int>() { 104, 102 } }, // Austria/hungary
					{ "Civilization_Era5_AfsharidPersia",           new List<int>() { 243, 144, 146, 230, 205, 142, 227, 235 } }, // Iran
					{ "Civilization_Era6_USSR",                     new List<int>() { 122, 116, 121, 119, 226, 149, 189, 148, 115, 205, 206, 228, 223, 224, 194, 151, 193, 153, 192, 225, 154, 229, 161, 157, 195, 158, 196, 155, 27, 28, 197, 0, 1, 110, 239, 114, 108, 124 } }, // Moscow
					{ "Civilization_Era6_USA",                      new List<int>() { 60, 61, 56, 63, 59, 58, 64, 65, 219, 204, 66, 62, 7, 198, 3, 4 } }, // Washington, USA
					{ "Civilization_Era6_Turkey",                   new List<int>() { 118, 123, 250 } }, // Anatolia
					{ "Civilization_Era6_Sweden",                   new List<int>() { 109, 110 } }, // Stockholm
					{ "Civilization_Era6_Japan",                    new List<int>() { 29, 30, 165, 27 } }, // Japan
					{ "Civilization_Era6_India",                    new List<int>() { 147, 145, 207, 231, 236, 237 } }, // India, Pakistan, Bengladesh 
					{ "Civilization_Era6_Egypt",                    new List<int>() { 128, 233 } }, // Egypt
					{ "Civilization_Era6_China",                    new List<int>() { 162, 163, 164, 221, 229, 208, 187, 150, 179, 173, 244, 241 } }, // China
					{ "Civilization_Era6_Brazil",                   new List<int>() { 54, 49, 53, 46 } }, // Brazil
					{ "Civilization_Era6_Australia",                new List<int>() { 35, 31, 32, 33, 34, 36, 37, 38, 39 } }  // Australia
				};

		static readonly IDictionary<string, List<int>> listMajorEmpireCoreTerritoriesGiantEarthMap = new Dictionary<string, List<int>>  // CivName, list of territory indexes
				{
					{ "Civilization_Era1_Phoenicia",                new List<int>() { 188, 190, 106, 127, 95, 100, 99, 22 } }, // Morocco, Carthage, Liban, Lybia, South Spain
					{ "Civilization_Era1_EgyptianKingdom",          new List<int>() { 128 } }, // Egypt
					{ "Civilization_Era1_HittiteEmpire",            new List<int>() { 118 } }, // East/West Anatiolia
					{ "Civilization_Era1_Babylon",                  new List<int>() { 142 } }, // Iraq
					{ "Civilization_Era1_Assyria",                  new List<int>() { 123 } }, // Iraq + Steppe
					{ "Civilization_Era1_HarappanCivilization",     new List<int>() { 207, 147, 145 } }, // Pakistan, North India
					{ "Civilization_Era1_MycenaeanCivilization",    new List<int>() { 101, 102, 238 } }, // Greece, Balkans, Thrace
					{ "Civilization_Era1_Nubia",                    new List<int>() { 233, 132, 209, 213 } }, // Sudan, Abyssinian, Ethiopia/Somalia, Central African Republic
					{ "Civilization_Era1_ZhouChina",                new List<int>() { 162, 163, 164, 221 } }, // China
					{ "Civilization_Era1_OlmecCivilization",        new List<int>() { 45, 41, 48, 219, 62, 58, 50, 47, 46 } }, // South Mexico/Central America
					{ "Civilization_Era2_RomanEmpire",              new List<int>() { 100 } }, // South Italy
					{ "Civilization_Era2_Persia",                   new List<int>() { 243 } }, // Persia
					{ "Civilization_Era2_MayaCivilization",         new List<int>() { 219 } }, // Yucatan
					{ "Civilization_Era2_MauryaEmpire",             new List<int>() { 147 } }, // North India
					{ "Civilization_Era2_Huns",                     new List<int>() { 226, 124, 149, 205, 206, 245, 230 } }, // Voronegia, Ciscaucasia, 3, 4, Kazachia, 6, 7
					{ "Civilization_Era2_Goths",                    new List<int>() { 117, 119, 103 } }, // Poland, Ukraine
					{ "Civilization_Era2_CelticCivilization",       new List<int>() { 104, 22, 24, 23, 21, 19, 96, 105 } }, // Danubia, Occitania, Neustria, Batavia, Britannia, Hibernia, Castella, Germania
					{ "Civilization_Era2_Carthage",                 new List<int>() { 190 } }, // Carthage
					{ "Civilization_Era2_AncientGreece",            new List<int>() { 101 } }, // Greece
					{ "Civilization_Era2_AksumiteEmpire",           new List<int>() { 132 } }, // Abyssinia
					{ "Civilization_Era3_Vikings",                  new List<int>() { 107, 191, 109 } }, // Norway, Sweden
					{ "Civilization_Era3_UmayyadCaliphate",         new List<int>() { 106 } }, // Syria
					{ "Civilization_Era3_MongolEmpire",             new List<int>() { 225, 240, 208, 187, 206 } }, // Mongolia, Gobi, Altai, Tarim, Kazachia
					{ "Civilization_Era3_MedievalEngland",          new List<int>() { 21 } }, // South England
					{ "Civilization_Era3_KhmerEmpire",              new List<int>() { 38 } }, // Cambodia
					{ "Civilization_Era3_HolyRomanEmpire",          new List<int>() { 105 } }, // Germany
					{ "Civilization_Era3_GhanaEmpire",              new List<int>() { 125  } }, // Mauretania
					{ "Civilization_Era3_FrankishKingdom",          new List<int>() { 24 } }, // North France
					{ "Civilization_Era3_Byzantium",                new List<int>() { 238, 101, 250, 118, 106, 128, 127 } }, // Thracia, Greece, Lydia, Cappadocia, Syria, Egypt, Libya
					{ "Civilization_Era3_AztecEmpire",              new List<int>() { 41 } }, // Mexica
					{ "Civilization_Era4_VenetianRepublic",         new List<int>() { 99 } }, // North Italy
					{ "Civilization_Era4_TokugawaShogunate",        new List<int>() { 29, 30, 165, 242 } }, // Japan
					{ "Civilization_Era4_Spain",                    new List<int>() { 96 } }, // Castella
					{ "Civilization_Era4_PolishKingdom",            new List<int>() { 117 } }, // Poland
					{ "Civilization_Era4_OttomanEmpire",            new List<int>() { 250 } }, // Lydia
					{ "Civilization_Era4_MughalEmpire",             new List<int>() { 147 } }, // North India
					{ "Civilization_Era4_MingChina",                new List<int>() { 241 } }, // Kiangnanum
					{ "Civilization_Era4_JoseonKorea",              new List<int>() { 159 } }, // Korea
					{ "Civilization_Era4_IroquoisConfederacy",      new List<int>() { 63 } }, // Great Lakes
					{ "Civilization_Era4_Holland",                  new List<int>() { 23 } }, // Holland
					{ "Civilization_Era5_ZuluKingdom",              new List<int>() { 136 } }, // South Africa
					{ "Civilization_Era5_Siam",                     new List<int>() { 168 } }, // Thailand
					{ "Civilization_Era5_RussianEmpire",            new List<int>() { 122, 116, 114, 189, 148, 110, 112 } }, // Moscow, Novogardia, Biarma, Casanum, Nenetsia, Permia, Kola
					{ "Civilization_Era5_Mexico",                   new List<int>() { 41 } }, // Mexica
					{ "Civilization_Era5_Italy",                    new List<int>() { 100, 99, 98 } }, // Italy, Sardinia
					{ "Civilization_Era5_Germany",                  new List<int>() { 105, 117 } }, // Germany, Poland
					{ "Civilization_Era5_FrenchRepublic",           new List<int>() { 24, 22 } }, // France
					{ "Civilization_Era5_BritishEmpire",            new List<int>() { 21, 20, 19 } }, // England, Scotland, Ireland
					{ "Civilization_Era5_AustriaHungary",           new List<int>() { 104, 102 } }, // Danubia, Illyria
					{ "Civilization_Era5_AfsharidPersia",           new List<int>() { 243, 144, 235 } }, // Persia, Media, Gedrosia
					{ "Civilization_Era6_USSR",                     new List<int>() { 122, 116, 226, 124 } }, // Moscow, Novogardia, Voronegia, Ciscaucasia
					{ "Civilization_Era6_USA",                      new List<int>() { 60, 61 } }, // Massachusetta, Carolinae
					{ "Civilization_Era6_Turkey",                   new List<int>() { 250, 118 } }, // Lydia, Cappadocia
					{ "Civilization_Era6_Sweden",                   new List<int>() { 109, 152 } }, // Suecia, Lapponia
					{ "Civilization_Era6_Japan",                    new List<int>() { 29, 30, 165, 242 } }, // Japan
					{ "Civilization_Era6_India",                    new List<int>() { 147, 145, 236, 237, 179, 231 } }, // India, Maharastra, Dravidia, Odisa, Casmiria, Bengala
					{ "Civilization_Era6_Egypt",                    new List<int>() { 128 } }, // Egypt
					{ "Civilization_Era6_China",                    new List<int>() { 229 } }, // Manchuria
					{ "Civilization_Era6_Brazil",                   new List<int>() { 54 } }, // Brasilia Australis
					{ "Civilization_Era6_Australia",                new List<int>() { 35, 31, 32, 33, 34, 39 } }  // Australia
				};

		static readonly IDictionary<int, string> continentNamesGiantEarthMap = new Dictionary<int, string>
				{
					{ 0, "Oceans"},
					{ 1, "Americas"},
					{ 2, "Eurasiafrica"},
					{ 3, "Australia"},
					{ 5, "Greenland"},
				};

		static readonly IDictionary<int, string> territoryNamesGiantEarthMap = new Dictionary<int, string>
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
					{ 118, "Cappadocia"},
					{ 119, "Ucraina"},
					{ 120, "Baltica"},
					{ 121, "Transcaucasia"},
					{ 122, "Moscovia"},
					{ 123, "Assyria"},
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
					{ 250, "Lydia"},
					{ 251, "Ultima Thule"},
					//{ 252, ""},
					//{ 253, ""},
					//{ 254, ""},
					//{ 255, ""}, // Last Index !
				};

		public static readonly List<int> GiantEarthMapHash = new List<int> { -819807177 /*1.1.0*/, -288044546 /*1.1.1*/, };

		#endregion

		static IDictionary<int, Hexagon.OffsetCoords> ExtraPositions = new Dictionary<int, Hexagon.OffsetCoords>();

		static IDictionary<int, Hexagon.OffsetCoords> ExtraPositionsNewWorld = new Dictionary<int, Hexagon.OffsetCoords>();

		static List<string>[] territoriesWithMinorFactions;
		static List<string>[] territoriesWithMajorEmpires;

		static IDictionary<string, List<int>> listMinorFactionTerritories = new Dictionary<string, List<int>>();
		static IDictionary<string, List<int>> listMajorEmpireTerritories = new Dictionary<string, List<int>>();
		static IDictionary<string, List<int>> listMajorEmpireCoreTerritories = new Dictionary<string, List<int>>();

		static IDictionary<int, string> continentNames = new Dictionary<int, string>();

		static IDictionary<int, string> territoryNames = new Dictionary<int, string>();

		static readonly IDictionary<string, List<int>> listSlots = new Dictionary<string, List<int>>  // civName, list of player slots (majorEmpire.Index) starting at 0
		{
			//{ "Civilization_Era1_Assyria", new List<int>() { 6, 7 } },
		};

		static readonly List<string> nomadCultures = new List<string> { "Civilization_Era2_Huns", "Civilization_Era3_MongolEmpire" };

		static readonly List<string> firstEraBackup = new List<string> { "Civilization_Era1_Assyria", };

		static readonly List<string> noCapitalTerritory = new List<string> { "Civilization_Era1_Assyria", "Civilization_Era1_HarappanCivilization", "Civilization_Era1_Nubia", "Civilization_Era1_ZhouChina", "Civilization_Era1_MycenaeanCivilization", "Civilization_Era1_Phoenicia", "Civilization_Era1_OlmecCivilization", "Civilization_Era2_Huns", "Civilization_Era2_Goths", "Civilization_Era2_CelticCivilization", "Civilization_Era3_Vikings", "Civilization_Era3_MongolEmpire", "Civilization_Era4_TokugawaShogunate" };

		public static readonly int knowledgeForBackupCiv = 25;

		public static readonly int maxNumTerritories = 256;

		public static int CurrentMapHash { get; set; } = 0;
		public static bool IsMapValidforTCL { get; set; } = false;
		public static void InitializeTCL()
		{

			Diagnostics.LogWarning($"[Gedemon] [CultureUnlock] Initializing TCL");

			if(GiantEarthMapHash.Contains(CurrentMapHash))
			{
				Diagnostics.LogWarning($"[Gedemon] [CultureUnlock] Giant Earth Map detected, set default values (CurrentMapHash = {CurrentMapHash})");
				ExtraPositions = ExtraPositionsGiantEarthMap;
				ExtraPositionsNewWorld = ExtraPositionsNewWorldGiantEarthMap;
				continentNames = continentNamesGiantEarthMap;
				territoryNames = territoryNamesGiantEarthMap;
				listMinorFactionTerritories = listMinorFactionTerritoriesGiantEarthMap;
				listMajorEmpireTerritories = listMajorEmpireTerritoriesGiantEarthMap;
				listMajorEmpireCoreTerritories = listMajorEmpireCoreTerritoriesGiantEarthMap;
			}

			ModLoading.BuildModdedLists();

			Diagnostics.LogWarning($"[Gedemon] [CultureUnlock] building territoriesWithMinorFactions[] and territoriesWithMajorEmpires[]");

			territoriesWithMinorFactions = new List<string>[maxNumTerritories];
			territoriesWithMajorEmpires = new List<string>[maxNumTerritories];

			for (int i = 0; i < maxNumTerritories; i++)
			{
				territoriesWithMinorFactions[i] = new List<string>();
				territoriesWithMajorEmpires[i] = new List<string>();
			}

			foreach (KeyValuePair<string, List<int>> minorTerritories in listMinorFactionTerritories)
			{
				foreach (int index in minorTerritories.Value)
				{
					territoriesWithMinorFactions[index].Add(minorTerritories.Key);
				}
			}

			foreach (KeyValuePair<string, List<int>> majorTerritories in listMajorEmpireTerritories)
			{
				if (HasNoCapitalTerritory(majorTerritories.Key))
				{
					foreach (int index in majorTerritories.Value)
					{
						territoriesWithMajorEmpires[index].Add(majorTerritories.Key);
					}
				}
				else
                {
					territoriesWithMajorEmpires[majorTerritories.Value[0]].Add(majorTerritories.Key);
				}
			}

			IsMapValidforTCL = ValidateMapTCL();

			if (!CultureUnlock.IsCompatibleMap())
			{
				Diagnostics.LogError($"[Gedemon] incompatible Map");
			}

		}

		public static bool ValidateMapTCL()
        {
			if(TrueCultureLocation.IsEnabled() && listMajorEmpireTerritories.Count == 0)
			{
				Diagnostics.LogError($"[Gedemon] Error : TrueCultureLocation.IsEnabled = {TrueCultureLocation.IsEnabled()} && listMajorEmpireTerritories.Count = {listMajorEmpireTerritories.Count}");
				return false;
            }

			if (TrueCultureLocation.GetTotalEmpireSlots() >= 10 && TrueCultureLocation.GetTotalEmpireSlots() > System.Math.Max(ExtraPositions.Count, ExtraPositionsNewWorld.Count))
			{
				Diagnostics.LogError($"[Gedemon] Error : TotalEmpireSlots ({TrueCultureLocation.GetTotalEmpireSlots()}) > ExtraPositions ({System.Math.Max(ExtraPositions.Count, ExtraPositionsNewWorld.Count)})");
				return false;
			}

			return true;
        }
		public static bool HasAnyMinorFactionPosition(int territoryIndex)
		{
			return territoriesWithMinorFactions[territoryIndex].Count > 0;
		}

		public static bool IsMinorFactionPosition(int territoryIndex, string minorFactionName)
		{
			return territoriesWithMinorFactions[territoryIndex].Contains(minorFactionName);
		}

		public static List<string> GetListMinorFactionsForTerritory(int territoryIndex)
		{
			return territoriesWithMinorFactions[territoryIndex];
		}

		public static bool HasAnyMajorEmpirePosition(int territoryIndex)
		{
			return territoriesWithMajorEmpires[territoryIndex].Count > 0;
		}

		public static bool IsMajorEmpirePosition(int territoryIndex, string majorEmpireName)
		{
			return territoriesWithMajorEmpires[territoryIndex].Contains(majorEmpireName);
		}
		public static bool IsMajorEmpirePosition(int territoryIndex, StaticString majorEmpireName)
		{
			return territoriesWithMajorEmpires[territoryIndex].Contains(majorEmpireName.ToString());
		}

		public static List<string> GetListMajorEmpiresForTerritory(int territoryIndex)
        {
			return territoriesWithMajorEmpires[territoryIndex];
		}
		public static bool HasCoreTerritories(string factionName)
		{
			return listMajorEmpireCoreTerritories.ContainsKey(factionName);
		}

		public static bool HasMajorTerritories(string factionName)
		{
			return listMajorEmpireTerritories.ContainsKey(factionName);
		}

		public static bool HasMinorTerritories(string factionName)
		{
			return listMinorFactionTerritories.ContainsKey(factionName);
		}

		public static bool HasMinorTerritories(StaticString FactionName)
		{
			return HasMinorTerritories(FactionName.ToString());
		}

		public static bool HasCoreTerritory(string factionName, int territoryIndex)
		{
			if(TrueCultureLocation.KeepOnlyCoreTerritories())
			{
				if (HasCoreTerritories(factionName))
					return listMajorEmpireCoreTerritories[factionName].Contains(territoryIndex);
				else
					return false;

			}
			return HasTerritory(factionName, territoryIndex);
		}

		public static bool HasCoreTerritory(StaticString FactionName, int territoryIndex)
		{
			return HasCoreTerritory(FactionName.ToString(), territoryIndex);
		}

		public static bool HasTerritory(string factionName, int territoryIndex)
		{
			if (HasMajorTerritories(factionName))
				return listMajorEmpireTerritories[factionName].Contains(territoryIndex);
			else if (HasMinorTerritories(factionName))
				return listMinorFactionTerritories[factionName].Contains(territoryIndex);
			else
				return false;
		}

		public static bool HasTerritory(StaticString FactionName, int territoryIndex)
		{
			return HasTerritory(FactionName.ToString(), territoryIndex);
		}

		public static bool HasCoreTerritory(string factionName, int territoryIndex, bool any)
		{
			if (any)
			{
				return HasCoreTerritory(factionName, territoryIndex);
			}
			else
			{
				if (HasCoreTerritories(factionName))
                {
					if (TrueCultureLocation.KeepOnlyCoreTerritories())
						return listMajorEmpireCoreTerritories[factionName][0] == territoryIndex;
					else
						return listMajorEmpireTerritories[factionName][0] == territoryIndex;
				}
			}
			return false;
		}


		public static bool IsUnlockedByPlayerSlot(string civilizationName, int empireIndex)
		{
			return listSlots.ContainsKey(civilizationName) && listSlots[civilizationName].Contains(empireIndex);
		}

		public static bool IsFirstEraBackupCivilization(string civilizationName)
		{
			return firstEraBackup.Contains(civilizationName);
		}

		public static bool IsNomadCulture(string civilizationName)
		{
			return nomadCultures.Contains(civilizationName);
		}

		public static bool HasNoCapitalTerritory(string civilizationName) // can be picked by multiple players
		{
			return noCapitalTerritory.Contains(civilizationName);
		}

		public static List<int> GetListTerritories(string factionName)
		{
			if (TrueCultureLocation.KeepOnlyCoreTerritories())
			{
				if (listMajorEmpireCoreTerritories.TryGetValue(factionName, out List<int> listTerritories))
				{
					return listTerritories;
				}
				else
				{
					return listMajorEmpireTerritories[factionName];
				}
			}
            else
			{
				return listMajorEmpireTerritories[factionName];

			}
		}
		public static List<int> GetListTerritories(StaticString FactionName)
		{
			return GetListTerritories(FactionName.ToString());
		}

		public static int GetCapitalTerritoryIndex(string civilizationName)
		{
			return listMajorEmpireTerritories[civilizationName][0];
		}

		public static bool TerritoryHasName(int territoryIndex)
		{
			return territoryNames.ContainsKey(territoryIndex);
		}

		public static string GetTerritoryName(int territoryIndex)
		{
			if (territoryNames.TryGetValue(territoryIndex, out string name))
			{
				return name;
			}
			return Amplitude.Mercury.UI.Utils.GameUtils.GetTerritoryName(territoryIndex);
		}
		public static string GetTerritoryName(int territoryIndex, bool hasName)
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

		public static Hexagon.OffsetCoords GetExtraStartingPosition(int empireIndex, bool OldWorldOnly)
		{
			if(OldWorldOnly)
            {
				return ExtraPositions.ContainsKey(empireIndex) ? ExtraPositions[empireIndex] : ExtraPositionsNewWorld[empireIndex];
			}
			else
			{
				return ExtraPositionsNewWorld.ContainsKey(empireIndex) ? ExtraPositionsNewWorld[empireIndex] : ExtraPositions[empireIndex];
			}
		}

		public static bool HasExtraStartingPosition(int empireIndex, bool OldWorldOnly)
		{
			return (ExtraPositions.ContainsKey(empireIndex) || ExtraPositionsNewWorld.ContainsKey(empireIndex));
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
			if (IsMapValidforTCL)
			{
				return true;
			}
			return false;
		}
		public static void UpdateListMajorEmpireTerritories(string factionName, List<int> listTerritories)
        {
			if(listMajorEmpireTerritories.ContainsKey(factionName))
            {
				listMajorEmpireTerritories[factionName] = listTerritories;
			}
			else
            {
				listMajorEmpireTerritories.Add(factionName, listTerritories);
			}
		}
		public static void UpdateListMajorEmpireCoreTerritories(string factionName, List<int> listTerritories)
		{
			if (listMajorEmpireCoreTerritories.ContainsKey(factionName))
			{
				listMajorEmpireCoreTerritories[factionName] = listTerritories;
			}
			else
			{
				listMajorEmpireCoreTerritories.Add(factionName, listTerritories);
			}
		}
		public static void UpdateListMinorFactionTerritories(string factionName, List<int> listTerritories)
		{
			if (listMinorFactionTerritories.ContainsKey(factionName))
			{
				listMinorFactionTerritories[factionName] = listTerritories;
			}
			else
			{
				listMinorFactionTerritories.Add(factionName, listTerritories);
			}
		}
		public static void UpdateListTerritoryNames(int territoryIndex, string name)
		{
			if (territoryNames.ContainsKey(territoryIndex))
			{
				territoryNames[territoryIndex] = name;
			}
			else
			{
				territoryNames.Add(territoryIndex, name);
			}
		}
		public static void UpdateListContinentNames(int index, string name)
		{
			if (continentNames.ContainsKey(index))
			{
				continentNames[index] = name;
			}
			else
			{
				continentNames.Add(index, name);
			}
		}

		public static void UpdateListExtraPositions(int index, Hexagon.OffsetCoords position)
		{
			if (ExtraPositions.ContainsKey(index))
			{
				ExtraPositions[index] = position;
			}
			else
			{
				ExtraPositions.Add(index, position);
			}
		}
		public static void UpdateListExtraPositionsNewWorld(int index, Hexagon.OffsetCoords position)
		{
			if (ExtraPositionsNewWorld.ContainsKey(index))
			{
				ExtraPositionsNewWorld[index] = position;
			}
			else
			{
				ExtraPositionsNewWorld.Add(index, position);
			}
		}
		public static void UpdateListNoCapitalTerritory(string factionName)
		{
			if (!noCapitalTerritory.Contains(factionName))
			{
				noCapitalTerritory.Add(factionName);
			}
		}
		public static void UpdateListNomads(string factionName)
		{
			if (!nomadCultures.Contains(factionName))
			{
				nomadCultures.Add(factionName);
			}
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

		public static void logEmpiresTerritories()
		{
			foreach (KeyValuePair<string, List<int>> kvp in listMajorEmpireTerritories)
            {
				Diagnostics.Log($"[Gedemon] Culture : {kvp.Key}");
				foreach(int territoryIndex in kvp.Value)
                {
					Diagnostics.Log($"[Gedemon] - {GetTerritoryName(territoryIndex)}");
				}
			}
		}
	}
}
