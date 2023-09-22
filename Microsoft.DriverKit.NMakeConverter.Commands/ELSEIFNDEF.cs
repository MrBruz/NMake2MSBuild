using System;
using System.Text.RegularExpressions;

namespace Microsoft.DriverKit.NMakeConverter.Commands;

[Serializable]
internal class ELSEIFNDEF : Conditional
{
	public ELSEIFNDEF()
	{
		base.DirectiveType = DirectiveTypes.Elseifndef;
	}

	public override SourcesDirective ParseIfApplies(string nmakeLine, out bool waitingForMoreLines, ConditionBlock conditionBlock)
	{
		Regex conditionRegex = new Regex("^!\\s*elseifndef(?<Condition>(\\(|\\s+).*\\S)\\s*$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
		return Conditional.CreateInstance(conditionBlock, conditionRegex, nmakeLine, out waitingForMoreLines, DirectiveTypes.Elseifndef);
	}
}
