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
				int lines = 0;
				if (CultureUnlock.HasTerritory(factionName) && flag)
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

					if (TrueCultureLocation.GetEraIndexCityRequiredForUnlock() < __instance.FactionDefinition.EraIndex + 1)
						__instance.lockedLabel.Text += Environment.NewLine + "(City or Attached to a City)";
					else
						__instance.lockedLabel.Text += Environment.NewLine + "(an Outpost is enough)";
				}
				else //if(__instance.isInConfirmationState)
                {
					__instance.lockedLabel.Text = "";

					IDictionary<Settlement, List<int>> territoryToDetachAndCreate = new Dictionary<Settlement, List<int>>();    // Territories attached to a City that we want to detach and keep separated because the city is going to be lost
					IDictionary<Settlement, List<int>> territoryToDetachAndFree = new Dictionary<Settlement, List<int>>();      // Territories attached to a City that we want to detach because they are going to be lost, but not the city
					List<Settlement> settlementToLiberate = new List<Settlement>();                                             // Full Settlements (including a City or being a single-territory City) that are lost
					List<Settlement> settlementToFree = new List<Settlement>();                                                 // Single Settlement (not a city) that are lost

					int localEmpireIndex = Amplitude.Mercury.Sandbox.SandboxManager.Sandbox.LocalEmpireIndex;
					MajorEmpire majorEmpire = Amplitude.Mercury.Sandbox.Sandbox.MajorEmpires[localEmpireIndex];

					District potentialCapital;// = capital;

					bool capitalChanged = CultureChange.GetTerritoryChangesOnEvolve(majorEmpire.DepartmentOfDevelopment, __instance.NextFactionInfo.FactionDefinitionName, out potentialCapital, ref territoryToDetachAndCreate, ref territoryToDetachAndFree, ref settlementToLiberate, ref settlementToFree);

					string city = settlementToLiberate.Count > 1 ? "Cities" : "City";

					List<int> listTerritoryLost = new List<int>();
					foreach (KeyValuePair<Settlement, List<int>> kvp in territoryToDetachAndFree)
					{
						foreach (int territoryIndex in kvp.Value)
                        {
							listTerritoryLost.Add(territoryIndex);
						}
					}
					foreach (Settlement settlement in settlementToFree)
					{
						listTerritoryLost.Add(settlement.Region.Entity.Territories[0].Index);
					}

					List<int> listTerritoryDetached = new List<int>();
					foreach (KeyValuePair<Settlement, List<int>> kvp in territoryToDetachAndCreate)
					{
						foreach (int territoryIndex in kvp.Value)
						{
							listTerritoryDetached.Add(territoryIndex);
						}
					}

					if (settlementToLiberate.Count > 0)
					{
						lines++;
						string listCity = city + " lost : ";
						foreach (Settlement settlement in settlementToLiberate)
						{
							listCity += Environment.NewLine + settlement.EntityName;
							lines++;
						}
						__instance.lockedLabel.Text += listCity + Environment.NewLine;
						lines++;
					}


					if (listTerritoryLost.Count > 0)
					{
						lines++;
						string territoryLost = listTerritoryLost.Count > 1 ? "Territories lost : " : "Territory lost : ";
						foreach (int territoryIndex in listTerritoryLost)
						{
							territoryLost += Environment.NewLine + CultureUnlock.GetTerritoryName(territoryIndex);
							lines++;
						}
						__instance.lockedLabel.Text += Environment.NewLine + territoryLost + Environment.NewLine;
						lines++;
					}

					if (listTerritoryDetached.Count > 0)
					{
						lines++;
						string territoryDetached = listTerritoryDetached.Count > 1 ? "Territories kept from lost "+ city +" : " : "Territory kept from lost " + city + " : ";
						foreach (int territoryIndex in listTerritoryDetached)
						{
							territoryDetached += Environment.NewLine + CultureUnlock.GetTerritoryName(territoryIndex);
							lines++;
						}
						__instance.lockedLabel.Text += Environment.NewLine + territoryDetached;
					}

					if (__instance.lockedLabel.Text != "")
                    {
						__instance.lockedOverlay.VisibleSelf = true; // to show the text
						__instance.lockedOverlayImage.Color = new Color(0f, 0f, 0f, 0.15f);
						__instance.lockedByPatch.VisibleSelf = false;
					}

					Diagnostics.LogWarning($"[Gedemon] Alignment.Vertical = {__instance.lockedLabel.Alignment.Vertical}, Alignment.VerticalOffset = {__instance.lockedLabel.Alignment.VerticalOffset}, Alignment.Horizontal = {__instance.lockedLabel.Alignment.Horizontal}, Alignment.HorizontalOffset = {__instance.lockedLabel.Alignment.HorizontalOffset}");

					//*/
				}

				__instance.lockedLabel.SetVerticalAlignment(Amplitude.UI.VerticalAlignment.Top);
				if (lines > 8)
				{
					int resize = lines < 16 ? lines : 16;
					__instance.lockedLabel.FontSize = (uint)(baseFontSize + 8 - resize);
				}
			}
		}
	}
}
