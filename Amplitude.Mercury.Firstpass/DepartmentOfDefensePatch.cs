using Amplitude.Mercury.Simulation;
using HarmonyLib;
using Amplitude;

namespace Gedemon.TrueCultureLocation
{

	[HarmonyPatch(typeof(DepartmentOfDefense))]
	public class TCL_DepartmentOfDefense
	{

		//*
		[HarmonyPrefix]
		[HarmonyPatch(nameof(TryGetInitialArmyPosition))]
		public static bool TryGetInitialArmyPosition(DepartmentOfDefense __instance, ref bool __result, MajorEmpire majorEmpire, ref int spawnTileIndex)
		{
			int spawnLocationsLength = World.Tables.SpawnLocations.Length;
			bool oldWorldOnly = TrueCultureLocation.UseOnlyOldWorldStart();

			Diagnostics.LogWarning($"[Gedemon] in DepartmentOfDefense, TryGetInitialArmyPosition for MajorEmpire #{majorEmpire.Index}, num spawn = {spawnLocationsLength}, Only OldWorld = {oldWorldOnly}, IsCompatibleMap = {CultureUnlock.IsCompatibleMap()}, UseExtraEmpireSlots = {TrueCultureLocation.UseExtraEmpireSlots()}, UseOnlyExtraStart = {TrueCultureLocation.UseOnlyExtraStart()} ");

			if (CultureUnlock.IsCompatibleMap() && (TrueCultureLocation.UseExtraEmpireSlots() || oldWorldOnly || TrueCultureLocation.UseOnlyExtraStart()))
			{

				if (majorEmpire.Index >= spawnLocationsLength)
				{

					Hexagon.OffsetCoords[] copy = new Hexagon.OffsetCoords[majorEmpire.Index + 1];
					World.Tables.SpawnLocations.CopyTo(copy, 0);

					Hexagon.OffsetCoords extraPosition = CultureUnlock.GetExtraStartingPosition(majorEmpire.Index, oldWorldOnly);

					Diagnostics.LogWarning($"[Gedemon] Set position {extraPosition}");

					copy[majorEmpire.Index] = extraPosition;
					World.Tables.SpawnLocations = copy;
				}
				else if (CultureUnlock.HasExtraStartingPosition(majorEmpire.Index, oldWorldOnly) && (oldWorldOnly || TrueCultureLocation.UseOnlyExtraStart()))
				{

					Hexagon.OffsetCoords extraPosition = CultureUnlock.GetExtraStartingPosition(majorEmpire.Index, oldWorldOnly);

					Diagnostics.LogWarning($"[Gedemon] Replace position {World.Tables.SpawnLocations[majorEmpire.Index]} => {extraPosition}");

					World.Tables.SpawnLocations[majorEmpire.Index] = extraPosition;
				}
			}

			return true;
		}
		//*/
	}
}
