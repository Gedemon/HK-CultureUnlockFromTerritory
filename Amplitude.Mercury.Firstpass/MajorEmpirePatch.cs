using Amplitude.Mercury.Simulation;
using HarmonyLib;
using Amplitude.Mercury.Sandbox;
using Amplitude;
using Amplitude.Mercury;
using Amplitude.Mercury.Data.Simulation;
using System;
using Amplitude.Mercury.Interop;
using System.Collections.Generic;

namespace Gedemon.TrueCultureLocation
{

	[HarmonyPatch(typeof(MajorEmpire))]
	public class TCL_MajorEmpire
	{

		//*
		[HarmonyPostfix]
		[HarmonyPatch(nameof(InitializeOnStart))]
		public static void InitializeOnStart(MajorEmpire __instance)
		{
			bool isHuman = TrueCultureLocation.IsEmpireHumanSlot(__instance.Index);

			Diagnostics.LogWarning($"[Gedemon] in MajorEmpire, InitializeOnStart for {__instance.majorEmpireDescriptorName}, index = {__instance.Index}, IsControlledByHuman = {isHuman}"); // IsEmpireHumanSlot(int empireIndex)
			WorldPosition worldPosition = new WorldPosition(World.Tables.SpawnLocations[__instance.Index]);

			bool isImmediate = true;
			FixedPoint bonusProduction = FixedPoint.Zero;
			SimulationEntityGUID GUID = SimulationEntityGUID.Zero;

			if (TrueCultureLocation.HasStartingOutpost(__instance.Index, isHuman)) // 
			{
				//__instance.DepartmentOfTheInterior.CreateCampAt(__instance.Armies[0].GUID, worldPosition, FixedPoint.Zero, isImmediate);
				//*
				Settlement settlement = __instance.DepartmentOfTheInterior.CreateSettlement(GUID, worldPosition);
				settlement.AddDescriptor(DepartmentOfTheInterior.campDescriptorName);

				__instance.DepartmentOfIndustry.AttachQueueTo(settlement);
				settlement.SetAvailableConstructionsDirty(AvailableConstructionsDirtyStatuses.Full);
				DepartmentOfTheInterior.AttachSettlementToEmpire(settlement, __instance);


				//Diagnostics.LogWarning($"[Gedemon] before AttachSettlementToEmpire");
				//__instance.DepartmentOfTheInterior.CreateRegion(settlement);
				//
				//Diagnostics.LogWarning($"[Gedemon] beforeSimulationController.AllocateSimulationEntity<Region>");
				Region region = Amplitude.Framework.Simulation.SimulationController.AllocateSimulationEntity<Region>();
				region.SurroundingTerritories.Count = 0;
				region.Settlement.SetEntity(settlement);
				//Diagnostics.LogWarning($"[Gedemon] before Regions.Add(region)");
				Amplitude.Mercury.Sandbox.Sandbox.World.Regions.Add(region);
				settlement.Region.SetEntity(region);
				//
				int num = settlement.WorldPosition.ToTileIndex();
				int territoryIndex = Amplitude.Mercury.Sandbox.Sandbox.World.TileInfo.Data[num].TerritoryIndex;
				////

				//Diagnostics.LogWarning($"[Gedemon] before AttachTerritoryToSettlementRegion");
				//__instance.DepartmentOfTheInterior.AttachTerritoryToSettlementRegion(settlement, territoryIndex);
				//
				settlement.TerritoryCount.Value++;
				Amplitude.Mercury.Simulation.Territory territory = Amplitude.Mercury.Sandbox.Sandbox.World.Territories[territoryIndex];
				Region entity = settlement.Region.Entity;
				entity.TerritoryIndices.Add(territoryIndex);
				entity.Territories.Add(territory);
				int[] adjacentTerritories = territory.AdjacentTerritories;
				int num3 = adjacentTerritories.Length;
				for (int i = 0; i < num3; i++)
				{
					int num2 = adjacentTerritories[i];
					ref Amplitude.Mercury.Interop.TerritoryInfo reference2 = ref Amplitude.Mercury.Sandbox.Sandbox.World.TerritoryInfo.Data[num2];
					SimulationEntityGUID simulationEntityGUID = reference2.RegionGUID;
					if (reference2.Claimed && simulationEntityGUID == entity.GUID)
					{
						entity.SurroundingTerritories.Remove((short)territoryIndex);
					}
					else
					{
						entity.SurroundingTerritories.Add((short)num2);
					}
				}
				////
				//Diagnostics.LogWarning($"[Gedemon] before ClaimTerritory");
				//Amplitude.Mercury.Sandbox.Sandbox.World.ClaimTerritory(territoryIndex, settlement);
				{
					byte b = (byte)settlement.Empire.Entity.Index;
					Region entity2 = settlement.Region.Entity;
					//Diagnostics.LogWarning($"[Gedemon] before TerritoryInfo");
					ref Amplitude.Mercury.Interop.TerritoryInfo reference3 = ref Amplitude.Mercury.Sandbox.Sandbox.World.TerritoryInfo.Data[territoryIndex];
					int num4 = (reference3.Claimed ? reference3.EmpireIndex : (-1));
					reference3.SettlementGUID = settlement.GUID;
					reference3.SettlementIndex = settlement.PoolAllocationIndex;
					//Diagnostics.LogWarning($"[Gedemon] before PrimaryColor");
					reference3.EmpireColor = Amplitude.Mercury.Sandbox.Sandbox.EmpireNamesRepository.GetEmpirePrimaryColor(b);
					reference3.EmpireIndex = b;
					reference3.RegionGUID = entity2.GUID;
					reference3.IsOwnedByCity = settlement.SettlementStatus == SettlementStatuses.City;
					reference3.Claimed = true;
					//Diagnostics.LogWarning($"[Gedemon] before Frame");
					Sandbox.World.TerritoryInfo.Frame = Amplitude.Mercury.Sandbox.Sandbox.Frame;
					//Diagnostics.LogWarning($"[Gedemon] before World.Territories");
					Amplitude.Mercury.Simulation.Territory territory2 = Sandbox.World.Territories[territoryIndex];
					//Diagnostics.LogWarning($"[Gedemon] before SetEntity");
					territory2.Region.SetEntity(entity2);
					if (b != num4)
					{
						//Diagnostics.LogWarning($"[Gedemon] before CultureManager.OnTerritoryOwnerChanged");
						Amplitude.Mercury.Sandbox.Sandbox.CultureManager.OnTerritoryOwnerChanged(territoryIndex, num4, b);
						//Diagnostics.LogWarning($"[Gedemon] before ReligionManager.OnTerritoryOwnerChanged");
						Amplitude.Mercury.Sandbox.Sandbox.ReligionManager.OnTerritoryOwnerChanged(territoryIndex, num4, b);
						//Diagnostics.LogWarning($"[Gedemon] before SimulationEvent_TerritoryOwnerChanged Sandbox.World = {Sandbox.World},  territoryIndex = {territoryIndex} num4 = {num4} b = {b}");
						//SimulationEvent_TerritoryOwnerChanged.Raise(Sandbox.World, territoryIndex, num4, b);
						//ControlAreaManager.cs
						{
							int num5 = b;
							if (num4 >= Amplitude.Mercury.Sandbox.Sandbox.NumberOfMajorEmpires)
							{
								num4 = -1;
							}
							if (b >= Amplitude.Mercury.Sandbox.Sandbox.NumberOfMajorEmpires)
							{
								num5 = -1;
							}
							if (num4 >= 0 || num5 >= 0)
							{
								//Diagnostics.LogWarning($"[Gedemon] before Sandbox.ControlAreaManager.areaIndicesPerTerritory {Sandbox.ControlAreaManager}, {Sandbox.ControlAreaManager.areaIndicesPerTerritory}");
								//
								if (Sandbox.ControlAreaManager.areaIndicesPerTerritory != null)
								{

									//Diagnostics.LogWarning($"[Gedemon] not null !");
									ControlAreaManager.AreaIndexWithProvider[] array = Sandbox.ControlAreaManager.areaIndicesPerTerritory[territoryIndex];
									int num6 = array.Length;
									for (int i = 0; i < num6; i++)
									{
										ControlAreaManager.AreaIndexWithProvider areaIndexWithProvider = array[i];
										int areaIndex = areaIndexWithProvider.AreaIndex;
										//Diagnostics.LogWarning($"[Gedemon] before ControlAreaInfo.GetReferenceAt");
										ref ControlAreaInfo referenceAt = ref Sandbox.ControlAreaManager.ControlAreaInfo.GetReferenceAt(areaIndex);
										if (num4 >= 0)
										{
											if (referenceAt.ClaimedTerritoryCountPerEmpire[num4] == referenceAt.TerritoryCount)
											{
												referenceAt.OwnerIndex = -1;
												areaIndexWithProvider.Provider.AreaLostBy(areaIndex, referenceAt.AssociatedInfoIndex, num4);
											}
											referenceAt.ClaimedTerritoryCountPerEmpire[num4]--;
										}
										if (num5 >= 0)
										{
											referenceAt.ClaimedTerritoryCountPerEmpire[num5]++;
											if (referenceAt.ClaimedTerritoryCountPerEmpire[num5] == referenceAt.TerritoryCount)
											{
												referenceAt.OwnerIndex = num5;
												areaIndexWithProvider.Provider.AreaOwnedBy(areaIndex, referenceAt.AssociatedInfoIndex, num5);
											}
										}
										//Diagnostics.LogWarning($"[Gedemon] before ControlAreaInfo.SetSynchronizationDirty");
										Sandbox.ControlAreaManager.ControlAreaInfo.SetSynchronizationDirty();
									}
								}
								///
							}

						}
						////
						//LandmarkManager.cs
						{
							//int num = 0;
							//if (e.NewOwnerEmpireIndex >= 0 && e.NewOwnerEmpireIndex < Amplitude.Mercury.Sandbox.Sandbox.NumberOfMajorEmpires)
							//{
							//	num = Amplitude.Mercury.Sandbox.Sandbox.MajorEmpires[e.NewOwnerEmpireIndex].DepartmentOfDevelopment.CurrentEraIndex;
							//}
							//Territory territory = Amplitude.Mercury.Sandbox.Sandbox.World.Territories[e.TerritoryIndex];
							//int count = territory.LandmarkParts.Count;
							//for (int i = 0; i < count; i++)
							//{
							//	territory.LandmarkParts[i].OwnerEraIndex.Value = num;
							//}
						}
						//
						if (Sandbox.World.IsContinentOwnedByEmpire(territory2.ContinentIndex, b))
						{
							//Diagnostics.LogWarning($"[Gedemon] before SimulationEvent_ContinentConquered");
							SimulationEvent_ContinentConquered.Raise(Sandbox.World, territory2.ContinentIndex, b);
						}
					}
				}
				//Diagnostics.LogWarning($"[Gedemon] after ClaimTerritory");


				//int num = worldPosition.ToTileIndex();
				//int territoryIndex = Amplitude.Mercury.Sandbox.Sandbox.World.TileInfo.Data[num].TerritoryIndex;
				ref Amplitude.Mercury.Interop.TerritoryInfo reference = ref Amplitude.Mercury.Sandbox.Sandbox.World.TerritoryInfo.Data[territoryIndex];
				settlement.EntityName.LocalizationKey = reference.LocalizedName;
				//Diagnostics.LogWarning($"[Gedemon] before GatherAdjacentSettlements");
				DepartmentOfTheInterior.GatherAdjacentSettlements(settlement);
				//Diagnostics.LogWarning($"[Gedemon] before GatherTerritoryDistricts");
				__instance.DepartmentOfTheInterior.GatherTerritoryDistricts(settlement);
				if (isImmediate)
				{
					__instance.DepartmentOfTheInterior.ApplyEvolutionToSettlement_Camp(settlement);
				}
				else
				{
					District district = settlement.GetDistrictAt(worldPosition);
					if (district != null)
					{
						__instance.DepartmentOfTheInterior.UpgradeDistrict(district, DepartmentOfTheInterior.BeforeCampDefinition);
					}
					else
					{
						district = __instance.DepartmentOfTheInterior.CreateExtensionDistrictInSettlement(settlement, worldPosition, DepartmentOfTheInterior.BeforeCampDefinition);
					}
					Amplitude.Mercury.Data.Simulation.ExploitationRuleDefinition exploitationRuleDefinition = DepartmentOfTheInterior.BeforeCampDefinition.ExploitationRuleDefinition;
					__instance.DepartmentOfTheInterior.GenerateExploitationsAround(district, exploitationRuleDefinition);
					DepartmentOfTheInterior.AddExploitationRuleDescriptorsFor(district, exploitationRuleDefinition);
					__instance.DepartmentOfIndustry.AddConstructionToSettlement(settlement, DepartmentOfTheInterior.evolutionCampDefinition, EnqueuePosition.AtBegin, worldPosition);
					settlement.ConstructionQueue.Entity.CurrentResourceStock += bonusProduction;
					__instance.DepartmentOfIndustry.InvestProductionFor(settlement.ConstructionQueue);
					isImmediate = settlement.SettlementStatus == SettlementStatuses.Camp;
					__instance.DepartmentOfTheInterior.UpdateAdministrativeCenter(district);
					DepartmentOfTheInterior.UpdateDistrictsAfterSettlementCreation(settlement);
				}
				//Diagnostics.LogWarning($"[Gedemon] before InitializeArmiesCollection");
				DepartmentOfTheInterior.InitializeFriendlyHostileArmiesSettlement(settlement);
				if (isImmediate)
				{
					SimulationEvent_CampFounded.Raise(__instance.DepartmentOfTheInterior, settlement);
					SimulationEvent_SettlementNameChanged.Raise(__instance.DepartmentOfTheInterior, settlement);
				}
				if (__instance != null)
				{
					__instance.DepartmentOfTheInterior.UpdateSettlementDistanceToCapital(settlement);
					if ((__instance.MiscFlags & EmpireMiscFlags.HasAlreadyFoundACamp) == 0)
					{
						__instance.SetMiscFlags(EmpireMiscFlags.HasAlreadyFoundACamp, set: true);
					}
				}
				//Diagnostics.LogWarning($"[Gedemon] before SimulationEvent_SettlementOwnerChanged");
				SimulationEvent_SettlementOwnerChanged.Raise(__instance.DepartmentOfTheInterior, settlement, -1);
				//Diagnostics.LogWarning($"[Gedemon] after SimulationEvent_SettlementOwnerChanged");
				if (__instance != null)
				{
					__instance.DepartmentOfCommunication.Notify(new Amplitude.Mercury.Interop.NewSettlementCreatedNotificationData
					{
						InitiatorGUID = GUID,
						SettlementGUID = settlement.GUID
					});
				}
				//*/

				//Amplitude.Mercury.Sandbox.Sandbox.Empires[__instance.Index].DepartmentOfTheInterior.CreateCampAt(SimulationEntityGUID.Zero, worldPosition, FixedPoint.Zero, true);
				//__instance.DepartmentOfTheInterior.CreateCampAt(SimulationEntityGUID.Zero, worldPosition, FixedPoint.Zero, false);
			}

		}
		//*/
	}

	/*
	[HarmonyPatch(typeof(ArmyActionHelper))]
	public class TCL_ArmyActionHelper
	{
		[HarmonyPatch("FillCreateCampFailures")]
		[HarmonyPatch(new Type[] { typeof(Army), typeof(ArmyActionFailureFlags), typeof(FixedPoint), typeof(FixedPoint), typeof(FixedPoint), typeof(bool) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Normal })]
		[HarmonyPrefix]
		public static bool FillCreateCampFailures(Army army, ref ArmyActionFailureFlags failureFlags, ref FixedPoint minimumMoneyCost, ref FixedPoint minimumInfluenceCost, ref FixedPoint instantUnitCost, bool computeMinimumCost)
		{
			Empire empire = army.Empire.Entity;
			int empireIndex = empire.Index;
			bool isHuman = TrueCultureLocation.IsEmpireHumanSlot(empireIndex);
			if (!TrueCultureLocation.IsSettlingEmpire(empireIndex, isHuman))
			{
				failureFlags |= ArmyActionFailureFlags_Part1.IsLesserEmpire;
				return false;
			}
			return true;
		}
	}
	//*/

}
