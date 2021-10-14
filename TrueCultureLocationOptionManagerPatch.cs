using Amplitude.Framework.Options;
using Amplitude.Mercury.Data.GameOptions;
using HarmonyLib;
using HumankindModTool;

namespace Gedemon.TrueCultureLocation
{
	[HarmonyPatch(typeof(OptionsManager<GameOptionDefinition>))]
	public class AllowDuplicateCultures_GameOptions
	{
		[HarmonyPatch("Load")]
		[HarmonyPrefix]
		public static bool Load(OptionsManager<GameOptionDefinition> __instance)
		{
			GameOptionHelper.Initialize(TrueCultureLocation.UseTrueCultureLocation, TrueCultureLocation.FirstEraRequiringCityToUnlock, TrueCultureLocation.TerritoryLossOption, TrueCultureLocation.TerritoryLossIgnoreAI, TrueCultureLocation.TerritoryLossLimitDecisionForAI);
			return true;
		}
	}
}