using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Mercury;
using Amplitude.Mercury.Data.Simulation;
using Amplitude.Mercury.Interop;
using Amplitude.Mercury.Presentation;
using Amplitude.Mercury.Simulation;

namespace Gedemon.TrueCultureLocation
{

	public class CultureUnlock
	{
		#region Giant Earth Map default settings

		public class TerritoryData // Hardcoded data referencing the Giant Earth Map territories, to build a territory unlocking list on generated maps
		{
			public int Index { get; set; }
			public byte Biome { get; set; }
			public int Row { get; set; }
			public int Column { get; set; }
			public int Continent { get; set; }
			public int Size { get; set; }
			public int LandSize { get; set; }

			public TerritoryData(int index, byte biome, int row, int column, int continent, int size, int landSize)
			{
				Index = index;
				Biome = biome;
				Row = row;
				Column = column;
				Continent = continent;
				Size = size;
				LandSize = landSize;
			}

		}

		public class ContinentData // data referencing the Giant Earth Map Continents
		{
			public int Index { get; set; }
			public string Name { get; set; }
			public int Row { get; set; }
			public int Column { get; set; }
			public int Size { get; set; }

			public ContinentData(string name, int index, int size, int column, int row)
			{
				Index = index;
				Name = name;
				Row = row;
				Column = column;
				Size = size;
			}

		}

		static readonly IDictionary<string, TerritoryData> TerritoryReferenceGiantEarthMap = new Dictionary<string, TerritoryData>
			{

				{ "Mare Beringianum", new TerritoryData(0, 0, 77, 109, 0, 35, 2) },
				{ "Chatanga", new TerritoryData(1, 9, 90, 76, 7, 46, 36) },
				{ "Flumen Cromwellianum", new TerritoryData(2, 0, 33, 135, 0, 161, 0) },
				{ "Insulae Aleutiae", new TerritoryData(3, 6, 76, 111, 0, 46, 4) },
				{ "Alasca", new TerritoryData(4, 0, 87, 116, 1, 127, 77) },
				{ "Saskatchewan", new TerritoryData(5, 3, 75, 138, 1, 50, 48) },
				{ "Columbia Britannica", new TerritoryData(6, 7, 77, 132, 1, 51, 46) },
				{ "Haida Gwaii", new TerritoryData(7, 7, 75, 127, 0, 24, 5) },
				{ "Kitlineq", new TerritoryData(8, 0, 91, 127, 0, 85, 36) },
				{ "Nunavut", new TerritoryData(9, 0, 86, 147, 1, 105, 52) },
				{ "Baffin", new TerritoryData(10, 0, 89, 151, 0, 80, 40) },
				{ "Groenlandia", new TerritoryData(11, 0, 88, 173, 1, 178, 124) }, // continent ID #5 on the Giant Earth, but attached to North-America (continent ID #1) for reference usage, as a one-territory continent would mess the pairing code
				{ "Keewatin", new TerritoryData(12, 6, 78, 151, 1, 104, 75) },
				{ "Canada", new TerritoryData(13, 7, 71, 163, 1, 62, 43) },
				{ "Ungava", new TerritoryData(14, 6, 80, 153, 1, 96, 60) },
				{ "Terra Nova", new TerritoryData(15, 7, 73, 166, 0, 43, 10) },
				{ "Mare Laboratorium", new TerritoryData(16, 0, 81, 163, 0, 47, 0) },
				{ "Islandia", new TerritoryData(17, 0, 88, 1, 0, 93, 15) },
				{ "Tzadia", new TerritoryData(18, 2, 41, 23, 6, 49, 48) },
				{ "Hibernia", new TerritoryData(19, 7, 76, 4, 0, 41, 14) },
				{ "Caledonia", new TerritoryData(20, 7, 82, 12, 0, 75, 22) },
				{ "Britannia", new TerritoryData(21, 7, 73, 13, 0, 59, 28) },
				{ "Occitania", new TerritoryData(22, 4, 62, 15, 2, 41, 26) },
				{ "Batavia", new TerritoryData(23, 7, 72, 17, 2, 23, 14) },
				{ "Neustria", new TerritoryData(24, 7, 67, 17, 2, 61, 44) },
				{ "Flumen Kuroshio", new TerritoryData(25, 7, 65, 108, 0, 215, 0) },
				{ "Flumen Californiense", new TerritoryData(26, 7, 65, 122, 0, 262, 0) },
				{ "Esonia", new TerritoryData(27, 7, 73, 99, 0, 45, 13) },
				{ "Camtschatca", new TerritoryData(28, 6, 80, 103, 7, 68, 33) },
				{ "Iaponia Boreales", new TerritoryData(29, 7, 64, 100, 0, 66, 22) },
				{ "Iaponia Australes", new TerritoryData(30, 7, 55, 91, 0, 55, 20) },
				{ "Australia Occidentalis", new TerritoryData(31, 8, 16, 88, 3, 125, 76) },
				{ "Terra Reginae", new TerritoryData(32, 8, 21, 101, 3, 104, 63) },
				{ "Australia Septentrionalis", new TerritoryData(33, 8, 22, 95, 3, 56, 43) },
				{ "Australia Australis", new TerritoryData(34, 8, 13, 92, 3, 54, 37) },
				{ "Cambria Australis", new TerritoryData(35, 7, 11, 106, 3, 79, 51) },
				{ "Te Ika-a-Maui", new TerritoryData(36, 7, 10, 118, 0, 79, 13) },
				{ "Te Waipounamu", new TerritoryData(37, 7, 4, 115, 0, 64, 16) },
				{ "Cambosia", new TerritoryData(38, 8, 39, 77, 7, 28, 15) },
				{ "Tasmania", new TerritoryData(39, 3, 5, 99, 0, 26, 5) },
				{ "Magnus Sinus Australianus", new TerritoryData(40, 0, 3, 93, 0, 139, 0) },
				{ "Mexicum", new TerritoryData(41, 7, 50, 138, 1, 56, 31) },
				{ "Samoa", new TerritoryData(42, 8, 25, 120, 0, 54, 5) },
				{ "Oceanus Indicus", new TerritoryData(43, 7, 25, 71, 0, 203, 0) },
				{ "Mare Tasmanianum", new TerritoryData(44, 8, 8, 107, 0, 49, 0) },
				{ "Guatemala", new TerritoryData(45, 8, 44, 150, 1, 50, 20) },
				{ "Amazonia", new TerritoryData(46, 8, 33, 157, 4, 53, 53) },
				{ "Peruvia", new TerritoryData(47, 8, 31, 152, 4, 55, 33) },
				{ "Columbia", new TerritoryData(48, 8, 39, 149, 4, 42, 26) },
				{ "Caatinga", new TerritoryData(49, 7, 30, 173, 4, 72, 43) },
				{ "Guiana", new TerritoryData(50, 8, 39, 162, 4, 36, 24) },
				{ "Patagonia", new TerritoryData(51, 6, 11, 161, 4, 34, 24) },
				{ "Atacama", new TerritoryData(52, 1, 23, 155, 4, 46, 28) },
				{ "Pantanal", new TerritoryData(53, 8, 26, 164, 4, 46, 46) },
				{ "Brasilia Australis", new TerritoryData(54, 3, 22, 168, 4, 68, 40) },
				{ "Mare Caribaeum", new TerritoryData(55, 8, 50, 154, 0, 73, 20) },
				{ "Florida", new TerritoryData(56, 8, 57, 149, 1, 42, 19) },
				{ "Antillae", new TerritoryData(57, 8, 47, 161, 0, 30, 4) },
				{ "Texia", new TerritoryData(58, 1, 58, 140, 1, 47, 37) },
				{ "Mississippia", new TerritoryData(59, 3, 61, 143, 1, 41, 33) },
				{ "Massachusetta", new TerritoryData(60, 7, 67, 160, 1, 44, 28) },
				{ "Carolinae", new TerritoryData(61, 7, 62, 152, 1, 48, 37) },
				{ "Chihuahua", new TerritoryData(62, 1, 55, 137, 1, 65, 34) },
				{ "Lacus Magni", new TerritoryData(63, 3, 68, 149, 1, 48, 40) },
				{ "Magnae Planities", new TerritoryData(64, 3, 65, 142, 1, 37, 37) },
				{ "Dacota", new TerritoryData(65, 3, 70, 141, 1, 44, 44) },
				{ "Oregonia", new TerritoryData(66, 7, 70, 132, 1, 35, 24) },
				{ "Flumen Aequatoriale Septentrionale", new TerritoryData(67, 7, 44, 134, 0, 236, 0) },
				{ "Havaii", new TerritoryData(68, 8, 52, 117, 0, 97, 15) },
				{ "Insulae Marianae", new TerritoryData(69, 8, 43, 101, 0, 27, 2) },
				{ "Insulae Marsaliensis", new TerritoryData(70, 8, 38, 112, 0, 59, 3) },
				{ "Insulae Marchionis", new TerritoryData(71, 8, 28, 132, 0, 44, 1) },
				{ "Insula Paschalis", new TerritoryData(72, 8, 18, 136, 0, 69, 4) },
				{ "Polynesia Centralis", new TerritoryData(73, 8, 32, 128, 0, 78, 0) },
				{ "Insulae Salomanis", new TerritoryData(74, 8, 32, 106, 0, 50, 4) },
				{ "Polynesia Australis", new TerritoryData(75, 8, 14, 126, 0, 290, 0) },
				{ "Flumen Circumpolare Antarcticum", new TerritoryData(76, 7, 3, 149, 0, 225, 0) },
				{ "Insulae Galapagenses", new TerritoryData(77, 8, 37, 146, 0, 42, 4) },
				{ "Gyrus Pacifici Australis", new TerritoryData(78, 7, 23, 139, 0, 181, 0) },
				{ "Flumen Humboldtianum", new TerritoryData(79, 7, 12, 144, 0, 143, 0) },
				{ "Insulae Bonin", new TerritoryData(80, 8, 51, 101, 0, 27, 1) },
				{ "Insulae Atlanticae Australis", new TerritoryData(81, 6, 4, 165, 0, 87, 7) },
				{ "Mare Argentinum", new TerritoryData(82, 0, 10, 171, 0, 84, 0) },
				{ "Fossa Mariana", new TerritoryData(83, 8, 46, 110, 0, 151, 0) },
				{ "Polynesia Borealis", new TerritoryData(84, 8, 44, 123, 0, 111, 0) },
				{ "Fretum Drakeanum", new TerritoryData(85, 7, 1, 158, 0, 13, 0) },
				{ "Oceanus Atlanticus Australis", new TerritoryData(86, 7, 12, 7, 0, 276, 0) },
				{ "Flumen Benguelense", new TerritoryData(87, 7, 10, 16, 0, 199, 0) },
				{ "Flumen Aequatoriale Australe", new TerritoryData(88, 8, 28, 7, 0, 211, 0) },
				{ "Mare Sargassum", new TerritoryData(89, 7, 60, 171, 0, 236, 0) },
				{ "Oceanus Atlanticus", new TerritoryData(90, 8, 44, 170, 0, 253, 0) },
				{ "Canariae Insulae", new TerritoryData(91, 5, 48, 1, 0, 34, 4) },
				{ "Azores", new TerritoryData(92, 4, 56, 179, 0, 28, 2) },
				{ "Oceanus Atlanticus Orientalis", new TerritoryData(93, 4, 53, 177, 0, 80, 0) },
				{ "Insulae Societalis", new TerritoryData(94, 8, 25, 123, 0, 32, 2) },
				{ "Andalusia", new TerritoryData(95, 4, 54, 10, 2, 41, 20) },
				{ "Castella", new TerritoryData(96, 4, 60, 6, 2, 39, 20) },
				{ "Aragonia", new TerritoryData(97, 4, 59, 8, 2, 26, 19) },
				{ "Sardinia", new TerritoryData(98, 4, 57, 17, 0, 23, 6) },
				{ "Italia Annonaria", new TerritoryData(99, 3, 63, 19, 2, 33, 26) },
				{ "Italia Suburbicaria", new TerritoryData(100, 4, 56, 22, 2, 47, 23) },
				{ "Graecia", new TerritoryData(101, 4, 57, 25, 2, 38, 16) },
				{ "Illyria", new TerritoryData(102, 4, 62, 24, 2, 22, 18) },
				{ "Dacia", new TerritoryData(103, 3, 65, 33, 2, 22, 20) },
				{ "Danubia", new TerritoryData(104, 7, 66, 28, 2, 21, 21) },
				{ "Germania", new TerritoryData(105, 7, 72, 20, 2, 54, 43) },
				{ "Syria", new TerritoryData(106, 2, 52, 37, 7, 26, 17) },
				{ "Norvegia", new TerritoryData(107, 7, 82, 20, 2, 56, 24) },
				{ "Crasnoiarium", new TerritoryData(108, 7, 79, 71, 7, 38, 38) },
				{ "Suecia", new TerritoryData(109, 7, 80, 26, 2, 44, 28) },
				{ "Permia", new TerritoryData(110, 7, 78, 50, 2, 45, 45) },
				{ "Finnia", new TerritoryData(111, 7, 82, 30, 2, 34, 26) },
				{ "Kola", new TerritoryData(112, 6, 89, 36, 2, 42, 29) },
				{ "Mare Barentsianum", new TerritoryData(113, 0, 92, 32, 0, 21, 0) },
				{ "Biarmia", new TerritoryData(114, 6, 84, 40, 2, 68, 55) },
				{ "Nova Zembla", new TerritoryData(115, 0, 91, 48, 0, 39, 9) },
				{ "Novogardia", new TerritoryData(116, 7, 80, 35, 2, 37, 33) },
				{ "Polonia", new TerritoryData(117, 7, 72, 25, 2, 43, 37) },
				{ "Cappadocia", new TerritoryData(118, 4, 60, 38, 7, 36, 26) },
				{ "Ucraina", new TerritoryData(119, 7, 68, 35, 2, 53, 43) },
				{ "Baltica", new TerritoryData(120, 7, 75, 29, 2, 42, 36) },
				{ "Transcaucasia", new TerritoryData(121, 7, 61, 43, 7, 26, 18) },
				{ "Moscovia", new TerritoryData(122, 7, 75, 39, 2, 51, 50) },
				{ "Assyria", new TerritoryData(123, 4, 56, 41, 7, 24, 24) },
				{ "Ciscaucasia", new TerritoryData(124, 7, 66, 41, 2, 37, 31) },
				{ "Mauretania", new TerritoryData(125, 2, 44, 6, 6, 45, 36) },
				{ "Sahara", new TerritoryData(126, 2, 47, 18, 6, 47, 47) },
				{ "Libya", new TerritoryData(127, 2, 48, 24, 6, 67, 52) },
				{ "Aegyptus", new TerritoryData(128, 2, 49, 29, 6, 56, 37) },
				{ "Malia", new TerritoryData(129, 5, 40, 12, 6, 33, 33) },
				{ "Cammarunia", new TerritoryData(130, 8, 31, 18, 6, 33, 23) },
				{ "Kenia", new TerritoryData(131, 8, 28, 33, 6, 50, 31) },
				{ "Abyssinia", new TerritoryData(132, 8, 35, 34, 6, 47, 45) },
				{ "Socotra", new TerritoryData(133, 8, 35, 48, 0, 32, 2) },
				{ "Insulae Mascarene", new TerritoryData(134, 8, 19, 50, 0, 98, 3) },
				{ "Madagascaria", new TerritoryData(135, 8, 19, 37, 0, 89, 25) },
				{ "Cape", new TerritoryData(136, 3, 10, 22, 6, 61, 28) },
				{ "Congo", new TerritoryData(137, 8, 27, 28, 6, 74, 68) },
				{ "Flumen Agulhas", new TerritoryData(138, 7, 5, 27, 0, 94, 0) },
				{ "Oceanus Indicus Occidentalis", new TerritoryData(139, 8, 25, 44, 0, 110, 0) },
				{ "Mare Arabicum", new TerritoryData(140, 8, 37, 49, 0, 133, 0) },
				{ "SriLanca", new TerritoryData(141, 8, 32, 61, 0, 49, 7) },
				{ "Mesopotamia", new TerritoryData(142, 2, 51, 38, 7, 27, 25) },
				{ "Hidiazum", new TerritoryData(143, 2, 42, 38, 7, 67, 47) },
				{ "Media", new TerritoryData(144, 1, 55, 47, 7, 32, 31) },
				{ "Maharastra", new TerritoryData(145, 8, 44, 59, 7, 51, 35) },
				{ "Afgania", new TerritoryData(146, 1, 55, 53, 7, 35, 35) },
				{ "Ganges", new TerritoryData(147, 8, 51, 64, 7, 55, 55) },
				{ "Nenetsia", new TerritoryData(148, 6, 86, 49, 2, 83, 59) },
				{ "Orenburgum", new TerritoryData(149, 7, 72, 49, 2, 38, 38) },
				{ "Tibetum ", new TerritoryData(150, 6, 56, 70, 7, 42, 42) },
				{ "Altai", new TerritoryData(151, 7, 73, 68, 7, 41, 40) },
				{ "Lapponia", new TerritoryData(152, 6, 87, 30, 2, 28, 25) },
				{ "Tungusca", new TerritoryData(153, 6, 85, 71, 7, 48, 48) },
				{ "Lena", new TerritoryData(154, 6, 85, 86, 7, 75, 64) },
				{ "Cisbaicalia", new TerritoryData(155, 6, 80, 76, 7, 43, 41) },
				{ "Mare Tschukotense", new TerritoryData(156, 0, 92, 95, 0, 42, 0) },
				{ "Amur", new TerritoryData(157, 6, 77, 86, 7, 33, 33) },
				{ "Magadanum", new TerritoryData(158, 6, 81, 92, 7, 54, 47) },
				{ "Corea", new TerritoryData(159, 7, 64, 90, 7, 53, 23) },
				{ "Insulae Lineae", new TerritoryData(160, 8, 38, 122, 0, 44, 1) },
				{ "Territorium Maritimum", new TerritoryData(161, 7, 73, 97, 7, 36, 23) },
				{ "Planum Sinense", new TerritoryData(162, 7, 62, 88, 7, 43, 30) },
				{ "Flumen Flavum", new TerritoryData(163, 3, 60, 83, 7, 60, 60) },
				{ "Flumen Margaritarum", new TerritoryData(164, 3, 50, 85, 7, 46, 36) },
				{ "Formosa", new TerritoryData(165, 3, 49, 91, 0, 29, 7) },
				{ "Birmania", new TerritoryData(166, 8, 45, 73, 7, 45, 32) },
				{ "Vietnamia", new TerritoryData(167, 8, 44, 79, 7, 42, 23) },
				{ "Thailandia", new TerritoryData(168, 8, 44, 76, 7, 31, 28) },
				{ "Malaesia", new TerritoryData(169, 8, 35, 79, 7, 40, 17) },
				{ "Sumatra", new TerritoryData(170, 8, 30, 74, 0, 58, 23) },
				{ "Andamanenses", new TerritoryData(171, 8, 39, 68, 0, 26, 4) },
				{ "Sinus Bengalensis", new TerritoryData(172, 8, 35, 68, 0, 48, 0) },
				{ "Hainania", new TerritoryData(173, 8, 46, 82, 0, 20, 2) },
				{ "Mare Philippinense", new TerritoryData(174, 8, 45, 96, 0, 90, 0) },
				{ "Iava", new TerritoryData(175, 8, 25, 89, 0, 53, 13) },
				{ "Celebis", new TerritoryData(176, 8, 30, 91, 0, 45, 18) },
				{ "Papua", new TerritoryData(177, 8, 31, 102, 0, 93, 28) },
				{ "Borneum", new TerritoryData(178, 8, 34, 82, 0, 98, 36) },
				{ "Casmiria", new TerritoryData(179, 6, 59, 60, 7, 45, 45) },
				{ "Melanesia", new TerritoryData(180, 8, 24, 110, 0, 96, 9) },
				{ "Flumen Australianum Occidentale", new TerritoryData(181, 7, 14, 79, 0, 168, 0) },
				{ "Gyrus Indici", new TerritoryData(182, 7, 17, 62, 0, 195, 0) },
				{ "Archipelagus Crozetense", new TerritoryData(183, 7, 6, 46, 0, 233, 0) },
				{ "Oceanus Indicus Australis", new TerritoryData(184, 0, 6, 74, 0, 255, 0) },
				{ "Oceanus Atlanticus Septentrionalis", new TerritoryData(185, 7, 70, 176, 0, 246, 0) },
				{ "Mare Norvegicum", new TerritoryData(186, 0, 88, 20, 0, 89, 0) },
				{ "Tarim", new TerritoryData(187, 1, 65, 62, 7, 47, 47) },
				{ "Marocum", new TerritoryData(188, 4, 50, 5, 6, 32, 22) },
				{ "Casanum", new TerritoryData(189, 7, 77, 42, 2, 53, 53) },
				{ "Numidia", new TerritoryData(190, 4, 53, 13, 6, 45, 26) },
				{ "Finmarchia", new TerritoryData(191, 9, 90, 24, 2, 26, 18) },
				{ "Taimyr", new TerritoryData(192, 9, 90, 70, 7, 50, 48) },
				{ "Tomium", new TerritoryData(193, 7, 77, 67, 7, 48, 48) },
				{ "Iamalia", new TerritoryData(194, 6, 87, 55, 7, 79, 56) },
				{ "Iana", new TerritoryData(195, 9, 87, 91, 7, 78, 53) },
				{ "Colyma", new TerritoryData(196, 9, 88, 96, 7, 59, 41) },
				{ "Tschucoticus", new TerritoryData(197, 9, 86, 112, 7, 64, 41) },
				{ "Kenai", new TerritoryData(198, 6, 81, 122, 1, 52, 29) },
				{ "Athabasca", new TerritoryData(199, 6, 81, 135, 1, 54, 51) },
				{ "Dene", new TerritoryData(200, 0, 86, 132, 1, 64, 46) },
				{ "Laboratoria", new TerritoryData(201, 6, 78, 166, 1, 38, 20) },
				{ "Flumen Argenteum", new TerritoryData(202, 7, 17, 164, 4, 53, 40) },
				{ "Terra Ignium", new TerritoryData(203, 0, 4, 161, 4, 46, 20) },
				{ "Nivata", new TerritoryData(204, 2, 64, 134, 1, 40, 40) },
				{ "Chorasmia", new TerritoryData(205, 2, 66, 51, 7, 51, 41) },
				{ "Kazachia", new TerritoryData(206, 2, 70, 59, 7, 61, 56) },
				{ "Indus", new TerritoryData(207, 2, 52, 54, 7, 48, 43) },
				{ "Desertum Gobium", new TerritoryData(208, 1, 68, 66, 7, 75, 75) },
				{ "Somalia", new TerritoryData(209, 2, 33, 40, 6, 44, 26) },
				{ "Naimbia", new TerritoryData(210, 2, 15, 21, 6, 40, 26) },
				{ "Guinea", new TerritoryData(211, 8, 35, 11, 6, 64, 34) },
				{ "Nigritania", new TerritoryData(212, 5, 41, 15, 6, 47, 47) },
				{ "Ngbandia ", new TerritoryData(213, 5, 35, 26, 6, 40, 40) },
				{ "Angolia", new TerritoryData(214, 5, 22, 20, 6, 47, 35) },
				{ "Zambia", new TerritoryData(215, 8, 22, 26, 6, 32, 30) },
				{ "Capitis Viridis", new TerritoryData(216, 5, 41, 177, 0, 30, 3) },
				{ "Tuvalu", new TerritoryData(217, 8, 32, 116, 0, 36, 2) },
				{ "Philippinae", new TerritoryData(218, 8, 41, 91, 0, 88, 16) },
				{ "Yucatania", new TerritoryData(219, 8, 49, 144, 1, 54, 23) },
				{ "Beninum", new TerritoryData(220, 8, 35, 16, 6, 41, 29) },
				{ "Sichuan", new TerritoryData(221, 3, 55, 76, 7, 51, 51) },
				{ "Zimbabua", new TerritoryData(222, 3, 17, 25, 6, 22, 22) },
				{ "Tobolium", new TerritoryData(223, 7, 80, 54, 7, 55, 55) },
				{ "Ienisea", new TerritoryData(224, 6, 87, 65, 7, 57, 45) },
				{ "Mongolia Ulterior", new TerritoryData(225, 7, 72, 80, 7, 40, 40) },
				{ "Voronegia", new TerritoryData(226, 7, 70, 45, 2, 32, 32) },
				{ "Arabia", new TerritoryData(227, 2, 44, 41, 7, 54, 30) },
				{ "Omium", new TerritoryData(228, 7, 74, 62, 7, 35, 35) },
				{ "Manchuria", new TerritoryData(229, 7, 70, 93, 7, 61, 56) },
				{ "Parthia", new TerritoryData(230, 2, 60, 54, 7, 36, 32) },
				{ "Bengala", new TerritoryData(231, 8, 51, 71, 7, 36, 32) },
				{ "California", new TerritoryData(232, 7, 64, 130, 1, 38, 21) },
				{ "Sudania", new TerritoryData(233, 2, 41, 33, 6, 51, 46) },
				{ "Mozambicum", new TerritoryData(234, 5, 19, 30, 6, 42, 22) },
				{ "Gedrosia", new TerritoryData(235, 2, 48, 52, 7, 44, 32) },
				{ "Dravidia", new TerritoryData(236, 8, 38, 63, 7, 48, 21) },
				{ "Odisa", new TerritoryData(237, 8, 46, 63, 7, 38, 26) },
				{ "Thracia", new TerritoryData(238, 3, 62, 33, 2, 24, 17) },
				{ "Transbaicalia", new TerritoryData(239, 6, 77, 81, 7, 60, 60) },
				{ "Mongolia Citerior", new TerritoryData(240, 7, 69, 82, 7, 50, 50) },
				{ "Kiangnanum", new TerritoryData(241, 3, 55, 83, 7, 46, 36) },
				{ "Alpes Iaponicae", new TerritoryData(242, 7, 59, 96, 0, 43, 21) },
				{ "Persia", new TerritoryData(243, 1, 50, 44, 7, 27, 24) },
				{ "Amdo", new TerritoryData(244, 3, 61, 74, 7, 46, 46) },
				{ "Transoxiana", new TerritoryData(245, 2, 64, 55, 7, 44, 43) },
				{ "Chilia", new TerritoryData(246, 6, 13, 156, 4, 45, 22) },
				{ "Venetiola", new TerritoryData(247, 8, 42, 160, 4, 50, 27) },
				{ "Paraensis", new TerritoryData(248, 8, 33, 165, 4, 42, 38) },
				{ "Flumen Brasiliense", new TerritoryData(249, 7, 14, 179, 0, 180, 0) },
				{ "Lydia", new TerritoryData(250, 4, 58, 30, 7, 42, 25) },
				//{ "Ultima Thule", new TerritoryReference(251, 4, 0, 0, 0, 360, 50) },

			};

		static readonly List<ContinentData> ContinentReferenceGiantEarthMap = new List<ContinentData>
			{
				{ new ContinentData("North America", 1, 26, 139, 66) },
				{ new ContinentData("Europa", 2, 32, 28, 71) },
				{ new ContinentData("Australia", 3, 5, 95, 17) },
				{ new ContinentData("South America", 4, 14, 162, 23) },
				{ new ContinentData("Africa", 6, 24, 22, 31) },
				{ new ContinentData("Asia", 7, 61, 71, 62) },
				//{ new ContinentReference("Oceans", 0, 89, 89, 46) },
				//{ new ContinentReference("Greenland", 5, 1, 170, 86) },
			};

		static int AsiastraliaSize = 66; // size referencing the biggest Giant Earth Map Continent

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
					{ "Civilization_Era6_Australia",                new List<int>() { 35, 31, 32, 33, 34, 36, 37, 38, 39 } },  // Australia
					{ "Civilization_Era1_Bantu",                    new List<int>() { 130, 137, 131, 214, 215, 234, 222, 136 }}, //Camerunia, Congo, Kenia, Angolia, Zambia, Mozambicum, Zimbabua, Cape
					{ "Civilization_Era2_Garamantes",               new List<int>() { 127 } }, // Libya
					{ "Civilization_Era3_Swahili",                  new List<int>() { 131, 234 } }, // Kenia, Mozambicum 
					{ "Civilization_Era4_Maasai",                   new List<int>() { 131 } }, // Kenya
					{ "Civilization_Era5_Ethiopia",                 new List<int>() { 132, 209 } }, // Abyssinia, Somalia
					{ "Civilization_Era6_Nigeria",					new List<int>() { 128, 233 } } // Beninum, Nigritania
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
					//{ 4, "South America"},
					{ 5, "Greenland"},
					//{ 6, "Africa"},
					//{ 7, "Asia"},
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

		public static readonly int GiantEarthHeight = 94;
		public static readonly int GiantEarthWidth = 180;

		public static readonly int AmericasColumnShift = 68;

		public static int ColumnShift = 0;


		public static int[] ColumnFromRefMapColumn;
		public static int[] RowFromRefMapRow;
		public static int[] RefMapColumnfromColumn;
		public static int[] RefMapRowfromRow;

		#endregion

		static IDictionary<int, Hexagon.OffsetCoords> ExtraPositions = new Dictionary<int, Hexagon.OffsetCoords>();

		static IDictionary<int, Hexagon.OffsetCoords> ExtraPositionsNewWorld = new Dictionary<int, Hexagon.OffsetCoords>();

		static List<string>[] TerritoriesWithMinorFactions;
		static public List<string>[] TerritoriesUnlockingMajorEmpires;

		static IDictionary<string, List<int>> ListMinorFactionTerritories = new Dictionary<string, List<int>>();
		static IDictionary<string, List<int>> ListMajorEmpireTerritories = new Dictionary<string, List<int>>();
		static IDictionary<string, List<int>> ListMajorEmpireCoreTerritories = new Dictionary<string, List<int>>();

		static IDictionary<int, string> ContinentNames = new Dictionary<int, string>();

		static IDictionary<int, string> TerritoryNames = new Dictionary<int, string>();

		static readonly IDictionary<string, List<int>> ListSlots = new Dictionary<string, List<int>>  // civName, list of player slots (majorEmpire.Index) starting at 0
		{
			//{ "Civilization_Era1_Assyria", new List<int>() { 6, 7 } },
		};

		static readonly List<string> NomadCultures = new List<string> { "Civilization_Era2_Huns", "Civilization_Era3_MongolEmpire" };

		static readonly List<string> FirstEraBackup = new List<string> { "Civilization_Era1_Assyria", };

		static readonly List<string> NoCapitalTerritory = new List<string> { "Civilization_Era1_Assyria", "Civilization_Era1_HarappanCivilization", "Civilization_Era1_Nubia", "Civilization_Era1_ZhouChina", "Civilization_Era1_MycenaeanCivilization", "Civilization_Era1_Phoenicia", "Civilization_Era1_OlmecCivilization", "Civilization_Era1_Bantu", "Civilization_Era2_Huns", "Civilization_Era2_Goths", "Civilization_Era2_CelticCivilization", "Civilization_Era3_Vikings", "Civilization_Era3_MongolEmpire", "Civilization_Era4_TokugawaShogunate" };

		public static readonly int knowledgeForBackupCiv = 25;

		public static readonly int maxNumTerritories = 256;

		public static int CurrentMapHash { get; set; } = 0;
		public static bool IsMapValidforTCL { get; set; } = false;
		public static bool MapCanUseGiantEarthReference { get; set; } = false;
		public static bool HasGiantEarthMapHash(List<int> listMapHash)
		{
			for (int i = 0; i < listMapHash.Count; i++)
			{
				if (GiantEarthMapHash.Contains(listMapHash[i]))
					return true;
			}
			return false;
		}
		public static bool HasCurrentMapHash(List<int> listMapHash)
		{
			if (listMapHash.Contains(CultureUnlock.CurrentMapHash))
				return true;
						
			return false;

		}
		public static bool HasValidMapHash(List<int> listMapHash)
		{
			if (listMapHash.Contains(CultureUnlock.CurrentMapHash))
				return true;

			if (MapCanUseGiantEarthReference)
			{
				for (int i = 0; i < listMapHash.Count; i++)
				{
					if (GiantEarthMapHash.Contains(listMapHash[i]))
						return true;
				}
			}

			return false;

		}
		public class SortedTerritory
        {
			public int Index;
			public int Distance;
			public int Size;
			public List<int> All = new List<int>();
		}

		public static int[] TerritoryFromRefMapTerritory;
		public static int[] RefMapTerritoryFromTerritory;
		public static List<int>[] RefMapContinentListFromContinent;
		public static string[] RefMapContinentNameFromContinent;
		public static int[] ContinentFromRefMapContinent;

		public static void ImportGiantEarthData()
        {
			//ExtraPositions = new Dictionary<int, Hexagon.OffsetCoords>(ExtraPositionsGiantEarthMap);
			//ExtraPositionsNewWorld = new Dictionary<int, Hexagon.OffsetCoords>(ExtraPositionsNewWorldGiantEarthMap);
			//ContinentNames = new Dictionary<int, string>(continentNamesGiantEarthMap);
			//TerritoryNames = new Dictionary<int, string>(territoryNamesGiantEarthMap);
			//ListMinorFactionTerritories = new Dictionary<string, List<int>>(listMinorFactionTerritoriesGiantEarthMap);
			//ListMajorEmpireTerritories = new Dictionary<string, List<int>>(listMajorEmpireTerritoriesGiantEarthMap);
			//ListMajorEmpireCoreTerritories = new Dictionary<string, List<int>>(listMajorEmpireCoreTerritoriesGiantEarthMap);
		}

		public static bool AreSameContinentByReference(Territory territory, CultureUnlock.TerritoryData territoryData)
        {
			if(territory.ContinentIndex == 0)
            {
				if (territoryData.Continent == 0)
					return true;
				else
					return false;
            }

			return CultureUnlock.RefMapContinentListFromContinent[territory.ContinentIndex].Contains(territoryData.Continent);
		}

		public static void PrepareForGiantEarthReference(World currentWorld)
        {

			if (TrueCultureLocation.UseShiftedCoordinates())
				ColumnShift = AmericasColumnShift;

			int numTerritories = currentWorld.Territories.Length;
			int numGiantEarthTerritories = TerritoryReferenceGiantEarthMap.Count;
			MapCanUseGiantEarthReference = true;

			int width = currentWorld.WorldWidth;
			int height = currentWorld.WorldHeight;
			bool wrap = currentWorld.WorldWrap;

			if(!wrap)
            {
				MapCanUseGiantEarthReference = false;
				return;
			}

			int numContinents = currentWorld.ContinentInfo.Length;
			int numGiantEarthContinents = ContinentReferenceGiantEarthMap.Count;

			ColumnFromRefMapColumn = new int[GiantEarthWidth];
			RowFromRefMapRow = new int[GiantEarthHeight];
			RefMapColumnfromColumn = new int[width];
			RefMapRowfromRow = new int[height];

			Diagnostics.LogWarning($"[Gedemon] [CultureUnlock] Unknown Map detected (width = {width}, height = {height}, wrap = {wrap}, numLandContinents = {numContinents-1}, numTerritories = {numTerritories}) (GiantEarth width = {GiantEarthWidth}, height = {GiantEarthHeight}), UseReferenceCoordinates = {TrueCultureLocation.UseReferenceCoordinates()}");

			{
				float factor = GiantEarthHeight / (float)height;
				float ratio = height / (float)GiantEarthHeight;
				Diagnostics.LogWarning($"[Gedemon] [CultureUnlock] Building Rows reference tables with Heigth Factor = {factor}, Heigth Ratio = {ratio}");
				for (int i = 0; i < height; i++)
				{
					RefMapRowfromRow[i] = (int)(i * factor);
					//Diagnostics.Log($"[Gedemon] [CultureUnlock] RefMapRowfromRow [{i}] = {RefMapRowfromRow[i]}");
				}
				for (int i = 0; i < GiantEarthHeight; i++)
				{
					RowFromRefMapRow[i] = (int)(i * ratio);
					//Diagnostics.Log($"[Gedemon] [CultureUnlock] RowFromRefMapRow [{i}] = {RowFromRefMapRow[i]}");
				}
			}

			{
				float factor = GiantEarthWidth / (float)width;
				float ratio = width / (float)GiantEarthWidth;
				Diagnostics.LogWarning($"[Gedemon] [CultureUnlock] Building Column reference table with Width Factor = {factor}, Width Ratio = {ratio}");
				for (int i = 0; i < width; i++)
				{
					RefMapColumnfromColumn[i] = (int)(i * factor);
					//Diagnostics.Log($"[Gedemon] [CultureUnlock] RefMapColumnfromColumn [{i}] = {RefMapColumnfromColumn[i]}");
				}
				for (int i = 0; i < GiantEarthWidth; i++)
				{
					ColumnFromRefMapColumn[i] = (int)(i * ratio);
					//Diagnostics.Log($"[Gedemon] [CultureUnlock] ColumnFromRefMapColumn [{i}] = {ColumnFromRefMapColumn[i]}");
				}
			}


			Diagnostics.LogWarning($"[Gedemon] [CultureUnlock] Initializing reference Arrays...");

			TerritoryFromRefMapTerritory = new int[numGiantEarthTerritories];
			RefMapTerritoryFromTerritory = new int[numTerritories];

			ContinentFromRefMapContinent = new int[numGiantEarthContinents];
			RefMapContinentListFromContinent = new List<int>[numContinents];
			RefMapContinentNameFromContinent = new string[numContinents];

			List<int>[] RefMapContinentLists = new List<int>[numContinents];
			int[] RefMapContinentSizeFromList = new int[numContinents];
			string[] RefMapContinentNameFromList = new string[numContinents];

			Diagnostics.LogWarning($"[Gedemon] [CultureUnlock] Fill reference Arrays with default values (-1)...");
			// fill the arrays with invalid values
			for (int i = 0; i < numGiantEarthTerritories; i++)
			{
				TerritoryFromRefMapTerritory[i] = -1;
			}
			for (int i = 0; i < numTerritories; i++)
			{
				RefMapTerritoryFromTerritory[i] = -1;
			}

			List<ContinentData> continentDataList = new List<ContinentData>();
			List<ContinentData> continentReferenceList = new List<ContinentData>();

			Diagnostics.LogWarning($"[Gedemon] [CultureUnlock] Create continentReferenceList...");

			foreach (ContinentData continent in ContinentReferenceGiantEarthMap)
			{

				int refColumn = continent.Column + ColumnShift;

				if (refColumn >= GiantEarthWidth)
					refColumn -= GiantEarthWidth;

				int row = RowFromRefMapRow[continent.Row];
				int column = ColumnFromRefMapColumn[refColumn];

				continentReferenceList.Add(new ContinentData(continent.Name, continent.Index, continent.Size, column, row));
			}

			Diagnostics.LogWarning($"[Gedemon] [CultureUnlock] Create continentDataList...");

			int biggestContinentSize = 0;

			for (int i = 1; i < numContinents; i++) // ignoring index #0 (Ocean)
			{
				RefMapContinentListFromContinent[i] = null;

				ContinentInfo continentInfo = currentWorld.ContinentInfo[i];
				WorldPosition visualCenter = new WorldPosition(continentInfo.VisualCenterTileIndex);
				int size = continentInfo.TerritoryIndexes.Length;
				biggestContinentSize = size > biggestContinentSize ? size : biggestContinentSize;
				continentDataList.Add(new ContinentData(continentInfo.ContinentName, i, size, visualCenter.Column, visualCenter.Row));
			}

			Diagnostics.LogWarning($"[Gedemon] [CultureUnlock] Sort continentDataList by size...");
			// Sort the current map continents by sizes
			continentDataList.Sort((x, y) => y.Size.CompareTo(x.Size));

			Diagnostics.LogWarning($"[Gedemon] [CultureUnlock] Building reference continents list based on number of Land Continents of the current map...");
			// Build reference continents list based on number of Continents of the current map
			switch (numContinents)
			{
				// #1 is the ocean continent
				case 2:
					RefMapContinentLists[0] = new List<int> { 1, 2, 3, 4, 6, 7 };
					RefMapContinentNameFromList[0] = "Pangea";
					break;
				case 3:
					RefMapContinentLists[0] = new List<int> { 1, 4 };
					RefMapContinentNameFromList[0] = "Americas";
					RefMapContinentLists[1] = new List<int> { 2, 3, 6, 7 };
					RefMapContinentNameFromList[1] = "Eurafricasiastralia";
					break;
				case 4:
					if(biggestContinentSize > AsiastraliaSize) // Compatibility with Huge Earth-like maps
					{
						RefMapContinentLists[0] = new List<int> { 1, 4 };
						RefMapContinentNameFromList[0] = "Americas";
						RefMapContinentLists[1] = new List<int> { 2, 7, 6 };
						RefMapContinentNameFromList[1] = "Eurafricasia";
						RefMapContinentLists[2] = new List<int> { 3 };
						RefMapContinentNameFromList[2] = "Australia";
					}
					else
					{
						RefMapContinentLists[0] = new List<int> { 1, 4 };
						RefMapContinentNameFromList[0] = "Americas";
						RefMapContinentLists[1] = new List<int> { 2, 6 };
						RefMapContinentNameFromList[1] = "Eurafrica";
						RefMapContinentLists[2] = new List<int> { 3, 7 };
						RefMapContinentNameFromList[2] = "Asiastralia";
					}
					break;
				case 5:
					RefMapContinentLists[0] = new List<int> { 1, 4 };
					RefMapContinentNameFromList[0] = "Americas";
					RefMapContinentLists[1] = new List<int> { 2 };
					RefMapContinentNameFromList[1] = "Europa";
					RefMapContinentLists[2] = new List<int> { 3, 7 };
					RefMapContinentNameFromList[2] = "Asiastralia";
					RefMapContinentLists[3] = new List<int> { 6 };
					RefMapContinentNameFromList[3] = "Africa";
					break;
				case 6:
					RefMapContinentLists[0] = new List<int> { 1 };
					RefMapContinentNameFromList[0] = "North America";
					RefMapContinentLists[1] = new List<int> { 2 };
					RefMapContinentNameFromList[1] = "Europa";
					RefMapContinentLists[2] = new List<int> { 3, 7 };
					RefMapContinentNameFromList[2] = "Asiastralia";
					RefMapContinentLists[3] = new List<int> { 6 };
					RefMapContinentNameFromList[3] = "Africa";
					RefMapContinentLists[4] = new List<int> { 4 };
					RefMapContinentNameFromList[4] = "South America";
					break;
				case 7:
					RefMapContinentLists[0] = new List<int> { 1 };
					RefMapContinentNameFromList[0] = "North America";
					RefMapContinentLists[1] = new List<int> { 2 };
					RefMapContinentNameFromList[1] = "Europa";
					RefMapContinentLists[2] = new List<int> { 3 };
					RefMapContinentNameFromList[2] = "Asia";
					RefMapContinentLists[3] = new List<int> { 6 };
					RefMapContinentNameFromList[3] = "Africa";
					RefMapContinentLists[4] = new List<int> { 4 };
					RefMapContinentNameFromList[4] = "South America";
					RefMapContinentLists[5] = new List<int> { 7 };
					RefMapContinentNameFromList[5] = "Australia";
					break;
				// [1] = "North America"
				// [2] = "Europa"
				// [3] = "Australia"
				// [4] = "South America"
				// [5] = "Greenland" (unused, attached to North America)
				// [6] = "Africa"
				// [7] = "Asia" 
				default:
					Diagnostics.LogError($"[Gedemon] [CultureUnlock] Invalid continent number ({numContinents})");
					MapCanUseGiantEarthReference = false;
					return;
			}

			// Calculate total territory sizes of each continent list
			//Diagnostics.LogWarning($"[Gedemon] [CultureUnlock] Calculate total territory sizes of each continent list");
			for (int i = 0; i < numContinents - 1; i++)
			{
				Diagnostics.LogWarning($"[Gedemon] [CultureUnlock] Calculate total territory sizes for reference Continents list #{i} ({RefMapContinentNameFromList[i]})");
				List<int> continents = RefMapContinentLists[i];
				int totalContinentTerritories = 0;
				for (int j = 0; j < continents.Count; j++)
				{
					int continentIndex = continents[j];

					foreach (ContinentData continentReference in ContinentReferenceGiantEarthMap)
					{
						if (continentReference.Index == continentIndex)
						{
							Diagnostics.Log($"[Gedemon] [CultureUnlock] Adding sizes of continent #{continentIndex} ({continentReference.Name}, num territories = {continentReference.Size})");
							totalContinentTerritories += continentReference.Size;
						}
					}

				}
				RefMapContinentSizeFromList[i] = totalContinentTerritories;
				Diagnostics.Log($"[Gedemon] [CultureUnlock] Total territory sizes for {RefMapContinentNameFromList[i]} = {totalContinentTerritories}");
			}

			//
			List<int> refContinentListPaired = new List<int>();
			List<int> refContinentPaired = new List<int>();
			foreach(ContinentData continent in continentDataList)
			{
				WorldPosition visualCenter = new WorldPosition(continent.Column, continent.Row);

				Diagnostics.LogWarning($"[Gedemon] [CultureUnlock] Trying to pair Continent #{continent.Index} (size = {continent.Size}) at {visualCenter}");

				int bestDistance = int.MaxValue;
				int bestListIndex = -1;

				foreach (ContinentData referenceContinent in continentReferenceList)
				{
					if(refContinentPaired.Contains(referenceContinent.Index))
						continue;

					WorldPosition referenceVisualCenter = new WorldPosition(referenceContinent.Column, referenceContinent.Row);

					int distance = visualCenter.GetDistance(referenceVisualCenter.ToTileIndex());

					Diagnostics.Log($"[Gedemon] [CultureUnlock] Checking reference Continent #{referenceContinent.Index} ({referenceContinent.Name}, size = {referenceContinent.Size}) at {referenceVisualCenter} (relative distance = {distance})");

					if (distance < bestDistance)
					{
						// Get list
						for (int j = 0; j < numContinents-1; j++)
						{
							List<int> continentList = RefMapContinentLists[j];
							if (continentList.Contains(referenceContinent.Index))
							{
								Diagnostics.Log($"[Gedemon] [CultureUnlock] Checking reference List #{j} ({RefMapContinentNameFromList[j]}, size = {RefMapContinentSizeFromList[j]})");
								if (TrueCultureLocation.UseReferenceCoordinates() || RefMapContinentSizeFromList[j] >= continent.Size)
								{
									bestDistance = distance;
									bestListIndex = j;
								}
								break;
							}
						}
					}
				}

				if(bestListIndex != -1)
				{
					List<int> continentList = RefMapContinentLists[bestListIndex];

					RefMapContinentListFromContinent[continent.Index] = continentList;
					RefMapContinentNameFromContinent[continent.Index] = RefMapContinentNameFromList[bestListIndex];

					Diagnostics.LogWarning($"[Gedemon] [CultureUnlock] Paired Continent #{continent.Index} (size = {continent.Size}) to reference list #{bestListIndex} ({RefMapContinentNameFromList[bestListIndex]}, size = {RefMapContinentSizeFromList[bestListIndex]})");

					// mark continents in list
					foreach (int continentIndex in continentList)
					{
						refContinentPaired.Add(continentIndex);
					}

				}
				else
				{
					Diagnostics.LogError($"[Gedemon] [CultureUnlock] Can't find reference Continent for current map continent #{continent.Index}");
					if(!TrueCultureLocation.UseReferenceCoordinates())
                    {
						MapCanUseGiantEarthReference = false;
						return;
					}
				}
			}

			IDictionary<string, WorldPosition> territoriesToAssign = new Dictionary<string, WorldPosition>();
			IDictionary<int, SortedTerritory> ListRefMapTerritories = new Dictionary<int, SortedTerritory>();

			foreach (KeyValuePair<string, TerritoryData> kvp in TerritoryReferenceGiantEarthMap)
			{
				string territoryName = kvp.Key;
				TerritoryData territoryData = kvp.Value;

				int refColumn = territoryData.Column + ColumnShift;

				if (refColumn >= GiantEarthWidth)
					refColumn -= GiantEarthWidth;

				int row = RowFromRefMapRow[territoryData.Row];
				int column = ColumnFromRefMapColumn[refColumn];

				Diagnostics.LogWarning($"[Gedemon] [CultureUnlock] Trying to pair Territory {territoryName} (Continent #{territoryData.Continent}) from GiantEarth ({territoryData.Column}, {territoryData.Row}) to current map ({column}, {row})");

				WorldPosition position = new WorldPosition(column, row);
				int territoryIndex = currentWorld.TileInfo.Data[position.ToTileIndex()].TerritoryIndex;
				Territory territory = currentWorld.Territories[territoryIndex];

				bool assigned = false;

				if (!TrueCultureLocation.UseReferenceCoordinates() && MapUtils.AreSameContinentType(territory, territoryData, currentWorld))
				{

					TerritoryFromRefMapTerritory[territoryData.Index] = territoryIndex;

					int distance = territory.VisualCenter.GetDistance(position.ToTileIndex());
					if (ListRefMapTerritories.TryGetValue(territoryIndex, out SortedTerritory sortedTerritory))
					{
						if (distance < sortedTerritory.Distance)
						{
							Diagnostics.Log($"[Gedemon] [CultureUnlock] Replacing GiantEarth Territory #{sortedTerritory.Index} by #{territoryData.Index} ({territoryName}) at {position} (GiantEarth Position = {territoryData.Column},{territoryData.Row}) for Current map Territory #{territoryIndex} at distance = {distance}");

							sortedTerritory.Index = territoryData.Index;
							sortedTerritory.Distance = distance;
							sortedTerritory.All.Add(territoryData.Index);
							ListRefMapTerritories[territoryIndex] = sortedTerritory;
						}
                        else
						{
							Diagnostics.Log($"[Gedemon] [CultureUnlock] Adding (secondary list) GiantEarth Territory #{territoryData.Index} ({territoryName}) at {position} (GiantEarth Position = {territoryData.Column},{territoryData.Row}) for Current map Territory #{territoryIndex} at distance = {distance}");
							sortedTerritory.All.Add(territoryData.Index);
							ListRefMapTerritories[territoryIndex] = sortedTerritory;
						}
						assigned = true;
					}
					else
					{
						Diagnostics.Log($"[Gedemon] [CultureUnlock] Adding (firt entry) GiantEarth Territory #{territoryData.Index} ({territoryName}) at {position} (GiantEarth Position = {territoryData.Column},{territoryData.Row}) for Current map Territory #{territoryIndex} at distance = {distance}");
						sortedTerritory = new SortedTerritory();
						sortedTerritory.Index = territoryData.Index;
						sortedTerritory.Distance = distance;
						sortedTerritory.All.Add(territoryData.Index);
						ListRefMapTerritories.Add(territoryIndex, sortedTerritory);
						assigned = true;
					}

				}
				else
				{
					int bestDistance = int.MaxValue;
					int bestIndex = -1;
					int numPotential = 0;
					for (int index = 0; index < numTerritories; index++)
					{
						Territory potentialTerritory = currentWorld.Territories[index];

						if (MapUtils.AreSameContinentType(potentialTerritory, territoryData, currentWorld))
						{
							float factor = 1;
							if (TrueCultureLocation.UseReferenceCoordinates())
                            {
								if (!AreSameContinentByReference(potentialTerritory, territoryData))
									factor = 1.5f;
							}
							int distance = (int)(potentialTerritory.VisualCenter.GetDistance(position.ToTileIndex())*factor);
							numPotential++;
							if (distance < bestDistance)
							{
								bestDistance = distance;
								bestIndex = potentialTerritory.Index;
							}
						}
					}
					Diagnostics.Log($"[Gedemon] [CultureUnlock] Potentials checked = {numPotential}");
					if (bestIndex != -1)
					{
						Diagnostics.Log($"[Gedemon] [CultureUnlock] Marking GiantEarth Territory #{territoryData.Index} ({territoryName}) at {position} (GiantEarth Position = {territoryData.Column},{territoryData.Row}) for Current map Territory #{territoryIndex} at distance = {bestDistance}");
						TerritoryFromRefMapTerritory[territoryData.Index] = bestIndex;
						assigned = true;
					}

				}
				if (!assigned)
				{
					Diagnostics.LogError($"[Gedemon] [CultureUnlock] Not assigned yet : GiantEarth Territory #{territoryData.Index} ({territoryName}) at {position} (GiantEarth Position = {territoryData.Column},{territoryData.Row})");
					territoriesToAssign.Add(territoryName, position);
				}
			}

			Diagnostics.LogWarning($"[Gedemon] [CultureUnlock] Num unassigned GiantEarth territories = {territoriesToAssign.Count}/{TerritoryReferenceGiantEarthMap.Count}, num unassigned Current Map territories = {numTerritories - ListRefMapTerritories.Count}/{numTerritories}");


			// assign last territory index to last GiantEarth territory index
			RefMapTerritoryFromTerritory[numTerritories - 1] = territoryNamesGiantEarthMap.Count - 1;

			List<int> refTerritoriesPaired = new List<int>();

			foreach (KeyValuePair<int, SortedTerritory> kvp in ListRefMapTerritories)
			{
				int index = kvp.Key;
				Territory territory = currentWorld.Territories[index];
				int refIndex = ListRefMapTerritories[index].Index;
				RefMapTerritoryFromTerritory[index] = refIndex;
				refTerritoriesPaired.Add(refIndex);
				Diagnostics.Log($"[Gedemon] [CultureUnlock] Current Map Territory #{index} at {territory.VisualCenter} paired with best match {territoryNamesGiantEarthMap[refIndex]} #{refIndex} at distance = {ListRefMapTerritories[index].Distance}");

			}

			for (int index = 0; index < numTerritories - 1; index++)
			{

				bool assigned = ListRefMapTerritories.ContainsKey(index);
				Territory territory = currentWorld.Territories[index];

				if (!assigned)
				{

					int bestDistance;
					int bestIndex;
					string bestName;

					Diagnostics.LogWarning($"[Gedemon] [CultureUnlock] Check Current Map Territory #{index} at {territory.VisualCenter} (Continet = {territory.ContinentIndex})");

					GetBestReferenceFromGiantEarthMap(territory, currentWorld, ref refTerritoriesPaired, out bestDistance, out bestIndex, out bestName, IgnoreIsland: false);

					if (bestIndex == -1)
						GetBestReferenceFromGiantEarthMap(territory, currentWorld, ref refTerritoriesPaired, out bestDistance, out bestIndex, out bestName, IgnoreIsland: true);

					if (bestIndex != -1)
					{
						Diagnostics.Log($"[Gedemon] [CultureUnlock] Marking Current Map Territory #{index} at {territory.VisualCenter} (continent = {territory.ContinentIndex}) paired with GiantEarth {bestName} #{bestIndex} (relative distance = {bestDistance})");
						RefMapTerritoryFromTerritory[index] = bestIndex;
						assigned = true;
						refTerritoriesPaired.Add(bestIndex);
					}

				}

				if (!assigned)
				{
					Diagnostics.LogError($"[Gedemon] [CultureUnlock] Not paired : Current Map Territory #{index} at {territory.VisualCenter}");
					MapCanUseGiantEarthReference = false;
				}
			}

		}

		public static void GetBestReferenceFromGiantEarthMap(Territory territory, World currentWorld, ref List<int> refTerritoriesPaired, out int bestDistance, out int bestIndex, out string bestName, bool IgnoreIsland = false)
        {

			bestDistance = int.MaxValue;
			bestIndex = -1;
			bestName = string.Empty;

			foreach (KeyValuePair<string, TerritoryData> kvp in TerritoryReferenceGiantEarthMap)
			{
				string territoryName = kvp.Key;
				TerritoryData territoryData = kvp.Value;

				if (refTerritoriesPaired.Contains(territoryData.Index))
					continue;

				if (MapUtils.AreSameContinentType(territory, territoryData, currentWorld, IgnoreIsland))
				{

					int refColumn = territoryData.Column + ColumnShift;

					if (refColumn >= GiantEarthWidth)
						refColumn -= GiantEarthWidth;

					int row = RowFromRefMapRow[territoryData.Row];
					int column = ColumnFromRefMapColumn[refColumn];

					WorldPosition refPosition = new WorldPosition(column, row);

					float factor = 1;
					if (TrueCultureLocation.UseReferenceCoordinates())
					{
						if (!AreSameContinentByReference(territory, territoryData))
							factor = 1.5f;
					}

					int distance = (int)(territory.VisualCenter.GetDistance(refPosition.ToTileIndex())*factor);

					//Diagnostics.LogWarning($"[Gedemon] [CultureUnlock]    - potential #{territoryData.Index} {territoryName} at ref position = {refPosition} (ref distance = {distance}, continent = {territoryData.Continent})");

					if (distance < bestDistance)
					{
						bestDistance = distance;
						bestIndex = territoryData.Index;
						bestName = territoryName;
					}
				}
			}
		}

		public static void UpdateTerritoryListFromReference(IDictionary<string, List<int>> listFactionTerritories)
        {
			List<string> keyList = new List<string>(listFactionTerritories.Keys);
			foreach(string key in keyList)
			{
				List<int> territoryList = new List<int>();
				List<int> referenceList = listFactionTerritories[key];
				foreach(int refIndex in referenceList)
				{
					int territoryIndex = TerritoryFromRefMapTerritory[refIndex];
					if (!territoryList.Contains(territoryIndex))
						territoryList.Add(territoryIndex);

				}
				listFactionTerritories[key] = territoryList;
			}
		}

		public static void BuildListTerritories()
        {
			Diagnostics.LogWarning($"[Gedemon] [CultureUnlock] building territoriesWithMinorFactions[] and territoriesWithMajorEmpires[]");

			TerritoriesWithMinorFactions = new List<string>[maxNumTerritories];
			TerritoriesUnlockingMajorEmpires = new List<string>[maxNumTerritories];

			for (int i = 0; i < maxNumTerritories; i++)
			{
				TerritoriesWithMinorFactions[i] = new List<string>();
				TerritoriesUnlockingMajorEmpires[i] = new List<string>();
			}

			foreach (KeyValuePair<string, List<int>> minorTerritories in ListMinorFactionTerritories)
			{
				Diagnostics.LogWarning($"[Gedemon] Adding {minorTerritories.Key} entry to TerritoriesWithMinorFactions");
				foreach (int index in minorTerritories.Value)
				{
					if (index < 0 || index >= TerritoriesWithMinorFactions.Length)
					{
						Diagnostics.LogError($"[Gedemon] index {index} is out of bound (TerritoriesWithMinorFactions.Length = {TerritoriesWithMinorFactions.Length})");
					}
					TerritoriesWithMinorFactions[index].Add(minorTerritories.Key);
				}
			}

			foreach (KeyValuePair<string, List<int>> majorTerritories in ListMajorEmpireTerritories)
			{
				Diagnostics.LogWarning($"[Gedemon] Adding {majorTerritories.Key} entry to TerritoriesWithMajorEmpires");
				if (HasNoCapitalTerritory(majorTerritories.Key))
				{
					foreach (int index in majorTerritories.Value)
					{
						if (index < 0 || index >= TerritoriesUnlockingMajorEmpires.Length)
						{
							Diagnostics.LogError($"[Gedemon] index {index} is out of bound (TerritoriesWithMajorEmpires.Length = {TerritoriesWithMinorFactions.Length})");
						}
						//Diagnostics.Log($"[Gedemon] adding at index {index} ({GetTerritoryName(index)})");
						TerritoriesUnlockingMajorEmpires[index].Add(majorTerritories.Key);
					}
				}
				else if (majorTerritories.Value.Count > 0)
				{
					//Diagnostics.Log($"[Gedemon] adding at index {majorTerritories.Value[0]} ({GetTerritoryName(majorTerritories.Value[0])})");
					TerritoriesUnlockingMajorEmpires[majorTerritories.Value[0]].Add(majorTerritories.Key);
				}
				if (!ListMajorEmpireCoreTerritories.ContainsKey(majorTerritories.Key))
				{
					Diagnostics.LogWarning($"[Gedemon] Adding missing {majorTerritories.Key} entry to listMajorEmpireCoreTerritories (using listMajorEmpireTerritories)");
					ListMajorEmpireCoreTerritories.Add(majorTerritories.Key, majorTerritories.Value);
				}
			}
		}

		public static bool ValidateGiantEarthReference(World currentWorld)
		{
			int numTerritories = currentWorld.Territories.Length;

			ExtraPositions.Clear();
			ExtraPositionsNewWorld.Clear();
			ContinentNames.Clear();

			int numContinents = currentWorld.ContinentInfo.Length;
			for (int continentIndex = 1; continentIndex < numContinents; continentIndex++) // ignoring index #0 (Ocean)
			{
				Diagnostics.Log($"[Gedemon] Set name to Continent #{continentIndex}");
				ContinentNames.Add(continentIndex, RefMapContinentNameFromContinent[continentIndex]);
			}

			for (int index = 0; index < numTerritories; index++)
			{
				Territory territory = currentWorld.Territories[index];
				int refIndex = RefMapTerritoryFromTerritory[index];

				Diagnostics.Log($"[Gedemon] Set name to territory #{index} (continent = {territory.ContinentIndex}) reference index #{refIndex}");

				if (territoryNamesGiantEarthMap.TryGetValue(refIndex, out string territoryName))
				{
					Diagnostics.LogWarning($"[Gedemon] index #{index}, refIndex #{refIndex}, name = {territoryName}");
					TerritoryNames[index] = territoryName;
				}
			}

			UpdateTerritoryListFromReference(ListMinorFactionTerritories);
			UpdateTerritoryListFromReference(ListMajorEmpireTerritories);
			UpdateTerritoryListFromReference(ListMajorEmpireCoreTerritories);

			BuildListTerritories();

			if (TrueCultureLocation.IsEnabled() && ListMajorEmpireTerritories.Count == 0)
			{
				Diagnostics.LogError($"[Gedemon] Error : TrueCultureLocation.IsEnabled = {TrueCultureLocation.IsEnabled()} && listMajorEmpireTerritories.Count = {ListMajorEmpireTerritories.Count}");
				return false;
			}

			return true;
        }

		public static void InitializeTCL(World currentWorld)
		{

			Diagnostics.LogWarning($"[Gedemon] [CultureUnlock] Initializing TCL");

			MapCanUseGiantEarthReference = false;

			if (GiantEarthMapHash.Contains(CurrentMapHash))
			{
				Diagnostics.LogWarning($"[Gedemon] [CultureUnlock] Giant Earth Map detected, set default values (CurrentMapHash = {CurrentMapHash})");
				ImportGiantEarthData();
			}
			else if(TrueCultureLocation.CanCreateTCL())
            {
				PrepareForGiantEarthReference(currentWorld);
			}

			if(MapCanUseGiantEarthReference)
            {
				ImportGiantEarthData();
			}

			ModLoading.BuildModdedLists();

			if (MapCanUseGiantEarthReference)
			{
				IsMapValidforTCL = ValidateGiantEarthReference(currentWorld);
			}
			else
			{
				IsMapValidforTCL = ValidateMapTCL();
			}

			if (GiantEarthMapHash.Contains(CurrentMapHash))
			{
				Diagnostics.LogWarning($"[Gedemon] [CultureUnlock] Giant Earth Map detected, Building Territory CityMap...");
				CityMap.BuildTerritoryCityMap(currentWorld);
			}

			if (CultureUnlock.IsCompatibleMap())
            {
				// Apply continents names
				int numContinents = currentWorld.ContinentInfo.Length;
				for (int continentIndex = 1; continentIndex < numContinents; continentIndex++) // ignoring index #0 (Ocean)
				{
					ref ContinentInfo continentInfo = ref currentWorld.ContinentInfo.Data[continentIndex];
					if (ContinentHasName(continentIndex))
					{
						continentInfo.ContinentName = CultureUnlock.GetContinentName(continentIndex);
					}
				}
			}
			else
			{
				Diagnostics.LogError($"[Gedemon] incompatible Map");
			}

		}

		public static bool ValidateMapTCL()
        {
			BuildListTerritories();

			if (TrueCultureLocation.IsEnabled() && ListMajorEmpireTerritories.Count == 0)
			{
				Diagnostics.LogError($"[Gedemon] Error : TrueCultureLocation.IsEnabled = {TrueCultureLocation.IsEnabled()} && listMajorEmpireTerritories.Count = {ListMajorEmpireTerritories.Count}");
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
			return TerritoriesWithMinorFactions[territoryIndex].Count > 0;
		}

		public static bool IsMinorFactionPosition(int territoryIndex, string minorFactionName)
		{
			return TerritoriesWithMinorFactions[territoryIndex].Contains(minorFactionName);
		}

		public static List<string> GetListMinorFactionsForTerritory(int territoryIndex)
		{
			return TerritoriesWithMinorFactions[territoryIndex];
		}

		public static bool HasAnyMajorEmpirePosition(int territoryIndex)
		{
			return TerritoriesUnlockingMajorEmpires[territoryIndex].Count > 0;
		}

		public static bool IsMajorEmpirePosition(int territoryIndex, string majorEmpireName)
		{
			return TerritoriesUnlockingMajorEmpires[territoryIndex].Contains(majorEmpireName);
		}
		public static bool IsMajorEmpirePosition(int territoryIndex, StaticString majorEmpireName)
		{
			return TerritoriesUnlockingMajorEmpires[territoryIndex].Contains(majorEmpireName.ToString());
		}

		public static List<string> GetListMajorEmpiresForTerritory(int territoryIndex)
        {
			return TerritoriesUnlockingMajorEmpires[territoryIndex];
		}

		public static bool HasMajorTerritories(string factionName)
		{
			if(ListMajorEmpireTerritories.TryGetValue(factionName, out List<int> territories))
            {
				return territories.Count > 0;
            }
			return false;
		}
		public static bool HasMinorTerritories(string factionName)
		{
			if (ListMinorFactionTerritories.TryGetValue(factionName, out List<int> territories))
			{
				return territories.Count > 0;
			}
			return false;
		}
		public static bool HasTerritory(string factionName, int territoryIndex)
		{
			if (HasMajorTerritories(factionName))
				return ListMajorEmpireTerritories[factionName].Contains(territoryIndex);
			else if (HasMinorTerritories(factionName))
				return ListMinorFactionTerritories[factionName].Contains(territoryIndex);
			else
				return false;
		}
		public static bool HasCoreTerritory(string factionName, int territoryIndex)
		{
			if(TrueCultureLocation.KeepOnlyCoreTerritories())
			{
				if (ListMajorEmpireCoreTerritories.TryGetValue(factionName, out List<int> territoryList))
					return territoryList.Contains(territoryIndex);
				else
					return false;

			}
			return HasTerritory(factionName, territoryIndex);
		}
		public static bool IsCapitalTerritory(string factionName, int territoryIndex, bool any)
		{
			if (any)
			{
				return HasCoreTerritory(factionName, territoryIndex);
			}
			else
			{
				if (TrueCultureLocation.KeepOnlyCoreTerritories() && ListMajorEmpireCoreTerritories.TryGetValue(factionName, out List<int> coreList))
				{
					if(coreList.Count > 0)
						return coreList[0] == territoryIndex;
				}
				else if(ListMajorEmpireTerritories.TryGetValue(factionName, out List<int> territoryList))
                {
					if(territoryList.Count > 0)
						return territoryList[0] == territoryIndex;
				}
			}
			return false;
		}

		public static bool IsNextEraUnlock(int territoryIndex, int currentEraIndex)
		{

			Diagnostics.LogWarning($"[Gedemon] IsNextEraUnlock for {GetTerritoryName(territoryIndex)} #ID = {territoryIndex} (local player era = {currentEraIndex})");
			List<string> listMajorEmpires = TerritoriesUnlockingMajorEmpires[territoryIndex];

			Diagnostics.LogWarning($"[Gedemon] IsNextEraUnlock listMajorEmpires exists = {listMajorEmpires != null}");

			if (listMajorEmpires != null)
			{
				Diagnostics.LogWarning($"[Gedemon] IsNextEraUnlock listMajorEmpires count = {listMajorEmpires.Count}");

				if (listMajorEmpires.Count > 0)
				{
					int nextEraIndex = currentEraIndex + 1;
					foreach (string keyName in listMajorEmpires)
					{
						bool anyTerritory = HasNoCapitalTerritory(keyName);

						Diagnostics.LogWarning($"[Gedemon] IsNextEraUnlock check faction = {keyName}, anyTerritory = {anyTerritory}");

						if (IsCapitalTerritory(keyName, territoryIndex, anyTerritory))
						{
							StaticString factionName = new StaticString(keyName);
							FactionDefinition factionDefinition = Amplitude.Mercury.Utils.GameUtils.GetFactionDefinition(factionName);

							if (factionDefinition == null)
								continue;

							Diagnostics.LogError($"[Gedemon] IsNextEraUnlock for {GetTerritoryName(territoryIndex)} and {factionName} (local player era = {currentEraIndex}, next Era = {nextEraIndex}, faction era = {factionDefinition.EraIndex})");

							if (factionDefinition.EraIndex == nextEraIndex)
							{
								return true;
							}
						}
					}
				}
			}
			return false;
        }

		public static bool IsUnlockedByPlayerSlot(string civilizationName, int empireIndex)
		{
			return ListSlots.ContainsKey(civilizationName) && ListSlots[civilizationName].Contains(empireIndex);
		}

		public static bool IsFirstEraBackupCivilization(string civilizationName)
		{
			return FirstEraBackup.Contains(civilizationName);
		}

		public static bool IsNomadCulture(string civilizationName)
		{
			return NomadCultures.Contains(civilizationName);
		}

		public static bool HasNoCapitalTerritory(string civilizationName) // can be picked by multiple players
		{
			return NoCapitalTerritory.Contains(civilizationName);
		}

		public static List<int> GetListTerritories(string factionName)
		{
			if (TrueCultureLocation.KeepOnlyCoreTerritories())
			{
				if (ListMajorEmpireCoreTerritories.TryGetValue(factionName, out List<int> listCoreTerritories))
				{
					return listCoreTerritories;
				}
				else if (ListMajorEmpireTerritories.TryGetValue(factionName, out List<int> listTerritories))
				{
					return listTerritories;
				}
                else
                {
					return new List<int>();
                }
			}
            else if (ListMajorEmpireTerritories.TryGetValue(factionName, out List<int> listTerritories))
			{
				return listTerritories;
			}
			else
			{
				return new List<int>();
			}
		}

		public static int GetCapitalTerritoryIndex(string civilizationName)
		{
			return ListMajorEmpireTerritories[civilizationName][0];
		}

		public static bool TerritoryHasName(int territoryIndex)
		{
			return TerritoryNames.ContainsKey(territoryIndex);
		}

		public static string GetTerritoryName(int territoryIndex)
		{
			if (TerritoryNames.TryGetValue(territoryIndex, out string name))
			{
				return name;
			}
			return Amplitude.Mercury.UI.Utils.GameUtils.GetTerritoryName(territoryIndex);
		}
		public static string GetTerritoryName(int territoryIndex, bool hasName)
		{
			return TerritoryNames[territoryIndex];
		}

		public static bool ContinentHasName(int continentIndex)
		{
			return ContinentNames.ContainsKey(continentIndex);
		}

		public static string GetContinentName(int continentIndex)
		{
			return ContinentNames[continentIndex];
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
			if(ListMajorEmpireTerritories.ContainsKey(factionName))
            {
				ListMajorEmpireTerritories[factionName] = listTerritories;
			}
			else
            {
				ListMajorEmpireTerritories.Add(factionName, listTerritories);
			}
		}
		public static void UpdateListMajorEmpireCoreTerritories(string factionName, List<int> listTerritories)
		{
			if (ListMajorEmpireCoreTerritories.ContainsKey(factionName))
			{
				ListMajorEmpireCoreTerritories[factionName] = listTerritories;
			}
			else
			{
				ListMajorEmpireCoreTerritories.Add(factionName, listTerritories);
			}
		}
		public static void UpdateListMinorFactionTerritories(string factionName, List<int> listTerritories)
		{
			if (ListMinorFactionTerritories.ContainsKey(factionName))
			{
				ListMinorFactionTerritories[factionName] = listTerritories;
			}
			else
			{
				ListMinorFactionTerritories.Add(factionName, listTerritories);
			}
		}
		public static void UpdateListTerritoryNames(int territoryIndex, string name)
		{
			if (TerritoryNames.ContainsKey(territoryIndex))
			{
				TerritoryNames[territoryIndex] = name;
			}
			else
			{
				TerritoryNames.Add(territoryIndex, name);
			}
		}
		public static void UpdateListContinentNames(int index, string name)
		{
			if (ContinentNames.ContainsKey(index))
			{
				ContinentNames[index] = name;
			}
			else
			{
				ContinentNames.Add(index, name);
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
			if (!NoCapitalTerritory.Contains(factionName))
			{
				NoCapitalTerritory.Add(factionName);
			}
		}
		public static void UpdateListNomads(string factionName)
		{
			if (!NomadCultures.Contains(factionName))
			{
				NomadCultures.Add(factionName);
			}
		}

		public static void logEmpiresTerritories()
		{
			foreach (KeyValuePair<string, List<int>> kvp in ListMajorEmpireTerritories)
			{
				Diagnostics.Log($"[Gedemon] Culture : {kvp.Key}");
				foreach (int territoryIndex in kvp.Value)
				{
					Diagnostics.Log($"[Gedemon] - {GetTerritoryName(territoryIndex)} (#{territoryIndex})");
				}
			}
		}
		public static void OnExitSandbox()
		{

			ExtraPositions.Clear();
			ExtraPositionsNewWorld.Clear();
			ContinentNames.Clear();
			TerritoryNames.Clear();
			ListMinorFactionTerritories.Clear();
			ListMajorEmpireTerritories.Clear();
			ListMajorEmpireCoreTerritories.Clear();
		}
	}
}
