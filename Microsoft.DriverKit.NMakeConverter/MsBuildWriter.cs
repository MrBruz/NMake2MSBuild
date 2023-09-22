#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.DriverKit.NMakeConverter.Commands;

namespace Microsoft.DriverKit.NMakeConverter;

internal class MsBuildWriter
{
	private ProjectPropertyGroupElement currentPropertyGroup;

	private Stack<ProjectElementContainer> parentElements = new Stack<ProjectElementContainer>();

	private Project project;

	private static TargetInferenceRules targetInferenceRules = new TargetInferenceRules();

	private static List<InvokedTarget> invokedTargetsList = new List<InvokedTarget>();

	private static TargetConverter targetConverter = new TargetConverter();

	private static List<DeferredTarget> targetsToConvert = new List<DeferredTarget>();

	public void Reset()
	{
		targetInferenceRules = new TargetInferenceRules();
		invokedTargetsList = new List<InvokedTarget>();
		targetsToConvert = new List<DeferredTarget>();
	}

	public void FinalizeProject(string sourcesPropsFile)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		if (targetsToConvert.Count > 0)
		{
			ProjectRootElement val = ProjectRootElement.Open(sourcesPropsFile, new ProjectCollection());
			targetConverter.ConvertDeferedTargets(targetsToConvert, invokedTargetsList, targetInferenceRules, val);
			val.Save();
		}
		Reset();
	}

	public void GenerateMsBuildFile(string msbuildFile, ref List<SourcesDirective> parsedCommands)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Expected O, but got Unknown
		Trace.Indent();
		project = new Project(new ProjectCollection());
		parsedCommands = RefactorAnyConditionalIncludeCommand(ref parsedCommands);
		parentElements.Push((ProjectElementContainer)(object)project.Xml);
		DirectiveTypes lastCommandType = DirectiveTypes.None;
		for (int i = 0; i < parsedCommands.Count; i++)
		{
			SourcesDirective sourcesDirective = parsedCommands[i];
			try
			{
				switch (sourcesDirective.DirectiveType)
				{
					case DirectiveTypes.If:
						{
							ProjectChooseElement val7 = project.Xml.CreateChooseElement();
							parentElements.Peek().AppendChild((ProjectElement)(object)val7);
							parentElements.Push((ProjectElementContainer)(object)val7);
							ProjectWhenElement val5 = project.Xml.CreateWhenElement((sourcesDirective as Conditional).MsBuildCondition);
							parentElements.Peek().AppendChild((ProjectElement)(object)val5);
							parentElements.Push((ProjectElementContainer)(object)val5);
							break;
						}
					case DirectiveTypes.Else:
						{
							parentElements.Pop();
							ProjectOtherwiseElement val6 = project.Xml.CreateOtherwiseElement();
							parentElements.Peek().AppendChild((ProjectElement)(object)val6);
							parentElements.Push((ProjectElementContainer)(object)val6);
							break;
						}
					case DirectiveTypes.Elseif:
						{
							parentElements.Pop();
							ProjectWhenElement val5 = project.Xml.CreateWhenElement((sourcesDirective as Conditional).MsBuildCondition);
							parentElements.Peek().AppendChild((ProjectElement)(object)val5);
							parentElements.Push((ProjectElementContainer)(object)val5);
							break;
						}
					case DirectiveTypes.Endif:
						parentElements.Pop();
						parentElements.Pop();
						break;
					case DirectiveTypes.MacroDefinition:
						{
							MacroDefinition macroDefinition = sourcesDirective as MacroDefinition;
							Match match = Regex.Match(macroDefinition.Name, "NTTARGETFILE(?<Pass>0|1|2|S)", RegexOptions.IgnoreCase);
							Dictionary<string, string> dictionary = new Dictionary<string, string>();
							if (match.Success)
							{
								string[] array = Regex.Split(macroDefinition.Value, "\\s");
								foreach (string text2 in array)
								{
									if (!string.IsNullOrWhiteSpace(text2))
									{
										string text3 = (match.Groups["Pass"].Value.Equals("S", StringComparison.OrdinalIgnoreCase) ? "All" : match.Groups["Pass"].Value);
										string text4 = ConditionBlock.ExtractCondition(macroDefinition.ConditionBlock);
										dictionary["Pass"] = text3;
										dictionary["Condition"] = text4;
										project.AddItem("InvokedTargetsList", text2, (IEnumerable<KeyValuePair<string, string>>)dictionary);
										invokedTargetsList.Add(new InvokedTarget(text2, text4, text3));
									}
								}
							}
							AddMacro(macroDefinition.Name, macroDefinition.Value, lastCommandType);
							break;
						}
					case DirectiveTypes.IncludeFile:
						{
							string text = (sourcesDirective as IncludeFile).NmakeFilePath + ".props";
							string uniquePropertyName = GetUniquePropertyName("ImportFilePath");
							AddMacro(uniquePropertyName, text, lastCommandType);
							ProjectPropertyElement val = AddMacro(uniquePropertyName, text, DirectiveTypes.MacroDefinition);
							val.Value = string.Format(CultureInfo.InvariantCulture, "$([System.IO.Path]::Combine($(MSBuildProjectDirectory),'{0}'))", new object[1] { text });
							((ProjectElement)val).Condition = string.Format(CultureInfo.InvariantCulture, "!$([System.IO.Path]::IsPathRooted('{0}'))", new object[1] { text });
							string uniquePropertyName2 = GetUniquePropertyName("AlternateImportFilePath");
							ProjectPropertyElement val2 = AddMacro(uniquePropertyName2, "", DirectiveTypes.MacroDefinition);
							val2.Value = string.Format(CultureInfo.InvariantCulture, "$([System.IO.Path]::Combine($(MSBuildThisFileDirectory),'{0}'))", new object[1] { text });
							ProjectImportElement val3 = project.Xml.CreateImportElement(string.Format(CultureInfo.InvariantCulture, "$({0})", new object[1] { uniquePropertyName }));
							ProjectImportElement val4 = project.Xml.CreateImportElement(string.Format(CultureInfo.InvariantCulture, "$({0})", new object[1] { uniquePropertyName2 }));
							((ProjectElement)val3).Condition = string.Format(CultureInfo.InvariantCulture, "Exists($({0}))", new object[1] { uniquePropertyName });
							((ProjectElement)val4).Condition = string.Format(CultureInfo.InvariantCulture, "!Exists($({0}))", new object[1] { uniquePropertyName });
							IncludeFile includeFile = sourcesDirective as IncludeFile;
							if (includeFile.ConditionBlock != null)
							{
								((ProjectElement)val3).Condition = ConditionalOperators.And(includeFile.ConditionBlock.Condition, ((ProjectElement)val3).Condition);
								((ProjectElement)val4).Condition = ConditionalOperators.And(includeFile.ConditionBlock.Condition, ((ProjectElement)val4).Condition);
								Logger.TraceEvent(TraceEventType.Verbose, null, "Import file \"{0}\" will be imported with MSBuild condition \"{1}\"", text, ((ProjectElement)val3).Condition);
							}
							parentElements.Peek().AppendChild((ProjectElement)(object)val3);
							parentElements.Peek().AppendChild((ProjectElement)(object)val4);
							Logger.TraceEvent(TraceEventType.Verbose, null, "Import file \"{0}\" will be imported relative to $(MSBuildProjectDirectory), not the current file, if a copy exists in that directory", text);
							break;
						}
					case DirectiveTypes.TargetDefinition:
						{
							TargetDefinition targetDefinition = sourcesDirective as TargetDefinition;
							string name = string.Format(CultureInfo.InvariantCulture, "Converted Target {0}", new object[1] { targetsToConvert.Count });
							bool isMakeFileTarget = Path.GetFileName(msbuildFile).Equals("makefile.inc.props", StringComparison.OrdinalIgnoreCase);
							targetsToConvert.Add(new DeferredTarget(targetDefinition, name, GetCurrentCondition(), new List<string>(), isMakeFileTarget));
							break;
						}
					case DirectiveTypes.Undef:
						AddMacro((sourcesDirective as UNDEF).Macro, string.Empty, lastCommandType);
						break;
					case DirectiveTypes.InferenceRuleDefinition:
						targetInferenceRules.AddRule(sourcesDirective as InferenceRule);
						break;
					case DirectiveTypes.None:
						Logger.TraceEvent(TraceEventType.Warning, null, "Directive type \"{0}\" was ignored and not included in the generated MSBuild file. The corresponding NMake line is :\n {1}", sourcesDirective.DirectiveType.ToString(), (sourcesDirective as None).NmakeLine);
						break;
					default:
						Logger.TraceEvent(TraceEventType.Error, null, "Unexpected directive type \"{0}\", don't know how to convert to MSBuild", sourcesDirective.DirectiveType.ToString());
						break;
					case DirectiveTypes.DotDirective:
						break;
				}
				lastCommandType = sourcesDirective.DirectiveType;
			}
			catch (Exception ex)
			{
				Logger.TraceEvent(TraceEventType.Critical, null, "Error while processing parsed command #{0} of type {1}.\nMessage:\n{2}", i, sourcesDirective.DirectiveType, ex.Message);
				Logger.TraceEvent(TraceEventType.Verbose, null, "StackTrace=\n{0}", ex.StackTrace);
			}
		}
		if (Path.GetFileName(msbuildFile).Equals("makefile.inc.props", StringComparison.OrdinalIgnoreCase))
		{
			targetConverter.ConvertDeferedTargets(targetsToConvert, invokedTargetsList, targetInferenceRules, project.Xml);
		}
		project.Save(msbuildFile);
		Logger.TraceEvent(TraceEventType.Information, null, "Generated file \"{0}\"", msbuildFile);
		project.ProjectCollection.UnloadAllProjects();
		Trace.Unindent();
	}

	private ProjectPropertyElement AddMacro(string Name, string Value, DirectiveTypes lastCommandType)
	{
		if (lastCommandType != DirectiveTypes.MacroDefinition && lastCommandType != DirectiveTypes.Undef)
		{
			ProjectPropertyGroupElement val = project.Xml.CreatePropertyGroupElement();
			parentElements.Peek().AppendChild((ProjectElement)(object)val);
			currentPropertyGroup = val;
		}
		return currentPropertyGroup.AddProperty(Name, Value);
	}

	private string GetCurrentCondition()
	{
		string text = string.Empty;
		Stack<ProjectElementContainer> stack = new Stack<ProjectElementContainer>();
		if (parentElements.Count > 1)
		{
			ProjectElementContainer val = parentElements.Pop();
			string text2 = ExtractCumulativeCondition(val);
			if (!string.IsNullOrWhiteSpace(text2))
			{
				text = text2;
			}
			stack.Push(val);
		}
		while (parentElements.Count > 1)
		{
			ProjectElementContainer val = parentElements.Pop();
			string text3 = ExtractCumulativeCondition(val);
			if (!string.IsNullOrWhiteSpace(text3))
			{
				text = ConditionalOperators.And(text, text3);
			}
			stack.Push(val);
		}
		while (stack.Count > 0)
		{
			parentElements.Push(stack.Pop());
		}
		return text;
	}

	private static string ExtractCumulativeCondition(ProjectElementContainer element)
	{
		if (element is ProjectChooseElement)
		{
			return element.Condition;
		}
		if (element is ProjectWhenElement)
		{
			ProjectWhenElement val = (ProjectWhenElement)(object)((element is ProjectWhenElement) ? element : null);
			string text = element.Condition;
			ProjectWhenElement val2 = val;
			while (true)
			{
				ProjectWhenElement val3 = (ProjectWhenElement)val2.PreviousSibling;
				val2 = val3;
				if (val3 == null)
				{
					break;
				}
				text = ConditionalOperators.And(text, ConditionalOperators.Not(val2.Condition));
			}
			return text;
		}
		if (element is ProjectOtherwiseElement)
		{
			ProjectOtherwiseElement val4 = (ProjectOtherwiseElement)(object)((element is ProjectOtherwiseElement) ? element : null);
			ProjectWhenElement val5 = (ProjectWhenElement)val4.PreviousSibling;
			string text2 = ConditionalOperators.Not(val5.Condition);
			while (true)
			{
				ProjectWhenElement val6 = (ProjectWhenElement)val5.PreviousSibling;
				val5 = val6;
				if (val6 == null)
				{
					break;
				}
				text2 = ConditionalOperators.And(text2, ConditionalOperators.Not(val5.Condition));
			}
			return text2;
		}
		return element.Condition;
	}

	public static string GetUniquePropertyName(string initialName)
	{
		return initialName + "_" + Guid.NewGuid().ToString().Replace("-", string.Empty)
			.ToUpper(CultureInfo.InvariantCulture);
	}

	private List<SourcesDirective> RefactorAnyConditionalIncludeCommand(ref List<SourcesDirective> parsedCommands)
	{
		List<SourcesDirective> commands = new List<SourcesDirective>();
		foreach (SourcesDirective parsedCommand in parsedCommands)
		{
			commands.Add((SourcesDirective)cloneObject(parsedCommand));
		}
		RefactorAnyConditionalIncludeCommands(ref commands);
		return commands;
	}

	private void RefactorAnyConditionalIncludeCommands(ref List<SourcesDirective> commands)
	{
		Trace.Indent();
		Stack<string> stack = new Stack<string>();
		int num = 0;
		for (int i = 0; i < commands.Count; i++)
		{
			switch (commands[i].DirectiveType)
			{
				case DirectiveTypes.If:
					if (stack.Count == 0)
					{
						num = i;
					}
					stack.Push((commands[i] as Conditional).MsBuildCondition);
					break;
				case DirectiveTypes.Else:
					stack.Push(ConditionalOperators.Not(stack.Pop()));
					break;
				case DirectiveTypes.Elseif:
					stack.Push(ConditionalOperators.And(ConditionalOperators.Not(stack.Pop()), (commands[i] as Conditional).MsBuildCondition));
					break;
				case DirectiveTypes.Endif:
					stack.Pop();
					break;
				case DirectiveTypes.IncludeFile:
					{
						if (stack.Count <= 0)
						{
							break;
						}
						Logger.TraceEvent(TraceEventType.Verbose, null, "Conditional include found: \"{0}\"", (commands[i] as IncludeFile).NmakeFilePath);
						List<SourcesDirective> includeCommands = new()
						{
							commands[i]
						};
						int num2 = i + 1;
						while (num2 < commands.Count && commands[num2].DirectiveType == DirectiveTypes.IncludeFile)
						{
							includeCommands.Add(commands[num2++]);
						}
						commands.RemoveRange(i, includeCommands.Count);
						int index = i;
						int num3 = 0;
						num2 = i;
						int num4 = 0;
						for (; num2 < commands.Count; num2++)
						{
							if (num3 + stack.Count == 0)
							{
								break;
							}
							if (commands[num2].DirectiveType == DirectiveTypes.If)
							{
								num3++;
							}
							else if (commands[num2].DirectiveType == DirectiveTypes.Endif)
							{
								num3--;
							}
							num4++;
						}
						List<SourcesDirective> commandsInSec = commands.GetRange(index, num4);
						commands.RemoveRange(i, num4);
						RefactorIncludes_Helper(ref commandsInSec, ref includeCommands, stack.Count);
						commands.InsertRange(i, commandsInSec);
						int num5 = num;
						int count = commands.IndexOf(commandsInSec[commandsInSec.Count - 1]) - num5 + 1;
						if (!Parser.PassesSanityChecks(commands.GetRange(num5, count)))
						{
							Logger.TraceEvent(TraceEventType.Critical, null, "Refactoring conditional !include command resulted in malformed list of parsed commands.");
							throw new Exception("Refactoring conditional !include command resulted in malformed list of parsed commands");
						}
						i = num - 1;
						stack.Clear();
						break;
					}
			}
		}
		Trace.Unindent();
	}

	private void RefactorIncludes_Helper(ref List<SourcesDirective> commandsInSec2, ref List<SourcesDirective> includeCommands, int currentConditionDepth)
	{
		string uniquePropertyName = GetUniquePropertyName("IncludeFile");
		Logger.TraceEvent(TraceEventType.Verbose, null, "Inserted property \"{0}\" while refactoring conditional include", uniquePropertyName);
		MacroDefinition macroDefinition = new MacroDefinition();
		macroDefinition.Name = uniquePropertyName;
		macroDefinition.Value = "true";
		string text = string.Format(CultureInfo.InvariantCulture, "'$({0})'=='true'", new object[1] { uniquePropertyName });
		List<SourcesDirective> commands = new List<SourcesDirective>();
		foreach (SourcesDirective item in commandsInSec2)
		{
			commands.Add((SourcesDirective)cloneObject(item));
		}
		int num = 0;
		while ((commandsInSec2[0].DirectiveType != DirectiveTypes.Else && commandsInSec2[0].DirectiveType != DirectiveTypes.Elseif && commandsInSec2[0].DirectiveType != DirectiveTypes.Endif) || num != 0)
		{
			if (commandsInSec2[0].DirectiveType == DirectiveTypes.If)
			{
				num++;
			}
			if (commandsInSec2[0].DirectiveType == DirectiveTypes.Endif)
			{
				num--;
			}
			commandsInSec2.RemoveAt(0);
		}
		AppendConditionToCommands(ref commandsInSec2, ConditionalOperators.Not(text), isFirstCommandInsideFullOrPartialCondition: true);
		commandsInSec2.Insert(0, macroDefinition);
		foreach (IncludeFile includeCommand in includeCommands)
		{
			includeCommand.ConditionBlock = new ConditionBlock(text);
			commandsInSec2.Add(includeCommand);
		}
		num = 1;
		int num2 = 0;
		bool flag = true;
		while (num2 < commands.Count)
		{
			switch (commands[num2].DirectiveType)
			{
				case DirectiveTypes.If:
					num++;
					break;
				case DirectiveTypes.Endif:
					if (num <= 1)
					{
						flag = true;
					}
					num--;
					break;
				case DirectiveTypes.Elseif:
				case DirectiveTypes.Else:
					if (flag && num <= 1)
					{
						flag = false;
						num = 1;
					}
					break;
			}
			if (!flag)
			{
				commands.RemoveAt(num2);
			}
			else
			{
				num2++;
			}
		}
		PrependIfCommands(ref commands, text);
		commandsInSec2.AddRange(commands);
	}

	private void PrependIfCommands(ref List<SourcesDirective> commands, string ifCondition)
	{
		int num = 0;
		foreach (SourcesDirective command in commands)
		{
			if (command.DirectiveType == DirectiveTypes.If)
			{
				num--;
			}
			else if (command.DirectiveType == DirectiveTypes.Endif)
			{
				num++;
			}
		}
		while (num > 0)
		{
			IF iF = new IF();
			iF.MsBuildCondition = "true";
			commands.Insert(0, iF);
			num--;
		}
		AppendConditionToCommands(ref commands, ifCondition);
	}

	private void AppendConditionToCommands(ref List<SourcesDirective> commands, string conditionToAdd, bool isFirstCommandInsideFullOrPartialCondition = false)
	{
		int num = 0;
		bool flag = false;
		bool flag2 = isFirstCommandInsideFullOrPartialCondition;
		int num2 = (!flag2) ? 1 : 0;
		for (int i = 0; i < commands.Count; i++)
		{
			if (commands[i] is Conditional && flag)
			{
				commands.Insert(i, new ENDIF());
				flag = false;
				num--;
				i++;
			}
			switch (commands[i].DirectiveType)
			{
				case DirectiveTypes.If:
					num++;
					if (num <= num2)
					{
						Conditional conditional2 = commands[i] as Conditional;
						conditional2.MsBuildCondition = ConditionalOperators.And(conditional2.MsBuildCondition, conditionToAdd);
						commands.RemoveAt(i);
						commands.Insert(i, conditional2);
						flag2 = true;
					}
					continue;
				case DirectiveTypes.Else:
					if (num <= num2)
					{
						ELSEIF eLSEIF = new()
						{
							MsBuildCondition = conditionToAdd
						};
						commands.RemoveAt(i);
						commands.Insert(i, eLSEIF);
						flag2 = true;
					}
					continue;
				case DirectiveTypes.Elseif:
					if (num <= num2)
					{
						Conditional conditional = commands[i] as Conditional;
						conditional.MsBuildCondition = ConditionalOperators.And(conditional.MsBuildCondition, conditionToAdd);
						commands.RemoveAt(i);
						commands.Insert(i, conditional);
						flag2 = true;
					}
					continue;
				case DirectiveTypes.Endif:
					num--;
					if (num < num2)
					{
						flag2 = false;
					}
					continue;
			}
			if (num <= num2 && !flag2)
			{
				IF iF = new()
				{
					MsBuildCondition = conditionToAdd
				};
				commands.Insert(i, iF);
				num++;
				flag = true;
				flag2 = true;
				i++;
			}
		}
	}

	private object cloneObject(object objToClone)
	{
		MemoryStream memoryStream = new MemoryStream();
		BinaryFormatter binaryFormatter = new BinaryFormatter();
		binaryFormatter.Binder = new VersionAgnosticSerializationBinder();
		binaryFormatter.Serialize(memoryStream, objToClone);
		memoryStream.Position = 0L;
		return binaryFormatter.Deserialize(memoryStream);
	}
}
