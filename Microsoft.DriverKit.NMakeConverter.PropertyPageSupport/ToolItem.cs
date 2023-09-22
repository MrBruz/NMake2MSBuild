using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.DriverKit.MakeSolution;

namespace Microsoft.DriverKit.NMakeConverter.PropertyPageSupport;

internal static class ToolItem
{
	internal static List<ProjectElement> elementsAddedToProject = new List<ProjectElement>();

	public static bool ExtractItemInfoFromCommand(Regex regexExp, Dictionary<string, ToolSwitch> switchMap, string commandText, Dictionary<string, string> itemMetadata, out List<string> itemIncludes)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Expected O, but got Unknown
		ProjectRootElement.Create(new ProjectCollection());
		bool result = true;
		itemIncludes = new List<string>();
		if (regexExp.IsMatch(commandText))
		{
			Match match = regexExp.Match(commandText);
			if (match.Value == commandText)
			{
				result = false;
				if (!string.IsNullOrEmpty(match.Groups[regexExp.GroupNumberFromName("input")].Captures[0].Value))
				{
					string value = match.Groups[regexExp.GroupNumberFromName("input")].Captures[0].Value;
					value = value.Trim();
					Regex regex = new Regex("((?<args>((\"[^\"]*\")|[^\\s]*))\\s*)*", RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.CultureInvariant);
					Match match2 = regex.Match(value);
					for (int i = 0; i < match2.Groups[regex.GroupNumberFromName("args")].Captures.Count; i++)
					{
						string value2 = match2.Groups[regex.GroupNumberFromName("args")].Captures[i].Value;
						if (!string.IsNullOrEmpty(value2))
						{
							itemIncludes.Add(value2);
						}
					}
				}
				for (int j = 0; j < match.Groups[regexExp.GroupNumberFromName("switchName")].Captures.Count; j++)
				{
					string value3 = match.Groups[regexExp.GroupNumberFromName("switchName")].Captures[j].Value;
					string value4 = match.Groups[regexExp.GroupNumberFromName("args")].Captures[j].Value;
					if (string.IsNullOrEmpty(value3))
					{
						continue;
					}
					ToolSwitch value5 = null;
					string inputValue = null;
					if (!switchMap.TryGetValue(value3, out value5))
					{
						continue;
					}
					AddItemMetadata(itemMetadata, value5, value4, out inputValue);
					if (string.IsNullOrEmpty(inputValue))
					{
						continue;
					}
					inputValue = inputValue.Trim();
					Regex regex2 = new Regex("(\\s*(?<args>((\"[^\"]*\")|[^\\s]*))\\s*)*", RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.CultureInvariant);
					Match match3 = regex2.Match(inputValue);
					for (int k = 0; k < match3.Groups[regex2.GroupNumberFromName("args")].Captures.Count; k++)
					{
						string value6 = match3.Groups[regex2.GroupNumberFromName("args")].Captures[k].Value;
						if (!string.IsNullOrEmpty(value6))
						{
							itemIncludes.Add(value6);
						}
					}
				}
				return result;
			}
			return result;
		}
		return result;
	}

	public static void ApplyModifier(string itemName, List<string> itemIncludes, out List<string> inputsToItem, string targetInput, string targetOutput, Build.Evaluation.Project projectFile)
	{
		inputsToItem = new List<string>();
		string pattern = "%\\((" + itemName + "\\.)?Identity\\)";
		string text = "%\\((" + itemName + "\\.)?Filename\\)";
		string text2 = "%\\((" + itemName + "\\.)?Extension\\)";
		string text3 = "%\\((" + itemName + "\\.)?Directory\\)";
		string text4 = "%\\((" + itemName + "\\.)?RootDir\\)";
		Regex regex = new Regex(pattern, RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.CultureInvariant);
		Regex regex2 = new Regex(text, RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.CultureInvariant);
		Regex regex3 = new Regex(text + text2, RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.CultureInvariant);
		Regex regex4 = new Regex(text4 + text3, RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.CultureInvariant);
		Regex regex5 = new Regex(text4 + text3 + text, RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.CultureInvariant);
		foreach (string itemInclude in itemIncludes)
		{
			if (regex3.IsMatch(itemInclude))
			{
				inputsToItem.Add(Path.GetFileName(targetInput));
			}
			else if (regex2.IsMatch(itemInclude))
			{
				inputsToItem.Add(Path.GetFileNameWithoutExtension(targetInput));
			}
			else if (regex5.IsMatch(itemInclude))
			{
				string empty = string.Empty;
				try
				{
					string relativePath = MakeSolution.Project.GetRelativePath(projectFile.FullPath, targetInput);
					string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(targetInput);
					if (Path.IsPathRooted(relativePath))
					{
						throw new InvalidOperationException();
					}
					empty = string.Format(CultureInfo.InvariantCulture, "$(MSBuildProjectDirectory)\\{0}\\{1}", new object[2] { relativePath, fileNameWithoutExtension });
					if (!string.IsNullOrEmpty(empty))
					{
						inputsToItem.Add(empty);
					}
				}
				catch (Exception)
				{
				}
			}
			else if (regex4.IsMatch(itemInclude))
			{
				string empty2 = string.Empty;
				try
				{
					string relativePath2 = MakeSolution.Project.GetRelativePath(projectFile.FullPath, targetInput);
					if (Path.IsPathRooted(relativePath2))
					{
						throw new InvalidOperationException();
					}
					empty2 = string.Format(CultureInfo.InvariantCulture, "$(MSBuildProjectDirectory)\\{0}", new object[1] { relativePath2 });
					if (!string.IsNullOrEmpty(empty2))
					{
						inputsToItem.Add(empty2);
					}
				}
				catch (Exception)
				{
				}
			}
			else if (regex.IsMatch(itemInclude))
			{
				inputsToItem.Add(targetInput);
			}
			else
			{
				inputsToItem.Add(itemInclude);
			}
		}
	}

	public static void AddMetadataToItem(string itemName, List<string> inputsToItem, Dictionary<string, string> itemMetadata, Build.Evaluation.Project projectFile, string itemCondition = "", string metadataCondition = "", bool alwaysGenerateNewItem = false)
	{
		AddMetadataToItem(itemName, inputsToItem, itemMetadata, projectFile, out var _, itemCondition, metadataCondition, alwaysGenerateNewItem);
	}

	public static void AddMetadataToItem(string itemName, List<string> inputsToItem, Dictionary<string, string> itemMetadata, Build.Evaluation.Project projectFile, out ProjectItemElement generatedItem, string itemCondition, string metadataCondition, bool alwaysGenerateNewItem)
	{
		generatedItem = null;
		if (inputsToItem.Count == 0)
		{
			return;
		}
		ProjectRootElement xml = projectFile.Xml;
		string text = "";
		foreach (string item in inputsToItem)
		{
			string text2 = StringUtilities.UnQuote(item);
			text = ((!string.IsNullOrEmpty(text)) ? (text + "; " + text2) : text2);
		}
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		ProjectItemGroupElement val = null;
		foreach (ProjectItemGroupElement itemGroup in xml.ItemGroups)
		{
			if (((ProjectElement)itemGroup).Label != null && ((ProjectElement)itemGroup).Label.Equals("WrappedTaskItems", StringComparison.OrdinalIgnoreCase))
			{
				val = itemGroup;
				break;
			}
		}
		if (val == null)
		{
			Logger.TraceEvent(TraceEventType.Error, null, "Did not find ItemGroup with the label \"WrappedTaskItems\" in the converted project. Don't know where to add generated items");
			return;
		}
		List<ProjectItemElement> list = (from i in projectFile.ItemsIgnoringCondition
										 where i.ItemType.Equals(itemName, StringComparison.OrdinalIgnoreCase)
										 where ((ProjectElement)i.Xml).Condition.Equals(itemCondition, StringComparison.OrdinalIgnoreCase)
										 select i.Xml).Distinct().ToList();
		if (!alwaysGenerateNewItem && list.Count > 0)
		{
			foreach (ProjectItemElement item2 in list)
			{
				foreach (string key in itemMetadata.Keys)
				{
					ProjectMetadataElement val2 = item2.AddMetadata(key, itemMetadata[key]);
					((ProjectElement)val2).Condition = metadataCondition;
					elementsAddedToProject.Add((ProjectElement)(object)val2);
				}
			}
			generatedItem = null;
			return;
		}
		ProjectItemElement val3 = val.AddItem(itemName, text);
		((ProjectElement)val3).Condition = itemCondition;
		foreach (string key2 in itemMetadata.Keys)
		{
			((ProjectElement)val3.AddMetadata(key2, itemMetadata[key2])).Condition = metadataCondition;
		}
		generatedItem = val3;
		elementsAddedToProject.Add((ProjectElement)(object)val3);
	}

	private static void AddItemMetadata(Dictionary<string, string> itemMetadata, ToolSwitch toolSwitch, string switchArgs, out string inputValue)
	{
		Regex regex = new Regex("(\\s*(?<args>((\"[^\"]*\")|[^\\s]*))\\s*)*", RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.CultureInvariant);
		Match match = regex.Match(switchArgs);
		inputValue = string.Empty;
		if (match.Groups[regex.GroupNumberFromName("args")].Captures.Count > 0)
		{
			switchArgs = match.Groups[regex.GroupNumberFromName("args")].Captures[0].Value;
		}
		for (int i = 1; i < match.Groups[regex.GroupNumberFromName("args")].Captures.Count; i++)
		{
			string value = match.Groups[regex.GroupNumberFromName("args")].Captures[i].Value;
			if (!string.IsNullOrEmpty(value))
			{
				inputValue = inputValue + " " + value;
			}
		}
		switch (toolSwitch.Type)
		{
			case ToolSwitchType.Boolean:
				if (!itemMetadata.ContainsKey(toolSwitch.Name))
				{
					itemMetadata.Add(toolSwitch.Name, "true");
				}
				if (!string.IsNullOrEmpty(switchArgs))
				{
					inputValue = inputValue + " " + switchArgs;
				}
				break;
			case ToolSwitchType.String:
				if (!itemMetadata.ContainsKey(toolSwitch.Name))
				{
					itemMetadata.Add(toolSwitch.Name, switchArgs);
				}
				break;
			case ToolSwitchType.StringArray:
				{
					string text = switchArgs;
					if (itemMetadata.ContainsKey(toolSwitch.Name))
					{
						string value2 = string.Empty;
						string empty = string.Empty;
						itemMetadata.TryGetValue(toolSwitch.Name, out value2);
						empty = value2 + ";" + text;
						itemMetadata.Remove(toolSwitch.Name);
						itemMetadata.Add(toolSwitch.Name, empty);
					}
					else
					{
						itemMetadata.Add(toolSwitch.Name, text);
					}
					break;
				}
			case ToolSwitchType.Integer:
				if (!itemMetadata.ContainsKey(toolSwitch.Name))
				{
					itemMetadata.Add(toolSwitch.Name, switchArgs);
				}
				break;
			case ToolSwitchType.File:
				if (!itemMetadata.ContainsKey(toolSwitch.Name))
				{
					itemMetadata.Add(toolSwitch.Name, switchArgs);
				}
				break;
			case ToolSwitchType.Directory:
				if (!itemMetadata.ContainsKey(toolSwitch.Name))
				{
					itemMetadata.Add(toolSwitch.Name, switchArgs);
				}
				break;
			case ToolSwitchType.ITaskItem:
				if (!itemMetadata.ContainsKey(toolSwitch.Name))
				{
					itemMetadata.Add(toolSwitch.Name, switchArgs);
				}
				break;
			case ToolSwitchType.ITaskItemArray:
				if (!itemMetadata.ContainsKey(toolSwitch.Name))
				{
					itemMetadata.Add(toolSwitch.Name, switchArgs);
				}
				break;
			case ToolSwitchType.AlwaysAppend:
				if (!itemMetadata.ContainsKey(toolSwitch.Name))
				{
					itemMetadata.Add(toolSwitch.Name, switchArgs);
				}
				break;
		}
		if (!string.IsNullOrEmpty(toolSwitch.AssociatedSwitch) && !itemMetadata.ContainsKey(toolSwitch.AssociatedSwitch))
		{
			itemMetadata.Add(toolSwitch.AssociatedSwitch, "true");
		}
	}
}
