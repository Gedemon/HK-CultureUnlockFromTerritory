using System.Linq;
using System.Reflection;
using Amplitude;
using Amplitude.Framework;
using Amplitude.Framework.Localization;
using Amplitude.Framework.Options;
using Amplitude.Mercury.Data.GameOptions;
using Amplitude.Mercury.Options;
using Amplitude.Mercury.UI;
using Amplitude.UI;
using UnityEngine;

namespace HumankindModTool
{
	public static class GameOptionHelper
	{
		private static IGameOptionsService _gameOptions;

		private static IGameOptionsService GameOptions
		{
			get
			{
				if (_gameOptions == null)
				{
					_gameOptions = Services.GetService<IGameOptionsService>();
				}
				return _gameOptions;
			}
		}

		public static string GetGameOption(GameOptionInfo info)
		{
			return GameOptions.GetOption(new StaticString(info.Key)).CurrentValue;
		}

		public static bool CheckGameOption(GameOptionInfo info, string checkValue, bool caseSensitive = false)
		{
			string val = GetGameOption(info);
			if (caseSensitive)
			{
				return val == checkValue;
			}
			return val?.ToLower() == checkValue?.ToLower();
		}

		public static void Initialize(params GameOptionInfo[] Options)
		{
			IDatabase<GameOptionDefinition> gameOptions = Databases.GetDatabase<GameOptionDefinition>();
			IDatabase<UIMapper> uiMappers = Databases.GetDatabase<UIMapper>();
			IDatabase<LocalizedStringElement> localizedStrings = Databases.GetDatabase<LocalizedStringElement>();
			foreach (GameOptionInfo optionVal in Options)
			{
				byte lastKey = gameOptions.Max((GameOptionDefinition x) => x.Key);
				string gameOptionName = optionVal.Key;
				GameOptionDefinition option = ScriptableObject.CreateInstance<GameOptionDefinition>();
				option.CanBeRandomized = false;
				lastKey = (option.Key = (byte)(lastKey + 1));
				option.XmlSerializableName = gameOptionName;
				option.name = gameOptionName;
				option.Default = optionVal.DefaultValue;
				option.States = new OptionState[optionVal.States.Count];
				for (int i = 0; i < option.States.Length; i++)
				{
					option.States[i] = new OptionState
					{
						Value = optionVal.States[i].Value
					};
				}
				gameOptions.Touch(option);
				LocalizedStringElement localizedStringElement = new LocalizedStringElement
				{
					LineId = "%" + gameOptionName + "Title",
					LocalizedStringElementFlag = LocalizedStringElementFlag.None
				};
				LocalizedStringElement localizedStringElement2 = localizedStringElement;
				LocalizedNode[] array = new LocalizedNode[1];
				LocalizedNode localizedNode = (array[0] = new LocalizedNode
				{
					Id = LocalizedNodeType.Terminal,
					TextValue = optionVal.Title
				});
				localizedStringElement2.CompactedNodes = array;
				localizedStringElement.TagCodes = new int[1];
				localizedStrings.Touch(localizedStringElement);
				localizedStringElement = new LocalizedStringElement
				{
					LineId = "%" + gameOptionName + "Description",
					LocalizedStringElementFlag = LocalizedStringElementFlag.None
				};
				LocalizedStringElement localizedStringElement3 = localizedStringElement;
				LocalizedNode[] array2 = new LocalizedNode[1];
				localizedNode = (array2[0] = new LocalizedNode
				{
					Id = LocalizedNodeType.Terminal,
					TextValue = optionVal.Description
				});
				localizedStringElement3.CompactedNodes = array2;
				localizedStringElement.TagCodes = new int[1];
				localizedStrings.Touch(localizedStringElement);
				foreach (GameOptionStateInfo opt in optionVal.States)
				{
					localizedStringElement = new LocalizedStringElement
					{
						LineId = "%" + gameOptionName + opt.Value + "Title",
						LocalizedStringElementFlag = LocalizedStringElementFlag.None
					};
					LocalizedStringElement localizedStringElement4 = localizedStringElement;
					LocalizedNode[] array3 = new LocalizedNode[1];
					localizedNode = (array3[0] = new LocalizedNode
					{
						Id = LocalizedNodeType.Terminal,
						TextValue = opt.Title
					});
					localizedStringElement4.CompactedNodes = array3;
					localizedStringElement.TagCodes = new int[1];
					localizedStrings.Touch(localizedStringElement);
					localizedStringElement = new LocalizedStringElement
					{
						LineId = "%" + gameOptionName + opt.Value + "Description",
						LocalizedStringElementFlag = LocalizedStringElementFlag.None
					};
					LocalizedStringElement localizedStringElement5 = localizedStringElement;
					LocalizedNode[] array4 = new LocalizedNode[1];
					localizedNode = (array4[0] = new LocalizedNode
					{
						Id = LocalizedNodeType.Terminal,
						TextValue = opt.Description
					});
					localizedStringElement5.CompactedNodes = array4;
					localizedStringElement.TagCodes = new int[1];
					localizedStrings.Touch(localizedStringElement);
				}
				FieldInfo optionGroupNameField = typeof(OptionsGroupUIMapper).GetField("optionsName", BindingFlags.Instance | BindingFlags.NonPublic);
				OptionUIMapper optionMapper = ScriptableObject.CreateInstance<OptionUIMapper>();
				optionMapper.name = gameOptionName;
				optionMapper.XmlSerializableName = gameOptionName;
				optionMapper.OptionFlags = OptionUIMapper.Flags.None;
				optionMapper.ControlType = optionVal.ControlType;
				optionMapper.Title = "%" + gameOptionName + "Title";
				optionMapper.Description = "%" + gameOptionName + "Description";
				optionMapper.Initialize();
				uiMappers.Touch(optionMapper);
				if (uiMappers.TryGetValue(new StaticString(optionVal.GroupKey), out var paceGroup))
				{
					OptionsGroupUIMapper optionGroup = (OptionsGroupUIMapper)paceGroup;
					string[] optionsName = (string[])optionGroupNameField.GetValue(optionGroup);
					optionsName = optionsName.Union(new string[1] { gameOptionName }).ToArray();
					optionGroupNameField.SetValue(optionGroup, optionsName);
					optionGroup.Initialize();
					uiMappers.Touch(optionGroup);
				}
			}
		}
	}
}
