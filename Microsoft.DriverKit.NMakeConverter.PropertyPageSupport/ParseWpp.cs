using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Evaluation;

namespace Microsoft.DriverKit.NMakeConverter.PropertyPageSupport;

internal static class ParseWpp
{
	public static bool AddTraceWppItem(string commandText, Project projectFile, string metadataCondition = "")
	{
		bool result = true;
		_ = projectFile.Xml;
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		SwitchInfo[] array = new SwitchInfo[18]
		{
			new SwitchInfo("p:", ToolSwitchType.String, "WppModuleName"),
			new SwitchInfo("ctl:", ToolSwitchType.String, "WppAddControlGuid"),
			new SwitchInfo("ini:", ToolSwitchType.String, "WppAdditionalConfigurationFile"),
			new SwitchInfo("I", ToolSwitchType.StringArray, "WppAdditionalIncludeDirectories"),
			new SwitchInfo("defwpp:", ToolSwitchType.File, "WppAlternateConfigurationFile"),
			new SwitchInfo("cfgdir:", ToolSwitchType.StringArray, "WppConfigurationDirectories"),
			new SwitchInfo("dll", ToolSwitchType.Boolean, "WppDllMacro"),
			new SwitchInfo("odir:", ToolSwitchType.String, "WppOutputDirectory", "WppEnableOutputDirectory"),
			new SwitchInfo("ext:", ToolSwitchType.StringArray, "WppFileExtensions"),
			new SwitchInfo("noshrieks", ToolSwitchType.Boolean, "WppIgnoreExclamationmarks"),
			new SwitchInfo("km", ToolSwitchType.Boolean, "WppKernelMode"),
			new SwitchInfo("argbase:", ToolSwitchType.Integer, "WppNumericBaseForFormatStrings"),
			new SwitchInfo("D", ToolSwitchType.StringArray, "WppPreprocessorDefinitions"),
			new SwitchInfo("preserveext:", ToolSwitchType.StringArray, "WppPreserveExtensions"),
			new SwitchInfo("scan:", ToolSwitchType.String, "WppScanConfigurationData"),
			new SwitchInfo("lookfor:", ToolSwitchType.String, "WppSearchString"),
			new SwitchInfo("func:", ToolSwitchType.StringArray, "WppTraceFunction"),
			new SwitchInfo("gen:", ToolSwitchType.StringArray, "WppGenerateUsingTemplateFile")
		};
		Dictionary<string, ToolSwitch> dictionary2 = new Dictionary<string, ToolSwitch>();
		SwitchInfo[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			SwitchInfo switchInfo = array2[i];
			dictionary2.Add(switchInfo.Flag, new ToolSwitch(switchInfo.Type, switchInfo.Metadata, switchInfo.AssociatedSwitch));
		}
		if (string.IsNullOrEmpty(commandText))
		{
			Logger.TraceEvent(TraceEventType.Verbose, null, "Command text is empty");
			return result;
		}
		Regex regexExp = new Regex("\\s*(?<input>[^-/]*)*((-|/)(?<switchName>(km|preserveext:|p:|defwpp:|dll|ini:|ctl:|I|D|odir:|ext:|noshrieks|argbase:|scan:|lookfor:|func:|gen:))(?<args>(\\s*(\"[^\"]*\")|(\\w[^\\s]*\\s*)|(\\{[^\\{]*\\})|[^-/]*)*))*", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.CultureInvariant);
		dictionary.Add("WppEnabled", "true");
		result = ToolItem.ExtractItemInfoFromCommand(regexExp, dictionary2, commandText, dictionary, out var itemIncludes);
		if (!result)
		{
			List<string> list = itemIncludes.Where((string include) => !include.EndsWith(".c", StringComparison.OrdinalIgnoreCase) && !include.EndsWith(".cpp", StringComparison.OrdinalIgnoreCase) && !include.EndsWith(".cxx", StringComparison.OrdinalIgnoreCase)).ToList();
			itemIncludes = itemIncludes.Except(list).ToList();
			ToolItem.AddMetadataToItem("ClCompile", itemIncludes, dictionary, projectFile, string.Empty, metadataCondition);
			ToolItem.AddMetadataToItem("OtherWpp", list, dictionary, projectFile, string.Empty, metadataCondition);
			if (itemIncludes.Count > 0)
			{
				Logger.TraceEvent(TraceEventType.Verbose, null, "Successfully added ClCompile item for : {0}", string.Join(";", itemIncludes));
			}
			if (list.Count > 0)
			{
				Logger.TraceEvent(TraceEventType.Verbose, null, "Successfully added OtherWpp item for: {0}", string.Join(";", list));
			}
		}
		return result;
	}
}
