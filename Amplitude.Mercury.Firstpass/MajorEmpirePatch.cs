using Amplitude.Mercury.Simulation;
using HarmonyLib;
using Amplitude.Mercury.Sandbox;
using Amplitude;
using Amplitude.Mercury;
using Amplitude.Mercury.Data.Simulation;
using System;
using Amplitude.Mercury.Interop;
using System.Collections.Generic;
using Amplitude.Serialization;

namespace Gedemon.TrueCultureLocation
{
	public class MajorEmpireExtension : ISerializable
	{
		public int SpawnTurn { get; set; }

		public MajorEmpireExtension()
		{
			SpawnTurn = 0;
		}

        public void Serialize(Serializer serializer)
        {
			SpawnTurn = serializer.SerializeElement("SpawnTurn", SpawnTurn);
		}
    }
	public static class MajorEmpireSaveExtension
    {
		public static IDictionary<int, MajorEmpireExtension> EmpireExtensionPerEmpireIndex;

		public static void OnSandboxStart()
        {
			if (!TrueCultureLocation.IsEnabled())
				return;

			EmpireExtensionPerEmpireIndex = new Dictionary<int, MajorEmpireExtension>();
		}

		public static void OnExitSandbox()
		{
			if (!TrueCultureLocation.IsEnabled())
				return;

			EmpireExtensionPerEmpireIndex = null;
		}

		public static MajorEmpireExtension GetExtension(int empireIndex)
        {
			return EmpireExtensionPerEmpireIndex[empireIndex];
		}
	}


	[HarmonyPatch(typeof(MajorEmpire))]
	public class TCL_MajorEmpire
	{

		//*
		[HarmonyPostfix]
		[HarmonyPatch(nameof(InitializeOnStart))]
		public static void InitializeOnStart(MajorEmpire __instance)
		{
			if (TrueCultureLocation.IsEnabled())
			{
				MajorEmpireExtension majorEmpireExtension = new MajorEmpireExtension();
				MajorEmpireSaveExtension.EmpireExtensionPerEmpireIndex.Add(__instance.Index, majorEmpireExtension);
			}

		}
		//*/

		[HarmonyPatch("Serialize")]
		[HarmonyPostfix]
		public static void Serialize(MajorEmpire __instance, Serializer serializer)
		{
			if (!TrueCultureLocation.IsEnabled())
				return;

			int empireIndex = __instance.Index;
			switch (serializer.SerializationMode)
			{
				case SerializationMode.Read:
					{
						MajorEmpireExtension majorEmpireExtension = serializer.SerializeElement("MajorEmpireExtension", new MajorEmpireExtension());
						MajorEmpireSaveExtension.EmpireExtensionPerEmpireIndex.Add(empireIndex, majorEmpireExtension);
						break;
					}
				case SerializationMode.Write:
					{
						MajorEmpireExtension majorEmpireExtension = MajorEmpireSaveExtension.EmpireExtensionPerEmpireIndex[empireIndex];
						serializer.SerializeElement("MajorEmpireExtension", majorEmpireExtension);
						break;
					}
			}
		}
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
