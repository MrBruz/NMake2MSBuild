using System.Collections.Generic;

namespace Microsoft.DriverKit.MakeSolution;

internal class SolutionFolder
{
	public const string SolutionFolderTypeGuid = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";

	public string Name;

	public string GUID;

	public SolutionFolder Parent;

	public List<SolutionFolder> ImmediateChildren = new List<SolutionFolder>();

	public SolutionFolder(string name, SolutionFolder parent, string guid)
	{
		Name = name;
		Parent = parent;
		GUID = guid;
	}
}
