#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.DriverKit.NMakeConverter;

namespace Microsoft.DriverKit.MakeSolution;

public static class ProjectUtilities
{
	public static void SetProjectProperty(string projectPath, string propertyName, string propertyValue, bool isGlobal)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		Trace.Indent();
		ProjectRootElement val = ProjectRootElement.Open(projectPath, new ProjectCollection());
		if (isGlobal)
		{
			ProjectPropertyGroupElement val2 = null;
			val2 = val.PropertyGroups.Where((ProjectPropertyGroupElement propertyGroup) => propertyGroup.Label.Equals("Globals", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
			if (val2 == null)
			{
				val2 = val.AddPropertyGroup();
				val2.Label = "Globals";
			}
			val2.SetProperty(propertyName, propertyValue);
		}
		else
		{
			IEnumerable<ProjectPropertyElement> enumerable = val.Properties.Where((ProjectPropertyElement x) => x.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
			if (enumerable.Count() > 0)
			{
				foreach (ProjectPropertyElement item in enumerable)
				{
					item.Value = propertyValue;
				}
			}
			else
			{
				val.AddProperty(propertyName, propertyValue);
			}
		}
		val.Save();
		Trace.Unindent();
	}

	public static string GetProjectProperty(string projectPath, string propertyName)
	{
		return GetProjectProperty(projectPath, propertyName, null);
	}

	public static string GetProjectProperty(string projectPath, string propertyName, string defaultValueIfEmpty)
	{
		Trace.Indent();
		Dictionary<string, string> dictionary = null;
		Build.Evaluation.Project val = new Build.Evaluation.Project(projectPath, (IDictionary<string, string>)dictionary, null, new ProjectCollection(), (ProjectLoadSettings)1);
		string text = val.GetPropertyValue(propertyName);
		val.ProjectCollection.UnloadAllProjects();
		if (string.IsNullOrEmpty(text) && defaultValueIfEmpty != null)
		{
			Logger.TraceEvent(TraceEventType.Verbose, null, "Property '{0}' in project '{1}' evaluated to empty. Assuming it to be the supplied default value of '{2}'", propertyName, projectPath, defaultValueIfEmpty);
			text = defaultValueIfEmpty;
		}
		Trace.Unindent();
		return text;
	}
}
