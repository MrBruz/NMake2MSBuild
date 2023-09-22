using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;

namespace Microsoft.DriverKit.NMakeConverter.PropertyPageSupport;

internal static class TargetCommandsParser
{
	public static bool IsTargetMarco(string macroName)
	{
		bool flag = true;
		string text;
		if ((text = macroName.ToLowerInvariant()) != null && text == "run_wpp")
		{
			flag = false;
		}
		return !flag;
	}

	public static bool ConvertMacro(string macroName, string commandText, Project projectFile, string condition = "")
	{
		ToolItem.elementsAddedToProject.Clear();
		if (!IsTargetMarco(macroName))
		{
			return false;
		}
		bool flag = true;
		string text;
		if ((text = macroName.ToLowerInvariant()) != null && text == "run_wpp")
		{
			flag = ParseWpp.AddTraceWppItem(commandText, projectFile, condition);
		}
		if (flag)
		{
			foreach (ProjectElement item in ToolItem.elementsAddedToProject)
			{
				item.Parent.RemoveChild(item);
			}
		}
		return !flag;
	}

	public static bool ConvertNmakeCommands(string[] commands, string targetInput, string targetOutput, Project projectFile)
	{
		ToolItem.elementsAddedToProject.Clear();
		bool flag = true;
		string text = null;
		string empty = string.Empty;
		string value = string.Empty;
		bool flag2 = false;
		Regex regex = new Regex("\\s*(\"[^\"]*\")|[^\\s]*", RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.CultureInvariant);
		if (targetOutput.EndsWith(".inf", StringComparison.OrdinalIgnoreCase))
		{
			int num = targetOutput.LastIndexOf("\\", StringComparison.OrdinalIgnoreCase);
			value = targetOutput.Substring(num + 1);
		}
		Logger.TraceEvent(TraceEventType.Verbose, null, "Target Input {0}", targetInput);
		Logger.TraceEvent(TraceEventType.Verbose, null, "Target Output {0}", targetOutput);
		foreach (string text2 in commands)
		{
			if (regex.IsMatch(text2))
			{
				Match match = regex.Match(text2);
				if (!string.IsNullOrEmpty(match.Value))
				{
					text = match.Value.Trim();
					empty = text2.Replace(text, "");
					if (text.EndsWith("mc", StringComparison.OrdinalIgnoreCase) || text.EndsWith("mc.exe", StringComparison.OrdinalIgnoreCase))
					{
						flag = ParseMc.AddMessageCompileItem(empty, projectFile, targetInput, targetOutput);
					}
					else if (text.EndsWith("stampinf", StringComparison.OrdinalIgnoreCase) || text.EndsWith("stampinf.exe", StringComparison.OrdinalIgnoreCase))
					{
						if (flag2 && !empty.Contains(value))
						{
							flag = true;
						}
						else if ((flag2 && empty.Contains(value)) || !flag2)
						{
							flag = ParseStampinf.AddStampInfItem(empty, targetInput, targetOutput, projectFile);
							flag2 = false;
						}
					}
					else if (text.EndsWith("ctrpp", StringComparison.OrdinalIgnoreCase) || text.EndsWith("ctrpp.exe", StringComparison.OrdinalIgnoreCase))
					{
						flag = ParseCtrpp.AddCtrppItem(empty, projectFile, targetInput, targetOutput);
					}
					else if (text.EndsWith("mofcomp", StringComparison.OrdinalIgnoreCase) || text.EndsWith("mofcomp.exe", StringComparison.OrdinalIgnoreCase))
					{
						flag = ParseMofcomp.AddMofcompItem(empty, projectFile, targetInput, targetOutput);
					}
					else if (text.EndsWith("wmimofck", StringComparison.OrdinalIgnoreCase) || text.EndsWith("wmimofck.exe", StringComparison.OrdinalIgnoreCase))
					{
						flag = ParseWmimofck.AddWmimofckItem(empty, projectFile, targetInput, targetOutput);
					}
					else if (text.EndsWith("copy", StringComparison.OrdinalIgnoreCase) || text.EndsWith("copy.exe", StringComparison.OrdinalIgnoreCase))
					{
						if (!string.IsNullOrEmpty(value))
						{
							if (empty.Contains(value))
							{
								flag2 = true;
								flag = false;
							}
							else
							{
								flag = true;
							}
						}
						else
						{
							flag = true;
						}
					}
					else
					{
						flag = true;
					}
				}
			}
			if (flag)
			{
				Logger.TraceEvent(TraceEventType.Verbose, null, "Didn't recognize the command: {0}", text2);
				foreach (ProjectElement item in ToolItem.elementsAddedToProject)
				{
					item.Parent.RemoveChild(item);
				}
				break;
			}
			Logger.TraceEvent(TraceEventType.Verbose, null, "Successfully recognized the command: {0}", text2);
		}
		return !flag;
	}
}
