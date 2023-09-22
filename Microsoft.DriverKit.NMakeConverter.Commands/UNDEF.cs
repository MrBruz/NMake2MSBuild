using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Microsoft.DriverKit.NMakeConverter.Commands;

[Serializable]
internal class UNDEF : SourcesDirective
{
	public string Macro { get; set; }

	public UNDEF()
	{
		base.DirectiveType = DirectiveTypes.Undef;
	}

	public override SourcesDirective ParseIfApplies(string nmakeLine, out bool waitingForMoreLines, ConditionBlock conditionBlock)
	{
		Regex regex = new Regex("^!\\s*undef(?<Macro>(\\(|\\s+).*\\S)\\s*$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
		Match match = regex.Match(nmakeLine);
		waitingForMoreLines = false;
		if (match.Success)
		{
			UNDEF uNDEF = new UNDEF();
			uNDEF.Macro = match.Groups["Macro"].Value.Trim();
			uNDEF.ConditionBlock = conditionBlock;
			uNDEF.DirectiveType = DirectiveTypes.Undef;
			Logger.TraceEvent(TraceEventType.Verbose, null, "\"{0}\" was parsed as a\"{1}\"", nmakeLine, uNDEF.DirectiveType.ToString());
			return uNDEF;
		}
		return null;
	}
}
