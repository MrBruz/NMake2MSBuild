using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Microsoft.DriverKit.NMakeConverter.Commands;

[Serializable]
internal class TargetDefinition : SourcesDirective
{
	private bool waitingForMoreInput;

	private Regex RegCommand = new Regex("^\\s+(?<Command>\\S.*)?$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

	public List<string[]> Inputs { get; set; }

	public string[] Outputs { get; set; }

	public List<string> Commands { get; set; }

	public string NmakeDefinition { get; set; }

	public string NmakeTargetHeader { get; set; }

	public TargetDefinition()
	{
		base.DirectiveType = DirectiveTypes.TargetDefinition;
	}

	public override SourcesDirective ParseIfApplies(string nmakeLine, out bool waitingForMoreLines, ConditionBlock conditionBlock = null)
	{
		if (waitingForMoreInput)
		{
			if (nmakeLine.Equals(Parser.EOF))
			{
				waitingForMoreLines = false;
				return FinishProcesssing();
			}
			if (TargetsParser.IsTargetHeader(nmakeLine))
			{
				SourcesDirective result = FinishProcesssing();
				Reset();
				StartProcessingHeader(nmakeLine, conditionBlock);
				waitingForMoreLines = waitingForMoreInput;
				return result;
			}
			if (string.IsNullOrWhiteSpace(nmakeLine))
			{
				waitingForMoreLines = (waitingForMoreInput = true);
				NmakeDefinition = NmakeDefinition + Environment.NewLine + nmakeLine;
				return null;
			}
			if (RegCommand.IsMatch(nmakeLine))
			{
				Match match = RegCommand.Match(nmakeLine);
				Commands.Add(match.Groups["Command"].Value.Trim());
				NmakeDefinition = NmakeDefinition + Environment.NewLine + nmakeLine;
				waitingForMoreLines = (waitingForMoreInput = true);
				return null;
			}
			waitingForMoreLines = (waitingForMoreInput = false);
			SourcesDirective result2 = FinishProcesssing();
			Reset();
			return result2;
		}
		if (TargetsParser.IsTargetHeader(nmakeLine))
		{
			Reset();
			StartProcessingHeader(nmakeLine, conditionBlock);
			waitingForMoreLines = waitingForMoreInput;
			return null;
		}
		waitingForMoreLines = false;
		return null;
	}

	private SourcesDirective FinishProcesssing()
	{
		TargetDefinition targetDefinition = new TargetDefinition();
		targetDefinition.ConditionBlock = base.ConditionBlock;
		targetDefinition.DirectiveType = DirectiveTypes.TargetDefinition;
		targetDefinition.Inputs = Inputs;
		targetDefinition.Outputs = Outputs;
		targetDefinition.NmakeDefinition = NmakeDefinition;
		targetDefinition.NmakeTargetHeader = NmakeTargetHeader;
		targetDefinition.Commands = Commands;
		waitingForMoreInput = false;
		Logger.TraceEvent(TraceEventType.Verbose, null, "Concluded parsing multi-line directive type \"{0}\" from definition:\n {1}", targetDefinition.DirectiveType.ToString(), NmakeDefinition);
		return targetDefinition;
	}

	private void Reset()
	{
		Inputs = null;
		Outputs = null;
		Commands = null;
		base.ConditionBlock = null;
		waitingForMoreInput = false;
	}

	private void StartProcessingHeader(string nmakeLine, ConditionBlock conditionBlock)
	{
		Commands = new List<string>();
		waitingForMoreInput = true;
		base.ConditionBlock = conditionBlock;
		NmakeDefinition = nmakeLine;
		NmakeTargetHeader = nmakeLine;
		TargetsParser.GetTargetOutputsAndInputsStrings(nmakeLine, out var _, out var rawInputsString);
		Commands = new List<string>();
		int num = StringUtilities.FindFirstUngroupedIdxOf(";", rawInputsString, '{', '}');
		if (num >= 0)
		{
			Inputs = TargetsParser.ExtractTargetInputs(rawInputsString.Substring(0, num));
			string text = rawInputsString.Substring(num);
			if (!string.IsNullOrWhiteSpace(text))
			{
				Commands.Add(text);
			}
		}
		else
		{
			Inputs = TargetsParser.ExtractTargetInputs(rawInputsString);
		}
		Outputs = TargetsParser.ExtractTargetOutputs(nmakeLine);
		Logger.TraceEvent(TraceEventType.Verbose, null, "\"{0}\" was parsed as start of directive type \"{1}\"", nmakeLine, DirectiveTypes.TargetDefinition.ToString());
	}
}
