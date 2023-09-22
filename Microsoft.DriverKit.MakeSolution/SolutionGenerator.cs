#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.DriverKit.NMakeConverter;

namespace Microsoft.DriverKit.MakeSolution;

internal static class SolutionGenerator
{
	private class ConfigurationComparer : IComparer<string>
	{
		int IComparer<string>.Compare(string a, string b)
		{
			int configurationScore = GetConfigurationScore(a);
			int configurationScore2 = GetConfigurationScore(b);
			if (configurationScore == configurationScore2)
			{
				return 0;
			}
			if (configurationScore <= configurationScore2)
			{
				return 1;
			}
			return -1;
		}

		private int GetConfigurationScore(string configuration)
		{
			configuration = configuration.ToLowerInvariant();
			int num = 0;
			if (configuration.Contains("Arm".ToLowerInvariant()))
			{
				num++;
			}
			if (configuration.Contains("x64".ToLowerInvariant()))
			{
				num += 2;
			}
			if (configuration.Contains("Win32".ToLowerInvariant()))
			{
				num += 3;
			}
			if (configuration.Contains(" Release".Trim().ToLowerInvariant()))
			{
				num++;
			}
			if (configuration.Contains(" Debug".Trim().ToLowerInvariant()))
			{
				num += 4;
			}
			if (configuration.Contains("Vista".ToLowerInvariant()))
			{
				num++;
			}
			if (configuration.Contains("Win7".ToLowerInvariant()))
			{
				num += 7;
			}
			if (configuration.Contains("Win8".ToLowerInvariant()))
			{
				num += 13;
			}
			return num;
		}
	}

	private static Dictionary<string, Project> projects;

	private static List<string> solutionConfigurations;

	private static string solutionFilePath;

	private static List<string[]> prerequisitesLists;

	private static List<string> solutionProjects;

	private static List<string> projectsBeingScanned;

	public static bool CreateSolution(string outFilePath, string[] projectPaths, List<string[]> pathsToPrerequisiteProjects)
	{
		Trace.Indent();
		solutionFilePath = Path.GetFullPath(outFilePath);
		InitializeProjectsForSolution(projectPaths, pathsToPrerequisiteProjects);
		TextWriter solutionFile;
		try
		{
			if (!Directory.Exists(Path.GetDirectoryName(solutionFilePath)))
			{
				Directory.CreateDirectory(Path.GetDirectoryName(solutionFilePath));
			}
			solutionFile = new StreamWriter(solutionFilePath, append: false, Encoding.UTF8);
		}
		catch (UnauthorizedAccessException ex)
		{
			Logger.TraceEvent(TraceEventType.Critical, null, "Error opening solution file: {0}", ex.Message);
			return false;
		}
		try
		{
			solutionFile.WriteLine();
			solutionFile.WriteLine("Microsoft Visual Studio Solution File, Format Version 12.00");
			solutionFile.WriteLine("# Visual Studio 11");
			WriteProjectEntries(ref solutionFile);
			solutionFile.WriteLine("Global");
			WriteSolutionConfiguration(ref solutionFile);
			WriteProjectConfigurations(ref solutionFile);
			solutionFile.WriteLine("\tGlobalSection(SolutionProperties) = preSolution");
			solutionFile.WriteLine("\t\tHideSolutionNode = FALSE");
			solutionFile.WriteLine("\tEndGlobalSection");
			WriteSolutionFolderMapping(solutionFile);
			solutionFile.WriteLine("EndGlobal");
		}
		catch (Exception ex2)
		{
			Logger.TraceEvent(TraceEventType.Critical, null, "Error attempting to generate solution '{0}':\nMessage:\n{1}", solutionFilePath, ex2.Message);
			Logger.TraceEvent(TraceEventType.Verbose, null, "StackTrace=\n{0}", ex2.StackTrace);
			solutionFile.Close();
			return false;
		}
		solutionFile.Close();
		Trace.Unindent();
		return true;
	}

	private static void InitializeProjectsForSolution(string[] projectPaths, List<string[]> pathsToPrerequisiteProjects)
	{
		Trace.Indent();
		projects = new Dictionary<string, Project>(StringComparer.OrdinalIgnoreCase);
		if (projectPaths.Length != pathsToPrerequisiteProjects.Count())
		{
			throw new ArgumentException("Number of elements in the projectPaths list was not equal to number of elements in the pathsToPrerequisiteProjects list");
		}
		string text = Environment.NewLine + "    ";
		Logger.TraceEvent(TraceEventType.Information, null, "Generating solution {0} from:" + text + "{1}", solutionFilePath, string.Join(text, projectPaths));
		projectPaths = projectPaths.Select((string p) => Path.GetFullPath(p)).ToArray();
		string[] array = projectPaths;
		foreach (string text2 in array)
		{
			if (!File.Exists(text2))
			{
				Logger.TraceEvent(TraceEventType.Critical, null, "Project {0} does not exist", text2);
				Trace.Unindent();
				throw new FileNotFoundException("File not found:", text2);
			}
			projects[text2] = new MakeSolution.Project(text2);
		}
		for (int j = 0; j < projectPaths.Length; j++)
		{
			Project project = projects[projectPaths[j]];
			if (pathsToPrerequisiteProjects[j] == null || pathsToPrerequisiteProjects[j].Length == 0)
			{
				Logger.TraceEvent(TraceEventType.Verbose, null, "Project {0} does not depend on others", project.projectPath);
				continue;
			}
			Logger.TraceEvent(TraceEventType.Verbose, null, "Project {0} depends on:" + text + "{1}", project.projectPath, string.Join(text, pathsToPrerequisiteProjects[j]));
			string[] array2 = pathsToPrerequisiteProjects[j];
			foreach (string text3 in array2)
			{
				Project project2 = projects[text3];
				if (project2 == null)
				{
					string text4 = string.Format(CultureInfo.InvariantCulture, "Project {0} is a prerequisite for project {1}, but it was not specified in the list of projects to be added to the solution", new object[2] { text3, project.projectPath });
					Logger.TraceEvent(TraceEventType.Critical, null, text4);
					throw new Exception(text4);
				}
				project.AddPreReqProjectGUID(project2.GUID);
			}
		}
		int length = StringUtilities.GetCommonDirectoryPath(projects.Keys).Length;
		Dictionary<string, SolutionFolder> dictionary = new Dictionary<string, SolutionFolder>(StringComparer.OrdinalIgnoreCase);
		foreach (Project value in projects.Values)
		{
			string text5 = string.Empty;
			string text6 = Path.GetDirectoryName(value.projectPath).Substring(length).Trim(new char[1] { '\\' });
			SolutionFolder solutionFolder = null;
			while (!string.IsNullOrWhiteSpace(text6))
			{
				int num = text6.IndexOf('\\');
				string text7 = text6.Substring(0, (num > 0) ? num : text6.Length);
				string text8 = Path.Combine(text5, text7);
				if (dictionary.ContainsKey(text8))
				{
					solutionFolder = dictionary[text8];
				}
				else
				{
					SolutionFolder solutionFolder2 = ((!string.IsNullOrEmpty(text5)) ? dictionary[text5] : null);
					if (text7.Length > 0)
					{
						text7 = char.ToUpperInvariant(text7[0]) + text7.Substring((text7.Length > 1) ? 1 : 0);
					}
					solutionFolder = new SolutionFolder(text7, solutionFolder2, "{" + Guid.NewGuid().ToString().ToUpper(CultureInfo.InvariantCulture) + "}");
					solutionFolder2?.ImmediateChildren.Add(solutionFolder);
					dictionary.Add(text8, solutionFolder);
				}
				text5 = text8;
				text6 = text6.Substring((num > 0) ? num : text6.Length).Trim(new char[1] { '\\' });
			}
			value.SolutionFolder = solutionFolder;
		}
		Trace.Unindent();
	}

	private static void WriteSolutionFolderMapping(TextWriter solutionFile)
	{
		if (!projects.Values.Any((Project p) => p.SolutionFolder != null))
		{
			return;
		}
		solutionFile.WriteLine("\tGlobalSection(NestedProjects) = preSolution");
		foreach (Project value in projects.Values)
		{
			if (value.SolutionFolder != null)
			{
				solutionFile.WriteLine(string.Format(CultureInfo.InvariantCulture, "\t\t{0} = {1}", new object[2]
				{
					value.GUID,
					value.SolutionFolder.GUID
				}));
			}
		}
		foreach (SolutionFolder allSolutionFolder in GetAllSolutionFolders(projects.Values))
		{
			if (allSolutionFolder.Parent != null)
			{
				solutionFile.WriteLine(string.Format(CultureInfo.InvariantCulture, "\t\t{0} = {1}", new object[2]
				{
					allSolutionFolder.GUID,
					allSolutionFolder.Parent.GUID
				}));
			}
		}
		solutionFile.WriteLine("\tEndGlobalSection");
	}

	private static List<SolutionFolder> GetAllSolutionFolders(IEnumerable<Project> projects)
	{
		List<SolutionFolder> list = new List<SolutionFolder>();
		foreach (Project project in projects)
		{
			if (project.SolutionFolder != null)
			{
				if (!list.Contains(project.SolutionFolder))
				{
					list.Add(project.SolutionFolder);
				}
				SolutionFolder parent = project.SolutionFolder.Parent;
				while (parent != null && !list.Contains(parent))
				{
					list.Add(parent);
					parent = parent.Parent;
				}
			}
		}
		return list;
	}

	private static void WriteSolutionConfiguration(ref TextWriter solutionFile)
	{
		solutionConfigurations = new List<string>();
		foreach (Project value in projects.Values)
		{
			List<string> configurations = value.Configurations;
			List<string> platforms = value.Platforms;
			for (int i = 0; i < platforms.Count; i++)
			{
				string text = configurations[i] + "|" + platforms[i];
				if (!solutionConfigurations.Contains<string>(text, StringComparer.OrdinalIgnoreCase))
				{
					solutionConfigurations.Add(text);
				}
			}
		}
		solutionConfigurations.Sort(new ConfigurationComparer());
		solutionFile.WriteLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
		foreach (string solutionConfiguration in solutionConfigurations)
		{
			solutionFile.WriteLine("\t\t{0} = {0}", solutionConfiguration);
		}
		solutionFile.WriteLine("\tEndGlobalSection");
	}

	private static void WriteProjectConfigurations(ref TextWriter solutionFile)
	{
		solutionFile.WriteLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
		foreach (Project value in projects.Values)
		{
			Logger.TraceEvent(TraceEventType.Verbose, null, "Writing project configurations for {0}", value.projectPath);
			foreach (string solutionConfiguration in solutionConfigurations)
			{
				string arg = FindBestConfigurationMatch(solutionConfiguration, value);
				solutionFile.WriteLine("\t\t{0}.{1}.ActiveCfg = {2}", value.GUID, solutionConfiguration, arg);
				solutionFile.WriteLine("\t\t{0}.{1}.Build.0 = {2}", value.GUID, solutionConfiguration, arg);
			}
		}
		solutionFile.WriteLine("\tEndGlobalSection");
	}

	private static string FindBestConfigurationMatch(string solutionConfiguration, Project project)
	{
		Trace.Indent();
		List<string> configurations = project.Configurations;
		List<string> platforms = project.Platforms;
		string text = solutionConfiguration.Split("|".ToCharArray())[0];
		string value = solutionConfiguration.Split("|".ToCharArray())[1];
		int num = -1;
		for (int i = 0; i < platforms.Count; i++)
		{
			if (platforms[i].Equals(value, StringComparison.OrdinalIgnoreCase))
			{
				num = i;
				if (configurations[i].Equals(text, StringComparison.OrdinalIgnoreCase))
				{
					Trace.Unindent();
					return configurations[i] + "|" + platforms[i];
				}
			}
		}
		string text2;
		if (num >= 0)
		{
			text2 = configurations[num] + "|" + platforms[num];
			Logger.TraceEvent(TraceEventType.Verbose, null, "Best match for solution configuration {0}  was {1} for project {2}, differs in configuraion", solutionConfiguration, text2, Path.GetFileName(project.projectPath));
			Trace.Unindent();
			return text2;
		}
		int num2 = configurations.IndexOf(text);
		if (num2 >= 0)
		{
			text2 = configurations[num2] + "|" + platforms[num2];
			Logger.TraceEvent(TraceEventType.Warning, null, "Best match for solution configuration {0}  was {1} for project {2}. Differrs in Platform", solutionConfiguration, text2, Path.GetFileName(project.projectPath));
			Trace.Unindent();
			return text2;
		}
		text2 = configurations[0] + "|" + platforms[0];
		Logger.TraceEvent(TraceEventType.Warning, null, "Best match for solution configuration {0}  was {1} for project {2}", solutionConfiguration, text2, Path.GetFileName(project.projectPath));
		Trace.Unindent();
		return text2;
	}

	private static void WriteProjectEntries(ref TextWriter solutionFile)
	{
		Trace.Indent();
		foreach (SolutionFolder allSolutionFolder in GetAllSolutionFolders(projects.Values))
		{
			solutionFile.WriteLine("Project(\"{0}\") = \"{1}\", \"{2}\", \"{3}\"", "{2150E333-8FDC-42A3-9474-1A3956D46DE8}", allSolutionFolder.Name, allSolutionFolder.Name, allSolutionFolder.GUID);
			solutionFile.WriteLine("EndProject");
		}
		foreach (Project value in projects.Values)
		{
			string name = value.name;
			solutionFile.WriteLine("Project(\"{0}\") = \"{1}\", \"{2}\", \"{3}\"", value.TypeGUID, name, value.PathRelativeTo(solutionFilePath), value.GUID);
			Logger.TraceEvent(TraceEventType.Verbose, null, "Added project {0} to solution with a GUID of {1}", name, value.GUID);
			if (value.PreReqProjectGUIDs.Count > 0)
			{
				Trace.Indent();
				solutionFile.WriteLine("\tProjectSection(ProjectDependencies) = postProject");
				foreach (string preReqProjectGUID in value.PreReqProjectGUIDs)
				{
					solutionFile.WriteLine("\t\t{0} = {0}", preReqProjectGUID);
				}
				string text = Environment.NewLine + "    ";
				Logger.TraceEvent(TraceEventType.Verbose, null, "Added prerequisite projects' GUIDs:" + text + "{0}", string.Join(text, value.PreReqProjectGUIDs));
				solutionFile.WriteLine("\tEndProjectSection");
				Trace.Unindent();
			}
			solutionFile.WriteLine("EndProject");
		}
		Trace.Unindent();
	}

	public static bool CreateSolutionIncludingDependencies(string[] fromProjectFiles, string solutionFile, bool generateSolutionEvenIfNoDependenciesExist = false)
	{
		string[] finalProjectsInSolution;
		return CreateSolutionIncludingDependencies(fromProjectFiles, solutionFile, generateSolutionEvenIfNoDependenciesExist, out finalProjectsInSolution, null);
	}

	public static void GetDependencyInformation(string[] fromProjectFiles, out string[] finalProjectsInSolution, out List<string[]> preRequisiteProjectsForEachProject)
	{
		Logger.TraceEvent(TraceEventType.Information, null, "Checking for external project dependencies");
		Logger.TraceEvent(TraceEventType.Verbose, null, "Processing the following list of primary projects\n{0}", string.Join(Environment.NewLine, fromProjectFiles));
		solutionProjects = new List<string>();
		prerequisitesLists = new List<string[]>();
		projectsBeingScanned = new List<string>();
		try
		{
			foreach (string project in fromProjectFiles)
			{
				AddPrerequisitesToSolution(project);
			}
			if (solutionProjects.Count == fromProjectFiles.Length)
			{
				Logger.TraceEvent(TraceEventType.Information, null, "The primary project collection does not contain any projects that depend on others.");
			}
			else
			{
				StringBuilder stringBuilder = new StringBuilder();
				foreach (string solutionProject in solutionProjects)
				{
					if (!fromProjectFiles.Contains<string>(solutionProject, StringComparer.OrdinalIgnoreCase))
					{
						stringBuilder.AppendLine("    " + solutionProject);
					}
				}
				Logger.TraceEvent(TraceEventType.Information, null, "The primary project collection depends on the following additional projects\n{0}", stringBuilder);
			}
		}
		catch (Exception ex)
		{
			Logger.TraceEvent(TraceEventType.Critical, null, "Error while attempting to resolve dependencies and generate solution:\nMessage:\n{0}\n\n Verbose Trace log: {1}", ex.Message, Logger.TextLogPath);
			Logger.TraceEvent(TraceEventType.Verbose, null, "StackTrace=\n{0}", ex.StackTrace);
		}
		finalProjectsInSolution = solutionProjects.ToArray();
		preRequisiteProjectsForEachProject = prerequisitesLists;
		solutionProjects = null;
		prerequisitesLists = null;
	}

	public static bool CreateSolutionIncludingDependencies(string[] fromProjectFiles, string solutionFile, bool generateSolutionEvenIfNoDependenciesExist, out string[] finalProjectsInSolution, string packageProjectFile)
	{
		bool flag = !string.IsNullOrEmpty(packageProjectFile);
		finalProjectsInSolution = new string[0];
		GetDependencyInformation(fromProjectFiles, out var finalProjectsInSolution2, out var preRequisiteProjectsForEachProject);
		if (finalProjectsInSolution2.Length > fromProjectFiles.Length || generateSolutionEvenIfNoDependenciesExist)
		{
			Logger.TraceEvent(TraceEventType.Verbose, null, "Attempting to generate solution file {0} from projects list", solutionFile);
			if (flag && PackageProjectGenerator.CreatePackage(finalProjectsInSolution2, packageProjectFile))
			{
				finalProjectsInSolution2 = new string[1] { packageProjectFile }.Concat(finalProjectsInSolution2).ToArray();
				List<string[]> list = new List<string[]>();
				list.Add(new string[0]);
				List<string[]> list2 = list;
				list2.AddRange(preRequisiteProjectsForEachProject);
				preRequisiteProjectsForEachProject = list2;
			}
			if (CreateSolution(solutionFile, finalProjectsInSolution2.ToArray(), preRequisiteProjectsForEachProject))
			{
				finalProjectsInSolution = finalProjectsInSolution2;
				Logger.TraceEvent(TraceEventType.Information, null, "Solution {0} was created successfully", solutionFile);
				return true;
			}
			Logger.TraceEvent(TraceEventType.Information, null, "An error occured while generating solution {0}. Verbose Trace log: {1}", solutionFile, Logger.TextLogPath);
			return false;
		}
		return false;
	}

	private static void AddPrerequisitesToSolution(string project)
	{
		Trace.Indent();
		Logger.TraceEvent(TraceEventType.Verbose, null, "Finding prerequisites for {0}", project);
		if (projectsBeingScanned.Contains<string>(project, StringComparer.OrdinalIgnoreCase))
		{
			Logger.TraceEvent(TraceEventType.Verbose, null, "Project {0} already processed/under investigation", project);
			return;
		}
		projectsBeingScanned.Add(project);
		Trace.Indent();
		string[] prerequisiteProjects = DependencyScanner.GetPrerequisiteProjects(project);
		Trace.Unindent();
		if (prerequisiteProjects.Length == 0)
		{
			Logger.TraceEvent(TraceEventType.Verbose, null, "Project {0} had no prerequisites, adding to solution", project);
			solutionProjects.Add(project);
			prerequisitesLists.Add(prerequisiteProjects);
			Trace.Unindent();
			return;
		}
		string[] array = prerequisiteProjects;
		foreach (string text in array)
		{
			if (!projectsBeingScanned.Contains<string>(text, StringComparer.OrdinalIgnoreCase))
			{
				AddPrerequisitesToSolution(text);
			}
		}
		Logger.TraceEvent(TraceEventType.Verbose, null, "Project {0} and its prerequisites analyzed and added to solution", project);
		solutionProjects.Add(project);
		prerequisitesLists.Add(prerequisiteProjects);
		Trace.Unindent();
	}
}
