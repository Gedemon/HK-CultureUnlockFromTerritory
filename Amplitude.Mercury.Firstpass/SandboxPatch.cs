using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude.Mercury.Simulation;
using HarmonyLib;
using Amplitude.Mercury.Sandbox;
using Amplitude.Framework;
using Amplitude;
//using Amplitude.Mercury.Game;
using Amplitude.Framework.Session;
//using Amplitude.Mercury;
using System.Reflection;
//using Amplitude.Mercury.Interop;
using Amplitude.Framework.Networking;
using Amplitude.Framework.Simulation;
using Amplitude.Serialization;
using Amplitude.Framework.Storage;

namespace Gedemon.TrueCultureLocation
{

	//*
	[HarmonyPatch(typeof(Sandbox))]
	public class TCL_Sandbox
	{

		//*
		[HarmonyPatch("Load")]
		[HarmonyPatch(new Type[] { typeof(StorageContainerInfo) } )]
		[HarmonyPrefix]
		public static bool Load(Sandbox __instance, StorageContainerInfo storageContainerInfo)
		{
			{
				Diagnostics.LogError($"[Gedemon] [Sandbox] [Load] {storageContainerInfo.GetMetadata("GameSaveMetadata::Title")}, {storageContainerInfo.GetMetadata("GameSaveMetadata::DateTime")}");
			}
			return true;
		}
		//*/

		[HarmonyPatch("ThreadStarted")]
		[HarmonyPostfix]
		public static void ThreadStarted(Sandbox __instance)
		{
			Diagnostics.LogWarning($"[Gedemon] [ThreadStarted] postfix");

			DatabaseUtils.OnSandboxStarted();
			ModLoading.OnSandboxStarted();

			if (!CurrentGame.Data.IsInitialized)
            {
				// initialize mod's stuff here
				TrueCultureLocation.CreateStartingOutpost();

				CurrentGame.Data.IsInitialized = true;
			}

		}

		[HarmonyPatch("ThreadStart")]
		[HarmonyPostfix]
		public static void ThreadStartExit(Sandbox __instance, object parameter)
		{
			Diagnostics.LogWarning($"[Gedemon] exiting Sandbox, ThreadStart");
			MajorEmpireSaveExtension.OnExitSandbox();
			DatabaseUtils.OnExitSandbox();
			CityMap.OnExitSandbox();
			ModLoading.OnExitSandbox();
			CurrentGame.OnExitSandbox();
			CultureUnlock.OnExitSandbox();
		}


		[HarmonyPatch("ThreadStart")]
		[HarmonyPrefix]
		public static bool ThreadStart(Sandbox __instance, object parameter)
		{
			Diagnostics.LogWarning($"[Gedemon] entering Sandbox, ThreadStart");
			MajorEmpireSaveExtension.OnSandboxStart();
			/*
			Sandbox.Frame = 1;
			SimulationEvent.CurrentPermission = SimulationEvent.Permission.Forbidden;
			Sandbox.SandboxStartType startType = Sandbox.SandboxStartType.None;
			bool flag = false;
			try
			{
				Sandbox.SandboxThreadStartSettings = parameter as SandboxThreadStartSettings;
				if (Sandbox.SandboxThreadStartSettings == null)
				{
					throw new ArgumentException("The thread start parameter should be of type 'SandboxThreadStartSettings'.", "parameter");
				}
				if (__instance.FiniteStateMachine.InitialStateType != null)
				{
					__instance.FiniteStateMachine.PostStateChange(__instance.FiniteStateMachine.InitialStateType);
				}
				INetworkingService service = Amplitude.Framework.Services.GetService<INetworkingService>();
				if (service == null)
				{
					throw new MissingServiceException(typeof(INetworkingService));
				}
				__instance.ReceiveMessageQueue = service.CreateMessageReceiver();
				__instance.ReceiveMessageQueue.SubscribeToMessages(Sandbox.SubscribedMessages);
				__instance.SendMessageQueue = service.CreateMessageSender();
				__instance.PostOrderController = new PostOrderController();
				__instance.PostRequestController = new PostRequestController();
				__instance.RehostData = new RehostData();
				Sandbox.WorldConstants = SimulationController.AllocateSimulationEntity<WorldConstants>();
				__instance.OrderHistory.LastOrderSerialChange += __instance.OrderHistory_LastOrderSerialChange; // ??
				__instance.FiniteStateMachine.StateChange += __instance.FiniteStateMachine_StateChange;
				BindingFlags bindingFlags2 = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
				MethodInfo[] array = Assembly.GetExecutingAssembly().GetTypes().SelectMany((Type type) => from method in type.GetMethods(bindingFlags2)
																										  where method.IsStatic && method.GetCustomAttributes(inherit: false).Any((object attribute) => attribute is InitializeOnStartMethodAttribute)
																										  select method)
					.ToArray();
				for (int i = 0; i < array.Length; i++)
				{
					array[i].Invoke(null, null);
				}
				StartAncillaries();
				SandboxStartSettings sandboxStartSettings = SandboxThreadStartSettings.Parameter as SandboxStartSettings;
				if (sandboxStartSettings != null)
				{
					startType = SandboxStartType.NewGame;
					sessionMode = sandboxStartSettings.SessionMode;
					matchmakingLobbyType = sandboxStartSettings.MatchmakingLobbyType;
					metadata = new Metadata[sandboxStartSettings.Metadata.Length];
					sandboxStartSettings.Metadata.CopyTo(metadata, 0);
					metadataFrame = Frame;
					Timeline = Timeline.Create(sandboxStartSettings);
					WorldSeed = sandboxStartSettings.WorldSeed;
					World = World.Create(sandboxStartSettings.WorldGeneratorOutput);
					NumberOfEmpires = 0;
					if (sandboxStartSettings.NumberOfMajorEmpires <= 0 || sandboxStartSettings.NumberOfMajorEmpires > 16)
					{
						throw new ArgumentOutOfRangeException("NumberOfMajorEmpires");
					}
					NumberOfMinorEmpires = MinorFactionUtils.ComputeNumberOfMinorEmpiresNeeded(sandboxStartSettings);
					if (NumberOfMinorEmpires < 0 || NumberOfMinorEmpires > 100)
					{
						throw new ArgumentOutOfRangeException($"NumberOfMinorEmpires ({NumberOfMinorEmpires}) must be greater than or equal to 0, and lesser than 100.");
					}
					NumberOfEmpires += sandboxStartSettings.NumberOfMajorEmpires;
					NumberOfEmpires += NumberOfMinorEmpires;
					NumberOfEmpires++;
					Empires = new Empire[NumberOfEmpires];
					int num = 0;
					NumberOfMajorEmpires = sandboxStartSettings.NumberOfMajorEmpires;
					MajorEmpires = new MajorEmpire[sandboxStartSettings.NumberOfMajorEmpires];
					if (sandboxStartSettings.StartingFactionNames == null || sandboxStartSettings.StartingFactionNames.Length != NumberOfMajorEmpires)
					{
						throw new Exception("Invalid SandboxStartSettings.StartingFactionNames.");
					}
					if (sandboxStartSettings.StartingColors == null || sandboxStartSettings.StartingColors.Length != NumberOfMajorEmpires)
					{
						throw new Exception("Invalid SandboxStartSettings.StartingColors.");
					}
					if (sandboxStartSettings.GameDifficultyNames == null || sandboxStartSettings.GameDifficultyNames.Length != NumberOfMajorEmpires)
					{
						throw new Exception("Invalid SandboxStartSettings.GameDifficultyNames.");
					}
					int num2 = 0;
					while (num2 < MajorEmpires.Length)
					{
						MajorEmpire majorEmpire = SimulationController.AllocateSimulationEntity<MajorEmpire>();
						majorEmpire.SetEmpireIndex(num);
						StaticString faction = sandboxStartSettings.StartingFactionNames[num2];
						majorEmpire.SetFaction(faction);
						int num3 = (majorEmpire.ColorIndex = sandboxStartSettings.StartingColors[num2]);
						StaticString gameDifficulty = sandboxStartSettings.GameDifficultyNames[num2];
						majorEmpire.SetGameDifficulty(gameDifficulty);
						majorEmpire.SetPersonaContent(ref sandboxStartSettings.PersonaContents[num2], ref sandboxStartSettings.AvatarSummaries[num2]);
						majorEmpire.SetEmpireSymbol(sandboxStartSettings.StartingSymbols[num2]);
						majorEmpire.SetBannerOrnament(sandboxStartSettings.StartingOrnamentBanner[num2]);
						MajorEmpires[num2] = majorEmpire;
						Empires[num] = majorEmpire;
						num2++;
						num++;
					}
					MinorEmpires = new MinorEmpire[NumberOfMinorEmpires];
					int num4 = 0;
					while (num4 < MinorEmpires.Length)
					{
						MinorEmpire minorEmpire = SimulationController.AllocateSimulationEntity<MinorEmpire>();
						minorEmpire.SetEmpireIndex(num);
						MinorEmpires[num4] = minorEmpire;
						Empires[num] = minorEmpire;
						num4++;
						num++;
					}
					LesserEmpire lesserEmpire = SimulationController.AllocateSimulationEntity<LesserEmpire>();
					lesserEmpire.SetEmpireIndex(num);
					lesserEmpire.SetFaction(new StaticString("LesserFaction"));
					LesserEmpire = lesserEmpire;
					Empires[num] = lesserEmpire;
					ScenarioEditionStartSettings scenarioEditionStartSettings = sandboxStartSettings as ScenarioEditionStartSettings;
					if (scenarioEditionStartSettings != null)
					{
						if (StaticString.IsNullOrEmpty(scenarioEditionStartSettings.ScenarioName))
						{
							throw new Exception("Missing ScenarioEditionStartSettings.ScenarioName.");
						}
						ScenarioController.SetScenario(scenarioEditionStartSettings.ScenarioName);
						ScenarioController.IsEditingScenario = true;
						ScenarioController.SerializedStartingEditorOrders = scenarioEditionStartSettings.SerializedEditionOrders;
					}
					if (!ScenarioController.IsEditingScenario)
					{
						if (!Metadata.TryGetMetadata("dlcs", out var value))
						{
							Diagnostics.LogError("Downloadable contents metadata could not be found.");
						}
						DownloadableContents = int.Parse(value);
					}
					string value2;
					if ((UnityEngine.Object)(object)ScenarioController.ScenarioDefinition == null)
					{
						if (!Metadata.TryGetMetadata("GameOption_WorldSize", out value2))
						{
							throw new Exception("Cannot find WorldSize in metadata");
						}
					}
					else
					{
						value2 = ScenarioController.ScenarioDefinition.GameSize;
					}
					MajorEmpire.SetWorldSize(value2);
				}
				GameSaveDescriptor gameSaveDescriptor = SandboxThreadStartSettings.Parameter as GameSaveDescriptor;
				if (gameSaveDescriptor != null)
				{
					startType = SandboxStartType.LoadGame;
					sessionMode = gameSaveDescriptor.GameSaveSessionDescriptor.SessionMode;
					matchmakingLobbyType = gameSaveDescriptor.GameSaveSessionDescriptor.MatchmakingLobbyType;
					metadata = new Metadata[gameSaveDescriptor.GameSaveSessionDescriptor.Metadata.Length];
					gameSaveDescriptor.GameSaveSessionDescriptor.Metadata.CopyTo(metadata, 0);
					metadataFrame = Frame;
					Load(gameSaveDescriptor.StorageContainerInfo);
					int numberOfMajorEmpires = NumberOfMajorEmpires;
					for (int j = 0; j < numberOfMajorEmpires; j++)
					{
						MajorEmpire majorEmpire2 = MajorEmpires[j];
						gameSaveDescriptor.GameSaveSessionSlotDescriptors[j].AvatarSummary.CopyTo(ref majorEmpire2.AvatarSummary);
						gameSaveDescriptor.GameSaveSessionSlotDescriptors[j].PersonaContent.CopyTo(ref majorEmpire2.PersonaContent);
					}
				}
				ScenarioStartSettings scenarioStartSettings = SandboxThreadStartSettings.Parameter as ScenarioStartSettings;
				if (scenarioStartSettings != null)
				{
					startType = SandboxStartType.LoadScenario;
					sessionMode = scenarioStartSettings.SessionMode;
					matchmakingLobbyType = scenarioStartSettings.MatchmakingLobbyType;
					metadata = new Metadata[scenarioStartSettings.Metadata.Length];
					scenarioStartSettings.Metadata.CopyTo(metadata, 0);
					metadataFrame = Frame;
					if (StaticString.IsNullOrEmpty(scenarioStartSettings.ScenarioName))
					{
						throw new ArgumentNullException("scenarioStartSettings.ScenarioName");
					}
					GameScenarioDefinition value3 = Databases.GetDatabase<GameScenarioDefinition>().GetValue(scenarioStartSettings.ScenarioName);
					if ((UnityEngine.Object)(object)value3 == null)
					{
						throw new ArgumentNullException("scenarioDefinition", $"Scenario definition '{scenarioStartSettings.ScenarioName}' not found.");
					}
					if (string.IsNullOrEmpty(value3.SaveFullPath) || !File.Exists(value3.SaveFullPath))
					{
						throw new FileNotFoundException(string.Format("The game scenario definition does not contain a valid save file name: '{0}'.", value3.SaveFullPath ?? "null"));
					}
					StorageContainerInfo scenarioStorageContainerInfo = (Amplitude.Framework.Services.GetService<IGameContainerService>() ?? throw new MissingServiceException(typeof(IGameContainerService))).GetScenarioStorageContainerInfo(value3.SaveName);
					Load(scenarioStorageContainerInfo);
					GUID = System.Guid.NewGuid();
					GameID = GUID.ToString();
					if ((UnityEngine.Object)(object)ScenarioController.ScenarioDefinition != (UnityEngine.Object)(object)value3)
					{
						throw new InvalidDataException($"Save file at path '{value3.SaveFullPath}' don't correspond to scenario '{value3.Name}'.");
					}
					if (scenarioStartSettings.GameDifficultyNames == null || scenarioStartSettings.GameDifficultyNames.Length != NumberOfMajorEmpires)
					{
						throw new Exception("Invalid ScenarioStartSettings.GameDifficultyNames.");
					}
					int numberOfMajorEmpires2 = NumberOfMajorEmpires;
					for (int k = 0; k < numberOfMajorEmpires2; k++)
					{
						MajorEmpire obj = MajorEmpires[k];
						StaticString gameDifficulty2 = scenarioStartSettings.GameDifficultyNames[k];
						obj.SetGameDifficulty(gameDifficulty2);
						obj.SetPersonaContent(ref scenarioStartSettings.PersonaContents[k], ref scenarioStartSettings.AvatarSummaries[k]);
						obj.SetEmpireSymbol(scenarioStartSettings.StartingSymbols[k]);
						obj.SetBannerOrnament(scenarioStartSettings.StartingOrnamentBanner[k]);
					}
				}
				if (sandboxStartSettings == null && gameSaveDescriptor == null && scenarioStartSettings == null)
				{
					throw new InvalidOperationException("Sandbox should be started with a SandboxThreadStartSettings.Parameter of either 'SandboxStartSettings' or 'GameSaveDescriptor' or 'GameScenarioDefinition' type.");
				}
				SimulationController.RefreshAll();
				foreach (Ancillary ancillary2 in Ancillaries)
				{
					ancillary2.InitializeOnLoad();
				}
				for (int l = 0; l < Empires.Length; l++)
				{
					Empires[l].InitializeOnLoad();
				}
				array = Assembly.GetExecutingAssembly().GetTypes().SelectMany((Type type) => from method in type.GetMethods(bindingFlags2)
																							 where method.IsStatic && method.GetCustomAttributes(inherit: false).Any((object attribute) => attribute is InitializeOnLoadMethodAttribute)
																							 select method)
					.ToArray();
				for (int i = 0; i < array.Length; i++)
				{
					array[i].Invoke(null, null);
				}
				if (sandboxStartSettings != null)
				{
					foreach (Ancillary ancillary3 in Ancillaries)
					{
						ancillary3.InitializeOnStart(sandboxStartSettings);
					}
					for (int m = 0; m < Empires.Length; m++)
					{
						Empires[m].InitializeOnStart(sandboxStartSettings);
					}
					string value4;
					if ((UnityEngine.Object)(object)ScenarioController.ScenarioDefinition == null)
					{
						if (!Metadata.TryGetMetadata("GameOption_WorldSize", out value4))
						{
							throw new Exception("Cannot find WorldSize in metadata");
						}
					}
					else
					{
						value4 = ScenarioController.ScenarioDefinition.GameSize;
					}
					PollutionManager.SetWorldSize(value4);
				}
				if (scenarioStartSettings != null)
				{
					foreach (Ancillary ancillary4 in Ancillaries)
					{
						ancillary4.InitializeOnScenarioStart(scenarioStartSettings);
					}
					for (int n = 0; n < Empires.Length; n++)
					{
						Empires[n].InitializeOnScenarioStart(scenarioStartSettings);
					}
				}
				Ancillary.Passes.InvokePasses(SimulationPasses.PassContext.SandboxStarted);
				for (int num5 = 0; num5 < Empires.Length; num5++)
				{
					Empires[num5].Passes.InvokePasses(SimulationPasses.PassContext.SandboxStarted);
				}
				SimulationController.RefreshAll();
				VisibilityController.ExecuteVisibilityRefresh(raiseSimulationEvents: false);
				Frame++;
				synchronization = new Synchronization();
				synchronizationEvent = new AutoResetEvent(initialState: false);
				synchronizationTimer = new Timer(synchronization.Tick, synchronizationEvent, 100, 100);
				Synchronize();
				LastAutoSaveTurn = Turn;
				IsInitialized = true;
				flag = true;
			}
			catch (Exception exception)
			{
				Diagnostics.LogException(exception);
				terminationPending = true;
			}
			if (!terminationPending)
			{
				Diagnostics.Log("[Sandbox] The sandbox thread has been started.");
				ThreadStarted();
				SimulationEvent.CurrentPermission = SimulationEvent.Permission.Allowed;
				SimulationEvent_SandboxStarted.Raise(this, startType);
				while (true)
				{
					if (terminationPending)
					{
						bool flag2 = false;
						int count = pendingSaveRequest.Count;
						for (int num6 = 0; num6 < count; num6++)
						{
							if (!pendingSaveRequest[num6].SaveOperation.IsDone)
							{
								flag2 = true;
								break;
							}
						}
						if (!flag2)
						{
							break;
						}
					}
					try
					{
						PostOrderController.Update();
						PostRequestController.Update();
						int count2 = ordersQueuedForValidationByPolicy.Count;
						for (int num7 = 0; num7 < count2; num7++)
						{
							KeyValuePair<Order, NetworkIdentifier> keyValuePair = ordersQueuedForValidationByPolicy.Dequeue();
							ValidateOrder(keyValuePair.Key, keyValuePair.Value);
						}
						if (orderMessagesQueuedInQuarantineByPolicy.Count != 0)
						{
							int count3 = orderMessagesQueuedInQuarantineByPolicy.Count;
							for (int num8 = 0; num8 < count3; num8++)
							{
								ProcessOrderMessage processOrderMessage = orderMessagesQueuedInQuarantineByPolicy.Peek();
								if (OrderPolicyController.GetPolicy(FiniteStateMachine.CurrentState, OrderPolicyChainType.Process, processOrderMessage.Order) != OrderPolicy.Accept)
								{
									break;
								}
								orderMessagesQueuedInQuarantineByPolicy.Dequeue();
								ProcessOrder(processOrderMessage);
							}
						}
						Message message;
						NetworkIdentifier sender;
						while (ReceiveMessageQueue.TryReceiveMessage(out message, out sender))
						{
							if (!SandboxManager.DropReceivedOrders)
							{
								DispatchMessage(message, sender);
							}
							else
							{
								Diagnostics.LogWarning("Dropping order because SandboxManager.DropReceivedOrders is set to true.");
							}
						}
						int count4 = Ancillaries.Count;
						for (int num9 = 0; num9 < count4; num9++)
						{
							Ancillary ancillary = Ancillaries[num9];
							needSynchronization |= ancillary.Update();
						}
						FiniteState currentState = FiniteStateMachine.CurrentState;
						FiniteStateMachine.Update();
						needSynchronization |= currentState != FiniteStateMachine.CurrentState;
						ProcessSaveRequests();
						if (needSynchronization)
						{
							needSynchronization = false;
							Frame++;
						}
						if (synchronizationEvent.WaitOne(0))
						{
							SimulationEvent.CurrentPermission = SimulationEvent.Permission.Forbidden;
							Synchronize();
							SimulationEvent.CurrentPermission = SimulationEvent.Permission.Allowed;
						}
						SimulationController.Update();
						AIController.LateUpdate();
						Thread.Sleep(1);
					}
					catch (Exception ex)
					{
						SandboxException ex2 = ex as SandboxException;
						if (ex2 != null && ex2.Terminal && SandboxManager.LastException == null)
						{
							SandboxManager.LastException = ex;
						}
						else
						{
							Diagnostics.LogException(ex);
						}
					}
				}
			}
			SimulationEvent.CurrentPermission = SimulationEvent.Permission.AllowedButIgnored;
			Diagnostics.Log("[Sandbox] Gracefully exited the sandbox loop; shutting down...");
			IsInitialized = false;
			try
			{
				for (int num10 = 0; num10 < pendingSaveRequest.Count; num10++)
				{
					Diagnostics.LogError("Cancel pending save request " + pendingSaveRequest[num10].StorageContainerInfo.Category + "." + pendingSaveRequest[num10].StorageContainerInfo.Name + ".");
					pendingSaveRequest[num10].SaveOperation.Close(new Exception("Sandbox shutdown is in progress."));
					if (Sandbox.OnSaveStateChange != null)
					{
						Sandbox.OnSaveStateChange(this, new SerializationEventArgs(SerializationEventArgs.SaveState.Failed, pendingSaveRequest[num10].StorageContainerInfo));
					}
				}
				pendingSaveRequest.Clear();
				IsSaving = false;
				if (flag)
				{
					if (Ancillaries != null)
					{
						Ancillary.Passes.InvokePasses(SimulationPasses.PassContext.SandboxShutdown);
					}
					if (Empires != null)
					{
						for (int num11 = 0; num11 < Empires.Length; num11++)
						{
							Empires[num11].Passes.InvokePasses(SimulationPasses.PassContext.SandboxShutdown);
						}
					}
				}
				OrderHistory.LastOrderSerialChange -= OrderHistory_LastOrderSerialChange;
				FiniteStateMachine.StateChange -= FiniteStateMachine_StateChange;
				if (synchronizationTimer != null)
				{
					synchronizationTimer.Dispose();
					synchronizationTimer = null;
				}
				if (synchronizationEvent != null)
				{
					synchronizationEvent.Close();
					synchronizationEvent = null;
				}
				synchronization = null;
				bool flag3 = false;
				if (flag)
				{
					try
					{
						if (Empires != null)
						{
							for (int num12 = 0; num12 < Empires.Length; num12++)
							{
								Empires[num12].Unload();
							}
						}
						if (Ancillaries != null)
						{
							for (int num13 = 0; num13 < Ancillaries.Count; num13++)
							{
								Ancillaries[num13].Unload();
							}
						}
						if (Empires != null)
						{
							for (int num14 = 0; num14 < Empires.Length; num14++)
							{
								SimulationController.FreeSimulationEntity(Empires[num14]);
								Empires[num14] = null;
							}
						}
						if (World != null)
						{
							World.Dispose();
						}
						if (WorldConstants != null)
						{
							SimulationController.FreeSimulationEntity(WorldConstants);
						}
						flag3 = true;
					}
					catch (Exception exception2)
					{
						Diagnostics.LogError("Unhandled exception in simulation safe-release. Will try a hard-release");
						Diagnostics.LogException(exception2);
					}
				}
				bool flag4 = !flag || !flag3;
				if (flag4)
				{
					SimulationController.HardReset();
				}
				ShutdownAncillaries(flag4);
				BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
				MethodInfo[] array = Assembly.GetExecutingAssembly().GetTypes().SelectMany((Type type) => from method in type.GetMethods(bindingFlags)
																										  where method.IsStatic && method.GetCustomAttributes(inherit: false).Any((object attribute) => attribute is ReleaseOnShutdownMethodAttribute)
																										  select method)
					.ToArray();
				for (int i = 0; i < array.Length; i++)
				{
					array[i].Invoke(null, null);
				}
				Empires = null;
				MajorEmpires = null;
				MinorEmpires = null;
				LesserEmpire = null;
				NumberOfEmpires = (NumberOfMajorEmpires = (NumberOfMinorEmpires = 0));
				World = null;
				WorldConstants = null;
				remoteSandboxLocalInfos.Clear();
				remoteSandboxReplicatedInfos.Clear();
				RemoteSandboxIdentifierArray.Clear();
				PostRequestController.Dispose();
				PostRequestController = null;
				PostOrderController.Dispose();
				PostOrderController = null;
				INetworkingService service2 = Amplitude.Framework.Services.GetService<INetworkingService>();
				if (service2 != null)
				{
					if (ReceiveMessageQueue != null)
					{
						ReceiveMessageQueue.UnsubscribeFromAllMessages();
						service2.ReleaseMessageController(ReceiveMessageQueue);
					}
					if (SendMessageQueue != null)
					{
						service2.ReleaseMessageController(SendMessageQueue);
					}
				}
				ReceiveMessageQueue = null;
				SendMessageQueue = null;
				ordersQueuedForValidationByPolicy.Clear();
			}
			catch (Exception exception3)
			{
				Diagnostics.LogError("Unhandled exception in sandbox shutdown.");
				Diagnostics.LogException(exception3);
			}
			ThreadShutdown();


			//*/
			return true;
		}
	}
	//*/

}

