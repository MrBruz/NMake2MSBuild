using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Microsoft.DriverKit.NMakeConverter.Commands;

[Serializable]
internal class DotDirective : SourcesDirective
{
	public string NmakeLine { get; set; }

	public DotDirective()
	{
		base.DirectiveType = DirectiveTypes.DotDirective;
	}

	public override SourcesDirective ParseIfApplies(string nmakeLine, out bool waitingForMoreLines, ConditionBlock conditionBlock)
	{
		Regex regex = new Regex("^\\.(?<Directive>IGNORE|PRECIOUS|SILENT|SUFFIXES)\\s?:.*", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
		Match match = regex.Match(nmakeLine);
		waitingForMoreLines = false;
		if (match.Success)
		{
			DotDirective dotDirective = new DotDirective();
			dotDirective.NmakeLine = nmakeLine;
			dotDirective.ConditionBlock = conditionBlock;
			dotDirective.DirectiveType = DirectiveTypes.DotDirective;
			Logger.TraceEvent(TraceEventType.Verbose, null, "\"{0}\" was parsed as a\"{1}\"", nmakeLine, dotDirective.DirectiveType.ToString());
			return dotDirective;
		}
		return null;
	}
}
