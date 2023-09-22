using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.DriverKit.NMakeConverter;

internal class ConversionSettings : IConversionSettings
{
	public string PrimaryInputFile { get; set; }

	public bool SafeMode { get; set; }

	public bool AddArmConfigurations { get; set; }

	public string OverrideProjectName { get; set; }

	public bool NoSolutionFile { get; set; }

	public bool NoPackageProject { get; set; }

	public string PackageProjectFile { get; set; }

	public string SolutionFile { get; set; }

	public List<string> SourcesFileList { get; set; }

	public string DirSolutionFileName { get; set; }

	public SourceLevels TextTraceLevel { get; set; }

	public SourceLevels ConsoleTraceLevel { get; set; }

	public string TextLogName { get; set; }

	public ConversionSettings(string primaryInputFile)
	{
		PrimaryInputFile = primaryInputFile;
		SafeMode = false;
		AddArmConfigurations = false;
		OverrideProjectName = null;
		NoSolutionFile = false;
		NoPackageProject = false;
		PackageProjectFile = null;
		SolutionFile = null;
		SourcesFileList = null;
		DirSolutionFileName = "dirs.sln";
		TextTraceLevel = SourceLevels.Verbose;
		ConsoleTraceLevel = SourceLevels.Information;
		TextLogName = null;
	}

	public ConversionSettings()
		: this(null)
	{
	}
}
