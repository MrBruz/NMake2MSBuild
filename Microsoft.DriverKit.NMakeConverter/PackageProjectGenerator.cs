#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.DriverKit.MakeSolution;

namespace Microsoft.DriverKit.NMakeConverter;

public static class PackageProjectGenerator
{
	public static string GetDefaultPackageFileNameFor(string solutionFilePath)
	{
		return Path.Combine(Path.GetDirectoryName(Path.GetFullPath(solutionFilePath)), Path.GetFileNameWithoutExtension(solutionFilePath) + "-Package", Path.GetFileNameWithoutExtension(solutionFilePath) + "-Package.vcxproj");
	}

	public static bool CreatePackage(string[] projectFiles, string packageProjectPath)
	{
		Trace.Indent();
		packageProjectPath = Path.GetFullPath(packageProjectPath);
		string text = Path.Combine(FileConverter.GetConversionRoot(), "Package.vcxproj.template");
		List<ProjectRootElement> list = new List<ProjectRootElement>();
		Logger.TraceEvent(TraceEventType.Verbose, null, "Checking if any of the following projects should be packaged\n {0}", string.Join(Environment.NewLine, projectFiles));
		foreach (string text2 in projectFiles)
		{
			if (Path.GetExtension(text2).Equals(".csproj", StringComparison.OrdinalIgnoreCase))
			{
				Logger.TraceEvent(TraceEventType.Verbose, null, "Project {0} will not be packaged because it is a {1} project", Path.GetFileName(text2), ".csproj");
				continue;
			}
			ProjectRootElement val = ProjectRootElement.Open(text2, new ProjectCollection());
			if (val.Properties.Where((ProjectPropertyElement x) => x.Name.Equals("SupportsPackaging", StringComparison.OrdinalIgnoreCase)).Any((ProjectPropertyElement p) => p.Value.Equals("False", StringComparison.OrdinalIgnoreCase)) || val.Properties.Where((ProjectPropertyElement x) => x.Name.Equals("DriverType", StringComparison.OrdinalIgnoreCase)).Any((ProjectPropertyElement p) => p.Value.Equals("None", StringComparison.OrdinalIgnoreCase)))
			{
				Logger.TraceEvent(TraceEventType.Verbose, null, "Project {0} opted out of being packaged by setting property {1} to false", Path.GetFileName(text2), "SupportsPackaging");
			}
			else if (val.Properties.Where((ProjectPropertyElement x) => x.Name.Equals("ConfigurationType", StringComparison.OrdinalIgnoreCase)).Any((ProjectPropertyElement p) => p.Value.Equals("Application", StringComparison.OrdinalIgnoreCase)))
			{
				Logger.TraceEvent(TraceEventType.Verbose, null, "Project {0} is an application project and will not be packaged.", Path.GetFileName(text2));
			}
			else
			{
				list.Add(val);
			}
		}
		if (list.Count == 0)
		{
			Logger.TraceEvent(TraceEventType.Verbose, null, "None of the supplied projects require a package project");
			return false;
		}
		Logger.TraceEvent(TraceEventType.Verbose, null, "Generating package project");
		ProjectRootElement val2 = ProjectRootElement.Open(text, new ProjectCollection());
		string projectGuid = "{" + Guid.NewGuid().ToString().ToUpper(CultureInfo.InvariantCulture) + "}";
		val2.Properties.Where((ProjectPropertyElement x) => x.Name.Equals("ProjectGuid", StringComparison.OrdinalIgnoreCase)).ToList().ForEach(delegate (ProjectPropertyElement p)
		{
			p.Value = projectGuid;
		});
		val2.Properties.Where((ProjectPropertyElement x) => x.Name.Equals("SampleGuid", StringComparison.OrdinalIgnoreCase)).ToList().ForEach(delegate (ProjectPropertyElement p)
		{
			((ProjectElement)p).Parent.RemoveChild((ProjectElement)(object)p);
		});
		foreach (ProjectRootElement item in list)
		{
			string relativePath = MakeSolution.Project.GetRelativePath(packageProjectPath, item.FullPath);
			string text3 = (from p in item.Properties
							where p.Name.Equals("ProjectGuid", StringComparison.OrdinalIgnoreCase)
							select p.Value).FirstOrDefault();
			val2.AddItem("ProjectReference", relativePath).AddMetadata("Project", text3);
			Logger.TraceEvent(TraceEventType.Verbose, null, "Adding reference to project {0} with project guid {1}", relativePath, text3);
		}
		List<string> list2 = list.Select((ProjectRootElement p) => from i in p.Items
																   where i.ItemType.Equals("ProjectConfiguration", StringComparison.OrdinalIgnoreCase)
																   select i.Include).SelectMany((IEnumerable<string> x) => x).ToList();
		IEnumerable<string> first = (from i in val2.Items
									 where i.ItemType.Equals("ProjectConfiguration", StringComparison.OrdinalIgnoreCase)
									 select i.Include).Distinct();
		IEnumerable<string> enumerable = first.Except<string>(list2, StringComparer.OrdinalIgnoreCase);
		foreach (string configuration in enumerable)
		{
			ICollection<ProjectItemElement> items = val2.Items;
			Func<ProjectItemElement, bool> predicate = (ProjectItemElement i) => i.ItemType.Equals("ProjectConfiguration", StringComparison.OrdinalIgnoreCase) && i.Include.Equals(configuration, StringComparison.OrdinalIgnoreCase);
			foreach (ProjectItemElement item2 in items.Where(predicate))
			{
				if (((ProjectElement)item2).Parent != null)
				{
					((ProjectElement)item2).Parent.RemoveChild((ProjectElement)(object)item2);
				}
			}
			IEnumerable<ProjectElement> enumerable2 = ((ProjectElementContainer)val2).AllChildren.Where((ProjectElement e) => e.Condition.Trim().Equals(string.Format(CultureInfo.InvariantCulture, "'$(Configuration)|$(Platform)'=='{0}'", new object[1] { configuration }), StringComparison.OrdinalIgnoreCase));
			foreach (ProjectElement item3 in enumerable2)
			{
				if (item3.Parent != null)
				{
					item3.Parent.RemoveChild(item3);
				}
			}
		}
		string text4 = list2.First();
		val2.AddProperty("Configuration", text4.Substring(0, text4.IndexOf("|", StringComparison.OrdinalIgnoreCase)));
		if (!Directory.Exists(Path.GetDirectoryName(packageProjectPath)))
		{
			Directory.CreateDirectory(Path.GetDirectoryName(packageProjectPath));
		}
		val2.Save(packageProjectPath);
		Logger.TraceEvent(TraceEventType.Information, null, "Generated Package Project file '{0}'", packageProjectPath);
		Trace.Unindent();
		return true;
	}
}
