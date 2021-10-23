using BepInEx;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amplitude.Mercury.Simulation;
using HarmonyLib;
using Amplitude.Mercury.Sandbox;
using Amplitude.Framework;
using Amplitude.Mercury.UI;
using UnityEngine;
using HumankindModTool;
using Amplitude.Mercury.Presentation;
using Amplitude;
using Amplitude.Mercury.Data;
using Amplitude.Mercury.Terrain;
using Amplitude.Mercury.Game;
using Amplitude.Framework.Session;
using Amplitude.Mercury.Session;
using Amplitude.Mercury;
using System.Reflection;
using Amplitude.Mercury.Data.Simulation;

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
			Title = "True Culture Location",
			Description = "Toggles unlocking Culture by owned Territories on compatible maps",
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
			Description = "Add extra Empire Slots in game (AI only), allowing a maximum of 16 Empires (10 from setup + 6 extra AI slots). With TCL, Nomadic Tribes controlled by those AI players will be able to spawn as a new Empire on the location of a Culture that has not been controlled yet, or take control of some territories of an old Empire during a split. (this setting override the default Starting Positions when using the Giant Earth Map)",
			GroupKey = "GameOptionGroup_LobbyDifficultyOptions",
			States = NumEmpireSlots
		};

		public static GameOptionInfo SettlingEmpireSlotsOption = new GameOptionInfo
		{
			ControlType = UIControlType.DropList,
			Key = "GameOption_TCL_SettlingEmpireSlotsOption",
			DefaultValue = "10",
			Title = "[TCL] number of slot IDs that start in Neolithic",
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
			Description = "Toggle if Empires will start with an Outpost",
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

		public static readonly GameOptionInfo StartPositionList = new GameOptionInfo
		{
			ControlType = UIControlType.DropList,
			Key = "GameOption_TCL_StartPositionList",
			GroupKey = "GameOptionGroup_LobbyDifficultyOptions",
			DefaultValue = "Default",
			Title = "Starting Position List",
			Description = "Choose if you want to use the map's default Starting Positions, or another list",
			States =
			{
				new GameOptionStateInfo
				{
					Title = "Map Default",
					Description = "Use the Map's default Starting Positions, and the alternate positions only for extra player slots.",
					Value = "Default"
				},
				new GameOptionStateInfo
				{
					Title = "Alternate",
					Description = "Use only the Alternate Starting Positions (Old World and Americas), ignoring the map's default positions. Some starting positions will be adjacent from each other, even with a low number of players",
					Value = "ExtraStart"
				},
				new GameOptionStateInfo
				{
					Title = "Old World",
					Description = "Use only the Alternate starting positions (Old World List), ignoring the map's default positions. Some starting positions will be adjacent from each other, even with a low number of players",
					Value = "OldWorld"
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
		
		public bool Enabled => GameOptionHelper.CheckGameOption(UseTrueCultureLocation, "True");
		public bool OnlyCultureTerritory => !GameOptionHelper.CheckGameOption(TerritoryLossOption, "TerritoryLoss_None");
		public bool KeepAttached => GameOptionHelper.CheckGameOption(TerritoryLossOption, "TerritoryLoss_KeepAttached");
		public bool NoLossForAI => GameOptionHelper.CheckGameOption(TerritoryLossIgnoreAI, "True");
		public bool LimitDecisionForAI => GameOptionHelper.CheckGameOption(TerritoryLossLimitDecisionForAI, "True");
		public bool OnlyOldWorldStart => GameOptionHelper.CheckGameOption(StartPositionList, "OldWorld");
		public bool OnlyExtraStart => GameOptionHelper.CheckGameOption(StartPositionList, "ExtraStart");
		public bool StartingOutpostForAI => !GameOptionHelper.CheckGameOption(StartingOutpost, "Off");
		public bool StartingOutpostForHuman => GameOptionHelper.CheckGameOption(StartingOutpost, "On");
		public bool StartingOutpostForMinorFaction => GameOptionHelper.CheckGameOption(StartingOutpostForMinorOption, "True");
		public bool LargerSpawnAreaForMinorFaction => GameOptionHelper.CheckGameOption(LargerSpawnAreaForMinorOption, "True");

		public int EraIndexCityRequiredForUnlock => int.Parse(GameOptionHelper.GetGameOption(FirstEraRequiringCityToUnlock));
		public int TotalEmpireSlots => int.Parse(GameOptionHelper.GetGameOption(ExtraEmpireSlots));
		public int SettlingEmpireSlots => int.Parse(GameOptionHelper.GetGameOption(SettlingEmpireSlotsOption));

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


}
