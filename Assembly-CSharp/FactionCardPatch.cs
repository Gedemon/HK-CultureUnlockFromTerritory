using System;
using System.Collections.Generic;
using Amplitude;
using HarmonyLib;
using UnityEngine;
using Amplitude.Mercury.Interop;
using Amplitude.Mercury.UI;
using Amplitude.Mercury.Simulation;

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

			if (CultureUnlock.UseTrueCultureLocation())
			{
				int localEmpireIndex = Amplitude.Mercury.Sandbox.SandboxManager.Sandbox.LocalEmpireIndex;
				MajorEmpire majorEmpire = Amplitude.Mercury.Sandbox.Sandbox.MajorEmpires[localEmpireIndex];

				int lines = 0;
				if (CultureUnlock.HasMajorTerritories(factionName) && flag)
				{

					__instance.lockedPatch.VisibleSelf = (status & FactionStatus.LockedByOthers) == 0;

					if (CultureUnlock.HasNoCapitalTerritory(factionName))
					{
						string territoriesList = "Must own one of those Territories : ";
						List<int> territoryIndexes = CultureUnlock.GetListTerritories(factionName);
						lines = territoryIndexes.Count;
						foreach (int territoryIndex in territoryIndexes)
						{
							territoriesList += Environment.NewLine + CultureUnlock.GetTerritoryName(territoryIndex);
						}
						__instance.lockedLabel.Text = territoriesList;

					}
					else
					{
						int territoryIndex = CultureUnlock.GetCapitalTerritoryIndex(factionName);
						__instance.lockedLabel.Text = "Must own Capital Territory : " + Environment.NewLine + CultureUnlock.GetTerritoryName(territoryIndex);

					}

					if (TrueCultureLocation.GetEraIndexCityRequiredForUnlock() < __instance.FactionDefinition.EraIndex + 1 && !CultureUnlock.IsNomadCulture(majorEmpire.FactionDefinition.name))
						__instance.lockedLabel.Text += Environment.NewLine + "(City or Attached to a City)";
					else
						__instance.lockedLabel.Text += Environment.NewLine + "(an Outpost is enough)";
				}
				else //if(__instance.isInConfirmationState)
                {
					__instance.lockedLabel.Text = "";


					TerritoryChange territoryChange = new TerritoryChange(majorEmpire, __instance.NextFactionInfo.FactionDefinitionName);

					string city = territoryChange.settlementLost.Count > 1 ? "Cities" : "City";

					if (territoryChange.settlementLost.Count > 0)
					{
						lines++;
						string listCity = city + " lost : ";
						foreach (Settlement settlement in territoryChange.settlementLost)
						{
							listCity += Environment.NewLine + settlement.EntityName;
							lines++;
						}
						__instance.lockedLabel.Text += listCity + Environment.NewLine;
						lines++;
					}


					if (territoryChange.territoriesLost.Count > 0)
					{
						lines++;
						string territoryLost = territoryChange.territoriesLost.Count > 1 ? "Territories lost : " : "Territory lost : ";
						foreach (int territoryIndex in territoryChange.territoriesLost)
						{
							territoryLost += Environment.NewLine + CultureUnlock.GetTerritoryName(territoryIndex);
							lines++;
						}
						__instance.lockedLabel.Text += Environment.NewLine + territoryLost + Environment.NewLine;
						lines++;
					}

					if (territoryChange.territoriesKeptFromLostCities.Count > 0)
					{
						lines++;
						string territoryDetached = territoryChange.territoriesKeptFromLostCities.Count > 1 ? "Territories kept from lost " + city + " : " : "Territory kept from lost " + city + " : ";
						foreach (int territoryIndex in territoryChange.territoriesKeptFromLostCities)
						{
							territoryDetached += Environment.NewLine + CultureUnlock.GetTerritoryName(territoryIndex);
							lines++;
						}
						__instance.lockedLabel.Text += Environment.NewLine + territoryDetached;
					}

					if (__instance.lockedLabel.Text != "")
					{
						__instance.lockedOverlay.VisibleSelf = true; // to show the text
						__instance.lockedOverlayImage.Color = new Color(0f, 0f, 0f, 0.20f);
						__instance.lockedByPatch.VisibleSelf = false;
					}
				}

				__instance.lockedLabel.SetVerticalAlignment(Amplitude.UI.VerticalAlignment.Top);
				if (lines > 8)
				{
					int resize = lines < 14 ? lines : 14;
					__instance.lockedLabel.FontSize = (uint)(baseFontSize + 8 - resize);
				}
			}
		}
	}
}
