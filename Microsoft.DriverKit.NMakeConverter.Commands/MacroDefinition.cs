#define TRACE
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Microsoft.DriverKit.NMakeConverter.Commands;

[Serializable]
internal class MacroDefinition : SourcesDirective
{
	public string Name { get; set; }

	public string Value { get; set; }

	public MacroDefinition()
	{
		base.DirectiveType = DirectiveTypes.MacroDefinition;
	}

	public override SourcesDirective ParseIfApplies(string nmakeLine, out bool waitingForMoreLines, ConditionBlock conditionBlock)
	{
		Regex regex = new Regex("^(?<Name>\\w+)\\s*=\\s*(?<Value>.*)$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
		Match match = regex.Match(nmakeLine);
		waitingForMoreLines = false;
		if (match.Success)
		{
			MacroDefinition macroDefinition = new MacroDefinition();
			macroDefinition.Name = CoerceMacroNameIfNeeded(match.Groups["Name"].Value.Trim());
			macroDefinition.Value = match.Groups["Value"].Value.TrimEnd(new char[0]);
			macroDefinition.ConditionBlock = conditionBlock;
			macroDefinition.DirectiveType = DirectiveTypes.MacroDefinition;
			Logger.TraceEvent(TraceEventType.Verbose, null, "\"{0}\" was parsed as a\"{1}\"", nmakeLine, macroDefinition.DirectiveType.ToString());
			return macroDefinition;
		}
		return null;
	}

	public static string CoerceMacroNameIfNeeded(string name)
	{
		if (Regex.IsMatch(name, "^\\d.*$"))
		{
			string text = "CoercedMacro_" + name;
			Trace.Indent();
			Logger.TraceEvent(TraceEventType.Verbose, null, "Macro {0} is being coerced to {1} since MSBuild does not support property names that begin with numbers.", name, text);
			Trace.Unindent();
			return text;
		}
		return name;
	}
}
