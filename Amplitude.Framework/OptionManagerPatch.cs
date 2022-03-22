using Amplitude.Framework.Options;
using Amplitude.Mercury.Data.GameOptions;
using HarmonyLib;
using HumankindModTool;

namespace Gedemon.TrueCultureLocation
{
	[HarmonyPatch(typeof(OptionsManager<GameOptionDefinition>))]
	public class TCL_GameOptions
	{
		[HarmonyPatch("Load")]
		[HarmonyPrefix]
		public static bool Load(OptionsManager<GameOptionDefinition> __instance)
		{
			var myLogSource = BepInEx.Logging.Logger.CreateLogSource("True Culture Location");
			myLogSource.LogInfo("Initializing GameOptionDefinition...");
			GameOptionHelper.Initialize(TrueCultureLocation.EmpireIconsNumColumnOption, TrueCultureLocation.HistoricalDistrictsOption, TrueCultureLocation.ExtraEmpireSlots, TrueCultureLocation.StartingOutpost, TrueCultureLocation.CityMapOption, TrueCultureLocation.StartPositionList, TrueCultureLocation.UseTrueCultureLocation, TrueCultureLocation.CreateTrueCultureLocationOption, TrueCultureLocation.SettlingEmpireSlotsOption, TrueCultureLocation.FirstEraRequiringCityToUnlock, TrueCultureLocation.StartingOutpostForMinorOption, TrueCultureLocation.LargerSpawnAreaForMinorOption, TrueCultureLocation.TerritoryLossOption, TrueCultureLocation.NewEmpireSpawningOption, TrueCultureLocation.RespawnDeadPlayersOption, TrueCultureLocation.EliminateLastEmpiresOption, TrueCultureLocation.CompensationLevel, TrueCultureLocation.TerritoryLossIgnoreAI, TrueCultureLocation.TerritoryLossLimitDecisionForAI, TrueCultureLocation.DebugCityNameOption);
			myLogSource.LogInfo("GameOptionDefinition Initialized");
			BepInEx.Logging.Logger.Sources.Remove(myLogSource);
			return true;
		}
	}
}