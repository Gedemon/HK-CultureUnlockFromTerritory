using HarmonyLib;
using Amplitude.Framework;
using Amplitude;
using Amplitude.Mercury.Game;
using Amplitude.Framework.Session;
using Amplitude.Mercury.Session;
using Amplitude.Mercury.PlayerProfile;
using Amplitude.Mercury.Persona;
using Amplitude.Mercury;

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
				Diagnostics.LogWarning($"[Gedemon] in Game, Worker_StartSandbox");
				Diagnostics.LogWarning($"[Gedemon] Plugin = {TrueCultureLocation.pluginGuid} Version = {TrueCultureLocation.pluginVersion}");
				ISessionService service = Services.GetService<ISessionService>();

				ISessionSlotController slots = ((Amplitude.Mercury.Session.Session)service.Session).Slots;
				if (totalEmpireSlots > slots.Count)
				{
					IPersonaListingService personaListingService = Services.GetService<IPersonaListingService>();
					ListOfStruct<AvailablePersona> listOfAvailablePersona = new ListOfStruct<AvailablePersona>();
					listOfAvailablePersona.Clear();
					personaListingService.FillAvailablePersona(listOfAvailablePersona, fillOnlyUseable: true);
					int numAvatars = listOfAvailablePersona.Length + 1; // to do, add MP avatars, not just the local human player avatar ?

					Diagnostics.LogWarning($"[Gedemon] Asking for {totalEmpireSlots} slots, valid Avatars = {numAvatars}");
					totalEmpireSlots = totalEmpireSlots > numAvatars ? numAvatars : totalEmpireSlots;
					Diagnostics.LogWarning($"[Gedemon] Setting Slot Count to {totalEmpireSlots}");
					slots.SetSlotCount(totalEmpireSlots);
				}
			}
						
			return true;
		}
		//*/
	}
}
