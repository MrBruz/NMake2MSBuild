#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using Microsoft.DriverKit.NMakeConverter;

namespace Microsoft.DriverKit.MakeSolution;

internal class DependencyScanner
{
	private static string OutDir;

	private static List<string> referencedPaths = new List<string>();

	private static string[] supportedProjectFormats = new string[2]
	{
		".vcxproj".ToLowerInvariant(),
		".csproj".ToLowerInvariant()
	};

	private static Build.Evaluation.Project vsProject;

	private static Dictionary<string, string[]> dependencyCache = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

	private DependencyScanner()
	{
	}

	internal static void ClearCache()
	{
		dependencyCache.Clear();
	}

	public static string[] GetPrerequisiteProjects(string projectPath, bool cachedResultsAcceptable = true)
	{
		projectPath = Path.GetFullPath(projectPath);
		if (!File.Exists(projectPath))
		{
			Logger.TraceEvent(TraceEventType.Critical, null, "Project {0} does not exist", projectPath);
			throw new FileNotFoundException("File not found:", projectPath);
		}
		if (cachedResultsAcceptable && dependencyCache.ContainsKey(projectPath))
		{
			Logger.TraceEvent(TraceEventType.Verbose, null, "Satisfying dependency check for {0} using cached data", projectPath);
			return dependencyCache[projectPath];
		}
		if (Array.IndexOf(supportedProjectFormats, Path.GetExtension(projectPath).ToLowerInvariant()) < 0)
		{
			string text = string.Format(CultureInfo.InvariantCulture, "Unsupported format for project file {0}", new object[1] { projectPath });
			Logger.TraceEvent(TraceEventType.Critical, null, text);
			throw new Exception(text);
		}
		if (Path.GetExtension(projectPath).Equals(".csproj", StringComparison.OrdinalIgnoreCase))
		{
			Logger.TraceEvent(TraceEventType.Verbose, null, "Dependency detection for c# projects is not supported. Ignoring potential dependencies of project {0}", projectPath);
			return new string[0];
		}
		Logger.TraceEvent(TraceEventType.Verbose, null, "Scanning for prerequisites for project {0}", projectPath);
		EvaluateProject(projectPath);
		ExtractIncludesFromItem("ClCompile");
		ExtractIncludesFromItem("MessageCompile");
		ExtractIncludesFromItem("Midl");
		ExtractIncludesFromItem("ResourceCompile");
		ExtractIncludesFromItemMetadata("Link", "AdditionalDependencies", ";");
		ExtractIncludesFromItemMetadata("ClCompile", "AdditionalIncludeDirectories", ";");
		string[] array = FindPreReqProjectsInReferences(projectPath);
		if (array.Length > 0)
		{
			string text2 = Environment.NewLine + new string(' ', Trace.IndentSize * (Trace.IndentLevel + 1));
			Logger.TraceEvent(TraceEventType.Verbose, null, "Final list of pre requisite projects for {0}: " + text2 + "{1}", projectPath, string.Join(text2, array));
		}
		else
		{
			Logger.TraceEvent(TraceEventType.Verbose, null, "No prerequisite projects found for project {0}", projectPath);
		}
		UnloadProject();
		dependencyCache.Add(projectPath, array);
		return array;
	}

	private static void ExtractIncludesFromItem(string itemType)
	{
		Logger.TraceEvent(TraceEventType.Verbose, null, "Extracting include elements from all items of type {0}", itemType);
		ICollection<ProjectItem> itemsIgnoringCondition = vsProject.GetItemsIgnoringCondition(itemType);
		bool flag = false;
		foreach (ProjectItem item in itemsIgnoringCondition)
		{
			flag = true;
			string text = item.EvaluatedInclude.Trim();
			if (!referencedPaths.Contains<string>(text, StringComparer.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(text))
			{
				referencedPaths.Add(text);
			}
		}
		if (!flag)
		{
			Logger.TraceEvent(TraceEventType.Verbose, null, "Didn't find any items of type {0} in project {1}", itemType, vsProject.FullPath);
		}
	}

	private static void ExtractIncludesFromItemMetadata(string itemType, string metadataName, string splitter)
	{
		Logger.TraceEvent(TraceEventType.Verbose, null, "Extracting include elements from metadata {0} on item type {1}, using splitter {2}", metadataName, itemType, (splitter == null) ? "None" : splitter);
		ICollection<ProjectItem> itemsIgnoringCondition = vsProject.GetItemsIgnoringCondition(itemType);
		using IEnumerator<ProjectItem> enumerator = itemsIgnoringCondition.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			return;
		}
		ProjectItem current = enumerator.Current;
		string text = null;
		foreach (ProjectMetadata metadatum in current.Metadata)
		{
			if (metadatum.Name.Equals(metadataName, StringComparison.OrdinalIgnoreCase))
			{
				text = metadatum.EvaluatedValue;
				break;
			}
		}
		string[] array = new string[1] { text };
		if (text == null)
		{
			Logger.TraceEvent(TraceEventType.Information, null, "Didn't find metadata {0} on item {1} in project {2}", metadataName, itemType, vsProject.FullPath);
			return;
		}
		if (splitter != null)
		{
			array = text.Split(splitter.ToCharArray());
		}
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			string text3 = text2.Trim();
			if (!referencedPaths.Contains<string>(text3, StringComparer.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(text3))
			{
				referencedPaths.Add(text3);
			}
		}
	}

	private static void EvaluateProject(string projectPath)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		OutDir = Guid.NewGuid().ToString();
		Logger.TraceEvent(TraceEventType.Verbose, null, "$(O) set to {0}", OutDir);
		string directoryName = Path.GetDirectoryName(Path.GetFullPath(projectPath));
		dictionary.Add("IntDir", OutDir);
		dictionary.Add("OutDir", OutDir);
		dictionary.Add("O", OutDir);
		dictionary.Add("SolutionDir", directoryName);
		vsProject = new Build.Evaluation.Project(projectPath, dictionary, null, new ProjectCollection(), (ProjectLoadSettings)1);
		vsProject.AddItem("Link", "Dependency_Finder_Dummy.obj");
		vsProject.AddItem("ClCompile", "Dependency_Finder_Dummy.c");
		vsProject.ReevaluateIfNecessary();
	}

	private static string[] FindPreReqProjectsInReferences(string projectPath)
	{
		string propertyValue = vsProject.GetPropertyValue("SolutionDir");
		List<string> list = new List<string>();
		Trace.Indent();
		string currentDirectory = Directory.GetCurrentDirectory();
		Directory.SetCurrentDirectory(Path.GetDirectoryName(projectPath));
		for (int i = 0; i < referencedPaths.Count; i++)
		{
			if (referencedPaths[i].IndexOf(OutDir, StringComparison.OrdinalIgnoreCase) < 0)
			{
				continue;
			}
			string fullPath = Path.GetFullPath(referencedPaths[i]);
			if (fullPath.IndexOf(Path.Combine(propertyValue, OutDir), StringComparison.OrdinalIgnoreCase) >= 0)
			{
				Logger.TraceEvent(TraceEventType.Verbose, null, "Ignoring following path since it references the project's own $(O) {0}", fullPath);
				continue;
			}
			string text = fullPath.Split(new string[1] { OutDir }, StringSplitOptions.None)[0];
			if (!Directory.Exists(text))
			{
				string text2 = string.Format(CultureInfo.InvariantCulture, "Expected to find directory {0} due to entry {1}. Note that every project this project depends on must already have an exisitng VS project in its directory.", new object[2]
				{
					text,
					referencedPaths[i].Replace(OutDir, "$(O)")
				});
				Logger.TraceEvent(TraceEventType.Error, null, text2);
				Directory.SetCurrentDirectory(currentDirectory);
				throw new Exception(text2);
			}
			List<string> list2 = new List<string>();
			Logger.TraceEvent(TraceEventType.Verbose, null, "Scanning path {0} for projects of format {1}", text, string.Join("  ", supportedProjectFormats));
			string[] array = supportedProjectFormats;
			foreach (string text3 in array)
			{
				string[] files = Directory.GetFiles(text, "*" + text3, SearchOption.TopDirectoryOnly);
				if (files != null && files.Length > 0)
				{
					list2.AddRange(files);
				}
			}
			if (list2.Count < 1 || GenerateProject.ConversionSettings.AddArmConfigurations)
			{
				string text4 = Path.Combine(text, "sources");
				if (File.Exists(text4))
				{
					Logger.TraceEvent(TraceEventType.Information, null, "Converting project \"{0}\" in order to investigate and resolve dependencies of project {0}", text4, projectPath);
					string text5 = null;
					try
					{
						text5 = FileConverter.Convert(text4, text4 + ".props", !GenerateProject.ConversionSettings.SafeMode);
						Directory.SetCurrentDirectory(Path.GetDirectoryName(projectPath));
					}
					catch (Exception ex)
					{
						Logger.TraceEvent(TraceEventType.Critical, null, "Critical failure while converting '{0}', skipping conversion. Error:\nMessage:{1}", text4, ex.Message);
						Logger.TraceEvent(TraceEventType.Verbose, null, "StackTrace=\n{0}", ex.StackTrace);
					}
					if (!string.IsNullOrEmpty(text5) && File.Exists(text5))
					{
						list2.Add(text5);
					}
				}
			}
			if (list2.Count < 1)
			{
				string format = string.Format(CultureInfo.InvariantCulture, "Expected to find project files at {0} due to entry {1}. Note that every project this project depends on must alreadyhave an exisitng VS project or sources file in its directory.", new object[2]
				{
					text,
					referencedPaths[i].Replace(OutDir, "$(O)")
				});
				Logger.TraceEvent(TraceEventType.Error, null, format);
			}
			string text6 = Environment.NewLine + "   ";
			Logger.TraceEvent(TraceEventType.Verbose, null, "Found the following pre requisite projects for project {0}: " + text6 + "{1}", projectPath, string.Join(text6, list2));
			foreach (string item in list2)
			{
				if (!list.Contains<string>(item, StringComparer.OrdinalIgnoreCase) && !item.Equals(projectPath, StringComparison.OrdinalIgnoreCase))
				{
					list.Add(item);
				}
			}
		}
		Directory.SetCurrentDirectory(currentDirectory);
		Trace.Unindent();
		return list.ToArray();
	}

	private static void UnloadProject()
	{
		vsProject.ProjectCollection.UnloadAllProjects();
		vsProject.ProjectCollection.Dispose();
		vsProject = null;
	}
}
