using BepInEx;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amplitude.Mercury.Simulation;
using Amplitude;
using HarmonyLib;
using System.Reflection;
using Amplitude.Mercury.Options;
using Amplitude.Mercury.Interop;
using Amplitude.Mercury.Data.Simulation;
using Amplitude.Mercury.Sandbox;
using FailureFlags = Amplitude.Mercury.Simulation.FailureFlags;
using Amplitude.Framework;
using Amplitude.Mercury.UI;
using Amplitude.Mercury.UI.Helpers;
using Amplitude.Mercury.WorldGenerator;
using UnityEngine;

namespace Gedemon.CultureUnlock
{
	[BepInPlugin(pluginGuid, "Culture Unlock From Territories", "1.0.0.4")]
	public class CultureUnlockFromTerritories : BaseUnityPlugin
	{
		public const string pluginGuid = "gedemon.humankind.cultureunlockfromterritories";

		private static ConfigEntry<bool> keepOnlyCultureTerritory;
		private static ConfigEntry<bool> keepTerritoryAttached;
		private static ConfigEntry<bool> noTerritoryLossForAI;
		private static ConfigEntry<int> eraIndexCityRequiredForUnlock;
		private static ConfigEntry<bool> limitDecisionForAI;

		// Awake is called once when both the game and the plug-in are loaded
		void Awake()
		{
			//UnityEngine.Debug.Log("Starting initialization !");

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

			Harmony harmony = new Harmony(pluginGuid);
			Instance = this;
			harmony.PatchAll();
		}
		public static CultureUnlockFromTerritories Instance;

		public static bool KeepOnlyCultureTerritory()
		{
			return keepOnlyCultureTerritory.Value;
		}
		public static bool KeepTerritoryAttached()
		{
			return keepTerritoryAttached.Value;
		}
		public static bool NoTerritoryLossForAI()
		{
			return noTerritoryLossForAI.Value;
		}
		public static int EraIndexCityRequiredForUnlock()
		{
			return eraIndexCityRequiredForUnlock.Value;
		}
		public static bool LimitDecisionForAI()
		{
			return limitDecisionForAI.Value;
		}

		public bool toggleShow = false;
		private void Update()
		{
			if (Input.GetKeyDown((KeyCode)284))
			{
				toggleShow = !toggleShow;
				int localEmpireIndex = SandboxManager.Sandbox.LocalEmpireIndex;

				if (toggleShow && Amplitude.Mercury.Presentation.Presentation.PresentationCursorController.CurrentCursor is Amplitude.Mercury.Presentation.DefaultCursor)
					Amplitude.Mercury.Presentation.Presentation.PresentationCursorController.ChangeToDiplomaticCursor(localEmpireIndex);
				else if (!toggleShow)
					Amplitude.Mercury.Presentation.Presentation.PresentationCursorController.ChangeToDefaultCursor(resetUnitDefinition: false);

				Amplitude.Mercury.Presentation.PresentationTerritoryHighlightController HighlightControllerControler = Amplitude.Mercury.Presentation.Presentation.PresentationTerritoryHighlightController;

				HighlightControllerControler.ClearAllTerritoryVisibility();

				int num = HighlightControllerControler.territoryHighlightingInfos.Length;
				for (int i = 0; i < num; i++)
				{
					HighlightControllerControler.SetTerritoryVisibility(i, toggleShow);
				}
			}
		}
	}
}
