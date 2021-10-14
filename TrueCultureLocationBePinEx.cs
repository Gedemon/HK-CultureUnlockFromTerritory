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

namespace Gedemon.TrueCultureLocation
{
	[BepInPlugin(pluginGuid, "True Culture Location", "1.0.0.0")]
	public class TrueCultureLocation : BaseUnityPlugin
	{
		public const string pluginGuid = "gedemon.humankind.trueculturelocation";

		//private static ConfigEntry<bool> keepOnlyCultureTerritory;
		//private static ConfigEntry<bool> keepTerritoryAttached;
		//private static ConfigEntry<bool> noTerritoryLossForAI;
		//private static ConfigEntry<int> eraIndexCityRequiredForUnlock;
		//private static ConfigEntry<bool> limitDecisionForAI;

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

		private static List<GameOptionStateInfo> ErasCityRequired = new List<GameOptionStateInfo>
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

		public bool Enabled => GameOptionHelper.CheckGameOption(UseTrueCultureLocation, "True");
		public bool OnlyCultureTerritory => !GameOptionHelper.CheckGameOption(TerritoryLossOption, "TerritoryLoss_None");
		public bool KeepAttached => GameOptionHelper.CheckGameOption(TerritoryLossOption, "TerritoryLoss_KeepAttached");
		public bool NoLossForAI => GameOptionHelper.CheckGameOption(TerritoryLossIgnoreAI, "True");
		public bool LimitDecisionForAI => GameOptionHelper.CheckGameOption(TerritoryLossLimitDecisionForAI, "True");

		public int EraIndexCityRequiredForUnlock => int.Parse(GameOptionHelper.GetGameOption(FirstEraRequiringCityToUnlock));

		// Awake is called once when both the game and the plug-in are loaded
		void Awake()
		{
			//UnityEngine.Debug.Log("Starting initialization !");
			/*
			keepOnlyCultureTerritory = Config.Bind("General",
									"KeepOnlyCultureTerritory",
									false,
									"Toggle to set if Empires will keep only the territories of a new Culture and liberate the other Territories");

			keepTerritoryAttached = Config.Bind("General",
									"KeepTerritoryAttached",
									false,
									"Toggle to set if Territories that are attached to a Settlement that has at least one territory belonging to the new Culture will not be detached and kept in the Empire, even when KeepOnlyCultureTerritory is active");

			noTerritoryLossForAI = Config.Bind("General",
									"NoTerritoryLossForAI",
									true,
									"Toggle to set if the AI ignore the other settings and always keep its full territory");

			eraIndexCityRequiredForUnlock = Config.Bind("General",
									"EraIndexCityRequiredForUnlock",
									3,
									"Minimal Era Index from whitch a City (or an Administrative Center attached to a City) is required on a Culture's Capital territory to unlock it (3 = Medieval Era)");

			limitDecisionForAI = Config.Bind("General",
									"LimitDecisionForAI",
									false,
									"Toggle to limit AI Empires to Culture choices that doesn't result in a big territory loss");
			//*/

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
		public static bool UseLimitDecisionForAI()
		{
			return Instance.LimitDecisionForAI;
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
}
