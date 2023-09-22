using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Build.Evaluation;

namespace Microsoft.DriverKit.NMakeConverter.PropertyPageSupport;

internal static class ParseCtrpp
{
	public static bool AddCtrppItem(string commandText, Project projectFile, string targetInput, string targetOutput)
	{
		bool result = true;
		_ = projectFile.Xml;
		Dictionary<string, string> itemMetadata = new Dictionary<string, string>();
		Dictionary<string, ToolSwitch> dictionary = new Dictionary<string, ToolSwitch>();
		SwitchInfo[] array = new SwitchInfo[11]
		{
			new SwitchInfo("prefix", ToolSwitchType.String, "AddPrefix"),
			new SwitchInfo("legacy", ToolSwitchType.Boolean, "EnableLegacy"),
			new SwitchInfo("ch", ToolSwitchType.File, "HeaderFileNameForCounter", "GenerateHeaderFileForCounter"),
			new SwitchInfo("o", ToolSwitchType.File, "HeaderFileNameForProvider", "GenerateHeaderFileForProvider"),
			new SwitchInfo("rc", ToolSwitchType.File, "ResourceFileName", "GenerateResourceSourceFile"),
			new SwitchInfo("backcompat", ToolSwitchType.Boolean, "BackwardCompatibility"),
			new SwitchInfo("MemoryRoutines", ToolSwitchType.Boolean, "GenerateMemoryRoutines"),
			new SwitchInfo("NotificationCallback", ToolSwitchType.Boolean, "GenerateNotificationCallback"),
			new SwitchInfo("sumPath", ToolSwitchType.String, "GeneratedCounterFilesPath"),
			new SwitchInfo("summary", ToolSwitchType.String, "GenerateSummaryGlobalFile"),
			new SwitchInfo("n", ToolSwitchType.Boolean, "Verbose")
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
		Regex regexExp = new Regex("\\s*(?<input>[^-/]*)*((-|/)(?<switchName>(prefix|legacy|ch|o|rc|backcompat|MemoryRoutines|NotificationCallback|sumPath|summary|n))(?<args>(\\s*(\"[^\"]*\")|[^-/]*)*))*", RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.CultureInvariant);
		result = ToolItem.ExtractItemInfoFromCommand(regexExp, dictionary, commandText, itemMetadata, out var itemIncludes);
		if (!result)
		{
			ToolItem.ApplyModifier("Ctrpp", itemIncludes, out var _, targetInput, targetOutput, projectFile);
			ToolItem.AddMetadataToItem("Ctrpp", itemIncludes, itemMetadata, projectFile);
			Logger.TraceEvent(TraceEventType.Verbose, null, "Successfully added Ctrpp item for : {0}", string.Join(";", itemIncludes));
		}
		return result;
	}
}
