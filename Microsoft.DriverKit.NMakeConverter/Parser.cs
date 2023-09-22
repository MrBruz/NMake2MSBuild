#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.DriverKit.NMakeConverter.Commands;

namespace Microsoft.DriverKit.NMakeConverter;

internal static class Parser
{
	public static string EOF = "@EOF@";

	private static List<SourcesDirective> ParserRules = null;

	private static SourcesDirective FallbackParser;

	public static string[] fileContents { get; set; }

	private static void SetParserRules()
	{
		Trace.Indent();
		ParserRules = new List<SourcesDirective>();
		ParserRules.Add(new DotDirective());
		ParserRules.Add(new MacroDefinition());
		ParserRules.Add(new IncludeFile());
		ParserRules.Add(new IF());
		ParserRules.Add(new IFDEF());
		ParserRules.Add(new IFNDEF());
		ParserRules.Add(new ELSEIF());
		ParserRules.Add(new ELSEIFDEF());
		ParserRules.Add(new ELSEIFNDEF());
		ParserRules.Add(new ELSE());
		ParserRules.Add(new ENDIF());
		ParserRules.Add(new UNDEF());
		ParserRules.Add(new InferenceRule());
		ParserRules.Add(new TargetDefinition());
		FallbackParser = new None();
		Trace.Unindent();
	}

	public static List<SourcesDirective> Parse(string file)
	{
		Trace.Indent();
		if (!File.Exists(file))
		{
			Logger.TraceEvent(TraceEventType.Critical, null, "The file to be parsed was not found: \"{0}\"", file);
			throw new FileNotFoundException("The file to be parsed was not found:", file);
		}
		fileContents = PreProcess(File.ReadAllText(file));
		Logger.TraceEvent(TraceEventType.Verbose, null, "File contents after preprocessing:\n{0}", string.Join(Environment.NewLine, fileContents));
		if (ParserRules == null)
		{
			SetParserRules();
		}
		List<SourcesDirective> list = new List<SourcesDirective>();
		bool waitingForMoreLines = false;
		SourcesDirective sourcesDirective = null;
		SourcesDirective sourcesDirective2 = FallbackParser;
		Trace.Indent();
		for (int i = 0; i < fileContents.Length; i++)
		{
			if (waitingForMoreLines)
			{
				sourcesDirective = sourcesDirective2.ParseIfApplies(fileContents[i], out waitingForMoreLines);
				if (sourcesDirective != null)
				{
					sourcesDirective.Origin = file;
					list.Add(sourcesDirective);
				}
				if (!waitingForMoreLines)
				{
					i--;
				}
				if (i == fileContents.Length - 1)
				{
					sourcesDirective = sourcesDirective2.ParseIfApplies(EOF, out waitingForMoreLines);
					if (sourcesDirective != null)
					{
						sourcesDirective.Origin = file;
						list.Add(sourcesDirective);
					}
				}
			}
			else
			{
				foreach (SourcesDirective parserRule in ParserRules)
				{
					sourcesDirective = parserRule.ParseIfApplies(fileContents[i], out waitingForMoreLines);
					if (sourcesDirective != null)
					{
						sourcesDirective.Origin = file;
						list.Add(sourcesDirective);
						break;
					}
					sourcesDirective2 = parserRule;
					if (!waitingForMoreLines)
					{
						continue;
					}
					if (i == fileContents.Length - 1)
					{
						sourcesDirective = sourcesDirective2.ParseIfApplies(EOF, out waitingForMoreLines);
						if (sourcesDirective != null)
						{
							sourcesDirective.Origin = file;
							list.Add(sourcesDirective);
						}
					}
					break;
				}
			}
			if (sourcesDirective == null && !waitingForMoreLines)
			{
				Logger.TraceEvent(TraceEventType.Warning, null, "\"{0}\" was not parsed.", fileContents[i]);
				list.Add(FallbackParser.ParseIfApplies(fileContents[i], out waitingForMoreLines));
			}
		}
		Trace.Unindent();
		SetConditionBlocksForEachDirective(list);
		Trace.Unindent();
		return list;
	}

	private static void SetConditionBlocksForEachDirective(List<SourcesDirective> ProcessedLines)
	{
		Stack<ConditionBlock> stack = new Stack<ConditionBlock>();
		foreach (SourcesDirective ProcessedLine in ProcessedLines)
		{
			switch (ProcessedLine.DirectiveType)
			{
			case DirectiveTypes.If:
				ProcessedLine.ConditionBlock = new ConditionBlock(((Conditional)ProcessedLine).MsBuildCondition);
				if (stack.Count > 0)
				{
					ProcessedLine.ConditionBlock.ParentConditionBlock = stack.Peek();
				}
				stack.Push(ProcessedLine.ConditionBlock);
				break;
			case DirectiveTypes.Endif:
				stack.Pop();
				break;
			case DirectiveTypes.Elseif:
			case DirectiveTypes.Else:
				ProcessedLine.ConditionBlock = new ConditionBlock(((Conditional)ProcessedLine).MsBuildCondition, negateParentCondition: true);
				ProcessedLine.ConditionBlock.ParentConditionBlock = stack.Pop();
				stack.Push(ProcessedLine.ConditionBlock);
				break;
			default:
				if (stack.Count > 0)
				{
					ProcessedLine.ConditionBlock = stack.Peek();
				}
				break;
			}
		}
	}

	private static string[] PreProcess(string input)
	{
		input = Regex.Replace(input, "\\n#.*", "", RegexOptions.Multiline);
		input = Regex.Replace(input, "#.*", "");
		input = Regex.Replace(input, "\\\\\\r?\\n(?=([^!]+))", " ", RegexOptions.Multiline);
		input = Regex.Replace(input, "\\\\\\r?\\n?$", Environment.NewLine, RegexOptions.Multiline);
		string[] referencedPropertyNames = GetReferencedPropertyNames(input);
		foreach (string text in referencedPropertyNames)
		{
			input = input.Replace("$(" + text + ")", "$(" + MacroDefinition.CoerceMacroNameIfNeeded(text) + ")");
		}
		input = Regex.Replace(input, "(?<preChar>[^\\w])\\$O(?<postChar>[^\\w])", "${preChar}$(O)${postChar}");
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		dictionary.Add("@", "%40");
		dictionary.Add("\\*\\*", "%2A%2A");
		dictionary.Add("\\*", "%2A");
		dictionary.Add("\\?", "%3F");
		dictionary.Add("<", "<");
		foreach (string key in dictionary.Keys)
		{
			input = Regex.Replace(input, "\\$" + key, "%24" + dictionary[key]);
			input = Regex.Replace(input, "\\$\\(" + key + "D\\)", string.Format(CultureInfo.InvariantCulture, "%24%28{0}D%29", new object[1] { dictionary[key] }));
			input = Regex.Replace(input, "\\$\\(" + key + "B\\)", string.Format(CultureInfo.InvariantCulture, "%24%28{0}B%29", new object[1] { dictionary[key] }));
			input = Regex.Replace(input, "\\$\\(" + key + "F\\)", string.Format(CultureInfo.InvariantCulture, "%24%28{0}F%29", new object[1] { dictionary[key] }));
			input = Regex.Replace(input, "\\$\\(" + key + "R\\)", string.Format(CultureInfo.InvariantCulture, "%24%28{0}R%29", new object[1] { dictionary[key] }));
		}
		List<string> list = new List<string>();
		string[] array = input.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
		foreach (string text2 in array)
		{
			if (!string.IsNullOrWhiteSpace(text2))
			{
				list.Add(text2);
			}
		}
		return list.ToArray();
	}

	public static string[] GetReferencedPropertyNames(string unevaluatedString)
	{
		if (string.IsNullOrWhiteSpace(unevaluatedString))
		{
			return new string[0];
		}
		MatchCollection matchCollection = Regex.Matches(unevaluatedString, "\\$\\((?<PropertyName>[0-9a-zA-Z_]+)\\)");
		List<string> list = new List<string>();
		foreach (Match item in matchCollection)
		{
			string value = item.Groups["PropertyName"].Value;
			if (!string.IsNullOrWhiteSpace(value) && !list.Contains<string>(value, StringComparer.OrdinalIgnoreCase))
			{
				list.Add(value);
			}
		}
		return list.ToArray();
	}

	public static bool PassesSanityChecks(List<SourcesDirective> parsedCommands)
	{
		int num = 0;
		for (int i = 0; i < parsedCommands.Count; i++)
		{
			switch (parsedCommands[i].DirectiveType)
			{
			case DirectiveTypes.If:
				num++;
				break;
			case DirectiveTypes.Endif:
				num--;
				if (num < 0)
				{
					return false;
				}
				break;
			case DirectiveTypes.Elseif:
			case DirectiveTypes.Else:
				if (num <= 0)
				{
					return false;
				}
				break;
			}
		}
		return num == 0;
	}
}
