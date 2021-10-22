using System;
using System.Collections.Generic;
using Amplitude;
using HarmonyLib;
using Amplitude.Mercury.Interop;
using Amplitude.Mercury.UI;

namespace Gedemon.TrueCultureLocation
{

	[HarmonyPatch(typeof(FactionCard))]
	public class TCL_FactionCard
	{

		[HarmonyPostfix]
		[HarmonyPatch(nameof(InternalRefresh))]
		public static void InternalRefresh(FactionCard __instance, bool instant)
		{
			FactionStatus status = __instance.NextFactionInfo.Status;

			int baseFontSize = 14;

			string factionName = __instance.NextFactionInfo.FactionDefinitionName.ToString();
			Diagnostics.LogWarning($"[Gedemon] in InternalRefresh, {factionName}");

			bool flag = (status & FactionStatus.LockedByEmpireMiscFlags) != 0;

			if (CultureUnlock.UseTrueCultureLocation() && CultureUnlock.HasTerritory(factionName) && flag)
			{
				if (CultureUnlock.HasNoCapitalTerritory(factionName))
				{

					string territoriesList = "Must own one of those Territories : ";
					List<int> territoryIndexes = CultureUnlock.GetListTerritories(factionName);
					foreach (int territoryIndex in territoryIndexes)
					{
						territoriesList += Environment.NewLine + Utils.GameUtils.GetTerritoryName(territoryIndex);
					}
					__instance.lockedLabel.Text = territoriesList;
					if (territoryIndexes.Count > 4)
						__instance.lockedLabel.FontSize = (uint)(baseFontSize + 3 - territoryIndexes.Count);

				}
				else
				{
					int territoryIndex = CultureUnlock.GetCapitalTerritoryIndex(factionName);
					__instance.lockedLabel.Text = "Must own Capital Territory : " + Environment.NewLine + Utils.GameUtils.GetTerritoryName(territoryIndex);

				}

				if (TrueCultureLocation.GetEraIndexCityRequiredForUnlock() < __instance.FactionDefinition.EraIndex + 1)
					__instance.lockedLabel.Text += Environment.NewLine + "(City or Attached to a City)";
				else
					__instance.lockedLabel.Text += Environment.NewLine + "(an Outpost is enough)";
			}
		}
	}
}
