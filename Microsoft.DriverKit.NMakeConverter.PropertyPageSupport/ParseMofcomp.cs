using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Build.Evaluation;

namespace Microsoft.DriverKit.NMakeConverter.PropertyPageSupport;

internal static class ParseMofcomp
{
	public static bool AddMofcompItem(string commandText, Project projectFile, string targetInput, string targetOutput)
	{
		bool result = true;
		_ = projectFile.Xml;
		Dictionary<string, string> itemMetadata = new Dictionary<string, string>();
		Dictionary<string, ToolSwitch> dictionary = new Dictionary<string, ToolSwitch>();
		SwitchInfo[] array = new SwitchInfo[15]
		{
			new SwitchInfo("AMENDMENT:", ToolSwitchType.String, "Amendment"),
			new SwitchInfo("A:", ToolSwitchType.String, "Authority"),
			new SwitchInfo("class:", ToolSwitchType.String, "MofClass"),
			new SwitchInfo("instance:", ToolSwitchType.String, "MofInstance"),
			new SwitchInfo("autorecover", ToolSwitchType.Boolean, "AutoRecover"),
			new SwitchInfo("WMI", ToolSwitchType.Boolean, "WmiSyntaxCheck"),
			new SwitchInfo("check", ToolSwitchType.Boolean, "SyntaxCheck"),
			new SwitchInfo("B:", ToolSwitchType.String, "CreateBinaryMofFile"),
			new SwitchInfo("N:", ToolSwitchType.String, "NamespacePath"),
			new SwitchInfo("P:", ToolSwitchType.String, "Password"),
			new SwitchInfo("ER:", ToolSwitchType.String, "ResourceName"),
			new SwitchInfo("L:", ToolSwitchType.String, "ResourceLocale"),
			new SwitchInfo("U:", ToolSwitchType.String, "UserName"),
			new SwitchInfo("MOF:", ToolSwitchType.File, "LanguageNeutralOutput"),
			new SwitchInfo("MFL:", ToolSwitchType.File, "LanguageSpecificOutput")
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
		Regex regexExp = new Regex("\\s*(?<input>[^-/]*)*((-|/)(?<switchName>(AMENDMENT:|autorecover|A:|check|class:|B:|P:|N:|ER:|L:|MOF:|MFL:|U:|WMI))(?<args>(\\s*(\"[^\"]*\")|[^-/]*)*))*", RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.CultureInvariant);
		result = ToolItem.ExtractItemInfoFromCommand(regexExp, dictionary, commandText, itemMetadata, out var itemIncludes);
		if (!result)
		{
			ToolItem.ApplyModifier("MofComp", itemIncludes, out var _, targetInput, targetOutput, projectFile);
			ToolItem.AddMetadataToItem("MofComp", itemIncludes, itemMetadata, projectFile);
			Logger.TraceEvent(TraceEventType.Verbose, null, "Successfully added MofComp item for : {0}", string.Join(";", itemIncludes));
		}
		return result;
	}
}
