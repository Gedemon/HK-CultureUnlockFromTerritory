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
			GameOptionHelper.Initialize(TrueCultureLocation.EmpireIconsNumColumnOption, TrueCultureLocation.ExtraEmpireSlots, TrueCultureLocation.StartingOutpost, TrueCultureLocation.StartPositionList, TrueCultureLocation.UseTrueCultureLocation, TrueCultureLocation.SettlingEmpireSlotsOption, TrueCultureLocation.FirstEraRequiringCityToUnlock, TrueCultureLocation.StartingOutpostForMinorOption, TrueCultureLocation.LargerSpawnAreaForMinorOption, TrueCultureLocation.TerritoryLossOption, TrueCultureLocation.NewEmpireSpawningOption, TrueCultureLocation.RespawnDeadPlayersOption, TrueCultureLocation.CompensationLevel, TrueCultureLocation.TerritoryLossIgnoreAI, TrueCultureLocation.TerritoryLossLimitDecisionForAI);
			return true;
		}
	}
}