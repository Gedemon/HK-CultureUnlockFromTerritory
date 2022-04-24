using BepInEx;
using System.Collections.Generic;
using Amplitude.Mercury.Simulation;
using HarmonyLib;
using Amplitude.Mercury.Sandbox;
using Amplitude.Framework;
using Amplitude.Mercury.UI;
using UnityEngine;
using HumankindModTool;
using Amplitude;
using Amplitude.Framework.Session;
using Amplitude.Mercury.Session;
using Amplitude.Mercury.Data.Simulation;
using Amplitude.Mercury.Interop;
using Amplitude.Mercury.Data.Simulation.Costs;
using Amplitude.Mercury;
using Amplitude.Mercury.Avatar;
using Amplitude.Mercury.Presentation;
using Amplitude.Mercury.AI.Brain.Analysis.ArmyBehavior;
using Amplitude.AI.Heuristics;
using Amplitude.Mercury.Terrain;
using System;
using DistrictTypes = Amplitude.Mercury.Simulation.DistrictTypes;

namespace Gedemon.TrueCultureLocation
{
	[BepInPlugin(pluginGuid, "True Culture Location", pluginVersion)]
	[BepInIncompatibility("gedemon.humankind.uchronia")]
	public class TrueCultureLocation : BaseUnityPlugin
	{
		public const string pluginGuid = "gedemon.humankind.trueculturelocation";
		public const string pluginVersion = "1.0.6.0";

		#region Define Options

		public static readonly GameOptionInfo UseTrueCultureLocation = new GameOptionInfo
		{
			ControlType = UIControlType.Toggle,
			Key = "GameOption_TCL_UseTrueCultureLocation",
			GroupKey = "GameOptionGroup_LobbyDifficultyOptions",
			DefaultValue = "True",
			Title = "[TCL] True Culture Location",
			Description = "Toggles unlocking Culture by owned Territories on compatible maps, all options tagged [TCL] are only active when this setting is set to 'ON'",
			States =
			{
				new GameOptionStateInfo
				{
					Title = "On",
					Description = "On",
					Value = "True"
				},
				new GameOptionStateInfo
				{
					Title = "Off",
					Description = "Off",
					Value = "False"
				}
			}
		};

		public static readonly GameOptionInfo CreateTrueCultureLocationOption = new GameOptionInfo
		{
			ControlType = UIControlType.DropList,
			Key = "GameOption_TCL_CreateTrueCultureLocation",
			GroupKey = "GameOptionGroup_LobbyDifficultyOptions",
			DefaultValue = "Off",
			Title = "[TCL] Generate TCL for any Map <c=FF0000>*Experimental*</c>",
			Description = "Rename territories on unsupported or generated maps, based on relative distances using the Giant Earth Map coordinates, to use True Culture Location.",
			States =
			{
				new GameOptionStateInfo
				{
					Title = "Disabled",
					Description = "No TCL on unsupported maps",
					Value = "Off"
				},
				new GameOptionStateInfo
				{
					Title = "Grouped by continents",
					Description = "Group Territories by Continents (or by SuperContinents when the map generate a low number of continents)",
					Value = "Continents"
				},
				new GameOptionStateInfo
				{
					Title = "By Coordinates",
					Description = "Use Giant Earth Map territories coordinates for reference",
					Value = "Coordinates"
				},
				new GameOptionStateInfo
				{
					Title = "By Coordinates with shift",
					Description = "Use Giant Earth Map territories coordinates, with Americas shifted back on the West (the Giant Earth use an unconventional presentation, with Americas on the East)",
					Value = "ShiftedCoordinates"
				}
			}
		};

		public static GameOptionStateInfo TerritoryLoss_Full = new GameOptionStateInfo
		{
			Value = "TerritoryLoss_Full",
			Title = "Keep Only New Empire",
			Description = "Lose all territories that were not controlled by the Empire of the new Culture"
		};

		public static GameOptionStateInfo TerritoryLoss_None = new GameOptionStateInfo
		{
			Value = "TerritoryLoss_None",
			Title = "None",
			Description = "Keep control of all your territories when changing Culture"
		};

		public static GameOptionStateInfo TerritoryLoss_KeepAttached = new GameOptionStateInfo
		{
			Value = "TerritoryLoss_KeepAttached",
			Title = "Keep Attached",
			Description = "Territories that are attached to a Settlement that has at least one territory belonging to the new Culture's Empire will not be detached and kept in the Empire, only the other territories will be lost."
		};

		public static GameOptionStateInfo TerritoryLoss_Full_Core = new GameOptionStateInfo
		{
			Value = "TerritoryLoss_Full_Core",
			Title = "Keep Only Core Empire",
			Description = "Lose all territories that were not controlled by the core Empire of the new Culture"
		};
		/*
		public static GameOptionStateInfo TerritoryLoss_ByStability = new GameOptionStateInfo
		{
			Value = "TerritoryLoss_ByStability",
			Title = "By Stability",
			Description = "Territories that were not controlled by the new Culture are kept only if they have a high Stability"
		};
		//*/


		public static GameOptionInfo TerritoryLossOption = new GameOptionInfo
		{
			ControlType = UIControlType.DropList,
			Key = "GameOption_TCL_TerritoryLoss",
			DefaultValue = "TerritoryLoss_None",
			Title = "[TCL] Territory Loss on Culture Change",
			Description = "Determines which territories you may loss when changing Culture",
			GroupKey = "GameOptionGroup_LobbyDifficultyOptions",
			States = { TerritoryLoss_None, TerritoryLoss_KeepAttached, TerritoryLoss_Full, TerritoryLoss_Full_Core }//, TerritoryLoss_ByStability }
		};

		private static readonly List<GameOptionStateInfo> ErasCityRequired = new List<GameOptionStateInfo>
		{
			new GameOptionStateInfo
			{
				Title = "Classical",
				Description = "A City or an Administrative Center attached to a City is required in a territory to unlock Cultures of the Classical Era or later",
				Value = "2"
			},
			new GameOptionStateInfo
			{
				Title = "Medieval",
				Description = "A City or an Administrative Center attached to a City is required in a territory to unlock Cultures of the Medieval Era or later",
				Value = "3"
			},
			new GameOptionStateInfo
			{
				Title = "Early Modern",
				Description = "A City or an Administrative Center attached to a City is required in a territory to unlock Cultures of the Early Modern Era or later",
				Value = "4"
			},
			new GameOptionStateInfo
			{
				Title = "Industrial",
				Description = "A City or an Administrative Center attached to a City is required in a territory to unlock Cultures of the Industrial Era or later",
				Value = "5"
			},
			new GameOptionStateInfo
			{
				Title = "Contemporary",
				Description = "A City or an Administrative Center attached to a City is required in a territory to unlock Cultures of the Contemporary Era or later",
				Value = "6"
			},
			new GameOptionStateInfo
			{
				Title = "None",
				Description = "A City or an Administrative Center attached to a City is never required",
				Value = "99"
			}
		};

		public static GameOptionInfo FirstEraRequiringCityToUnlock = new GameOptionInfo
		{
			ControlType = UIControlType.DropList,
			Key = "GameOption_TCL_FirstEraRequiringCityToUnlock",
			DefaultValue = "3",
			Title = "[TCL] First Era for City Requirement",
			Description = "First Era from whitch a City (or an Administrative Center attached to a City) is required on a Culture's territory to unlock it",
			GroupKey = "GameOptionGroup_LobbyDifficultyOptions",
			States = ErasCityRequired
		};

		private static readonly List<GameOptionStateInfo> NumEmpireSlots = new List<GameOptionStateInfo>
		{
			new GameOptionStateInfo
			{
				Title = "No Extra",
				Description = "No additional Empires, use the New Game setting for Competitors",
				Value = "0"
			},
			new GameOptionStateInfo
			{
				Title = "11",
				Description = "11 Empires",
				Value = "11"
			},
			new GameOptionStateInfo
			{
				Title = "12",
				Description = "12 Empires",
				Value = "12"
			},
			new GameOptionStateInfo
			{
				Title = "13",
				Description = "13 Empires",
				Value = "13"
			},
			new GameOptionStateInfo
			{
				Title = "14",
				Description = "14 Empires",
				Value = "14"
			},
			new GameOptionStateInfo
			{
				Title = "15",
				Description = "15 Empires",
				Value = "15"
			},
			new GameOptionStateInfo
			{
				Title = "16",
				Description = "16 Empires",
				Value = "16"
			},
			/*
			new GameOptionStateInfo
			{
				Title = "17",
				Description = "17 Empires",
				Value = "17"
			},
			new GameOptionStateInfo
			{
				Title = "18",
				Description = "18 Empires",
				Value = "18"
			},
			new GameOptionStateInfo
			{
				Title = "19",
				Description = "19 Empires",
				Value = "19"
			},
			new GameOptionStateInfo
			{
				Title = "20",
				Description = "20 Empires",
				Value = "20"
			},
			new GameOptionStateInfo
			{
				Title = "22",
				Description = "22 Empires",
				Value = "22"
			},
			new GameOptionStateInfo
			{
				Title = "25",
				Description = "25 Empires",
				Value = "25"
			},
			new GameOptionStateInfo
			{
				Title = "28",
				Description = "28 Empires",
				Value = "28"
			},
			new GameOptionStateInfo
			{
				Title = "32",
				Description = "32 Empires",
				Value = "32"
			},
			//*/
		};

		private static readonly List<GameOptionStateInfo> NumSettlingEmpireSlots = new List<GameOptionStateInfo>
		{
			new GameOptionStateInfo
			{
				Title = "No Limit",
				Description = "No limitation, all AI Empires can create outposts",
				Value = "99"
			},
			new GameOptionStateInfo
			{
				Title = "1",
				Description = "slot 1",
				Value = "1"
			},
			new GameOptionStateInfo
			{
				Title = "2",
				Description = "slot 1 and 2",
				Value = "2"
			},
			new GameOptionStateInfo
			{
				Title = "3",
				Description = "slot 1 to 3",
				Value = "3"
			},
			new GameOptionStateInfo
			{
				Title = "4",
				Description = "slot 1 to 4",
				Value = "4"
			},
			new GameOptionStateInfo
			{
				Title = "5",
				Description = "slot 1 to 5",
				Value = "5"
			},
			new GameOptionStateInfo
			{
				Title = "6",
				Description = "slot 1 to 6",
				Value = "6"
			},
			new GameOptionStateInfo
			{
				Title = "7",
				Description = "slot 1 to 7",
				Value = "7"
			},
			new GameOptionStateInfo
			{
				Title = "8",
				Description = "slot 1 to 8",
				Value = "8"
			},
			new GameOptionStateInfo
			{
				Title = "9",
				Description = "slot 1 to 9",
				Value = "9"
			},
			new GameOptionStateInfo
			{
				Title = "10",
				Description = "slot 1 to 10",
				Value = "10"
			},
		};

		public static GameOptionInfo ExtraEmpireSlots = new GameOptionInfo
		{
			ControlType = UIControlType.DropList,
			Key = "GameOption_TCL_ExtraEmpireSlots",
			DefaultValue = "0",
			Title = "Maximum number of Competitors",
			Description = "Add extra Empire Slots in game (AI only), allowing a maximum of 16 Empires (10 from setup + 6 extra AI slots). This can be used either on custom maps compatible with this mod or any random map, but will cause an error with incompatible custom maps. With [TCL], Nomadic Tribes controlled by those AI players will be able to spawn as a new Empire on the location of a Culture that has not been controlled yet, or take control of some territories of an old Empire during a split. (this setting override the default Starting Positions when using the Giant Earth Map). If you don't use [TCL], you should use (AOM) 'Allow Duplicate Cultures' Mod",
			GroupKey = "GameOptionGroup_LobbyDifficultyOptions",
			States = NumEmpireSlots
		};

		public static GameOptionInfo SettlingEmpireSlotsOption = new GameOptionInfo
		{
			ControlType = UIControlType.DropList,
			Key = "GameOption_TCL_SettlingEmpireSlotsOption",
			DefaultValue = "10",
			Title = "[TCL] Number of slot IDs that start in Neolithic",
			Description = "Set how many Slots (from the setup screen) are allowed to spawn in the Neolithic Era (this option ignore humans player slots). Empire controlled by the AI players from higher slots will be able to spawn only as a new Empire on the location of a Culture that has not been taken after changing Era, or take control of some territories of an old Empire during a split.",
			GroupKey = "GameOptionGroup_LobbyDifficultyOptions",
			States = NumSettlingEmpireSlots
		};

		public static readonly GameOptionInfo StartingOutpost = new GameOptionInfo
		{
			ControlType = UIControlType.DropList,
			Key = "GameOption_TCL_StartingOutpost",
			GroupKey = "GameOptionGroup_LobbyDifficultyOptions",
			DefaultValue = "Off",
			Title = "Start with an Outpost (disabled in MP)</c>",
			Description = "Toggle if Empires will start with an Outpost. This setting can be used on any map. Currently disbled when starting a MP game as it causes desyncs",
			States =
			{
				new GameOptionStateInfo
				{
					Title = "On",
					Description = "Everyone start with an Outpost",
					Value = "On"
				},
				new GameOptionStateInfo
				{
					Title = "Off",
					Description = "No starting Outposts",
					Value = "Off"
				},
				new GameOptionStateInfo
				{
					Title = "AI Only",
					Description = "Only AI players will start with an outpost",
					Value = "OnlyAI"
				}
			}
		};

		public static readonly GameOptionInfo HistoricalDistrictsOption = new GameOptionInfo
		{
			ControlType = UIControlType.Toggle,
			Key = "GameOption_TCL_HistoricalDistrictsOption",
			GroupKey = "GameOptionGroup_LobbyDifficultyOptions",
			DefaultValue = "True",
			Title = "Historical Districts",
			Description = "Toggle to keep Districts initial visual appearence",
			States =
			{
				new GameOptionStateInfo
				{
					Title = "On",
					Description = "On",
					Value = "True"
				},
				new GameOptionStateInfo
				{
					Title = "Off",
					Description = "Off",
					Value = "False"
				}
			}
		};

		public static readonly GameOptionInfo DebugCityNameOption = new GameOptionInfo
		{
			ControlType = UIControlType.Toggle,
			Key = "GameOption_TCL_DebugCityNameOption",
			GroupKey = "GameOptionGroup_LobbyDifficultyOptions",
			DefaultValue = "False",
			Title = "<c=FF0000>[DEBUG]</c> Show CityMap with [F3]",
			Description = "When ON the City Map Names will be dispalyed instead of territory names when pressing the [F3] key",
			States =
			{
				new GameOptionStateInfo
				{
					Title = "On",
					Description = "On",
					Value = "True"
				},
				new GameOptionStateInfo
				{
					Title = "Off",
					Description = "Off",
					Value = "False"
				}
			}
		};

		public static readonly GameOptionInfo CityMapOption = new GameOptionInfo
		{
			ControlType = UIControlType.Toggle,
			Key = "GameOption_TCL_CityMapOption",
			GroupKey = "GameOptionGroup_LobbyDifficultyOptions",
			DefaultValue = "True",
			Title = "[MAP] Use City Map for naming",
			Description = "Toggle to use the City Map (True Location naming for cities) when possible",
			States =
			{
				new GameOptionStateInfo
				{
					Title = "On",
					Description = "On",
					Value = "True"
				},
				new GameOptionStateInfo
				{
					Title = "Off",
					Description = "Off",
					Value = "False"
				}
			}
		};

		public static readonly GameOptionInfo NewEmpireSpawningOption = new GameOptionInfo
		{
			ControlType = UIControlType.DropList,
			Key = "GameOption_TCL_NewEmpireSpawningOption",
			GroupKey = "GameOptionGroup_LobbyDifficultyOptions",
			DefaultValue = "1",
			Title = "[TCL] New Empires spawn",
			Description = "Set how New Empires will Spawn mid-game from the pool of Empire Slots (ie when the Neolithic Start Slots are lower than the Maximum Number of Competitors)",
			States =
			{
				new GameOptionStateInfo
				{
					Title = "Only on Split",
					Description = "Only spawn when a previous Empire is split during Culture Change",
					Value = "0"
				},
				new GameOptionStateInfo
				{
					Title = "From Minor",
					Description = "Can spawn on Empire split and from Minor Factions",
					Value = "1"
				},
				/*
				new GameOptionStateInfo
				{
					Title = "From AI players",
					Description = "Can spawn on Empire split and from AI players (Minor Factions or Major Empires on transcending)",
					Value = "2"
				},
				new GameOptionStateInfo
				{
					Title = "From all players",
					Description = "Can spawn on Empire split and from all players on transcending",
					Value = "3"
				}
				//*/
			}
		};

		public static readonly GameOptionInfo StartPositionList = new GameOptionInfo
		{
			ControlType = UIControlType.DropList,
			Key = "GameOption_TCL_StartPositionList",
			GroupKey = "GameOptionGroup_LobbyDifficultyOptions",
			DefaultValue = "Default",
			Title = "[MAP] Starting Position List",
			Description = "Choose if you want the map's default Starting Positions or one of the alternate list (only active when compatible maps are used)",
			States =
			{
				new GameOptionStateInfo
				{
					Title = "Map Default",
					Description = "Use the Map's default Starting Positions (this setting is overriden if the maximum number of competitor is raised above 10, the Alternate list is then used if available)",
					Value = "Default"
				},
				new GameOptionStateInfo
				{
					Title = "Alternate",
					Description = "Use only the Alternate Starting Positions, ignoring the map's default positions aven with 10 players or less. Some starting positions may be adjacent from each other, even with a low number of players as the ist is made for 16 slots)",
					Value = "ExtraStart"
				},
				new GameOptionStateInfo
				{
					Title = "Old World",
					Description = "Use only the Alternate starting positions (Old World starts only, if the map allows it), ignoring the map's default positions.",
					Value = "OldWorld"
				}
			}
		};

		public static readonly GameOptionInfo CompensationLevel = new GameOptionInfo
		{
			ControlType = UIControlType.DropList,
			Key = "GameOption_TCL_CompensationLevel",
			GroupKey = "GameOptionGroup_LobbyDifficultyOptions",
			DefaultValue = "2",
			Title = "[TCL] Level of Compensation",
			Description = "Define the level of compensation an Empire will get per Settlement lost during an Evolution, based on total number of Settlements for Influence, and on yield per turn for Money, Science, Production",
			States =
			{
				new GameOptionStateInfo
				{
					Title = "None",
					Description = "No compensation.",
					Value = "0"
				},
				new GameOptionStateInfo
				{
					Title = "Low",
					Description = "Low compensation (x5)",
					Value = "1"
				},
				new GameOptionStateInfo
				{
					Title = "Average",
					Description = "Average compensation (x10)",
					Value = "2"
				},
				new GameOptionStateInfo
				{
					Title = "High",
					Description = "High compensation (x20)",
					Value = "3"
				}
			}
		};

		public static readonly GameOptionInfo TerritoryLossIgnoreAI = new GameOptionInfo
		{
			ControlType = UIControlType.Toggle,
			Key = "GameOption_TCL_NoTerritoryLossForAI",
			GroupKey = "GameOptionGroup_LobbyDifficultyOptions",
			DefaultValue = "False",
			Title = "[TCL] No Territory Loss For the AI",
			Description = "Toggle to set if the AI ignore the other settings and always keep its full territory",
			States =
			{
				new GameOptionStateInfo
				{
					Title = "On",
					Description = "On",
					Value = "True"
				},
				new GameOptionStateInfo
				{
					Title = "Off",
					Description = "Off",
					Value = "False"
				}
			}
		};

		public static readonly GameOptionInfo TerritoryLossLimitDecisionForAI = new GameOptionInfo
		{
			ControlType = UIControlType.Toggle,
			Key = "GameOption_TCL_TerritoryLossLimitDecisionForAI",
			GroupKey = "GameOptionGroup_LobbyDifficultyOptions",
			DefaultValue = "False",
			Title = "[TCL] Limit Choices For AI",
			Description = "Toggle to limit AI Empires to Culture choices that doesn't result in a big territory loss",
			States =
			{
				new GameOptionStateInfo
				{
					Title = "On",
					Description = "On",
					Value = "True"
				},
				new GameOptionStateInfo
				{
					Title = "Off",
					Description = "Off",
					Value = "False"
				}
			}
		};

		public static readonly GameOptionInfo StartingOutpostForMinorOption = new GameOptionInfo
		{
			ControlType = UIControlType.Toggle,
			Key = "GameOption_TCL_StartingOutpostForMinorOption",
			GroupKey = "GameOptionGroup_LobbyDifficultyOptions",
			DefaultValue = "False",
			Title = "[TCL] Starting Outpost For Minor Faction",
			Description = "Toggle to set if the minor factions directly spawn an outpost on their starting territory or if they are allowed to settle after moving on the map.",
			States =
			{
				new GameOptionStateInfo
				{
					Title = "On",
					Description = "On",
					Value = "True"
				},
				new GameOptionStateInfo
				{
					Title = "Off",
					Description = "Off",
					Value = "False"
				}
			}
		};

		public static readonly GameOptionInfo LargerSpawnAreaForMinorOption = new GameOptionInfo
		{
			ControlType = UIControlType.Toggle,
			Key = "GameOption_TCL_LargerSpawnAreaForMinorOption",
			GroupKey = "GameOptionGroup_LobbyDifficultyOptions",
			DefaultValue = "True",
			Title = "[TCL] Larger Spawn Area For Minor Faction",
			Description = "Toggle to set if the Minor Factions are allowed to spawn in adjacent territories or if they can only spawn in their starting positions territory. As a lot of territories are shared with the Major Empires, allowing a larger spawn area means more Minor Factions will be able to spawn.",
			States =
			{
				new GameOptionStateInfo
				{
					Title = "On",
					Description = "On",
					Value = "True"
				},
				new GameOptionStateInfo
				{
					Title = "Off",
					Description = "Off",
					Value = "False"
				}
			}
		};

		public static readonly GameOptionInfo RespawnDeadPlayersOption = new GameOptionInfo
		{
			ControlType = UIControlType.Toggle,
			Key = "GameOption_TCL_RespawnDeadPlayersOption",
			GroupKey = "GameOptionGroup_LobbyDifficultyOptions",
			DefaultValue = "True",
			Title = "[TCL] Respawning Dead Players",
			Description = "Toggle to set if dead players are allowed to re-spawn when there are no Empire left in the free Empires pool (set when the Neolithic Start Slots are lower than the Maximum Number of Competitors)",
			States =
			{
				new GameOptionStateInfo
				{
					Title = "On",
					Description = "On",
					Value = "True"
				},
				new GameOptionStateInfo
				{
					Title = "Off",
					Description = "Off",
					Value = "False"
				}
			}
		};

		public static readonly GameOptionInfo EliminateLastEmpiresOption = new GameOptionInfo
		{
			ControlType = UIControlType.Toggle,
			Key = "GameOption_TCL_EliminateLastEmpiresOption",
			GroupKey = "GameOptionGroup_LobbyDifficultyOptions",
			DefaultValue = "False",
			Title = "[TCL] Eliminate Last Empires <c=FF0000>*Experimental*</c>",
			Description = "Toggle to eliminate AI Empires that are lagging in a previous Era to free Slots for new Empires (if the Respawning Dead Players option is used)",
			States =
			{
				new GameOptionStateInfo
				{
					Title = "On",
					Description = "On",
					Value = "True"
				},
				new GameOptionStateInfo
				{
					Title = "Off",
					Description = "Off",
					Value = "False"
				}
			}
		};

		public static readonly GameOptionInfo EmpireIconsNumColumnOption = new GameOptionInfo
		{
			ControlType = UIControlType.DropList,
			Key = "GameOption_TCL_EmpireIconsNumColumnOption",
			GroupKey = "GameOptionGroup_LobbyDifficultyOptions",
			DefaultValue = "4",
			Title = "Max number of columns for Empire Icons",
			Description = "Set the maximum width for the Empire Icons panel displayed at the top left of the screen. You will have to change the UI size accordingly (for example 75% for 9 icons)",
			States =
			{
				new GameOptionStateInfo
				{
					Title = "4 (Any UI size)",
					Description = "4 icons (Can be used with any UI size)",
					Value = "4"
				},
				new GameOptionStateInfo
				{
					Title = "5 (90% UI size)",
					Description = "5 icons (maximum UI size: 90%)",
					Value = "5"
				},
				new GameOptionStateInfo
				{
					Title = "6 (85% UI size)",
					Description = "6 icons (maximum UI size: 85%)",
					Value = "6"
				},
				new GameOptionStateInfo
				{
					Title = "7 (80% UI size)",
					Description = "7 icons (maximum UI size: 80%)",
					Value = "7"
				},
				new GameOptionStateInfo
				{
					Title = "8 (75% UI size)",
					Description = "8 icons (maximum UI size: 75%)",
					Value = "8"
				},
				new GameOptionStateInfo
				{
					Title = "9 (75% UI size)",
					Description = "9 icons (maximum UI size: 75%)",
					Value = "9"
				},
			}
		};

		#endregion

		#region Set options

		public bool Enabled => GameOptionHelper.CheckGameOption(UseTrueCultureLocation, "True");
		public bool OnlyCultureTerritory => !GameOptionHelper.CheckGameOption(TerritoryLossOption, "TerritoryLoss_None");
		public bool RespawnDeadPlayer => GameOptionHelper.CheckGameOption(RespawnDeadPlayersOption, "True");
		public bool EliminateLastEmpires => GameOptionHelper.CheckGameOption(EliminateLastEmpiresOption, "True");
		public bool HistoricalDistricts => GameOptionHelper.CheckGameOption(HistoricalDistrictsOption, "True");
		public bool DebugCityName => GameOptionHelper.CheckGameOption(DebugCityNameOption, "True");		
		public bool UseCityMap => GameOptionHelper.CheckGameOption(CityMapOption, "True");
		public bool KeepAttached => GameOptionHelper.CheckGameOption(TerritoryLossOption, "TerritoryLoss_KeepAttached");
		public bool KeepOnlyCore => GameOptionHelper.CheckGameOption(TerritoryLossOption, "TerritoryLoss_Full_Core");
		public bool NoLossForAI => GameOptionHelper.CheckGameOption(TerritoryLossIgnoreAI, "True");
		public bool LimitDecisionForAI => GameOptionHelper.CheckGameOption(TerritoryLossLimitDecisionForAI, "True");
		public bool OnlyOldWorldStart => GameOptionHelper.CheckGameOption(StartPositionList, "OldWorld");
		public bool OnlyExtraStart => GameOptionHelper.CheckGameOption(StartPositionList, "ExtraStart");
		public bool StartingOutpostForAI => !GameOptionHelper.CheckGameOption(StartingOutpost, "Off");
		public bool StartingOutpostForHuman => GameOptionHelper.CheckGameOption(StartingOutpost, "On");
		public bool StartingOutpostForMinorFaction => GameOptionHelper.CheckGameOption(StartingOutpostForMinorOption, "True");
		public bool LargerSpawnAreaForMinorFaction => GameOptionHelper.CheckGameOption(LargerSpawnAreaForMinorOption, "True");
		public bool empireCanSpawnFromMinorFactions => int.Parse(GameOptionHelper.GetGameOption(NewEmpireSpawningOption)) > 0;
		public bool empireCanSpawnFromAI => int.Parse(GameOptionHelper.GetGameOption(NewEmpireSpawningOption)) > 1;
		public bool empireCanSpawnFromHuman => int.Parse(GameOptionHelper.GetGameOption(NewEmpireSpawningOption)) > 2;
		public bool CanCreateTrueCultureLocation => !GameOptionHelper.CheckGameOption(CreateTrueCultureLocationOption, "Off");
		public bool UseShiftToCreateTCL => GameOptionHelper.CheckGameOption(CreateTrueCultureLocationOption, "ShiftedCoordinates");
		public bool UseCoordinatesToCreateTCL => GameOptionHelper.CheckGameOption(CreateTrueCultureLocationOption, "Coordinates");
		public int EraIndexCityRequiredForUnlock => int.Parse(GameOptionHelper.GetGameOption(FirstEraRequiringCityToUnlock));
		public int TotalEmpireSlots => int.Parse(GameOptionHelper.GetGameOption(ExtraEmpireSlots));
		public int SettlingEmpireSlots => int.Parse(GameOptionHelper.GetGameOption(SettlingEmpireSlotsOption));
		public int CompensationLevelValue => int.Parse(GameOptionHelper.GetGameOption(CompensationLevel));
		public int EmpireIconsNumColumn => int.Parse(GameOptionHelper.GetGameOption(EmpireIconsNumColumnOption));

		#endregion

		#region Get Options

		public static bool IsEnabled()
		{
			return Instance.Enabled;
		}
		public static int GetEmpireIconsNumColumn()
		{
			return Instance.EmpireIconsNumColumn;
		}
		public static bool CanRespawnDeadPlayer()
		{
			return Instance.RespawnDeadPlayer;
		}
		public static bool CanEliminateLastEmpires()
		{
			return Instance.EliminateLastEmpires;
		}
		public static bool KeepHistoricalDistricts()
		{
			return Instance.HistoricalDistricts;
		}
		public static bool IsDebugCityName()
		{
			return Instance.DebugCityName;
		}		
		public static bool CanUseCityMap()
		{
			return Instance.UseCityMap;
		}		
		public static bool EmpireCanSpawnFromMinorFactions()
		{
			return Instance.empireCanSpawnFromMinorFactions;
		}
		public static bool EmpireCanSpawnFromAI()
		{
			return Instance.empireCanSpawnFromAI;
		}
		public static bool EmpireCanSpawnFromhuman()
		{
			return Instance.empireCanSpawnFromHuman;
		}
		public static bool KeepOnlyCultureTerritory()
		{
			return Instance.OnlyCultureTerritory;
		}
		public static bool KeepOnlyCoreTerritories()
		{
			return Instance.KeepOnlyCore;
		}
		public static bool KeepTerritoryAttached()
		{
			return Instance.KeepAttached;
		}
		public static bool NoTerritoryLossForAI()
		{
			return Instance.NoLossForAI;
		}
		public static int GetEraIndexCityRequiredForUnlock()
		{
			return Instance.EraIndexCityRequiredForUnlock;
		}

		public static int GetTotalEmpireSlots()
		{
			return Instance.TotalEmpireSlots;
		}

		public static int GetSettlingEmpireSlots()
		{
			return Instance.SettlingEmpireSlots;
		}

		public static int GetCompensationLevel()
		{
			return Instance.CompensationLevelValue;
		}

		public static bool UseExtraEmpireSlots()
		{
			return Instance.TotalEmpireSlots > 0;
		}
		public static bool UseLimitDecisionForAI()
		{
			return Instance.LimitDecisionForAI;
		}
		public static bool UseOnlyOldWorldStart()
		{
			return Instance.OnlyOldWorldStart;
		}
		public static bool UseOnlyExtraStart()
		{
			return Instance.OnlyExtraStart;
		}
		public static bool UseLargerSpawnAreaForMinorFaction()
		{
			return Instance.LargerSpawnAreaForMinorFaction;
		}
		public static bool UseStartingOutpostForMinorFaction()
		{
			return Instance.StartingOutpostForMinorFaction;
		}
		public static bool CanCreateTCL()
		{
			return Instance.CanCreateTrueCultureLocation;
		}
		public static bool UseReferenceCoordinates()
		{
			return Instance.UseCoordinatesToCreateTCL || Instance.UseShiftToCreateTCL;
		}
		public static bool UseShiftedCoordinates()
		{
			return Instance.UseShiftToCreateTCL;
		}

		public static bool HasStartingOutpost(int EmpireIndex, bool IsHuman)
		{

			Diagnostics.LogWarning($"[Gedemon] HasStartingOutpost EmpireIndex = {EmpireIndex}, IsHuman = {IsHuman},  StartingOutpostForAI = {Instance.StartingOutpostForAI}, StartingOutpostForHuman = {Instance.StartingOutpostForHuman}, option = {GameOptionHelper.GetGameOption(StartingOutpost)}");
			if (!IsSettlingEmpire(EmpireIndex, IsHuman))
			{
				return false;
			}
			if (IsHuman)
			{
				return Instance.StartingOutpostForHuman;
			}
			else
			{
				return Instance.StartingOutpostForAI;
			}
		}
		public static bool HasStartingOutpost(int EmpireIndex)
		{
			bool IsHuman = IsEmpireHumanSlot(EmpireIndex);
			Diagnostics.LogWarning($"[Gedemon] HasStartingOutpost EmpireIndex = {EmpireIndex}, IsHuman = {IsHuman},  StartingOutpostForAI = {Instance.StartingOutpostForAI}, StartingOutpostForHuman = {Instance.StartingOutpostForHuman}, option = {GameOptionHelper.GetGameOption(StartingOutpost)}");
			if (!IsSettlingEmpire(EmpireIndex, IsHuman))
			{
				return false;
			}
			if (IsHuman)
			{
				return Instance.StartingOutpostForHuman;
			}
			else
			{
				return Instance.StartingOutpostForAI;
			}
		}

		public static bool IsSettlingEmpire(int EmpireIndex, bool IsHuman)
		{
			if (EmpireIndex < GetSettlingEmpireSlots() || IsHuman)
			{
				return true;
			}
			return false;
		}
		public static bool IsSettlingEmpire(int EmpireIndex)
		{
			if (EmpireIndex < GetSettlingEmpireSlots() || IsEmpireHumanSlot(EmpireIndex))
			{
				return true;
			}
			return false;
		}

		public static bool IsEmpireHumanSlot(int empireIndex)
		{
			ISessionService service = Services.GetService<ISessionService>();
			ISessionSlotController slots = ((Amplitude.Mercury.Session.Session)service.Session).Slots;
			if (empireIndex >= slots.Count)
			{
				return false;
			}
			return slots[empireIndex].IsHuman;
		}

		#endregion

		public bool toggleShowTerritory = false;

		// Awake is called once when both the game and the plug-in are loaded
		void Awake()
		{
			Harmony harmony = new Harmony(pluginGuid);
			Instance = this;
			harmony.PatchAll();
			/*
			Logger.LogInfo($"Patching done for {pluginGuid}, patched methods:");
			foreach(var method in harmony.GetPatchedMethods())
			{
				Logger.LogInfo($" - {method.Name}"); // {method.FullDescription()}
			}
			//*/
		}
		public static TrueCultureLocation Instance;

		public static void CreateStartingOutpost()
		{

			ISessionService sessionService = Services.GetService<ISessionService>();
			bool isMultiplayer = sessionService != null && sessionService.Session != null && sessionService.Session.SessionMode == SessionMode.Online;
			if (isMultiplayer)
				return;

			int numMajor = Amplitude.Mercury.Sandbox.Sandbox.MajorEmpires.Length;
			for (int empireIndex = 0; empireIndex < numMajor; empireIndex++)
			{
				MajorEmpire majorEmpire = Sandbox.MajorEmpires[empireIndex];
				bool isHuman = TrueCultureLocation.IsEmpireHumanSlot(empireIndex);
				WorldPosition worldPosition = new WorldPosition(World.Tables.SpawnLocations[empireIndex]);
				SimulationEntityGUID GUID = SimulationEntityGUID.Zero;

				Diagnostics.LogWarning($"[Gedemon] [CreateStartingOutpost] for {majorEmpire.majorEmpireDescriptorName}, index = {empireIndex}, IsControlledByHuman = {isHuman}"); // IsEmpireHumanSlot(int empireIndex)

				if (TrueCultureLocation.HasStartingOutpost(empireIndex, isHuman)) // 
				{
					majorEmpire.DepartmentOfTheInterior.CreateCampAt(GUID, worldPosition, FixedPoint.Zero, isImmediate : true);
				}
			}
		}


		private void Update()
		{
			if (Input.GetKeyDown((KeyCode)284)) // press F3 to toggle
			{
				toggleShowTerritory = !toggleShowTerritory;
				int localEmpireIndex = SandboxManager.Sandbox.LocalEmpireIndex;
				UIManager UIManager = Services.GetService<IUIService>() as UIManager;

				// reset to default cursor
				Amplitude.Mercury.Presentation.Presentation.PresentationCursorController.ChangeToDefaultCursor(resetUnitDefinition: false);

				if (toggleShowTerritory)
				{
					// hide UI
					UIManager.isUiVisible = false;

					// switch to DiplomaticCursor, where territories can be highlighted
					Amplitude.Mercury.Presentation.Presentation.PresentationCursorController.ChangeToDiplomaticCursor(localEmpireIndex);
				}
				else
				{
					// restore UI
					UIManager.isUiVisible = true;
				}

				Amplitude.Mercury.Presentation.PresentationTerritoryHighlightController HighlightControllerControler = Amplitude.Mercury.Presentation.Presentation.PresentationTerritoryHighlightController;
				HighlightControllerControler.ClearAllTerritoryVisibility();
				int num = HighlightControllerControler.territoryHighlightingInfos.Length;
				for (int i = 0; i < num; i++)
				{
					HighlightControllerControler.SetTerritoryVisibility(i, toggleShowTerritory);
				}
			}
		}
	}


	//*
	[HarmonyPatch(typeof(DepartmentOfScience))]
	public class DepartmentOfScience_Patch
	{
		[HarmonyPatch("EndTurnPass_ClampResearchStock")]
		[HarmonyPrefix]
		public static bool EndTurnPass_ClampResearchStock(DepartmentOfScience __instance, SimulationPasses.PassContext context, string name)
		{
			if (__instance.majorEmpire.DepartmentOfDevelopment.CurrentEraIndex != 0)
			{
				FixedPoint value = __instance.majorEmpire.ResearchNet.Value;
				if (!(value <= 0) && (__instance.TechnologyQueue.CurrentResourceStock < value))
				{
					__instance.TechnologyQueue.CurrentResourceStock = value;
					Amplitude.Mercury.Sandbox.Sandbox.SimulationEntityRepository.SetSynchronizationDirty(__instance.TechnologyQueue);
				}
			}
			return false;
		}
	}
	//*/

	//*
	[HarmonyPatch(typeof(DepartmentOfIndustry))]
	public class DepartmentOfIndustry_Patch
	{
		[HarmonyPatch("InvestProductionFor")]
		[HarmonyPrefix]
		public static bool InvestProductionFor(DepartmentOfIndustry __instance, ConstructionQueue constructionQueue)
		{

			if (CultureUnlock.UseTrueCultureLocation())
			{

				FixedPoint left = DepartmentOfIndustry.ComputeProductionIncome(constructionQueue.Settlement);
				Settlement entity = constructionQueue.Settlement.Entity;
				FixedPoint fixedPoint = left + constructionQueue.CurrentResourceStock;
				constructionQueue.CurrentResourceStock = 0;
				bool flag = false;
				if (constructionQueue.Constructions.Count > 0)
				{
					flag = (constructionQueue.Constructions[0].ConstructibleDefinition.ProductionCostDefinition.Type == ProductionCostType.TurnBased);
				}

				bool flag2 = true;
				int num = 0;
				while ((fixedPoint > 0 || (flag2 && flag)) && num < constructionQueue.Constructions.Count)
				{
					int num2 = num++;
					Construction construction = constructionQueue.Constructions[num2];
					construction.Cost = __instance.GetConstructibleProductionCostForSettlement(entity, construction.ConstructibleDefinition);
					construction.Cost = __instance.ApplyPositionCostModifierIfNecessary(construction.Cost, construction.ConstructibleDefinition, construction.WorldPosition.ToTileIndex());
					constructionQueue.Constructions[num2] = construction;
					bool num3 = construction.FailureFlags != ConstructionFailureFlags.None;
					bool hasBeenBoughtOut = construction.HasBeenBoughtOut;
					bool flag3 = construction.InvestedResource >= construction.Cost;
					if (num3 | hasBeenBoughtOut | flag3)
					{
						continue;
					}

					switch (construction.ConstructibleDefinition.ProductionCostDefinition.Type)
					{
						case ProductionCostType.TurnBased:
							if (!flag2)
							{
								continue;
							}

							fixedPoint = 0;
							++construction.InvestedResource;
							break;
						case ProductionCostType.Infinite:
							fixedPoint = 0;
							break;
						case ProductionCostType.Production:
							{
								FixedPoint fixedPoint2 = construction.Cost - construction.InvestedResource;
								if (fixedPoint > fixedPoint2)
								{
									fixedPoint -= fixedPoint2;
									construction.InvestedResource = construction.Cost;
									__instance.NotifyEndedConstruction(constructionQueue, num2, ref construction);
									Amplitude.Framework.Simulation.SimulationController.RefreshAll();
								}
								else
								{
									construction.InvestedResource += fixedPoint;
									fixedPoint = 0;
								}

								break;
							}
						case ProductionCostType.Transfert:
							{
								FixedPoint productionIncome = fixedPoint * entity.EmpireWideConstructionProductionBoost.Value;
								fixedPoint = __instance.TransfertProduction(construction, productionIncome);
								break;
							}
						default:
							Diagnostics.LogError("Invalid production cost type.");
							break;
					}

					flag2 = false;
					constructionQueue.Constructions[num2] = construction;
				}

				__instance.CleanConstructionQueue(constructionQueue);
				if (entity.SettlementStatus == SettlementStatuses.City)
				{
					constructionQueue.CurrentResourceStock = FixedPoint.Max(left, fixedPoint); // was FixedPoint.Min
				}

				return false; // we've replaced the full method
			}
			return true;
		}
	}
	//*/


	//*
	[HarmonyPatch(typeof(Timeline))]
	public class Timeline_Patch
	{

		//*
		[HarmonyPatch("InitializeOnStart")]
		[HarmonyPostfix]
		public static void InitializeOnStart(Timeline __instance, SandboxStartSettings sandboxStartSettings)
		{
			// reinitialize globalEraThresholds
			int numSettlingEmpires = TrueCultureLocation.GetSettlingEmpireSlots();
			if (CultureUnlock.UseTrueCultureLocation() && numSettlingEmpires < sandboxStartSettings.NumberOfMajorEmpires)
			{
				Diagnostics.LogWarning($"[Gedemon] in Timeline, InitializeOnStart, reset globalEraThresholds for {numSettlingEmpires} Settling Empires / {sandboxStartSettings.NumberOfMajorEmpires} Major Empires");

				__instance.globalEraThresholds[__instance.StartingEraIndex] = __instance.eraDefinitions[__instance.StartingEraIndex].BaseGlobalEraThreshold * numSettlingEmpires;
				for (int l = __instance.StartingEraIndex + 1; l <= __instance.EndingEraIndex; l++)
				{
					__instance.globalEraThresholds[l] = __instance.globalEraThresholds[l - 1] + __instance.eraDefinitions[l].BaseGlobalEraThreshold * numSettlingEmpires;
				}
			}
		}
		//*/

		[HarmonyPatch("GetGlobalEraIndex")]
		[HarmonyPrefix]
		public static bool GetGlobalEraIndex(Timeline __instance, ref int __result)
		{
			int sumEras = 0;
			int numActive = 0;
			int topEra = 0;
			for (int i = 0; i < Amplitude.Mercury.Sandbox.Sandbox.NumberOfMajorEmpires; i++)
			{
				MajorEmpire majorEmpire = Amplitude.Mercury.Sandbox.Sandbox.MajorEmpires[i];
                if(majorEmpire.IsAlive && !CultureChange.IsSleepingEmpire(majorEmpire))
				{
					int empireEra = majorEmpire.DepartmentOfDevelopment.CurrentEraIndex;
					numActive++;
					sumEras += empireEra;
					topEra = empireEra > topEra ? empireEra : topEra;
				}
			}

			if(numActive > 0)
            {
				__result = System.Math.Max( sumEras / numActive, topEra - 1);
				return false;
			}

			__result = __instance.StartingEraIndex;
			return false;
		}
	}
	//*/


	//*
	[HarmonyPatch(typeof(DiplomaticBanner))]
	public class DiplomaticBanner_Patch
	{
		[HarmonyPatch("RefreshItemsPerLine")]
		[HarmonyPrefix]
		public static bool RefreshItemsPerLine(DiplomaticBanner __instance)
		{
			__instance.maxNumberOfItemsPerLine = TrueCultureLocation.GetEmpireIconsNumColumn(); // default = 4 (75% for 9 items)
			return true;
		}
	}
	//*/

	//*
	[HarmonyPatch(typeof(Amplitude.Mercury.UI.Helpers.GameUtils))]
	public class GameUtils_Patch
	{
		[HarmonyPatch("GetTerritoryName")]
		[HarmonyPrefix]
		public static bool GetTerritoryName(Amplitude.Mercury.UI.Helpers.GameUtils __instance, ref string __result, int territoryIndex, EmpireColor useColor = EmpireColor.None)
		{

			if (CultureUnlock.UseTrueCultureLocation())
			{
				//Diagnostics.Log($"[Gedemon] in GameUtils, GetTerritoryName: territoryIndex = {territoryIndex}");
				ref TerritoryInfo reference = ref Snapshots.GameSnapshot.PresentationData.TerritoryInfo.Data[territoryIndex];
				bool flag = useColor != EmpireColor.None;
				if (reference.AdministrativeDistrictGUID != 0)
				{
					//Diagnostics.Log($"[Gedemon] in GameUtils, GetTerritoryName: AdministrativeDistrictGUID = {reference.AdministrativeDistrictGUID}");
					ref SettlementInfo reference2 = ref Snapshots.GameSnapshot.PresentationData.SettlementInfo.Data[reference.SettlementIndex];
					if (reference2.TileIndex == reference.AdministrativeDistrictTileIndex)
					{
						//Diagnostics.Log($"[Gedemon] in GameUtils, GetTerritoryName: reference2.TileIndex = {reference2.TileIndex}, reference.AdministrativeDistrictTileIndex = {reference.AdministrativeDistrictTileIndex}");
						string text = CultureUnlock.TerritoryHasName(territoryIndex) ? CultureUnlock.GetTerritoryName(territoryIndex, hasName: true) : reference2.EntityName.ToString();// reference2.EntityName.ToString();
						if (flag)
						{
							//Color empireColor = __instance.GetEmpireColor(reference.EmpireIndex, useColor);
							//__result = Amplitude.Mercury.Utils.TextUtils.ColorizeText(text, empireColor);
							//return false;
						}

						__result = text;
						return false;
					}
				}

				string text2 = CultureUnlock.TerritoryHasName(territoryIndex) ? CultureUnlock.GetTerritoryName(territoryIndex, hasName: true) : reference.LocalizedName ?? string.Empty;// reference.LocalizedName ?? string.Empty;

				if (flag && reference.Claimed)
				{
					//Color empireColor2 = __instance.GetEmpireColor(reference.EmpireIndex, useColor);
					//__result = __instance.TextUtils.ColorizeText(text2, empireColor2);
					//return false;
				}

				__result = text2;
				return false;

			}
			return true;

		}
	}
	//*/

	//*
	[HarmonyPatch(typeof(AvatarManager))]
	public class AvatarManager_Patch
	{
		[HarmonyPatch("ForceAvatarSummaryTo")]
		[HarmonyPrefix]
		public static bool ForceAvatarSummaryTo(AvatarManager __instance, AvatarId avatarId, ref Amplitude.Mercury.Avatar.AvatarSummary avatarSummary)
		{
			// compatibility fix for January 2022 patch, seems that now slots > 10 don't get a random avatar summary in session initialization
			if (avatarSummary.ElementKeyBySlots == null || avatarSummary.ElementKeyBySlots.Length == 0)
			{
				Diagnostics.LogError($"[Gedemon] [AvatarManager] ForceAvatarSummaryTo: avatarID #{avatarId.Index} has no avatar summary, calling GetRandomAvatarSummary...");
				__instance.GetRandomAvatarSummary(avatarId.Index, ref avatarSummary);
			}

			return true;
		}

		// Compatibility fix for games with more than 10 player slots, as the slots above 10 may not have an Avatarsummary set when TryGetGender is called
		[HarmonyPatch("TryGetGender")]
		[HarmonyPrefix]
		public static bool TryGetGender(AvatarManager __instance, ref bool __result, AvatarSummary avatarSummary, out Gender gender)
		{
			gender = Gender.Male;
			if (avatarSummary.ElementKeyBySlots.Length == 0)
			{
				__result = false;
				return false;
			}
			return true;
		}
	}
	//*/


	//*
	[HarmonyPatch(typeof(StatisticReporter_EndTurn))]
	public class StatisticReporter_EndTurn_Patch
	{

		[HarmonyPrefix]
		[HarmonyPatch(nameof(Load))]
		public static bool Load()
		{
			CultureChange.Load();
			return true;
		}
		
		[HarmonyPrefix]
		[HarmonyPatch(nameof(Unload))]
		public static bool Unload()
		{
			CultureChange.Unload();
			return true;
		}	
	}
	//*/

	[HarmonyPatch(typeof(PresentationDistrict))]
	public class PresentationDistrict_Patch
	{

		[HarmonyPrefix]
		[HarmonyPatch(nameof(UpdateFromDistrictInfo))]
		public static bool UpdateFromDistrictInfo(PresentationDistrict __instance, ref DistrictInfo districtInfo, bool isStartOrEmpireChange, bool isNewDistrict)
		{

			if (!TrueCultureLocation.KeepHistoricalDistricts())
				return true;

			if (districtInfo.DistrictType == DistrictTypes.Exploitation)
            {
				return true;
            }
			if (isNewDistrict || districtInfo.DistrictDefinitionName == DepartmentOfTheInterior.CityCenterDistrictDefinitionName)
			{
				if(CurrentGame.Data.HistoricVisualAffinity.TryGetValue(districtInfo.TileIndex, out DistrictVisual cachedVisualAffinity))
				{
					//Diagnostics.LogError($"[Gedemon] UpdateFromDistrictInfo {districtInfo.DistrictDefinitionName}, isNewDistrict = {isNewDistrict}), TileIndex = {districtInfo.TileIndex}, VisualAffinityName = {districtInfo.VisualAffinityName}, InitialVisualAffinityName = {districtInfo.InitialVisualAffinityName}, cached = ({cachedVisualAffinity.VisualAffinity}) ");

				}
				if (isNewDistrict)
				{
					if (CurrentGame.Data.HistoricVisualAffinity.ContainsKey(districtInfo.TileIndex))
                    {
						//CurrentGame.Data.HistoricVisualAffinity.Remove(districtInfo.TileIndex); // need to clean somewhere else, "isNewDistrict" is true on capture)
					}
					ref TileInfo reference = ref Amplitude.Mercury.Sandbox.Sandbox.World.TileInfo.Data[districtInfo.TileIndex];
					Diagnostics.LogWarning($"[Gedemon] UpdateFromDistrictInfo (new district)  {districtInfo.DistrictDefinitionName}, TileIndex = {districtInfo.TileIndex} ({CultureUnlock.GetTerritoryName(reference.TerritoryIndex)}), VisualAffinityName = {districtInfo.VisualAffinityName}, InitialVisualAffinityName = {districtInfo.InitialVisualAffinityName}");
				}
				return true;
			}
            //Diagnostics.LogError($"[Gedemon] UpdateFromDistrictInfo districtInfo = {districtInfo.VisualAffinityName}, isStartOrEmpireChange = {isStartOrEmpireChange}, isNewDistrict = {isNewDistrict})");
            else
            {

				if (CurrentGame.Data.HistoricVisualAffinity.TryGetValue(districtInfo.TileIndex, out DistrictVisual historicDistrict) && districtInfo.VisualAffinityName != historicDistrict.VisualAffinity)
				{

					ref TileInfo reference = ref Amplitude.Mercury.Sandbox.Sandbox.World.TileInfo.Data[districtInfo.TileIndex];
                    //if(CultureUnlock.GetTerritoryName(reference.TerritoryIndex) == "Graecia")
					{
						//Diagnostics.LogWarning($"[Gedemon] UpdateFromDistrictInfo for {districtInfo.DistrictDefinitionName} at TileIndex #{districtInfo.TileIndex} ({CultureUnlock.GetTerritoryName(reference.TerritoryIndex)}) with different cached visual ({historicDistrict.VisualAffinity}, EraIndex = {historicDistrict.EraIndex}) and info visual ({districtInfo.VisualAffinityName}) (initial = {districtInfo.InitialVisualAffinityName})");
					}
					districtInfo.VisualAffinityName = historicDistrict.VisualAffinity;
					districtInfo.InitialVisualAffinityName = historicDistrict.VisualAffinity;
					districtInfo.EraIndex = historicDistrict.EraIndex;

				}
			}

			return true;
		}
	}

	//*
	[HarmonyPatch(typeof(PresentationTerritoryHighlightController))]
	public class PresentationTerritoryHighlightController_Patch
	{

		[HarmonyPatch("InitTerritoryLabels")]
		[HarmonyPrefix]
		public static bool InitTerritoryLabels(PresentationTerritoryHighlightController __instance)
		{

			if (!TrueCultureLocation.IsDebugCityName())
				return true;

			///
			GameSnapshot.Data presentationData = Snapshots.GameSnapshot.PresentationData;
			int length = presentationData.TerritoryInfo.Length;
			int val = Presentation.WorldMapProvider.MapWidth * Presentation.WorldMapProvider.MapHeight;
			__instance.territoryHighlightingInfos = new TerritoryHighlightingInfo[val];
			__instance.territoryLabelsRenderer.InitializeLabelsSizeIFN(val);
			for (int i = 0; i < val; i++)
			{
				//ref TerritoryInfo reference = ref presentationData.TerritoryInfo.Data[i];
				//int[] tileIndexes = reference.TileIndexes;
				int num = 1;// ((tileIndexes != null) ? tileIndexes.Length : 0);
				if (num != 0)
				{
					if (PresentationTerritoryHighlightController.territoryPlacementCache.Tiles == null)
					{
						PresentationTerritoryHighlightController.territoryPlacementCache.Tiles = new Hexagon.OffsetCoords[num];
					}
					else if (PresentationTerritoryHighlightController.territoryPlacementCache.Tiles.Length < num)
					{
						int newSize = System.Math.Min(num * 2, val);
						Array.Resize(ref PresentationTerritoryHighlightController.territoryPlacementCache.Tiles, newSize);
					}
					for (int j = 0; j < num; j++)
					{
						PresentationTerritoryHighlightController.territoryPlacementCache.Tiles[j] = WorldPosition.GetHexagonOffsetFromTileIndex(i);// reference.TileIndexes[j]);
					}
					__instance.territoryHighlightingInfos[i] = new TerritoryHighlightingInfo
					{
						TerritoryIndex = i,
						IsVisible = false
					};
					PresentationTerritoryHighlightController.territoryPlacementCache.TilesCount = num;
                    if(!CityMap.PositionCity.TryGetValue(i,out string name))
					{

						WorldPosition position = new WorldPosition(i);
						name = "("+position.Column.ToString()+","+position.Row.ToString()+")";
                    }
					PresentationTerritoryHighlightController.territoryPlacementCache.Name = name;//Amplitude.Mercury.UI.Utils.GameUtils.GetTerritoryName(i);
					__instance.territoryLabelsRenderer.InitializeLabel(i, ref PresentationTerritoryHighlightController.territoryPlacementCache, WorldLabelRenderer.MaterialType.Territory);
					__instance.territoryLabelsRenderer.UpdateLabel(ref __instance.territoryHighlightingInfos[i]);
				}

				ref Amplitude.Mercury.Terrain.TerrainLabel[] terrainLabels = ref Amplitude.Mercury.Presentation.Presentation.PresentationTerritoryHighlightController.territoryLabelsRenderer.terrainLabels;

				int numTerritories = Sandbox.World.Territories.Length;//terrainLabels.Length;
				bool alterne = true;
				for (int territoryIndex = 0; territoryIndex < numTerritories; territoryIndex++)
				{
					if (alterne)
					{
						//terrainLabels[territoryIndex].Text = "*" + CultureUnlock.GetTerritoryName(territoryIndex) + "*";
						terrainLabels[territoryIndex].OptionalColor = UnityEngine.Color.cyan;
					}
                    else
					{
						terrainLabels[territoryIndex].OptionalColor = UnityEngine.Color.white;
						//terrainLabels[territoryIndex].Curviness = 0.75f;

					}
					alterne = !alterne;
				}
			}
			///
			return false;
		}

		[HarmonyPatch("InitTerritoryLabels")]
		[HarmonyPostfix]
		public static void InitTerritoryLabelsPost(PresentationTerritoryHighlightController __instance)
		{
			if (TrueCultureLocation.IsDebugCityName())
				return;

			int localEmpireIndex = SandboxManager.Sandbox.LocalEmpireIndex;
			MajorEmpire majorEmpire = Sandbox.MajorEmpires[localEmpireIndex];
			CultureChange.UpdateTerritoryLabels(majorEmpire.DepartmentOfDevelopment.CurrentEraIndex);
		}
	}

	[HarmonyPatch(typeof(WorldLabelRenderer))]
	public class WorldLabelRenderer_Patch
	{

		[HarmonyPatch("ResolveDependencies")]
		[HarmonyPostfix]
		public static void ResolveDependencies(ref WorldLabelRenderer __instance)
		{
			float baseFonSize = 5;
			float smallFontSize = 1.75f;

			if (TrueCultureLocation.IsDebugCityName())
			{
				Diagnostics.LogWarning($"[Gedemon] WorldLabelRenderer in ResolveDependencies: change Territories FontSize from {__instance.resolvedSettings.Materials[(int)WorldLabelRenderer.MaterialType.Territory].FontSize} to {smallFontSize} for Debugging CityMap");
				__instance.resolvedSettings.Materials[(int)WorldLabelRenderer.MaterialType.Territory].FontSize = smallFontSize;
			}
			else if(__instance.resolvedSettings.Materials[(int)WorldLabelRenderer.MaterialType.Territory].FontSize != baseFonSize)
            {
				Diagnostics.LogWarning($"[Gedemon] WorldLabelRenderer in ResolveDependencies: restore Territories FontSize from {__instance.resolvedSettings.Materials[(int)WorldLabelRenderer.MaterialType.Territory].FontSize} to {baseFonSize}");
				__instance.resolvedSettings.Materials[(int)WorldLabelRenderer.MaterialType.Territory].FontSize = baseFonSize;
			}
		}
	}
	//*/

	/*
	[HarmonyPatch(typeof(PresentationTerritoryHighlightController))]
	public class PresentationTerritoryHighlightController_Patch
	{

		[HarmonyPatch("InitTerritoryLabels")]
		[HarmonyPostfix]
		public static void InitTerritoryLabelsPost(PresentationTerritoryHighlightController __instance)
		{
			int localEmpireIndex = SandboxManager.Sandbox.LocalEmpireIndex;
			MajorEmpire majorEmpire = Sandbox.MajorEmpires[localEmpireIndex];
			CultureChange.UpdateTerritoryLabels(majorEmpire.DepartmentOfDevelopment.CurrentEraIndex);
		}
	}
	//*/


	//*
	[HarmonyPatch(typeof(ComputeSpecificMissions))]
	public class ComputeSpecificMissions_Patch
	{
		[HarmonyPatch("ComputeTerritoryClaimScore")]
		[HarmonyPostfix]
		public static void ComputeTerritoryClaimScore(ComputeSpecificMissions __instance, ref HeuristicFloat __result, Amplitude.Mercury.Interop.AI.Entities.MajorEmpire majorEmpire, Amplitude.Mercury.Interop.AI.Entities.Army army, Amplitude.Mercury.AI.Brain.AnalysisData.Territory.TerritoryData territoryData)
		{
			bool hasSettlement = majorEmpire.Settlements.Length > 0;

			float unlockMotivation = hasSettlement ? 5.0f : 15.0f;
			//Diagnostics.LogWarning($"[Gedemon] ComputeTerritoryClaimScore by {majorEmpire.FactionName} (has settlement = {majorEmpire.Settlements.Length > 0}) for ({CultureUnlock.GetTerritoryName(army.TerritoryIndex)}), result = {__result.Value}, Era = {majorEmpire.EraDefinitionIndex}");
			if (CultureUnlock.IsNextEraUnlock(army.TerritoryIndex, majorEmpire.EraDefinitionIndex))
			{
				__result.Add(unlockMotivation);
				//Diagnostics.LogWarning($"[Gedemon] IsNextEraUnlock = true - New result = {__result.Value}");
			}
			else
			{
				if (!hasSettlement)
					__result.Divide(2.0f);

				//Diagnostics.LogWarning($"[Gedemon] IsNextEraUnlock = false - New result = {__result.Value}");
			}
		}

		/*
		[HarmonyPatch("ComputeTerritoryClaimScore")]
		[HarmonyPostfix]
		public static void ComputeTerritoryToClaim(ComputeSpecificMissions __instance, ref HeuristicValue<int> __result, Amplitude.Mercury.AI.Brain.MajorEmpireBrain brain, Army army, bool allowRansack)
		{
			
		}
		//*/


	}
	//*/

	[HarmonyPatch(typeof(LobbyScreen_LobbySlotsPanel))]
	public class LobbyScreen_LobbySlotsPanel_Patch
	{

		[HarmonyPrefix]
		[HarmonyPatch(nameof(Refresh))]
		public static bool Refresh(LobbyScreen_LobbySlotsPanel __instance)
		{
			if (__instance.session == null)
			{
				return false;
			}
			for (int i = 0; i < 10; i++)
			{
				__instance.allLobbySlots[i].Unbind();
			}
			int count = System.Math.Min(10, __instance.session.Slots.Count);
			for (int j = 0; j < count; j++)
			{
				LobbySlot lobbySlot = __instance.allLobbySlots[j];
				lobbySlot.Bind(__instance.session.Slots[j], __instance.session, __instance.lobbyScreen, __instance);
				lobbySlot.Show();
				if (__instance.lobbySlotSettingsPanel.IsAttachedTo(lobbySlot))
				{
					__instance.lobbySlotSettingsPanel.Dirtyfy();
				}
			}
			for (int k = count; k < 10; k++)
			{
				__instance.allLobbySlots[k].Hide();
			}
			__instance.addSlotButton.UITransform.VisibleSelf = __instance.session.IsHosting && count < __instance.lobbyScreen.AllowedMaxLobbySlots && !__instance.lobbyScreen.IsMultiplayerSave;

			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(nameof(UpdateSlot))]
		public static bool UpdateSlot(LobbyScreen_LobbySlotsPanel __instance, SessionSlot sessionSlot)
		{
			if (sessionSlot.Index >= 10)
			{
				return false;
			}
			return true;
		}
	}

	/*
	[HarmonyPatch(typeof(EliminationController))]
	public class EliminationController_Patch
	{
		[HarmonyPatch("Eliminate")]
		[HarmonyPrefix]
		public static bool Eliminate(EliminationController __instance, MajorEmpire majorEmpire)
		{

			if (CultureUnlock.UseTrueCultureLocation())
			{
				//majorEmpire.OnFreeing();
				//majorEmpire.InitializeOnStart(SandboxStartSettings);
				//return false;
            }
			return true;
		}
	}
	//*/

	/*
	[HarmonyPatch(typeof(PresentationPawn))]
	public class CultureUnlock_PresentationPawn
	{

		[HarmonyPrefix]
		[HarmonyPatch(nameof(Initialize))]
		public static bool Initialize(PresentationPawn __instance)
		{
			Diagnostics.LogWarning($"[Gedemon] in PresentationPawn, Initialize for {__instance.name}");
			Diagnostics.Log($"[Gedemon] Transform localScale =  {__instance.Transform.localScale}, gameObject.name =  {__instance.Transform.gameObject.name}, lossyScale =  {__instance.Transform.lossyScale}, childCount =  {__instance.Transform.childCount}");

			__instance.Transform.localScale = new Vector3(0.5f, 0.5f, 0.5f );
			return true;
		}		
	}
	//*/


}
