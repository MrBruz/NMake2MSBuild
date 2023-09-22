using System;
using System.Text.RegularExpressions;

namespace Microsoft.DriverKit.NMakeConverter.Commands;

[Serializable]
internal class ELSEIFDEF : Conditional
{
	public ELSEIFDEF()
	{
		base.DirectiveType = DirectiveTypes.Elseifdef;
	}

	public override SourcesDirective ParseIfApplies(string nmakeLine, out bool waitingForMoreLines, ConditionBlock conditionBlock)
	{
		Regex conditionRegex = new Regex("^!\\s*elseifdef(?<Condition>(\\(|\\s+).*\\S)\\s*$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
		return Conditional.CreateInstance(conditionBlock, conditionRegex, nmakeLine, out waitingForMoreLines, DirectiveTypes.Elseifdef);
	}
}
