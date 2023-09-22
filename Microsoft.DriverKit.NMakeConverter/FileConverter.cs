#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.DriverKit.MakeSolution;
using Microsoft.DriverKit.NMakeConverter.Commands;
using Microsoft.DriverKit.NMakeConverter.PropertyPageSupport;
using Microsoft.Win32;

namespace Microsoft.DriverKit.NMakeConverter;

internal class FileConverter
{
	private enum Referent
	{
		TargetInput,
		TargetOutput
	}

	public const string filtersFileTemplate = "Filters.template";

	public const string packageTemplate = "Package.vcxproj.template";

	private static Dictionary<string, string> initialStateProperties = null;

	private static bool generatePropertyPages;

	private static readonly MsBuildWriter msBuild = new();

	private static string primaryInputFile;

	private static string primaryConvertedFile = null;

	private static string overrideProjectName = null;

	private static bool primaryInputFileIsSources = false;

	private static void AddIDESupport(string vcxProjFile)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected O, but got Unknown
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected O, but got Unknown
		Trace.Indent();
		Logger.TraceEvent(TraceEventType.Verbose, null, "Attempting to add propery page support for converted targets and macros in project {0}", vcxProjFile);
		Build.Evaluation.Project val = new Build.Evaluation.Project(vcxProjFile, null, null, new ProjectCollection());
		CheckForWellKnownTargets(val);
		CheckForWellKnownTargetMacros(val);
		val.Save();
		val.ProjectCollection.UnloadAllProjects();
		Logger.TraceEvent(TraceEventType.Verbose, null, "Finished adding propery page support for converted targets and macros, changes saved to {0}", vcxProjFile);
		Trace.Unindent();
	}

	private static void CheckForWellKnownTargetMacros(Build.Evaluation.Project project)
	{
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Expected O, but got Unknown
		Trace.Indent();
		Logger.TraceEvent(TraceEventType.Verbose, null, "Attempting to find and convert nmake target macros to well-known MSBuild items, as appropriate.");
		List<ProjectPropertyElement> propertyElements = GetPropertyElements(project);
		List<string> list = new List<string>();
		for (int num = propertyElements.Count - 1; num >= 0; num--)
		{
			ProjectPropertyElement val = propertyElements[num];
			if (!list.Contains<string>(val.Name, StringComparer.OrdinalIgnoreCase) && TargetCommandsParser.IsTargetMarco(val.Name) && IsChildElementOfSourcesOrMakeFile(val, project) && !IsPropertySetConditionally(val.Name, project, checkRecursively: false, propertyElements))
			{
				list.Add(val.Name);
				string commandText = PartiallyEvaluate(val.Value, project, propertyElements);
				Logger.TraceEvent(TraceEventType.Verbose, null, "Target-Macro \"{0}\" is well known. Appropriate MSBuild wrapped tasks will be invoked instead. Property \"{0}\" is being set to null at the end of {1}", val.Name, Path.GetFileName(project.FullPath));
				if (TargetCommandsParser.ConvertMacro(val.Name, commandText, project))
				{
					Logger.TraceEvent(TraceEventType.Verbose, null, "Target-Macro \"{0}\" is well known and was converted to use the appropriate MSBuild wrapped tasks", val.Name);
					ProjectRootElement val2 = ProjectRootElement.Open(primaryConvertedFile, new ProjectCollection());
					ProjectPropertyGroupElement val3 = val2.CreatePropertyGroupElement();
					val2.AppendChild((ProjectElement)(object)val3);
					val3.AddProperty(val.Name, string.Empty);
					val2.Save();
				}
			}
		}
		Trace.Unindent();
	}

	private static void CheckForWellKnownTargets(Build.Evaluation.Project project)
	{
		Trace.Indent();
		Logger.TraceEvent(TraceEventType.Verbose, null, "Attempting to find and convert nmake targets to well-known MSBuild items, as appropriate.");
		foreach (ProjectItem item2 in project.GetItemsIgnoringCondition("NmakeTarget"))
		{
			if (Regex.Split(item2.UnevaluatedInclude, "\\s").Length > 1)
			{
				Logger.TraceEvent(TraceEventType.Verbose, null, "Skipping property page support for target {0} since it contains multiple inputs ({1})", item2.GetMetadataValue("TargetName"), item2.UnevaluatedInclude);
			}
			string unevaluatedValue = item2.GetMetadata("Outputs").UnevaluatedValue;
			string unevaluatedValue2 = item2.GetMetadata("TargetKillSwitch").UnevaluatedValue;
			string unevaluatedValue3 = item2.GetMetadata("Condition").UnevaluatedValue;
			if (string.IsNullOrWhiteSpace(unevaluatedValue3))
			{
				List<string> list = new List<string>();
				int num = 0;
				List<ProjectPropertyElement> propertyElements = GetPropertyElements(project);
				string unevaluatedInclude = item2.UnevaluatedInclude;
				ProjectMetadata metadata;
				while ((metadata = item2.GetMetadata("Cmd" + num++)) != null)
				{
					string unevaluatedValue4 = metadata.UnevaluatedValue;
					Logger.TraceEvent(TraceEventType.Verbose, null, "Target Command {0} = {1}", num, unevaluatedValue4);
					string originalCommand = PartiallyEvaluate(unevaluatedValue4, project, propertyElements);
					string item = ReplaceTargetCommandTokens(originalCommand, unevaluatedValue, project.FullPath);
					list.Add(item);
				}
				if (TargetCommandsParser.ConvertNmakeCommands(list.ToArray(), unevaluatedInclude, unevaluatedValue, project))
				{
					Logger.TraceEvent(TraceEventType.Verbose, null, "All commands in target {0} are well known. Appropriate MSBuild wrapped tasks will be invoked instead. Target is being disabled by setting {1}=true", item2.GetMetadataValue("TargetName"), unevaluatedValue2);
					project.Xml.AddProperty(unevaluatedValue2, "True");
				}
			}
			else
			{
				Logger.TraceEvent(TraceEventType.Verbose, null, "Target \"{0}\" is executed conditionally (Conditon =\"{1}\"). Will not attempt to convert to wrapped tasks.", item2.GetMetadataValue("TargetName"), unevaluatedValue3);
			}
		}
		Trace.Unindent();
	}

	internal static string ReplaceTargetCommandTokens(string originalCommand, string targetOutput, string vcxProjFilePath, string inputItemType = null)
	{
		Trace.Indent();
		inputItemType = ((inputItemType != null) ? (inputItemType + ".") : string.Empty);
		string text = originalCommand;
		Regex[] array = new Regex[3]
		{
			new Regex("^(@)?(?<Command>.*)$"),
			new Regex("^(-(0-9))?(?<Command>.*)$"),
			new Regex("^(!)?(?<Command>.*)$")
		};
		Regex[] array2 = array;
		foreach (Regex regex in array2)
		{
			text = regex.Replace(text, "${Command}");
		}
		Dictionary<string, Referent> dictionary = new Dictionary<string, Referent>
		{
			{ "%40", Referent.TargetOutput },
			{ "%2A%2A", Referent.TargetInput },
			{ "%2A", Referent.TargetOutput },
			{ "<", Referent.TargetInput },
			{ "%3F", Referent.TargetInput }
		};
		foreach (string key in dictionary.Keys)
		{
			string pattern = "(?<FullToken>%24" + key + "|%24%28" + key + "(?<Modifier>D|B|F|R)?%29)";
			Match match;
			while ((match = Regex.Match(text, pattern)).Success)
			{
				string value = match.Groups["FullToken"].Value;
				string value2 = match.Groups["Modifier"].Value;
				bool flag = false;
				string empty = string.Empty;
				switch (value2)
				{
					case "D":
						if (dictionary[key] == Referent.TargetInput)
						{
							empty = string.Format(CultureInfo.InvariantCulture, "%({0}RootDir)%({0}Directory)", new object[1] { inputItemType });
						}
						else
						{
							try
							{
								string relativePath = MakeSolution.Project.GetRelativePath(vcxProjFilePath, targetOutput);
								if (Path.IsPathRooted(relativePath))
								{
									throw new InvalidOperationException();
								}
								empty = string.Format(CultureInfo.InvariantCulture, "$(MSBuildProjectDirectory)\\{0}", new object[1] { relativePath });
							}
							catch (Exception)
							{
								empty = string.Format(CultureInfo.InvariantCulture, "$([System.IO.Path]::GetDirectoryName($([System.IO.Path]::GetFullPath('{0}'))))", new object[1] { targetOutput });
							}
						}
						flag = true;
						break;
					case "B":
						empty = (dictionary[key] != 0) ? Path.GetFileNameWithoutExtension(targetOutput) : string.Format(CultureInfo.InvariantCulture, "%({0}Filename)", new object[1] { inputItemType });
						break;
					case "F":
						empty = (dictionary[key] != 0) ? Path.GetFileName(targetOutput) : string.Format(CultureInfo.InvariantCulture, "%({0}Filename)%({0}Extension)", new object[1] { inputItemType });
						break;
					case "R":
						if (dictionary[key] == Referent.TargetInput)
						{
							empty = string.Format(CultureInfo.InvariantCulture, "%({0}RootDir)%({0}Directory)%({0}Filename)", new object[1] { inputItemType });
						}
						else
						{
							try
							{
								string relativePath2 = MakeSolution.Project.GetRelativePath(vcxProjFilePath, targetOutput);
								string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(targetOutput);
								if (Path.IsPathRooted(relativePath2))
								{
									throw new InvalidOperationException();
								}
								empty = string.Format(CultureInfo.InvariantCulture, "$(MSBuildProjectDirectory)\\{0}\\{1}", new object[2] { relativePath2, fileNameWithoutExtension });
							}
							catch (Exception)
							{
								empty = string.Format(CultureInfo.InvariantCulture, "$([System.IO.Path]::GetDirectoryName('{0}'))\\$([System.IO.Path]::GetFileName('{0}'))", new object[1] { targetOutput });
							}
						}
						flag = true;
						break;
					default:
						empty = (dictionary[key] != 0) ? targetOutput : string.Format(CultureInfo.InvariantCulture, "%({0}Identity)", new object[1] { inputItemType });
						flag = true;
						break;
				}
				string replacement = empty;
				empty = string.Format(CultureInfo.InvariantCulture, "\"{0}\"", new object[1] { empty });
				if (flag)
				{
					text = Regex.Replace(text, string.Format(CultureInfo.InvariantCulture, "(?<left>.){0} ", new object[1] { Regex.Escape(value) }), "${left}" + empty + " ");
					text = Regex.Replace(text, string.Format(CultureInfo.InvariantCulture, "^{0} ", new object[1] { Regex.Escape(value) }), empty + " ");
					text = Regex.Replace(text, string.Format(CultureInfo.InvariantCulture, " {0}$", new object[1] { Regex.Escape(value) }), " " + empty);
				}
				text = Regex.Replace(text, Regex.Escape(value), replacement);
			}
		}
		Logger.TraceEvent(TraceEventType.Verbose, null, "After token replacement, \"{0}\" was converted to \"{1}\"", originalCommand, text);
		Trace.Unindent();
		return text;
	}

	internal static List<ProjectPropertyElement> GetPropertyElements(Build.Evaluation.Project project)
	{
		List<ProjectPropertyElement> list = new List<ProjectPropertyElement>();
		foreach (ProjectElement item in project.GetLogicalProject())
		{
			if (item is ProjectPropertyGroupElement)
			{
				list.AddRange(((ProjectPropertyGroupElement)((item is ProjectPropertyGroupElement) ? item : null)).Properties);
			}
			else
			{
				if (!(item is ProjectChooseElement))
				{
					continue;
				}
				foreach (ProjectElement allChild in ((ProjectElementContainer)((item is ProjectChooseElement) ? item : null)).AllChildren)
				{
					if (allChild is ProjectPropertyElement)
					{
						list.Add((ProjectPropertyElement)(object)((allChild is ProjectPropertyElement) ? allChild : null));
					}
				}
			}
		}
		return list;
	}

	internal static ProjectPropertyElement GetLastPropertyElement(Build.Evaluation.Project project, List<ProjectPropertyElement> orderedList, string propertyToFind)
	{
		ProjectPropertyElement result = null;
		try
		{
			result = orderedList.FindLast((ProjectPropertyElement x) => x.Name.Equals(propertyToFind, StringComparison.OrdinalIgnoreCase) && IsChildElementOfSourcesOrMakeFile(x, project));
		}
		catch (InvalidOperationException)
		{
		}
		return result;
	}

	private static string PartiallyEvaluateRecursively(string unevaluatedValue, List<ProjectPropertyElement> propertyElements, Build.Evaluation.Project project)
	{
		if (string.IsNullOrWhiteSpace(unevaluatedValue))
		{
			return unevaluatedValue;
		}
		string text = unevaluatedValue;
		List<string> list = new List<string>(Parser.GetReferencedPropertyNames(unevaluatedValue));
		foreach (string item in list)
		{
			ProjectPropertyElement lastPropertyElement = GetLastPropertyElement(project, propertyElements, item);
			if (lastPropertyElement != null && !IsPropertyElementSetConditionally(lastPropertyElement, project, checkRecursively: false) && !string.IsNullOrEmpty(lastPropertyElement.Value))
			{
				int num = propertyElements.IndexOf(lastPropertyElement);
				string newValue;
				if (num > 0)
				{
					List<ProjectPropertyElement> range = propertyElements.GetRange(0, num);
					newValue = PartiallyEvaluateRecursively(lastPropertyElement.Value, range, project);
				}
				else
				{
					newValue = lastPropertyElement.Value;
				}
				text = text.Replace("$(" + item + ")", newValue);
			}
		}
		return text;
	}

	internal static string PartiallyEvaluate(string unevaluatedValue, Build.Evaluation.Project project, List<ProjectPropertyElement> projectPropertyElements = null)
	{
		Trace.Indent();
		if (!Path.GetExtension(project.FullPath).Equals(".vcxproj", StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException("Expected input project to be associated with a .vcxproj file");
		}
		Logger.TraceEvent(TraceEventType.Verbose, null, "Partially expanding \"{0}\"", unevaluatedValue);
		if (projectPropertyElements == null)
		{
			projectPropertyElements = GetPropertyElements(project);
		}
		string text = PartiallyEvaluateRecursively(unevaluatedValue, projectPropertyElements, project);
		Logger.TraceEvent(TraceEventType.Verbose, null, "\"{0}\" partially expanded to \"{1}\" ", unevaluatedValue, text);
		Trace.Unindent();
		return text;
	}

	private static bool IsPropertyElementSetConditionally(ProjectPropertyElement propertyElement, Build.Evaluation.Project project, bool checkRecursively = true)
	{
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		if (propertyElement == null)
		{
			return true;
		}
		Trace.Indent();
		foreach (ProjectElementContainer allParent in ((ProjectElement)propertyElement).AllParents)
		{
			if (allParent is ProjectWhenElement || allParent is ProjectOtherwiseElement || !string.IsNullOrWhiteSpace(((ProjectElement)propertyElement).Condition))
			{
				Logger.TraceEvent(TraceEventType.Verbose, null, "Property {0} is set conditionally. Immediate condition =\"{1}\". Conditional parent element = {2}", propertyElement.Name, ((ProjectElement)propertyElement).Condition, ((object)allParent).GetType());
				Trace.Unindent();
				return true;
			}
		}
		if (!IsChildElementOfSourcesOrMakeFile(propertyElement, project, checkRecursively: false) && IsChildElementOfSourcesOrMakeFile(propertyElement, project))
		{
			ProjectRootElement containingProject = ((ProjectElement)propertyElement).ContainingProject;
			while (!object.Equals(project.FullPath, containingProject.FullPath))
			{
				foreach (ResolvedImport import in project.Imports)
				{
					ResolvedImport current2 = import;
					if (object.Equals(current2.ImportedProject.FullPath, containingProject.FullPath))
					{
						if (!string.IsNullOrWhiteSpace(current2.ImportingElement.Condition) && !object.Equals(project.FullPath, current2.ImportingElement.ContainingProject.FullPath))
						{
							Logger.TraceEvent(TraceEventType.Verbose, null, "Property {0} is set conditionally. Imported via {1}, which has condition \"{2}\"", propertyElement.Name, containingProject.FullPath, ((ProjectElement)((ResolvedImport)(current2)).ImportingElement).Condition);
							Trace.Unindent();
							return true;
						}
						containingProject = current2.ImportingElement.ContainingProject;
						break;
					}
				}
			}
		}
		if (checkRecursively)
		{
			List<ProjectPropertyElement> propertyElements = GetPropertyElements(project);
			if (RecursivelyCheckIfPropertyElementIsSetConditionally(propertyElement, propertyElements, project))
			{
				return true;
			}
		}
		Trace.Unindent();
		return false;
	}

	internal static bool IsChildElementOfSourcesOrMakeFile(ProjectPropertyElement propertyElement, Build.Evaluation.Project project, bool checkRecursively = true)
	{
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		if (propertyElement == null)
		{
			return false;
		}
		ProjectRootElement containingProject = (propertyElement).ContainingProject;
		if (Path.GetFileName(containingProject.FullPath).Equals("sources.props", StringComparison.OrdinalIgnoreCase) || Path.GetFileName(containingProject.FullPath).Equals("makefile.inc.props", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		if (checkRecursively)
		{
			while (!object.Equals(project.FullPath, containingProject.FullPath))
			{
				foreach (ResolvedImport import in project.Imports)
				{
					ResolvedImport current = import;
					if (object.Equals(current.ImportedProject.FullPath, containingProject.FullPath))
					{
						containingProject = current.ImportingElement.ContainingProject;
						break;
					}
				}
				if (Path.GetFileName(containingProject.FullPath).Equals("sources.props", StringComparison.OrdinalIgnoreCase) || Path.GetFileName(containingProject.FullPath).Equals("makefile.inc.props", StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool RecursivelyCheckIfPropertyElementIsSetConditionally(ProjectPropertyElement propertyElement, List<ProjectPropertyElement> projectPropertyElements, Build.Evaluation.Project project)
	{
		if (IsPropertyElementSetConditionally(propertyElement, project, checkRecursively: false))
		{
			return true;
		}
		string[] referencedPropertyNames = Parser.GetReferencedPropertyNames(propertyElement.Value);
		foreach (string propertyToFind in referencedPropertyNames)
		{
			ProjectPropertyElement lastPropertyElement = GetLastPropertyElement(project, projectPropertyElements, propertyToFind);
			if (lastPropertyElement != null)
			{
				int num = projectPropertyElements.IndexOf(propertyElement) + 1;
				if (num == 0)
				{
					throw new ArgumentException("Did not find ProjectPropertyElement for property \"{0}\" in specified list", propertyElement.Name);
				}
				List<ProjectPropertyElement> range = projectPropertyElements.GetRange(0, num);
				if (RecursivelyCheckIfPropertyElementIsSetConditionally(lastPropertyElement, range, project))
				{
					Trace.Indent();
					Logger.TraceEvent(TraceEventType.Verbose, null, "Property {0} is set conditionally because a referenced property ({1}) is set conditionally.", propertyElement.Name, lastPropertyElement.Name);
					Trace.Unindent();
					return true;
				}
			}
		}
		return false;
	}

	private static bool IsPropertySetConditionally(string propertyName, Build.Evaluation.Project project, bool checkRecursively = true, List<ProjectPropertyElement> allPropertyElements = null)
	{
		bool result = false;
		if (propertyName == null)
		{
			return false;
		}
		if (allPropertyElements == null)
		{
			allPropertyElements = GetPropertyElements(project);
		}
		ProjectPropertyElement lastPropertyElement = GetLastPropertyElement(project, allPropertyElements, propertyName);
		if (lastPropertyElement != null)
		{
			ProjectProperty property = project.GetProperty(propertyName);
			result = (property != null && (property.IsEnvironmentProperty || property.IsGlobalProperty || property.IsReservedProperty)) || IsPropertyElementSetConditionally(lastPropertyElement, project, checkRecursively);
		}
		return result;
	}

	private FileConverter()
	{
	}

	internal static string GetConversionRoot()
	{
		if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
		{
			string text = null;
			RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey("SOFTWARE\\Microsoft\\Windows Kits\\WDK");
			if (registryKey != null)
			{
				try
				{
					object value = registryKey.GetValue("WDKContentRoot");
					if (value != null)
					{
						text = Path.Combine(value.ToString(), "bin\\conversion\\");
					}
					else
					{
						Logger.TraceEvent(TraceEventType.Critical, null, "Registry {0}: is empty\n", registryKey);
					}
				}
				catch (Exception ex)
				{
					Logger.TraceEvent(TraceEventType.Critical, null, "Error Reading Registry {0}:\nMessage:\n{1}", registryKey, ex.Message);
				}
				finally
				{
					registryKey.Close();
				}
			}
			if (!Directory.Exists(text))
			{
				throw new DirectoryNotFoundException(string.Format(CultureInfo.InvariantCulture, "Directory \"{0}\" not found", new object[1] { text }));
			}
			return text;
		}
		else
		{
			return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "conversion");
		}
	}

	public static string Convert(string nmakeFile, string convertedFile, bool supportPropertyPages, string projectFileName = null)
	{
		primaryInputFile = Path.GetFullPath(nmakeFile);
		msBuild.Reset();
		generatePropertyPages = supportPropertyPages;
		overrideProjectName = projectFileName;
		if (Path.GetFileName(nmakeFile).Equals("sources", StringComparison.OrdinalIgnoreCase))
		{
			primaryInputFileIsSources = true;
		}
		else
		{
			primaryInputFileIsSources = false;
		}
		string result = ConvertFile(primaryInputFile, convertedFile);
		msBuild.FinalizeProject(convertedFile);
		return result;
	}

	private static string ConvertFile(string nmakeFile, string convertedFile)
	{
		string result = null;
		Trace.Indent();
		Logger.TraceEvent(TraceEventType.Information, null, "Converting \"{0}\"", nmakeFile);
		try
		{
			List<SourcesDirective> parsedCommands = Parser.Parse(nmakeFile);
			if (!Parser.PassesSanityChecks(parsedCommands))
			{
				Logger.TraceEvent(TraceEventType.Critical, null, "Parsed data does not pass sanity checks, aborting. Please see the verbose text log \"{0}\"", Logger.TextLogPath);
				return result;
			}
			msBuild.GenerateMsBuildFile(convertedFile, ref parsedCommands);
			if (primaryInputFileIsSources && object.Equals(nmakeFile, primaryInputFile))
			{
				primaryConvertedFile = convertedFile;
			}
			ConvertImportedFiles(nmakeFile, convertedFile, ref parsedCommands);
			result = convertedFile;
			if (primaryInputFileIsSources && object.Equals(nmakeFile, primaryInputFile))
			{
				string text = Path.Combine(Path.GetDirectoryName(primaryInputFile), "makefile.inc");
				if (File.Exists(text))
				{
					ConvertFile(text, text + ".props");
				}
				string text2 = PostProcessing(nmakeFile, convertedFile);
				if (generatePropertyPages && !string.IsNullOrEmpty(text2) && File.Exists(text2))
				{
					AddIDESupport(text2);
				}
				result = text2;
				AddOverrideConditionsToProperies(convertedFile);
				if (File.Exists(text))
				{
					AddOverrideConditionsToProperies(text + ".props");
				}
			}
		}
		catch (Exception ex)
		{
			Logger.TraceEvent(TraceEventType.Critical, null, "Error while processing file {0}:\nMessage:\n{1}", nmakeFile, ex.Message);
			Console.Error.WriteLine("Error while converting:\n {0}\nVerbose Trace log: {1}", ex.Message, Logger.TextLogPath + Environment.NewLine);
			Logger.TraceEvent(TraceEventType.Verbose, null, "StackTrace=\n{0}", ex.StackTrace);
		}
		Trace.Unindent();
		return result;
	}

	private static void ConvertImportedFiles(string nmakeFile, string convertedFile, ref List<SourcesDirective> parsedCommands)
	{
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Expected O, but got Unknown
		//IL_01e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Expected O, but got Unknown
		//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Expected O, but got Unknown
		Trace.Indent();
		if (!primaryInputFileIsSources)
		{
			Logger.TraceEvent(TraceEventType.Warning, null, "Cannot automatically convert imported projects, if any, unless the primary input to the tool is a dirs or sources file. You specified \"{0}\". Please convert each file included by \"{1}\" individually.", primaryInputFile, nmakeFile);
			Trace.Unindent();
			return;
		}
		List<IncludeFile> list = new List<IncludeFile>();
		foreach (SourcesDirective parsedCommand in parsedCommands)
		{
			if (parsedCommand is IncludeFile)
			{
				list.Add(parsedCommand as IncludeFile);
			}
		}
		Directory.SetCurrentDirectory(Path.GetDirectoryName(primaryInputFile));
		List<string> list2 = new List<string>();
		ProjectRootElement val = ProjectRootElement.Open(convertedFile, new ProjectCollection());
		foreach (ProjectImportElement import in val.Imports)
		{
			ProjectImportElement val2 = (import is not null) ? import : null;
			string[] referencedPropertyNames = Parser.GetReferencedPropertyNames(val2.Project);
			if (referencedPropertyNames.Length != 1)
			{
				Logger.TraceEvent(TraceEventType.Error, 0, "Could not parse import \"{0}\" and identify the property that contains the path to imported file, skipping conversion of import.", val2.Project);
			}
			else
			{
				list2.Add(referencedPropertyNames[0]);
			}
		}
		if (list2.Count != 2 * list.Count)
		{
			Logger.TraceEvent(TraceEventType.Error, null, "Found {0} ProjectImportElements in project {1} yet {2} in the associated parsed commands. Expected 2 ProjectImportElements per !include command. Skipping conversion of imports, please convert the files that were omitted and re-run this tool on the sources file", list2.Count, convertedFile, list.Count);
			return;
		}
		Build.Evaluation.Project val3 = null;
		int num = 0;
		bool flag = false;
		bool flag2 = false;
		string text = null;
		foreach (string item in list2)
		{
			if (num % 2 == 0)
			{
				flag = false;
				flag2 = false;
			}
			else
			{
				flag = true;
			}
			try
			{
				ProjectRootElement projectRootWithInitialStateDefined = GetProjectRootWithInitialStateDefined(primaryConvertedFile);
				ProjectImportElement val4 = projectRootWithInitialStateDefined.CreateImportElement("makefile.inc.props");
				val4.Condition = "Exists('makefile.inc.props')";
				projectRootWithInitialStateDefined.AppendChild((ProjectElement)(object)val4);
				val3 = new Build.Evaluation.Project(projectRootWithInitialStateDefined, null, null, new ProjectCollection(), (ProjectLoadSettings)1);
			}
			catch (Exception ex)
			{
				Logger.TraceEvent(TraceEventType.Warning, null, "Cannot detect and convert files imported by file \"{0}\", which was derived from \"{1}\". Please convert each file included by \"{1}\" individually.\n\nMessage = {2}", convertedFile, nmakeFile, ex.Message);
				return;
			}
			IncludeFile includeFile = list[num / 2];
			string propertyValue = val3.GetPropertyValue(item);
			if (string.IsNullOrWhiteSpace(propertyValue))
			{
				Logger.TraceEvent(TraceEventType.Error, null, "Could not parse MSBuild import associated with \"{0}\", skipping conversion of import.", includeFile.NmakeFilePath);
				val3.ProjectCollection.UnloadAllProjects();
				continue;
			}
			propertyValue = Path.Combine(Path.GetDirectoryName(propertyValue), Path.GetFileNameWithoutExtension(propertyValue));
			if (!flag || propertyValue != text)
			{
				if (Parser.GetReferencedPropertyNames(includeFile.NmakeFilePath).Length > 0)
				{
					Logger.TraceEvent(TraceEventType.Warning, null, "The path to the file to be imported \"{0}\", contains references to other macros. It is possible that not all imported files will be converted if any macros referenced by this path are set conditionally. If not all the files are converted, please convert the files that were omitted and re-run this tool on the sources file.", includeFile.NmakeFilePath);
				}
				if (!Path.IsPathRooted(propertyValue))
				{
					Logger.TraceEvent(TraceEventType.Error, null, "Expected import path \"{0}\" corresponding to nmake path \"{1}\", to be fully rooted, skipping conversion of import.", propertyValue, includeFile.NmakeFilePath);
					val3.ProjectCollection.UnloadAllProjects();
					continue;
				}
				if (File.Exists(propertyValue))
				{
					Logger.TraceEvent(TraceEventType.Information, null, "Include file \"{0}\" resolved to \"{1}\"", includeFile.NmakeFilePath, propertyValue);
					flag2 = true;
					ConvertFile(propertyValue, propertyValue + ".props");
				}
				else
				{
					Logger.TraceEvent(TraceEventType.Verbose, null, "Did not find the file to be imported under the {0} directory", flag ? "alternate" : "primary");
					if (flag && !flag2)
					{
						Logger.TraceEvent(TraceEventType.Error, null, "Did not find the file associated with import \"{0}\", neither relative to the sources file directory, nor \"{1}\"", includeFile.NmakeFilePath, Path.GetDirectoryName(propertyValue));
					}
				}
			}
			val3.ProjectCollection.UnloadAllProjects();
			text = propertyValue;
			num++;
		}
		Trace.Unindent();
	}

	private static string PostProcessing(string nmakeFile, string convertedFile)
	{
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Expected O, but got Unknown
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Expected O, but got Unknown
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		//IL_0240: Expected O, but got Unknown
		Trace.Indent();
		string text = null;
		string conversionRoot = GetConversionRoot();
		string text2 = Path.Combine(conversionRoot, "AutoConverted.vcxproj.template");
		string text3 = Path.Combine(conversionRoot, "PreToolsetRules.props");
		string[] array = new string[4] { "TARGETTYPE", "DRIVERTYPE", "UMDF_VERSION_MAJOR", "KMDF_VERSION_MAJOR" };
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ "PlatformToolset", string.Empty },
			{ "ConfigurationType", string.Empty },
			{ "DriverType", string.Empty },
			{ "TARGETNAME", string.Empty }
		};
		try
		{
			ProjectRootElement projectRootWithInitialStateDefined = GetProjectRootWithInitialStateDefined(convertedFile);
			ProjectImportElement val = projectRootWithInitialStateDefined.CreateImportElement("makefile.inc.props");
			val.Condition = "Exists('makefile.inc.props')";
			projectRootWithInitialStateDefined.AppendChild((ProjectElement)(object)val);
			projectRootWithInitialStateDefined.AppendChild((ProjectElement)(object)projectRootWithInitialStateDefined.CreateImportElement(text3));
			Build.Evaluation.Project val2 = new Build.Evaluation.Project(projectRootWithInitialStateDefined, (IDictionary<string, string>)null, (string)null, new ProjectCollection(), (ProjectLoadSettings)1);
			List<ProjectPropertyElement> propertyElements = GetPropertyElements(val2);
			string[] array2 = array;
			foreach (string text4 in array2)
			{
				ProjectProperty property = val2.GetProperty(text4);
				if (IsPropertySetConditionally(text4, val2, checkRecursively: true, propertyElements) && (property == null || (property != null && property.Xml != null && !object.Equals(((ProjectElement)property.Xml).ContainingProject.FullPath, text3))))
				{
					Logger.TraceEvent(TraceEventType.Warning, null, "Property \"{0}\" is set conditionally. If it has multiple possible values the platform toolset selected for this project during conversion may be incorrect. Please review the PlatformToolset property in the final VcxProjFile", text4);
					break;
				}
			}
			foreach (string item in new List<string>(dictionary.Keys))
			{
				dictionary[item] = val2.GetPropertyValue(item);
				Logger.TraceEvent(TraceEventType.Verbose, null, "Extracted property \"{0}\" with a value of \"{1}\" from sources.", item, dictionary[item]);
			}
			val2.ProjectCollection.UnloadAllProjects();
			string text5 = dictionary["TARGETNAME"] + ".vcxproj";
			text5 = (string.IsNullOrWhiteSpace(text5) ? "AutoConverted.vcxproj.template" : text5);
			ProjectRootElement val3 = ProjectRootElement.Open(text2, new ProjectCollection());
			Logger.TraceEvent(TraceEventType.Verbose, null, "VcxProj file copied to \"{0}\"", text5);
			ProjectPropertyGroupElement val4 = val3.CreatePropertyGroupElement();
			((ProjectElement)val4).Label = "PropertySheets";
			((ProjectElementContainer)val3).PrependChild((ProjectElement)(object)val4);
			foreach (string key in dictionary.Keys)
			{
				val4.AddProperty(key, dictionary[key]);
			}
			if (!string.IsNullOrWhiteSpace(overrideProjectName))
			{
				Logger.TraceEvent(TraceEventType.Information, null, "Renaming project {0} to {1}, as requested", text5, overrideProjectName + ".vcxproj");
				text5 = overrideProjectName + ".vcxproj";
			}
			string text6 = "{" + Guid.NewGuid().ToString().ToUpper(CultureInfo.InvariantCulture) + "}";
			val3.AddProperty("ProjectGuid", text6);
			Logger.TraceEvent(TraceEventType.Verbose, null, "Added Project GUID: {0}", text6);
			text = Path.Combine(Path.GetDirectoryName(nmakeFile), text5);
			val3.Save(text);
			AddProjectConfigurations(text);
			Logger.TraceEvent(TraceEventType.Information, null, "Generated Project File {0}", text5);
		}
		catch (Exception ex)
		{
			Logger.TraceEvent(TraceEventType.Critical, null, "Error while post processing for {0}.\nMessage:\n{1}", nmakeFile, ex.Message);
			Logger.TraceEvent(TraceEventType.Critical, null, "Are are the associated *.inc files already converted?");
			Logger.TraceEvent(TraceEventType.Verbose, null, "StackTrace=\n{0}", ex.StackTrace);
		}
		Trace.Unindent();
		return text;
	}

	private static void AddProjectConfigurations(string finalProjectPath)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Expected O, but got Unknown
		Build.Evaluation.Project val = new Build.Evaluation.Project(finalProjectPath, (IDictionary<string, string>)null, (string)null, new ProjectCollection());
		string[] array = new string[2]
		{
			" Release".Trim(),
			" Debug".Trim()
		};
		List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
		if (GenerateProject.ConversionSettings.AddArmConfigurations)
		{
			list.Add(new KeyValuePair<string, string>("Arm", "DDK_BLOCK_ON_ARM"));
			Logger.TraceEvent(TraceEventType.Information, null, "Addition of Arm platform configurations to the project was requested");
		}
		list.Add(new KeyValuePair<string, string>("x64", "DDK_BLOCK_ON_AMD64"));
		list.Add(new KeyValuePair<string, string>("Win32", "DDK_BLOCK_ON_X86"));
		List<string> list2 = new List<string>();
		foreach (KeyValuePair<string, string> item in list)
		{
			if (val.GetProperty(item.Value) == null)
			{
				list2.Add(item.Key);
			}
		}
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		char[] trimChars = new char[2] { '0', 'x' };
		int result = 0;
		int result2 = int.MaxValue;
		ProjectProperty property = val.GetProperty("_NT_TARGET_VERSION");
		string propertyValue = val.GetPropertyValue("_NT_TARGET_VERSION");
		if (property != null && !IsPropertySetConditionally("_NT_TARGET_VERSION", val) && !string.IsNullOrEmpty(propertyValue) && IsChildElementOfSourcesOrMakeFile(property.Xml, val))
		{
			if (!int.TryParse(propertyValue.TrimStart(trimChars), NumberStyles.HexNumber, null, out result))
			{
				result = 0;
			}
		}
		else
		{
			property = val.GetProperty("MINIMUM_NT_TARGET_VERSION");
			propertyValue = val.GetPropertyValue("MINIMUM_NT_TARGET_VERSION");
			if (property != null && IsChildElementOfSourcesOrMakeFile(property.Xml, val) && !IsPropertySetConditionally("MINIMUM_NT_TARGET_VERSION", val) && !string.IsNullOrEmpty(propertyValue) && !int.TryParse(propertyValue.TrimStart(trimChars), NumberStyles.HexNumber, null, out result))
			{
				result = 0;
			}
		}
		ProjectProperty property2 = val.GetProperty("MAXIMUM_NT_TARGET_VERSION");
		string propertyValue2 = val.GetPropertyValue("MAXIMUM_NT_TARGET_VERSION");
		if (property2 != null && IsChildElementOfSourcesOrMakeFile(property2.Xml, val) && !IsPropertySetConditionally("MAXIMUM_NT_TARGET_VERSION", val) && !string.IsNullOrEmpty(propertyValue2) && !int.TryParse(propertyValue2.TrimStart(trimChars), NumberStyles.HexNumber, null, out result2))
		{
			result2 = int.MaxValue;
		}
		if (result <= 1536 && 1536 <= result2)
		{
			dictionary.Add("Vista", "Vista");
		}
		if (result <= 1537 && 1537 <= result2)
		{
			dictionary.Add("Win7", "Win7");
		}
		if (result <= 1538 && 1538 <= result2)
		{
			dictionary.Add("Win8", "Win8");
		}
		KeyValuePair<string, string>[] source = new KeyValuePair<string, string>[2]
		{
			new KeyValuePair<string, string>("Arm", "Vista"),
			new KeyValuePair<string, string>("Arm", "Win7")
		};
		List<string[]> list3 = new List<string[]>();
		foreach (string item2 in list2)
		{
			string[] array2 = array;
			foreach (string text in array2)
			{
				foreach (string key3 in dictionary.Keys)
				{
					if (!source.Contains(new KeyValuePair<string, string>(item2, key3)))
					{
						list3.Add(new string[3] { key3, item2, text });
					}
				}
			}
		}
		ProjectPropertyGroupElement val2 = null;
		foreach (ProjectPropertyGroupElement propertyGroup in val.Xml.PropertyGroups)
		{
			if (!string.IsNullOrEmpty(((ProjectElement)propertyGroup).Label) && ((ProjectElement)propertyGroup).Label.Equals("Globals", StringComparison.OrdinalIgnoreCase))
			{
				val2 = propertyGroup;
			}
		}
		if (val2 == null)
		{
			Logger.TraceEvent(TraceEventType.Critical, null, "No PropertyGroup labeled as \"Globals\" was found in the project template");
		}
		foreach (string[] item3 in list3)
		{
			string text2 = item3[0];
			string text3 = item3[1];
			string text4 = item3[2];
			ProjectPropertyGroupElement val3 = val.Xml.CreatePropertyGroupElement();
			((ProjectElementContainer)val.Xml).InsertAfterChild((ProjectElement)(object)val3, (ProjectElement)(object)val2);
			((ProjectElement)val3).Label = "Configuration";
			((ProjectElement)val3).Condition = string.Format(CultureInfo.InvariantCulture, "'$(Configuration)|$(Platform)'=='{0} {1}|{2}'", new object[3]
			{
				dictionary[text2],
				text4,
				text3
			});
			val3.AddProperty("TargetVersion", text2);
			val3.AddProperty("UseDebugLibraries", (text4 == "Debug") ? "True" : "False");
			val.Xml.AddProperty("Configuration", dictionary[text2] + " " + text4);
		}
		ProjectItemGroupElement val4 = val.Xml.CreateItemGroupElement();
		((ProjectElementContainer)val.Xml).InsertBeforeChild((ProjectElement)(object)val4, ((ProjectElementContainer)val.Xml).FirstChild);
		((ProjectElement)val4).Label = "ProjectConfigurations";
		foreach (string[] item4 in list3)
		{
			string key = item4[0];
			string text5 = item4[1];
			string text6 = item4[2];
			string text7 = dictionary[key] + " " + text6;
			ProjectItemElement val5 = val.Xml.CreateItemElement("ProjectConfiguration", text7 + "|" + text5);
			((ProjectElementContainer)val4).PrependChild((ProjectElement)(object)val5);
			val5.AddMetadata("Configuration", text7);
			val5.AddMetadata("Platform", text5);
		}
		val.Save();
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.Load(finalProjectPath);
		List<XmlNode> list4 = new List<XmlNode>();
		foreach (XmlNode childNode in xmlDocument.GetElementsByTagName("Project").Item(0).ChildNodes)
		{
			if (childNode.Attributes != null)
			{
				XmlNode namedItem = childNode.Attributes.GetNamedItem("Label");
				if (namedItem != null && namedItem.Value.Contains("ConfigurationDependent"))
				{
					namedItem.Value = namedItem.Value.Replace("ConfigurationDependent;", string.Empty).Replace(";ConfigurationDependent", string.Empty).Replace("ConfigurationDependent", string.Empty);
					list4.Insert(0, childNode);
				}
			}
		}
		foreach (XmlNode item5 in list4)
		{
			foreach (string[] item6 in list3)
			{
				string key2 = item6[0];
				string text8 = item6[1];
				string text9 = item6[2];
				XmlNode xmlNode2 = item5.ParentNode.InsertBefore(item5.CloneNode(deep: true), item5);
				((XmlElement)xmlNode2).SetAttribute("Condition", string.Format(CultureInfo.InvariantCulture, "'$(Configuration)|$(Platform)'=='{0} {1}|{2}'", new object[3]
				{
					dictionary[key2],
					text9,
					text8
				}));
			}
			item5.ParentNode.RemoveChild(item5);
		}
		xmlDocument.Save(finalProjectPath);
	}

	private static void AddOverrideConditionsToProperies(string propsFile)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected O, but got Unknown
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		Trace.Indent();
		Build.Evaluation.Project val = new Build.Evaluation.Project(propsFile, (IDictionary<string, string>)GetInitialStateProperties(), (string)null, new ProjectCollection(), (ProjectLoadSettings)1);
		List<ProjectRootElement> list = val.Imports.Select((ResolvedImport i) => ((ResolvedImport)i).ImportedProject).ToList();
		list.Add(val.Xml);
		foreach (ProjectRootElement item in list)
		{
			foreach (ProjectPropertyElement property in item.Properties)
			{
				if (property != null)
				{
					string condition = ((ProjectElement)property).Condition;
					string text = string.Format(CultureInfo.InvariantCulture, "'$({0})'!='true'", new object[1] { GetOverRidePropertyName(property.Name) });
					if (!string.IsNullOrWhiteSpace(condition))
					{
						text = string.Format(CultureInfo.InvariantCulture, "({0}) And ({1})", new object[2] { condition, text });
					}
					((ProjectElement)property).Condition = text;
				}
			}
			item.Save();
		}
		val.Save();
		val.ProjectCollection.UnloadAllProjects();
		Logger.TraceEvent(TraceEventType.Verbose, null, "Appended override conditions to properties in file '{0}' and its imports", propsFile);
		Trace.Unindent();
	}

	public static string GetOverRidePropertyName(string propertyName)
	{
		if (string.IsNullOrWhiteSpace(propertyName))
		{
			throw new ArgumentException("Argument macroName cannot be null or empty");
		}
		return "OVERRIDE_" + propertyName.Trim();
	}

	private static ProjectRootElement GetProjectRootWithInitialStateDefined(string filePath)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Expected O, but got Unknown
		if (initialStateProperties == null)
		{
			initialStateProperties = GetInitialStateProperties();
		}
		ProjectRootElement val = ProjectRootElement.Open(filePath, new ProjectCollection());
		ProjectPropertyGroupElement val2 = val.CreatePropertyGroupElement();
		val.PrependChild((ProjectElement)(object)val2);
		foreach (string key in initialStateProperties.Keys)
		{
			val2.AddProperty(key, initialStateProperties[key]);
		}
		return val;
	}

	private static Dictionary<string, string> GetInitialStateProperties()
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Expected O, but got Unknown
		string conversionRoot = GetConversionRoot();
		string text = Path.Combine(conversionRoot, "Conversion.Default.props");
		Build.Evaluation.Project val = new Build.Evaluation.Project(text, (IDictionary<string, string>)null, (string)null, new ProjectCollection());
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		foreach (ProjectProperty property in val.Properties)
		{
			if (!property.IsEnvironmentProperty && !property.IsGlobalProperty && !property.IsReservedProperty)
			{
				dictionary.Add(property.Name, property.EvaluatedValue);
			}
		}
		val.ProjectCollection.UnloadAllProjects();
		return dictionary;
	}

	public static bool RequiredFilesArePresent()
	{
		Trace.Indent();
		string conversionRoot = GetConversionRoot();
		string[] array = new string[5]
		{
			Path.Combine(conversionRoot, "AutoConverted.vcxproj.template"),
			Path.Combine(conversionRoot, "PreToolsetRules.props"),
			Path.Combine(conversionRoot, "Conversion.Default.props"),
			Path.Combine(conversionRoot, "Filters.template"),
			Path.Combine(conversionRoot, "Package.vcxproj.template")
		};
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (!File.Exists(text))
			{
				Logger.TraceEvent(TraceEventType.Critical, null, "File \"{0}\" not found", text);
				Trace.Unindent();
				return false;
			}
		}
		Trace.Unindent();
		return true;
	}
}
