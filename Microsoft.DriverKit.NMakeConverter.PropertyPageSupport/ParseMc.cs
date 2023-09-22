using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Build.Evaluation;

namespace Microsoft.DriverKit.NMakeConverter.PropertyPageSupport;

internal static class ParseMc
{
	public static bool AddMessageCompileItem(string commandText, Project projectFile, string targetInput, string targetOutput)
	{
		bool result = true;
		_ = projectFile.Xml;
		Dictionary<string, string> itemMetadata = new Dictionary<string, string>();
		Dictionary<string, ToolSwitch> dictionary = new Dictionary<string, ToolSwitch>();
		SwitchInfo[] array = new SwitchInfo[26]
		{
			new SwitchInfo("a", ToolSwitchType.Boolean, "AnsiInputFile"),
			new SwitchInfo("A", ToolSwitchType.Boolean, "AnsiMessageInBinFile"),
			new SwitchInfo("t", ToolSwitchType.File, "BaselinePath", "ValidateAgainstBaselineResource"),
			new SwitchInfo("s", ToolSwitchType.File, "BaselineResourcePath", "GenerateBaselineResource"),
			new SwitchInfo("x", ToolSwitchType.File, "DebugOutputPath", "EnableDebugOutputPath"),
			new SwitchInfo("co", ToolSwitchType.Boolean, "EnableCalloutMacro"),
			new SwitchInfo("cs", ToolSwitchType.String, "GenerateCSharpLoggingClass"),
			new SwitchInfo("css", ToolSwitchType.String, "GenerateStaticCSharpLoggingClass"),
			new SwitchInfo("z", ToolSwitchType.String, "GeneratedFilesBaseName"),
			new SwitchInfo("h", ToolSwitchType.File, "HeaderFilePath", "GeneratedHeaderPath"),
			new SwitchInfo("r", ToolSwitchType.File, "RCFilePath", "GeneratedRCAndMessagesPath"),
			new SwitchInfo("km", ToolSwitchType.Boolean, "GenerateKernelModeLoggingMacros"),
			new SwitchInfo("mof", ToolSwitchType.Boolean, "GenerateMofFile"),
			new SwitchInfo("o", ToolSwitchType.Boolean, "GenerateOle2Header"),
			new SwitchInfo("um", ToolSwitchType.String, "GenerateUserModeLoggingMacros"),
			new SwitchInfo("e", ToolSwitchType.String, "HeaderExtension"),
			new SwitchInfo("m", ToolSwitchType.Integer, "MaximumMessageLength"),
			new SwitchInfo("P", ToolSwitchType.String, "RemoveCharsFromSymbolName"),
			new SwitchInfo("p", ToolSwitchType.String, "PrefixMacroName"),
			new SwitchInfo("c", ToolSwitchType.Boolean, "SetCustomerbit"),
			new SwitchInfo("n", ToolSwitchType.Boolean, "TerminateMessageWithNull"),
			new SwitchInfo("U", ToolSwitchType.Boolean, "UnicodeMessageInBinFile"),
			new SwitchInfo("u", ToolSwitchType.Boolean, "UnicodeInputFile"),
			new SwitchInfo("b", ToolSwitchType.Boolean, "UseBaseNameOfInput"),
			new SwitchInfo("d", ToolSwitchType.Boolean, "UseDecimalValues"),
			new SwitchInfo("v", ToolSwitchType.Boolean, "Verbose")
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
		Regex regexExp = new Regex("\\s*(?<input>[^-/]*)*((-|/)(?<switchName>(a|A|t|s|x|co|css|cs|z|h|r|km|mof|o|um|e|m|p|P|c|n|U|u|b|d|v))(?<args>(\\s*(\"[^\"]*\")|[^-/]*)*))*", RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.CultureInvariant);
		result = ToolItem.ExtractItemInfoFromCommand(regexExp, dictionary, commandText, itemMetadata, out var itemIncludes);
		if (!result)
		{
			ToolItem.ApplyModifier("MessageCompile", itemIncludes, out var inputsToItem, targetInput, targetOutput, projectFile);
			ToolItem.AddMetadataToItem("MessageCompile", inputsToItem, itemMetadata, projectFile);
			Logger.TraceEvent(TraceEventType.Verbose, null, "Successfully added MessageCompile item for : {0}", string.Join(";", itemIncludes));
		}
		return result;
	}
}
