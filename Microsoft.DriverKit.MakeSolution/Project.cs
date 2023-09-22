using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Microsoft.Build.Evaluation;
using Microsoft.DriverKit.NMakeConverter;

namespace Microsoft.DriverKit.MakeSolution;

internal class Project
{
	public SolutionFolder SolutionFolder;

	private string mProjectPath;

	private string mGUID;

	private List<string> mPreReqProjectGUIDs = new List<string>();

	private List<string> mConfigurations = new List<string>();

	private List<string> mPlatforms = new List<string>();
	private string text2;

	public string name => Path.GetFileNameWithoutExtension(projectPath);

	public List<string> PreReqProjectGUIDs => mPreReqProjectGUIDs;

	public List<string> Configurations => mConfigurations;

	public List<string> Platforms => mPlatforms;

	public string projectPath => mProjectPath;

	public string GUID => mGUID;

	public string TypeGUID => GetProjectTypeGuid(projectPath);

	public Project(string fullPath, IDictionary<string, string> dictionary)
	{
		Initialize(fullPath);
	}

	public Project(string text2)
	{
		this.text2 = text2;
	}

	private void Initialize(string fullPath)
	{
		if (!File.Exists(fullPath))
		{
			throw new FileNotFoundException(fullPath);
		}
		Dictionary<string, string> dictionary = null;
		Build.Evaluation.Project vsProject = new Build.Evaluation.Project(fullPath, dictionary, null, new ProjectCollection(), (ProjectLoadSettings)1);
		mProjectPath = fullPath;
		mGUID = ExtractProjectGuid(ref vsProject);
		ParseProjectConfigurationsAndPlatforms(ref vsProject);
		vsProject.ProjectCollection.UnloadAllProjects();
	}

	private string ExtractProjectGuid(ref Build.Evaluation.Project vsProject)
	{
		if (!File.Exists(projectPath))
		{
			Logger.TraceEvent(TraceEventType.Critical, null, "Project file {0} does not exist", projectPath);
			throw new FileNotFoundException(projectPath);
		}
		string propertyValue = vsProject.GetPropertyValue("ProjectGuid");
		if (!string.IsNullOrWhiteSpace(propertyValue))
		{
			Logger.TraceEvent(TraceEventType.Verbose, null, " GUID {0} was extracted from project {1}", propertyValue, projectPath);
			return propertyValue;
		}
		string text = "GUID not found in " + projectPath;
		Logger.TraceEvent(TraceEventType.Critical, null, text);
		throw new Exception(text);
	}

	private void ParseProjectConfigurationsAndPlatforms(ref Build.Evaluation.Project vsProject)
	{
		if (Path.GetExtension(vsProject.FullPath).Equals(".csproj", StringComparison.OrdinalIgnoreCase))
		{
			IDictionary<string, List<string>> conditionedProperties = vsProject.ConditionedProperties;
			if (conditionedProperties.ContainsKey("Configuration") && conditionedProperties.ContainsKey("Platform"))
			{
				foreach (string item in conditionedProperties["Configuration"])
				{
					foreach (string item2 in conditionedProperties["Platform"])
					{
						mConfigurations.Add(item);
						if (item2.Equals("AnyCPU", StringComparison.OrdinalIgnoreCase))
						{
							mPlatforms.Add("Any CPU");
						}
						else
						{
							mPlatforms.Add(item2);
						}
					}
				}
			}
		}
		else
		{
			if (!Path.GetExtension(vsProject.FullPath).Equals(".vcxproj", StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidDataException("Unknown project type for project " + vsProject.FullPath);
			}
			foreach (ProjectItem item3 in vsProject.GetItemsIgnoringCondition("ProjectConfiguration"))
			{
				string metadataValue = item3.GetMetadataValue("Configuration");
				string metadataValue2 = item3.GetMetadataValue("Platform");
				if (string.IsNullOrWhiteSpace(metadataValue) || string.IsNullOrWhiteSpace(metadataValue2))
				{
					string text = string.Format(CultureInfo.InvariantCulture, "Project Configuration and/or Platform definition not found for project {0}", new object[1] { projectPath });
					Logger.TraceEvent(TraceEventType.Critical, null, text);
					throw new Exception(text);
				}
				mConfigurations.Add(metadataValue);
				mPlatforms.Add(metadataValue2);
			}
		}
		if (mConfigurations.Count == 0)
		{
			string text2 = string.Format(CultureInfo.InvariantCulture, "No project configurations/platforms were found for project {0}", new object[1] { projectPath });
			Logger.TraceEvent(TraceEventType.Critical, null, text2);
			throw new Exception(text2);
		}
		string text3 = Environment.NewLine + new string(' ', Trace.IndentSize * (Trace.IndentLevel + 1));
		string text4 = string.Empty;
		for (int i = 0; i < Platforms.Count; i++)
		{
			string text5 = text4;
			text4 = text5 + text3 + Configurations[i] + "|" + Platforms[i];
		}
	}

	public void AddPreReqProjectGUID(string guid)
	{
		mPreReqProjectGUIDs.Add(guid);
	}

	public string PathRelativeTo(string fromPath)
	{
		return GetRelativePath(fromPath, projectPath);
	}

	public static string GetProjectTypeGuid(string project)
	{
		string result = null;
		string extension = Path.GetExtension(project);
		switch (extension.ToLowerInvariant())
		{
			case ".csproj":
				result = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
				break;
			case ".vcproj":
			case ".vcxproj":
				result = "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}";
				break;
			default:
				Logger.TraceEvent(TraceEventType.Error, null, "Project {0} was not a cpp or c# project", project);
				break;
		}
		return result;
	}

	public static string GetRelativePath(string fromPath, string toPath)
	{
		if (string.IsNullOrEmpty(fromPath))
		{
			throw new ArgumentNullException(nameof(fromPath));
		}
		if (string.IsNullOrEmpty(toPath))
		{
			throw new ArgumentNullException(nameof(toPath));
		}
		Uri uri = new(Path.GetFullPath(fromPath));
		Uri uri2 = new(Path.GetFullPath(toPath));
		Uri uri3 = uri.MakeRelativeUri(uri2);
		return Uri.UnescapeDataString(uri3.ToString()).Replace("/", "\\");
	}
}
