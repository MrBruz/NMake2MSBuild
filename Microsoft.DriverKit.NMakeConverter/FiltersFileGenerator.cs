#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;

namespace Microsoft.DriverKit.NMakeConverter;

public static class FiltersFileGenerator
{
	public static void CreateFor(string projectFile)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Expected O, but got Unknown
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Expected O, but got Unknown
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Expected O, but got Unknown
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Expected O, but got Unknown
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Expected O, but got Unknown
		Trace.Indent();
		if (Path.GetExtension(projectFile).Equals(".csproj", StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException("Attempted to generate a .Filtersfile for a .csproj file. The C# project system does not support .Filters files.");
		}
		string text = Path.Combine(FileConverter.GetConversionRoot(), "Filters.template");
		string text2 = projectFile + ".Filters";
		ProjectRootElement val = ProjectRootElement.Open(text, new ProjectCollection());
		foreach (ProjectItemElement item in val.Items)
		{
			if (item.ItemType == "Filter")
			{
				item.AddMetadata("UniqueIdentifier", "{" + Guid.NewGuid().ToString().ToUpper(CultureInfo.InvariantCulture) + "}");
			}
		}
		val.Save(text2);
		Dictionary<string, string> dictionary = null;
		Project val2 = new Project(projectFile, (IDictionary<string, string>)dictionary, (string)null, new ProjectCollection(), (ProjectLoadSettings)1);
		List<string> toolsetProperties = val2.ProjectCollection.Toolsets.Select((Toolset t) => t.Properties.Keys).SelectMany((ICollection<string> x) => x).ToList();
		Dictionary<string, string> dictionary2 = val2.Properties.Where((ProjectProperty p) => !p.IsEnvironmentProperty && !p.IsGlobalProperty && !p.IsReservedProperty && !toolsetProperties.Contains<string>(p.Name, StringComparer.OrdinalIgnoreCase)).ToDictionary((ProjectProperty n) => n.Name, (ProjectProperty n) => n.EvaluatedValue);
		if (dictionary != null)
		{
			dictionary2 = dictionary2.Union(dictionary).ToDictionary((KeyValuePair<string, string> entry) => entry.Key, (KeyValuePair<string, string> entry) => entry.Value);
		}
		Project val3 = new Project(text2, (IDictionary<string, string>)dictionary2, (string)null, new ProjectCollection(), (ProjectLoadSettings)1);
		IEnumerable<ProjectItem> enumerable = val3.GetItemsIgnoringCondition("Filter").Except(val3.GetItems("Filter"));
		val3.RemoveItems(enumerable);
		foreach (ProjectItem item2 in val3.GetItems("Filter"))
		{
			((ProjectElement)item2.Xml).Condition = string.Empty;
		}
		val3.Save();
		val2.ProjectCollection.UnloadAllProjects();
		val3.ProjectCollection.UnloadAllProjects();
		Logger.TraceEvent(TraceEventType.Verbose, null, "Generated VC Filters file '{0}'", text2);
		Trace.Unindent();
	}
}
