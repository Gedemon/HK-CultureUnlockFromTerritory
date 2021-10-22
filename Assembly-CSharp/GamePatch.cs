using HarmonyLib;
using Amplitude.Framework;
using Amplitude;
using Amplitude.Mercury.Game;
using Amplitude.Framework.Session;
using Amplitude.Mercury.Session;

namespace Gedemon.TrueCultureLocation
{

	[HarmonyPatch(typeof(Game))]
	public class TCL_Game
	{
		//*
		[HarmonyPrefix]
		[HarmonyPatch(nameof(Worker_StartSandbox))]
		public static bool Worker_StartSandbox(Game __instance, object sender, System.ComponentModel.DoWorkEventArgs e)
		{
			int totalEmpireSlots = TrueCultureLocation.GetTotalEmpireSlots();
			if (totalEmpireSlots > 0)
			{
				Diagnostics.LogError($"[Gedemon] in Game, Worker_StartSandbox");
				ISessionService service = Services.GetService<ISessionService>();

				ISessionSlotController slots = ((Amplitude.Mercury.Session.Session)service.Session).Slots;
				if (totalEmpireSlots > slots.Count)
				{
					Diagnostics.LogError($"[Gedemon] Set Slots Count to {totalEmpireSlots}");
					slots.SetSlotCount(totalEmpireSlots);
				}
			}
						
			return true;
		}
		//*/
	}
}
