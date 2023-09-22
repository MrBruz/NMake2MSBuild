using System;
using System.Text.RegularExpressions;

namespace Microsoft.DriverKit.NMakeConverter.Commands;

[Serializable]
internal class IFDEF : Conditional
{
	public IFDEF()
	{
		base.DirectiveType = DirectiveTypes.Ifdef;
	}

	public override SourcesDirective ParseIfApplies(string nmakeLine, out bool waitingForMoreLines, ConditionBlock conditionBlock)
	{
		Regex conditionRegex = new Regex("^!\\s*ifdef(?<Condition>(\\(|\\s+).*\\S)\\s*$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
		return Conditional.CreateInstance(conditionBlock, conditionRegex, nmakeLine, out waitingForMoreLines, DirectiveTypes.Ifdef);
	}
}
