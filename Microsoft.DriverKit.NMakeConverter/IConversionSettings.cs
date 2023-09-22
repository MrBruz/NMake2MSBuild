using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.DriverKit.NMakeConverter;

public interface IConversionSettings
{
	string PrimaryInputFile { get; }

	bool SafeMode { get; }

	bool AddArmConfigurations { get; }

	string OverrideProjectName { get; }

	bool NoSolutionFile { get; }

	bool NoPackageProject { get; }

	string PackageProjectFile { get; }

	string SolutionFile { get; }

	List<string> SourcesFileList { get; }

	string DirSolutionFileName { get; }

	SourceLevels TextTraceLevel { get; }

	SourceLevels ConsoleTraceLevel { get; }

	string TextLogName { get; }
}
