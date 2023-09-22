using System;
using System.Text.RegularExpressions;

namespace Microsoft.DriverKit.NMakeConverter.Commands;

[Serializable]
public abstract class SourcesDirective
{
	public const RegexOptions ParserRegexOptions = RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture;

	public string Origin { get; set; }

	public ConditionBlock ConditionBlock { get; set; }

	public DirectiveTypes DirectiveType { get; set; }

	public SourcesDirective ParseIfApplies(string nmakeLine, out bool waitingForMoreLines)
	{
		return ParseIfApplies(nmakeLine, out waitingForMoreLines, null);
	}

	public abstract SourcesDirective ParseIfApplies(string nmakeLine, out bool waitingForMoreLines, ConditionBlock conditionBlock);
}
