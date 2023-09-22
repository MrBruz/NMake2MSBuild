#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.DriverKit.NMakeConverter.Commands;

namespace Microsoft.DriverKit.NMakeConverter;

internal static class TargetsParser
{
	private static RegexOptions ParserRegexOptions = RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture;

	private static Regex regFilePaths = new Regex("(\\{(?<Directories>.*)\\})?(?<FileName>.*)", ParserRegexOptions);

	private static Regex[] regStripCommandModifiers = new Regex[3]
	{
		new Regex("^(@)?(?<Command>.*)$", ParserRegexOptions),
		new Regex("^(-(0-9))?(?<Command>.*)$", ParserRegexOptions),
		new Regex("^(!)?(?<Command>.*)$", ParserRegexOptions)
	};

	public static bool IsTargetHeader(string nMakeLine)
	{
		GetTargetOutputsAndInputsStrings(nMakeLine, out var rawOutputsString, out var _);
		return !string.IsNullOrWhiteSpace(rawOutputsString);
	}

	public static void GetTargetOutputsAndInputsStrings(string nMakeLine, out string rawOutputsString, out string rawInputsString)
	{
		if (string.IsNullOrWhiteSpace(nMakeLine) || string.IsNullOrWhiteSpace(nMakeLine.Substring(0, 1)))
		{
			rawOutputsString = (rawInputsString = null);
			return;
		}
		int num;
		if (0 <= (num = nMakeLine.IndexOf("::", StringComparison.OrdinalIgnoreCase)))
		{
			rawOutputsString = nMakeLine.Substring(0, num);
			rawInputsString = nMakeLine.Substring(num + "::".Length);
		}
		int minAllowdMatchIdx = 0;
		while (0 <= (num = StringUtilities.FindFirstUnquotedIdxOf(":", nMakeLine, '"', minAllowdMatchIdx)))
		{
			if (Regex.IsMatch(nMakeLine.Substring(0, num), ".*\\s$") || Regex.IsMatch(nMakeLine.Substring(0, num), ".*\\S\\S+$"))
			{
				rawOutputsString = nMakeLine.Substring(0, num);
				rawInputsString = nMakeLine.Substring(num + ":".Length);
				return;
			}
			minAllowdMatchIdx = num + 1;
		}
		rawOutputsString = (rawInputsString = null);
	}

	public static string[] ExtractTargetOutputs(string nMakeLine)
	{
		GetTargetOutputsAndInputsStrings(nMakeLine, out var rawOutputsString, out var _);
		List<string> list = new List<string>();
		int num = 0;
		rawOutputsString = rawOutputsString.Trim();
		while (0 <= (num = StringUtilities.FindFirstUnquotedIdxOf(" ", rawOutputsString)))
		{
			list.Add(StringUtilities.UnQuote(rawOutputsString.Substring(0, num).Trim()));
			rawOutputsString = rawOutputsString.Substring(num).Trim();
			if (string.IsNullOrWhiteSpace(rawOutputsString))
			{
				break;
			}
		}
		if (!string.IsNullOrWhiteSpace(rawOutputsString))
		{
			list.Add(StringUtilities.UnQuote(rawOutputsString));
		}
		if (list.Count == 0)
		{
			list.Add(StringUtilities.UnQuote(rawOutputsString));
		}
		return list.ToArray();
	}

	public static List<string[]> ExtractTargetInputs(string rawInputsString)
	{
		List<string[]> list = new List<string[]>();
		if (string.IsNullOrEmpty(rawInputsString))
		{
			return list;
		}
		List<string> list2 = new List<string>();
		int num = 0;
		int minAllowdMatchIdx = 0;
		rawInputsString = rawInputsString.Trim();
		while (0 <= (num = StringUtilities.FindFirstUngroupedIdxOf(" ", rawInputsString, '{', '}', '"', minAllowdMatchIdx)))
		{
			if (StringUtilities.IsQuoted(num, rawInputsString))
			{
				minAllowdMatchIdx = num + 1;
				continue;
			}
			list2.Add(rawInputsString.Substring(0, num));
			rawInputsString = rawInputsString.Substring(num).Trim();
			if (string.IsNullOrWhiteSpace(rawInputsString))
			{
				break;
			}
		}
		if (!string.IsNullOrWhiteSpace(rawInputsString))
		{
			list2.Add(rawInputsString.Trim());
		}
		foreach (string item in list2)
		{
			List<string> list3 = new List<string>();
			Match match = regFilePaths.Match(item);
			string value = match.Groups["FileName"].Value;
			if (string.IsNullOrWhiteSpace(value))
			{
				continue;
			}
			string value2 = match.Groups["Directories"].Value;
			if (!string.IsNullOrWhiteSpace(value2))
			{
				string[] array = value2.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
				foreach (string text in array)
				{
					list3.Add(Path.Combine(StringUtilities.UnQuote(text.Trim()), StringUtilities.UnQuote(value.Trim())));
				}
			}
			else
			{
				list3.Add(StringUtilities.UnQuote(item.Trim()));
			}
			list.Add(list3.ToArray());
		}
		return list;
	}

	public static string StripCommandModifiers(string command)
	{
		Regex[] array = regStripCommandModifiers;
		foreach (Regex regex in array)
		{
			command = regex.Replace(command, "${Command}");
		}
		return command;
	}

	public static List<string> ApplyTargetInferenceRules(List<string[]> inputs, string[] outputs, TargetInferenceRules targetInferenceRules)
	{
		Trace.Indent();
		List<string> list = new List<string>();
		foreach (string path in outputs)
		{
			string extension = Path.GetExtension(path);
			if (string.IsNullOrWhiteSpace(extension))
			{
				continue;
			}
			extension = extension.Substring(1);
			foreach (InferenceRule rule in targetInferenceRules.Rules)
			{
				if (!rule.ToExtension.Equals(extension, StringComparison.OrdinalIgnoreCase) || (!string.IsNullOrWhiteSpace(rule.ToPath) && !ComparePaths(rule.ToPath, Path.GetDirectoryName(path))))
				{
					continue;
				}
				foreach (string[] input in inputs)
				{
					string extension2 = Path.GetExtension(input[0]);
					if (!string.IsNullOrWhiteSpace(extension2) && extension2.Substring(1).Equals(rule.FromExtension, StringComparison.OrdinalIgnoreCase) && (string.IsNullOrWhiteSpace(rule.FromPath) || ComparePaths(rule.FromPath, Path.GetDirectoryName(input[0]))))
					{
						list.AddRange(rule.Commands);
						Logger.TraceEvent(TraceEventType.Verbose, null, "Added commands to target based on inference rule {0}.{1}. Commands added:\n{2}", rule.FromExtension, rule.ToExtension, string.Join(Environment.NewLine, rule.Commands));
						return list;
					}
				}
			}
		}
		Trace.Unindent();
		return list;
	}

	public static bool ComparePaths(string basePath, string candidatePath)
	{
		if (string.IsNullOrWhiteSpace(basePath))
		{
			return true;
		}
		if (string.IsNullOrWhiteSpace(candidatePath))
		{
			return false;
		}
		return string.Equals(Path.GetFullPath(basePath).TrimEnd(new char[1] { '\\' }), Path.GetFullPath(candidatePath).TrimEnd(new char[1] { '\\' }), StringComparison.OrdinalIgnoreCase);
	}
}
