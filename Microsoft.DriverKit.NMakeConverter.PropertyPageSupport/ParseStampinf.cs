using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;

namespace Microsoft.DriverKit.NMakeConverter.PropertyPageSupport;

internal static class ParseStampinf
{
	public static bool AddStampInfItem(string commandText, string targetInput, string targetOutput, Project projectFile)
	{
		bool result = true;
		_ = projectFile.Xml;
		Dictionary<string, string> itemMetadata = new Dictionary<string, string>();
		Dictionary<string, ToolSwitch> dictionary = new Dictionary<string, ToolSwitch>();
		SwitchInfo[] array = new SwitchInfo[9]
		{
			new SwitchInfo("c", ToolSwitchType.String, "CatalogFile"),
			new SwitchInfo("k", ToolSwitchType.String, "KmdfVersionNumber"),
			new SwitchInfo("v", ToolSwitchType.File, "TimeStamp", "SpecifyDriverVerDirectiveVersion"),
			new SwitchInfo("d", ToolSwitchType.File, "DateStamp", "SpecifyDriverVerDirectiveDate"),
			new SwitchInfo("a", ToolSwitchType.File, "Architecture", "SpecifyArchitecture"),
			new SwitchInfo("f", ToolSwitchType.Boolean, "Source"),
			new SwitchInfo("u", ToolSwitchType.String, "UmdfVersionNumber"),
			new SwitchInfo("s", ToolSwitchType.String, "DriverVersionSectionName"),
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
		Regex regexExp = new Regex("\\s*(?<input>[^-/]*)*((-|/)(?<switchName>(c|k|v|d|a|f|u|n|s))(?<args>(\\s*(\"[^\"]*\")|[^-/]*)*))*", RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.CultureInvariant);
		result = ToolItem.ExtractItemInfoFromCommand(regexExp, dictionary, commandText, itemMetadata, out var itemIncludes);
		if (!result)
		{
			ToolItem.AddMetadataToItem("Inf", itemIncludes, itemMetadata, projectFile, out var generatedItem, string.Empty, string.Empty, alwaysGenerateNewItem: true);
			string include = generatedItem.Include;
			generatedItem.AddMetadata("CopyOutput", include);
			generatedItem.Include = targetInput;
			ICollection<ProjectMetadataElement> metadata = generatedItem.Metadata;
			foreach (ProjectMetadataElement item in metadata)
			{
				if (item.Name == "Source")
				{
					((ProjectElementContainer)generatedItem).RemoveChild((ProjectElement)(object)item);
				}
			}
			Logger.TraceEvent(TraceEventType.Verbose, null, "Successfully added Inf item for : {0}", generatedItem.Include);
		}
		return result;
	}
}
