using System.Collections.Generic;
using Amplitude.Mercury.UI;

namespace HumankindModTool
{
	public class GameOptionInfo
	{
		public string Key { get; set; }

		public string GroupKey { get; set; }

		public string Title { get; set; }

		public string Description { get; set; }

		public string DefaultValue { get; set; }

		public UIControlType ControlType { get; set; }

		public List<GameOptionStateInfo> States { get; set; } = new List<GameOptionStateInfo>();

	}
}
