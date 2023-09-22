using System;
using System.Text.RegularExpressions;

namespace Microsoft.DriverKit.NMakeConverter.Commands;

[Serializable]
internal class ELSEIF : Conditional
{
	public ELSEIF()
	{
		base.DirectiveType = DirectiveTypes.Elseif;
	}

	public override SourcesDirective ParseIfApplies(string nmakeLine, out bool waitingForMoreLines, ConditionBlock conditionBlock)
	{
		Regex conditionRegex = new Regex("^!\\s*else\\s*if(?<Condition>(\\(|\\s+).*\\S)\\s*$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
		return Conditional.CreateInstance(conditionBlock, conditionRegex, nmakeLine, out waitingForMoreLines, DirectiveTypes.Elseif);
	}
}
