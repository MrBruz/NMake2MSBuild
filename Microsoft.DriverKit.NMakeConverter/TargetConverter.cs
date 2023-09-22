#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Construction;
using Microsoft.DriverKit.NMakeConverter.Commands;

namespace Microsoft.DriverKit.NMakeConverter;

internal class TargetConverter
{
	private static string targetInputName = "t_TargetInput_";

	private int targetCounter;

	private string defaultTargetName = "Converted Target ";

	private string tokenReplacementTask = "ReplaceNmakeCommandTokens";

	private string tokenRepTaskParam_TargetInputs = "TaskInputFiles";

	private string tokenRepTaskParam_TargetOutputs = "TaskOutputFile";

	private string tokenRepTaskParam_Command = "Command";

	private string tokenRepTaskParam_ShouldExecute = "ShouldExecute";

	private string tokenRepTaskParam_ParsedCommand = "ProcessedCommand";

	public void ConvertDeferedTargets(List<DeferredTarget> targetsToConvert, List<InvokedTarget> invokedTargetsList, TargetInferenceRules targetInferenceRules, ProjectRootElement projRoot)
	{
		foreach (DeferredTarget item in targetsToConvert)
		{
			foreach (string[] input in item.TargetDefinition.Inputs)
			{
				string[] array = input;
				foreach (string value in array)
				{
					foreach (DeferredTarget item2 in targetsToConvert)
					{
						if (item2.TargetDefinition.Outputs.Contains<string>(value, StringComparer.OrdinalIgnoreCase) && !item.DependsOnTargets.Contains<string>(item2.Name, StringComparer.OrdinalIgnoreCase))
						{
							item.DependsOnTargets.Add(item2.Name);
						}
					}
				}
			}
		}
		string beforeTargets = string.Empty;
		string afterTargets = string.Empty;
		List<InvokedTarget> list = new List<InvokedTarget>();
		foreach (DeferredTarget item3 in targetsToConvert)
		{
			bool flag = false;
			if (item3.IsMakeFileTarget)
			{
				foreach (InvokedTarget invokedTargets in invokedTargetsList)
				{
					string[] outputs = item3.TargetDefinition.Outputs;
					foreach (string path in outputs)
					{
						if (object.Equals(Path.GetFullPath(path), Path.GetFullPath(invokedTargets.TargetOutput)))
						{
							beforeTargets = invokedTargets.MsBuildBeforeTargets;
							afterTargets = invokedTargets.MsBuildAfterTargets;
							flag = true;
							break;
						}
					}
					if (flag && !list.Contains(invokedTargets))
					{
						list.Add(invokedTargets);
					}
				}
				foreach (DeferredTarget item4 in targetsToConvert)
				{
					if (item4.DependsOnTargets.Contains<string>(item3.Name, StringComparer.OrdinalIgnoreCase))
					{
						Logger.TraceEvent(TraceEventType.Verbose, null, "Target {0} will be implicitly invoked since target {1} depends on it", item3.Name, item4.Name);
						flag = true;
						break;
					}
				}
			}
			else
			{
				beforeTargets = "$(BuildGenerateSourcesTargets)";
				flag = true;
			}
			if (!flag)
			{
				Logger.TraceEvent(TraceEventType.Warning, null, "A makefile.inc target that is neither invoked via NTTARGETFILE* macros, nor required by any other targets, was discovered. The target will not be converted. The NMake definition of the associated target is:\n{0}", item3.TargetDefinition.NmakeDefinition);
				Logger.TraceEvent(TraceEventType.Verbose, null, "The target's potential outputs are:\n{0}", string.Join(Environment.NewLine, item3.TargetDefinition.Outputs));
				StringBuilder stringBuilder = new StringBuilder();
				foreach (InvokedTarget invokedTargets2 in invokedTargetsList)
				{
					stringBuilder.AppendLine(invokedTargets2.TargetOutput);
				}
				Logger.TraceEvent(TraceEventType.Verbose, null, "The list of targets invoked via NTTARGETFILE* contains:\n{0}", stringBuilder);
			}
			else
			{
				string condition = item3.Condition;
				if (!string.IsNullOrWhiteSpace(condition))
				{
					Logger.TraceEvent(TraceEventType.Verbose, null, "A conditional NMake target was found. Attempting conversion to MSBuild target with base condition \"{0}\"", condition);
				}
				ConvertTarget(item3.TargetDefinition, targetInferenceRules, projRoot, condition, item3.Name, item3.DependsOnTargets, beforeTargets, afterTargets);
			}
		}
		targetsToConvert.Clear();
		ResolveInvokedTargets(invokedTargetsList.Except(list).ToList(), targetInferenceRules, projRoot);
	}

	private void ResolveInvokedTargets(List<InvokedTarget> invokedTargetsList, TargetInferenceRules targetInferenceRules, ProjectRootElement projRoot)
	{
		while (invokedTargetsList.Count > 0)
		{
			bool flag = false;
			foreach (InferenceRule rule in targetInferenceRules.Rules)
			{
				string directoryName = Path.GetDirectoryName(invokedTargetsList[0].TargetOutput);
				string extension = Path.GetExtension(invokedTargetsList[0].TargetOutput);
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(invokedTargetsList[0].TargetOutput);
				string condition = invokedTargetsList[0].Condition;
				if ((directoryName.Equals(rule.ToPath, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(rule.ToPath) || (!string.IsNullOrEmpty(directoryName) && object.Equals(Path.GetFullPath(directoryName), Path.GetFullPath(rule.ToPath)))) && extension.Equals("." + rule.ToExtension, StringComparison.OrdinalIgnoreCase))
				{
					flag = true;
					TargetDefinition targetDefinition = new TargetDefinition();
					string text = Path.Combine(rule.FromPath, fileNameWithoutExtension + "." + rule.FromExtension);
					string targetOutput = invokedTargetsList[0].TargetOutput;
					targetDefinition.Outputs = new string[1] { targetOutput };
					targetDefinition.Commands = new List<string>();
					targetDefinition.Inputs = new List<string[]>();
					targetDefinition.Inputs.Add(new string[1] { text });
					targetDefinition.NmakeTargetHeader = string.Format(CultureInfo.InvariantCulture, "{0} : {1}", new object[2] { targetOutput, text });
					targetDefinition.NmakeDefinition = string.Format(CultureInfo.InvariantCulture, "{0}\n#This infered target was generated due to \"{1}\" invoked by NTTARGETFILE* macros\"", new object[2] { targetDefinition.NmakeTargetHeader, targetOutput });
					Logger.TraceEvent(TraceEventType.Verbose, null, "Generating target from an inference rule, due to \"{0}\" invoked by NTTARGETFILE{0} macro", targetOutput, invokedTargetsList[0].Pass);
					string msBuildBeforeTargets = invokedTargetsList[0].MsBuildBeforeTargets;
					string msBuildAfterTargets = invokedTargetsList[0].MsBuildAfterTargets;
					ConvertTarget(targetDefinition, targetInferenceRules, projRoot, condition, null, null, msBuildBeforeTargets, msBuildAfterTargets);
					break;
				}
			}
			if (!flag)
			{
				Logger.TraceEvent(TraceEventType.Warning, null, "No inference rule or target was found that could generate \"{0}\".\nSkipping conversion of this target", invokedTargetsList[0].TargetOutput);
				StringBuilder stringBuilder = new StringBuilder();
				foreach (InferenceRule rule2 in targetInferenceRules.Rules)
				{
					if (!string.IsNullOrWhiteSpace(rule2.NmakeDefinition))
					{
						stringBuilder.AppendLine(rule2.NmakeDefinition);
						stringBuilder.AppendLine("----------------------------");
					}
				}
				Logger.TraceEvent(TraceEventType.Verbose, null, "The list of (non-default) inference rules parsed from the project contains:\n{0}", stringBuilder);
			}
			invokedTargetsList.RemoveAt(0);
		}
	}

	private void ConvertTarget(TargetDefinition parsedTarget, TargetInferenceRules targetInferenceRules, ProjectRootElement projRoot, string initialCondition, string targetName = null, List<string> dependsOnTargets = null, string beforeTargets = null, string afterTargets = null)
	{
		Trace.Indent();
		if (string.IsNullOrEmpty(targetName))
		{
			targetName = defaultTargetName + targetCounter;
		}
		ProjectTargetElement val = projRoot.CreateTargetElement(targetName);
		((ProjectElement)val).Condition = initialCondition;
		((ProjectElementContainer)projRoot).AppendChild((ProjectElement)(object)val);
		if (dependsOnTargets != null && dependsOnTargets.Count > 0)
		{
			val.DependsOnTargets = string.Join(";", dependsOnTargets);
			Logger.TraceEvent(TraceEventType.Verbose, null, "Target \"{0}\" depends on: \"{1}\"", targetName, val.DependsOnTargets);
		}
		if (!string.IsNullOrEmpty(beforeTargets))
		{
			val.BeforeTargets = beforeTargets;
			Logger.TraceEvent(TraceEventType.Verbose, null, "Set BeforeTargets=\"{0}\" for target \"{1}\"", val.BeforeTargets, targetName);
		}
		if (!string.IsNullOrEmpty(afterTargets))
		{
			val.AfterTargets = afterTargets;
			Logger.TraceEvent(TraceEventType.Verbose, null, "Set AfterTargets=\"{0}\" for target \"{1}\"", val.AfterTargets, targetName);
		}
		PreProcessTarget(ref parsedTarget, targetInferenceRules);
		if (string.IsNullOrEmpty(val.DependsOnTargets) && string.IsNullOrEmpty(val.AfterTargets) && string.IsNullOrEmpty(val.BeforeTargets))
		{
			val.BeforeTargets = "BeforeClCompile";
			Logger.TraceEvent(TraceEventType.Verbose, null, "Target \"{0}\" was changed to execute before \"{1}\" by default", targetName, val.BeforeTargets);
		}
		string text = Regex.Replace(val.Name, "\\s", string.Empty) + "_Disabled";
		string text2 = string.Format(CultureInfo.InvariantCulture, "'$({0})'!='true'", new object[1] { text });
		if (!string.IsNullOrEmpty(((ProjectElement)val).Condition))
		{
			((ProjectElement)val).Condition = ConditionalOperators.And(((ProjectElement)val).Condition, text2);
		}
		else
		{
			((ProjectElement)val).Condition = text2;
		}
		ProjectItemGroupElement val2 = val.AddItemGroup();
		List<string> list = new List<string>();
		int num = 0;
		string text3 = string.Empty;
		foreach (string[] input in parsedTarget.Inputs)
		{
			string text4 = targetInputName + num;
			list.Add(text4);
			ProjectItemElement val3 = val2.AddItem(targetInputName + num, " ");
			val3.Include = null;
			val3.Remove = string.Format(CultureInfo.InvariantCulture, "@({0})", new object[1] { text4 });
			string[] array = input;
			foreach (string text5 in array)
			{
				val3 = val2.AddItem(text4, text5);
				((ProjectElement)val3).Condition = string.Format(CultureInfo.InvariantCulture, "Exists('{0}') And ('@({1})'=='')", new object[2] { text5, text4 });
			}
			val3 = val2.AddItem(text4, input[0]);
			((ProjectElement)val3).Condition = string.Format(CultureInfo.InvariantCulture, "'@({1})'==''", new object[2]
			{
				input[0],
				text4
			});
			text3 += string.Format(CultureInfo.InvariantCulture, "@({0});", new object[1] { text4 });
			num++;
		}
		string text6 = "AllEvaluatedTargetInputs";
		ProjectItemElement val4 = val2.AddItem(text6, " ");
		val4.Include = null;
		val4.Remove = string.Format(CultureInfo.InvariantCulture, "@({0})", new object[1] { text6 });
		val4 = val2.AddItem(text6, string.IsNullOrEmpty(text3) ? " " : text3);
		((ProjectElementContainer)val2).RemoveChild((ProjectElement)(object)val4);
		((ProjectElementContainer)val2).AppendChild((ProjectElement)(object)val4);
		StringBuilder stringBuilder = new StringBuilder();
		foreach (string[] input2 in parsedTarget.Inputs)
		{
			stringBuilder.Append(string.Join(Environment.NewLine, input2));
			stringBuilder.Append(Environment.NewLine);
			stringBuilder.AppendLine();
		}
		Logger.TraceEvent(TraceEventType.Verbose, null, "Generated MSBuildTarget \"{0}\" with inputs :\n{1}", targetName, stringBuilder);
		string text7 = "TargetOutDated";
		string[] outputs = parsedTarget.Outputs;
		foreach (string text8 in outputs)
		{
			bool flag = true;
			int num2 = 1;
			foreach (string command in parsedTarget.Commands)
			{
				string text9 = "ResolvedCommand_" + num2++;
				ProjectTaskElement val5 = val.AddTask(tokenReplacementTask);
				val5.SetParameter(tokenRepTaskParam_Command, command);
				val5.SetParameter(tokenRepTaskParam_TargetInputs, string.Format(CultureInfo.InvariantCulture, "@({0})", new object[1] { text6 }));
				val5.SetParameter(tokenRepTaskParam_TargetOutputs, StringUtilities.UnQuote(text8));
				val5.AddOutputProperty(tokenRepTaskParam_ShouldExecute, flag ? text7 : "Junk_Property");
				val5.AddOutputProperty(tokenRepTaskParam_ParsedCommand, text9);
				val5 = val.AddTask("Exec");
				((ProjectElement)val5).Condition = string.Format(CultureInfo.InvariantCulture, "'$({0})'=='true'", new object[1] { text7 });
				val5.SetParameter("Command", string.Format(CultureInfo.InvariantCulture, "$({0})", new object[1] { text9 }));
				val5.SetParameter("WorkingDirectory", "$(MSBuildProjectDirectory)");
				flag = false;
			}
			ProjectTaskElement val6 = val.AddTask("Message");
			((ProjectElement)val6).Condition = string.Format(CultureInfo.InvariantCulture, "'$({0})'!='true'", new object[1] { text7 });
			val6.SetParameter("Text", string.Format(CultureInfo.InvariantCulture, "File {0} is up-to-date", new object[1] { text8 }));
			Logger.TraceEvent(TraceEventType.Verbose, null, "Added commands to MSBuild Target \"{0}\" for output file \"{1}\" with commands:\n{2}", targetName, text8, string.Join(Environment.NewLine, parsedTarget.Commands));
		}
		ProjectItemGroupElement val7 = val.AddItemGroup();
		((ProjectElementContainer)val).RemoveChild((ProjectElement)(object)val7);
		((ProjectElement)val).Parent.InsertBeforeChild((ProjectElement)(object)val7, (ProjectElement)(object)val);
		TargetsParser.GetTargetOutputsAndInputsStrings(parsedTarget.NmakeTargetHeader, out var rawOutputsString, out var rawInputsString);
		ProjectItemElement val8 = val7.AddItem("NmakeTarget", string.IsNullOrWhiteSpace(rawInputsString) ? " " : rawInputsString.Trim());
		val8.AddMetadata("TargetKillSwitch", text);
		val8.AddMetadata("TargetName", val.Name);
		val8.AddMetadata("Outputs", rawOutputsString.Trim());
		val8.AddMetadata("Condition", initialCondition.Trim());
		for (int k = 0; k < parsedTarget.Commands.Count; k++)
		{
			val8.AddMetadata("Cmd" + k, parsedTarget.Commands[k]);
		}
		targetCounter++;
		Trace.Unindent();
	}

	private static void PreProcessTarget(ref TargetDefinition target, TargetInferenceRules targetInferenceRules)
	{
		if (target.Commands.Count == 0)
		{
			Logger.TraceEvent(TraceEventType.Verbose, null, "Searching for applicable inference rules for target with nmake definition:\n{0}", target.NmakeDefinition);
			target.Commands = TargetsParser.ApplyTargetInferenceRules(target.Inputs, target.Outputs, targetInferenceRules);
		}
		for (int i = 0; i < target.Commands.Count; i++)
		{
			target.Commands[i] = TargetsParser.StripCommandModifiers(target.Commands[i]);
		}
	}
}
