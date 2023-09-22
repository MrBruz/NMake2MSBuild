using System;
using System.Diagnostics;

namespace Microsoft.DriverKit.NMakeConverter.Commands;

[Serializable]
internal class None : SourcesDirective
{
	public string NmakeLine;

	public None()
	{
		base.DirectiveType = DirectiveTypes.None;
	}

	public override SourcesDirective ParseIfApplies(string nmakeLine, out bool waitingForMoreLines, ConditionBlock conditionBlock)
	{
		None none = new None();
		none.NmakeLine = nmakeLine;
		none.ConditionBlock = conditionBlock;
		none.DirectiveType = DirectiveTypes.None;
		waitingForMoreLines = false;
		Logger.TraceEvent(TraceEventType.Verbose, null, "\"{0}\" was parsed as a\"{1}\"", nmakeLine, none.DirectiveType.ToString());
		return none;
	}
}
