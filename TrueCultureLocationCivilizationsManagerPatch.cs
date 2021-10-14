
using Amplitude.Mercury.Simulation;
using Amplitude;
using HarmonyLib;

namespace Gedemon.TrueCultureLocation
{

	[HarmonyPatch(typeof(CivilizationsManager))]
	public class CultureUnlockCivilizationsManager
	{
		[HarmonyPatch(nameof(IsLockedBy))]
		[HarmonyPrefix]
		public static bool IsLockedBy(CivilizationsManager __instance, ref int __result, StaticString factionName)
		{
			if (CultureUnlock.UseTrueCultureLocation() && CultureUnlock.HasNoCapitalTerritory(factionName.ToString()))
			{
				__result = -1;
				return false;
			}
			else
			{
				return true;
			}
		}

		[HarmonyPatch(nameof(LockFaction))]
		[HarmonyPrefix]
		public static bool LockFaction(CivilizationsManager __instance, StaticString factionName, int lockingEmpireIndex)
		{
			if (CultureUnlock.UseTrueCultureLocation() && CultureUnlock.HasNoCapitalTerritory(factionName.ToString()))
			{
				return false;
			}
			else
			{
				return true;
			}
		}
	}

}
