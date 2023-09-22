using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Microsoft.DriverKit.NMakeConverter.Commands;

[Serializable]
internal class IncludeFile : SourcesDirective
{
	public string NmakeFilePath { get; set; }

	public IncludeFile()
	{
		base.DirectiveType = DirectiveTypes.IncludeFile;
	}

	public override SourcesDirective ParseIfApplies(string nmakeLine, out bool waitingForMoreLines, ConditionBlock ConditionBlock)
	{
		Regex regex = new Regex("^!\\s*include\\s+(?<FilePath>.*\\S)\\s*$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
		Match match = regex.Match(nmakeLine);
		waitingForMoreLines = false;
		if (match.Success)
		{
			IncludeFile includeFile = new IncludeFile();
			includeFile.NmakeFilePath = StringUtilities.UnQuote(match.Groups["FilePath"].Value);
			includeFile.ConditionBlock = ConditionBlock;
			includeFile.DirectiveType = DirectiveTypes.IncludeFile;
			Logger.TraceEvent(TraceEventType.Verbose, null, "\"{0}\" was parsed as a\"{1}\"", nmakeLine, includeFile.DirectiveType.ToString());
			return includeFile;
		}
		return null;
	}
}
