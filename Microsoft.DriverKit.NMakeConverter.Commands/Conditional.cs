using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Microsoft.DriverKit.NMakeConverter.Commands;

[Serializable]
internal class Conditional : SourcesDirective
{
	public string MsBuildCondition { get; set; }

	public string nMakeCondition { get; set; }

	public static Conditional CreateInstance(ConditionBlock conditionBlock, Regex conditionRegex, string nMakeLine, out bool waitingForMoreLines, DirectiveTypes dt = DirectiveTypes.If)
	{
		Match match = conditionRegex.Match(nMakeLine);
		waitingForMoreLines = false;
		if (!match.Success)
		{
			return null;
		}
		string text = match.Groups["Condition"].Value;
		Conditional conditional = new Conditional();
		conditional.nMakeCondition = text;
		conditional.ConditionBlock = conditionBlock;
		switch (dt)
		{
		case DirectiveTypes.Ifdef:
			text = string.Format(CultureInfo.InvariantCulture, "Defined({0})", new object[1] { text });
			dt = DirectiveTypes.If;
			break;
		case DirectiveTypes.Ifndef:
			text = string.Format(CultureInfo.InvariantCulture, "!Defined({0})", new object[1] { text });
			dt = DirectiveTypes.If;
			break;
		case DirectiveTypes.Elseifdef:
			text = string.Format(CultureInfo.InvariantCulture, "Defined({0})", new object[1] { text });
			dt = DirectiveTypes.Elseif;
			break;
		case DirectiveTypes.Elseifndef:
			text = string.Format(CultureInfo.InvariantCulture, "!Defined({0})", new object[1] { text });
			dt = DirectiveTypes.Elseif;
			break;
		}
		if (!string.IsNullOrWhiteSpace(text))
		{
			conditional.MsBuildCondition = ConditionParser.ConvertToMSBuildSyntax(text);
		}
		else
		{
			conditional.MsBuildCondition = null;
		}
		conditional.DirectiveType = dt;
		Logger.TraceEvent(TraceEventType.Verbose, null, "\"{0}\" was parsed as a\"{1}\" with a condition of \"{2}\"", nMakeLine, conditional.DirectiveType.ToString(), text);
		return conditional;
	}

	public override SourcesDirective ParseIfApplies(string nmakeLine, out bool waitingForMoreLines, ConditionBlock conditionBlock)
	{
		waitingForMoreLines = false;
		return null;
	}
}
