namespace Microsoft.DriverKit.NMakeConverter.PropertyPageSupport;

internal class ToolSwitch
{
	private string name = string.Empty;

	private ToolSwitchType type;

	private string relatedSwitch = string.Empty;

	internal string AssociatedSwitch
	{
		get
		{
			return relatedSwitch;
		}
		set
		{
			relatedSwitch = value;
		}
	}

	internal string Name
	{
		get
		{
			return name;
		}
		set
		{
			name = value;
		}
	}

	internal ToolSwitchType Type
	{
		get
		{
			return type;
		}
		set
		{
			type = value;
		}
	}

	internal ToolSwitch()
	{
	}

	internal ToolSwitch(ToolSwitchType toolType, string switchName, string associatedSwitch = null)
	{
		type = toolType;
		name = switchName;
		relatedSwitch = associatedSwitch;
	}
}
