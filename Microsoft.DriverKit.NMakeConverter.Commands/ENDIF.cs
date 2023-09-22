using System;
using System.Text.RegularExpressions;

namespace Microsoft.DriverKit.NMakeConverter.Commands;

[Serializable]
internal class ENDIF : Conditional
{
	public ENDIF()
	{
		base.DirectiveType = DirectiveTypes.Endif;
	}

	public override SourcesDirective ParseIfApplies(string nmakeLine, out bool waitingForMoreLines, ConditionBlock conditionBlock)
	{
		Regex conditionRegex = new Regex("^!\\s*endif\\s*$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
		return Conditional.CreateInstance(conditionBlock, conditionRegex, nmakeLine, out waitingForMoreLines, DirectiveTypes.Endif);
	}
}
