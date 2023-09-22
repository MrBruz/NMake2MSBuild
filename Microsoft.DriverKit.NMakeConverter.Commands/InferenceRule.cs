using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Microsoft.DriverKit.NMakeConverter.Commands;

[Serializable]
internal class InferenceRule : SourcesDirective
{
	public string NmakeDefinition;

	private Regex RegRuleHeader = new Regex("^(\\{(?<FromPath>[^\\}]*)\\})?\\.(?<FromExt>[^<>\\|\\\\/:\\*\\?]+)(\\{(?<ToPath>[^\\}]*)\\})?\\.(?<ToExt>[^<>\\|\\\\/:\\*\\?]+)(:|::)\\s*(;(?<Command>.*))?$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

	private Regex RegCommand = new Regex("^\\s+(?<Command>\\S.*)?$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

	private bool waitingForMoreInput;

	public string ToExtension { get; set; }

	public string FromExtension { get; set; }

	public string ToPath { get; set; }

	public string FromPath { get; set; }

	public List<string> Commands { get; set; }

	public InferenceRule()
	{
		base.DirectiveType = DirectiveTypes.InferenceRuleDefinition;
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
			if (RegRuleHeader.IsMatch(nmakeLine))
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
		if (RegRuleHeader.IsMatch(nmakeLine))
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
		InferenceRule inferenceRule = new InferenceRule();
		inferenceRule.ConditionBlock = base.ConditionBlock;
		inferenceRule.DirectiveType = DirectiveTypes.InferenceRuleDefinition;
		inferenceRule.ToExtension = ToExtension;
		inferenceRule.FromExtension = FromExtension;
		inferenceRule.ToPath = ToPath;
		inferenceRule.FromPath = FromPath;
		inferenceRule.NmakeDefinition = NmakeDefinition;
		inferenceRule.Commands = Commands;
		waitingForMoreInput = false;
		Logger.TraceEvent(TraceEventType.Verbose, null, "Concluded parsing multi-line directive type \"{0}\" from definition:\n {1}", inferenceRule.DirectiveType.ToString(), NmakeDefinition);
		return inferenceRule;
	}

	private void Reset()
	{
		Commands = null;
		base.ConditionBlock = null;
		waitingForMoreInput = false;
	}

	private void StartProcessingHeader(string nmakeLine, ConditionBlock conditionBlock)
	{
		Commands = new List<string>();
		waitingForMoreInput = true;
		NmakeDefinition = nmakeLine;
		base.ConditionBlock = conditionBlock;
		Match match = RegRuleHeader.Match(nmakeLine);
		ToExtension = match.Groups["ToExt"].Value.Trim();
		FromExtension = match.Groups["FromExt"].Value.Trim();
		ToPath = match.Groups["ToPath"].Value.Trim();
		FromPath = match.Groups["FromPath"].Value.Trim();
		string text = match.Groups["Command"].Value.Trim();
		if (!string.IsNullOrWhiteSpace(text))
		{
			Commands.Add(text);
		}
		Logger.TraceEvent(TraceEventType.Verbose, null, "\"{0}\" was parsed as start of directive type \"{1}\"", nmakeLine, DirectiveTypes.InferenceRuleDefinition.ToString());
	}
}
