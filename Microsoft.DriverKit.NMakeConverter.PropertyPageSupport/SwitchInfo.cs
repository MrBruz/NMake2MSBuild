namespace Microsoft.DriverKit.NMakeConverter.PropertyPageSupport;

internal struct SwitchInfo
{
	private string m_Flag;

	private ToolSwitchType m_Type;

	private string m_Metadata;

	private string m_AssociatedSwitch;

	internal string Flag
	{
		get
		{
			return m_Flag;
		}
		set
		{
			m_Flag = value;
		}
	}

	internal ToolSwitchType Type
	{
		get
		{
			return m_Type;
		}
		set
		{
			m_Type = value;
		}
	}

	internal string Metadata
	{
		get
		{
			return m_Metadata;
		}
		set
		{
			m_Metadata = value;
		}
	}

	internal string AssociatedSwitch
	{
		get
		{
			return m_AssociatedSwitch;
		}
		set
		{
			m_AssociatedSwitch = value;
		}
	}

	internal SwitchInfo(string flag, ToolSwitchType type, string metadata, string associatedSwitch = null)
	{
		m_Flag = flag;
		m_Type = type;
		m_Metadata = metadata;
		m_AssociatedSwitch = associatedSwitch;
	}
}
