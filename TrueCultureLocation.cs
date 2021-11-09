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

namespace Gedemon.TrueCultureLocation
{
	[BepInPlugin(pluginGuid, "True Culture Location", "1.0.1.0")]
	public class TrueCultureLocation : BaseUnityPlugin
	{
		public const string pluginGuid = "gedemon.humankind.trueculturelocation";

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

		public static GameOptionStateInfo TerritoryLoss_Full = new GameOptionStateInfo
		{
			Value = "TerritoryLoss_Full",
			Title = "Full",
			Description = "Lose all territories that were not controlled by the new Culture"
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
			Description = "Territories that are attached to a Settlement that has at least one territory belonging to the new Culture will not be detached and kept in the Empire, the other territories will be lost."
		};

		/*
		public static GameOptionStateInfo TerritoryLoss_ByStability = new GameOptionStateInfo
		{
			Value = "TerritoryLoss_ByStability",
			Title = "By Stability",
			Description = "territories that were not controlled by the new Culture are kept only if they have a high Stability"
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
			States = { TerritoryLoss_Full, TerritoryLoss_None, TerritoryLoss_KeepAttached }//, TerritoryLoss_ByStability }
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
			//*
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
			Description = "Add extra Empire Slots in game (AI only), allowing a maximum of 16 Empires (10 from setup + 6 extra AI slots). This can be used either on the Giant Earth Map or any random map, but no other custom map. With [TCL], Nomadic Tribes controlled by those AI players will be able to spawn as a new Empire on the location of a Culture that has not been controlled yet, or take control of some territories of an old Empire during a split. (this setting override the default Starting Positions when using the Giant Earth Map). If you don't use [TCL], you should use (AOM) 'Allow Duplicate Cultures' Mod",
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
			Title = "Start with an Outpost",
			Description = "Toggle if Empires will start with an Outpost. This setting can be used on any map.",
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
			Title = "[GEM] Starting Position List",
			Description = "When you use the Giant Earth Map, choose if you want the map's default Starting Positions or one of the alternate list (only active when that map is used)",
			States =
			{
				new GameOptionStateInfo
				{
					Title = "Map Default",
					Description = "Use the Map's default Starting Positions (this setting is overriden if the maximum number of competitor is raised above 10, the Alternate list is then used)",
					Value = "Default"
				},
				new GameOptionStateInfo
				{
					Title = "Alternate",
					Description = "Use only the Alternate Starting Positions, ignoring the map's default positions. Some starting positions will be adjacent from each other, even with a low number of players. This setting allows the 10 Ancient Cultures to spawn with the [TCL] option (Slots 1 to 9 + 13 to 16 = Old World, 10 to 12 = Americas)",
					Value = "ExtraStart"
				},
				new GameOptionStateInfo
				{
					Title = "Old World",
					Description = "Use only the Alternate starting positions (16 slots max, Old World starts only), ignoring the map's default positions. Some starting positions will be adjacent from each other, even with a low number of players.",
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

		public bool Enabled => GameOptionHelper.CheckGameOption(UseTrueCultureLocation, "True");
		public bool OnlyCultureTerritory => !GameOptionHelper.CheckGameOption(TerritoryLossOption, "TerritoryLoss_None");
		public bool RespawnDeadPlayer => GameOptionHelper.CheckGameOption(RespawnDeadPlayersOption, "True");
		public bool KeepAttached => GameOptionHelper.CheckGameOption(TerritoryLossOption, "TerritoryLoss_KeepAttached");
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
		public int EraIndexCityRequiredForUnlock => int.Parse(GameOptionHelper.GetGameOption(FirstEraRequiringCityToUnlock));
		public int TotalEmpireSlots => int.Parse(GameOptionHelper.GetGameOption(ExtraEmpireSlots));
		public int SettlingEmpireSlots => int.Parse(GameOptionHelper.GetGameOption(SettlingEmpireSlotsOption));
		public int CompensationLevelValue => int.Parse(GameOptionHelper.GetGameOption(CompensationLevel));
		public int EmpireIconsNumColumn => int.Parse(GameOptionHelper.GetGameOption(EmpireIconsNumColumnOption));

		// Awake is called once when both the game and the plug-in are loaded
		void Awake()
		{
			Harmony harmony = new Harmony(pluginGuid);
			Instance = this;
			harmony.PatchAll();
		}
		public static TrueCultureLocation Instance;


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

		public bool toggleShowTerritory = false;
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
					UIManager.IsUiVisible = false;

					// switch to DiplomaticCursor, where territories can be highlighted
					Amplitude.Mercury.Presentation.Presentation.PresentationCursorController.ChangeToDiplomaticCursor(localEmpireIndex);
				}
				else
				{
					// restore UI
					UIManager.IsUiVisible = true;
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


	/*
	[HarmonyPatch(typeof(Amplitude.Mercury.Sandbox.Sandbox))]
	public class Sandbox_Patch
	{
		[HarmonyPatch("ThreadStart")]
		[HarmonyPrefix]
		public static bool ThreadStart(Amplitude.Mercury.Sandbox.Sandbox __instance, object parameter)
		{

		}
	}
	//*/


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
				//Diagnostics.Log($"[Gedemon] in GameUtils, GetTerritoryName");
				ref TerritoryInfo reference = ref Snapshots.GameSnapshot.PresentationData.TerritoryInfo.Data[territoryIndex];
				bool flag = useColor != EmpireColor.None;
				if (reference.AdministrativeDistrictGUID != 0)
				{
					ref SettlementInfo reference2 = ref Snapshots.GameSnapshot.PresentationData.SettlementInfo.Data[reference.SettlementIndex];
					if (reference2.TileIndex == reference.AdministrativeDistrictTileIndex)
					{
						string text = CultureUnlock.GetTerritoryName(territoryIndex);// reference2.EntityName.ToString();
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

				string text2 = CultureUnlock.GetTerritoryName(territoryIndex);// reference.LocalizedName ?? string.Empty;
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

}
