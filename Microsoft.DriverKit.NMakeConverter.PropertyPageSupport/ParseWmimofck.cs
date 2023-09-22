using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Build.Evaluation;

namespace Microsoft.DriverKit.NMakeConverter.PropertyPageSupport;

internal static class ParseWmimofck
{
	public static bool AddWmimofckItem(string commandText, Project projectFile, string targetInput, string targetOutput)
	{
		bool result = true;
		_ = projectFile.Xml;
		Dictionary<string, string> itemMetadata = new Dictionary<string, string>();
		Dictionary<string, ToolSwitch> dictionary = new Dictionary<string, ToolSwitch>();
		SwitchInfo[] array = new SwitchInfo[9]
		{
			new SwitchInfo("m", ToolSwitchType.Boolean, "GenerateStructureDefinitionsForMethodParameters"),
			new SwitchInfo("u", ToolSwitchType.Boolean, "GenerateStructureDefinitionsForDatablocks"),
			new SwitchInfo("w", ToolSwitchType.File, "HtmlOutputDirectory", "HtmlUIOutputDirectory"),
			new SwitchInfo("t", ToolSwitchType.File, "VBScriptTestOutputFile"),
			new SwitchInfo("c", ToolSwitchType.File, "SourceOutputFile"),
			new SwitchInfo("y", ToolSwitchType.File, "MofFile"),
			new SwitchInfo("z", ToolSwitchType.File, "MflFile"),
			new SwitchInfo("h", ToolSwitchType.File, "HeaderOutputFile"),
			new SwitchInfo("x", ToolSwitchType.File, "HexdumpOutputFile")
		};
		SwitchInfo[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			SwitchInfo switchInfo = array2[i];
			dictionary.Add(switchInfo.Flag, new ToolSwitch(switchInfo.Type, switchInfo.Metadata, switchInfo.AssociatedSwitch));
		}
		if (string.IsNullOrEmpty(commandText))
		{
			Logger.TraceEvent(TraceEventType.Verbose, null, "Command text is empty");
			return result;
		}
		Regex regexExp = new Regex("\\s*(?<input>[^-/]*)*((-|/)(?<switchName>(m|u|w|t|c|y|z|h|x))(?<args>(\\s*(\"[^\"]*\")|[^-/]*)*))*", RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.CultureInvariant);
		result = ToolItem.ExtractItemInfoFromCommand(regexExp, dictionary, commandText, itemMetadata, out var itemIncludes);
		if (!result)
		{
			ToolItem.ApplyModifier("Wmimofck", itemIncludes, out var _, targetInput, targetOutput, projectFile);
			ToolItem.AddMetadataToItem("Wmimofck", itemIncludes, itemMetadata, projectFile);
			Logger.TraceEvent(TraceEventType.Verbose, null, "Successfully added Wmimofck item for : {0}", string.Join(";", itemIncludes));
		}
		return result;
	}
}
