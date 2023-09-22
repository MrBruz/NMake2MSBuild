namespace Microsoft.DriverKit.NMakeConverter;

internal static class Constants
{
	public const string FiltersFileExtension = ".Filters";

	public const string CsProjExtension = ".csproj";

	public const string VcxProjExtension = ".vcxproj";

	public const string VcProjExtension = ".vcproj";

	public const string SolutionExtension = ".sln";

	public const string PropsFileExtension = ".props";

	public const string LogFileExtension = ".log";

	public const string DirsFile = "dirs";

	public const string SourcesFile = "sources";

	public const string MakefileInc = "makefile.inc";

	public const string SourcesPropsFile = "sources.props";

	public const string MakefileIncPropsFile = "makefile.inc.props";

	public const string PackageProjectDirSuffix = "-Package";

	public const string FilterItemType = "Filter";

	public const string ProjectConfigurationItemType = "ProjectConfiguration";

	public const string ProjectGuidPropertyName = "ProjectGuid";

	public const string SampleGuidPropertyName = "SampleGuid";

	public const string FilterGuidPropertyname = "UniqueIdentifier";

	public const string ProjectReferenceItemType = "ProjectReference";

	public const string ProjectReferenceMetadataName = "Project";

	public const string DriverTypePropertyName = "DriverType";

	public const string NoDriverTypeValue = "None";

	public const string SupportsPackagingPropertyName = "SupportsPackaging";

	public const string IntDirPropertyName = "IntDir";

	public const string OutDirPropertyName = "OutDir";

	public const string SolutionDirPropertyName = "SolutionDir";

	public const string ObjPathPropertyName = "O";

	public const string ObjPathReference = "$(O)";

	public const string AnyCPUProjectPlatform = "AnyCPU";

	public const string AnyCPUSolutionPlatform = "Any CPU";

	public const string UMAppsToolsetName = "WindowsApplicationForDrivers8.0";

	public const string BeforeTargetsForConvertedTargets = "BeforeClCompile";

	public const string ConvertedTargetManifestItemType = "NmakeTarget";

	public const string ConvertedTargetKillSwitchMetadata = "TargetKillSwitch";

	public const string InvokedConvertedTargetsItemType = "InvokedTargetsList";

	public const string Pass0BeforeTargets = "$(BuildGenerateSourcesTargets)";

	public const string Pass1BeforeTargets = "$(BuildLinkTargets)";

	public const string Pass1AfterTargets = "$(AfterBuildCompileTargets)";

	public const string Pass2BeforeTargets = "$(AfterBuildLinkTargets)";

	public const string Pass2AfterTargets = "$(BuildLinkTargets)";

	public const string Win7ConfigName = "Win7";

	public const string Win8ConfigName = "Win8";

	public const string VistaConfigName = "Vista";

	public const string ReleaseConfigSuffix = " Release";

	public const string DebugConfigSuffix = " Debug";

	public const string MsBuildArmPlatform = "Arm";

	public const string MsBuildx86Platform = "Win32";

	public const string MsBuildx64Platform = "x64";

	public const string SkipX86Macro = "DDK_BLOCK_ON_X86";

	public const string SkipX64Macro = "DDK_BLOCK_ON_AMD64";

	public const string SkipArmMacro = "DDK_BLOCK_ON_ARM";

	public const string NTTargetVersionMacro = "_NT_TARGET_VERSION";

	public const string MinimumTargetVersionMacro = "MINIMUM_NT_TARGET_VERSION";

	public const string MaximumTargetVersionMacro = "MAXIMUM_NT_TARGET_VERSION";

	public const string ConfigurationDependentLabel = "ConfigurationDependent";

	public const string EndElementLabel = "EndElement";

	public const string GlobalsLabel = "Globals";

	public const string PropertySheetsLabel = "PropertySheets";

	public const string MacroOverrideSwitchPrepend = "OVERRIDE_";
}
