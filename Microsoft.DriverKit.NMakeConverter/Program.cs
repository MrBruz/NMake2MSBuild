using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.DriverKit.Sqm;

namespace Microsoft.DriverKit.NMakeConverter;

internal class Program
{
	private static string usageInfo = Environment.NewLine + " Usage:" + Environment.NewLine + "       NMake2MSBuild.exe  < sources [<sources> ...] | dirs >" + Environment.NewLine + "                          [-Name:<Name of output project>] " + Environment.NewLine + "                          [-Package:<Path to package project file to generate>]" + Environment.NewLine + "                          [-Solution:<Path to Solution file to generate>]" + Environment.NewLine + "                          [-Log:[<LogFile>]:[<Verbosity>]]" + Environment.NewLine + "                          [-ConsoleLog:<Verbosity>]" + Environment.NewLine + "                          [-NoPackageProject]" + Environment.NewLine + "                          [-NoSolution]" + Environment.NewLine + "                          [-SafeMode]" + Environment.NewLine + "                          [-Arm]" + Environment.NewLine + Environment.NewLine + " <Verbosity> is one of System.Diagnostics.SourceLevels" + Environment.NewLine + Environment.NewLine + " Anything enclosed in [] may be omitted." + Environment.NewLine + Environment.NewLine + " Multiple sources files may be specified at a time. All resulting projects will" + Environment.NewLine + " share the same Solution and Package Project." + Environment.NewLine + Environment.NewLine + " You may use -Name to specify a custom name for the VcxProj file that will be " + Environment.NewLine + " generated, when converting a sources file. Alternatively, If a dirs file is " + Environment.NewLine + " being converted, this parameter is used to specify the name of the generated " + Environment.NewLine + " solution" + Environment.NewLine + Environment.NewLine + " Default logging levels for Log file and Console logging are Verbose and " + Environment.NewLine + " Information respectively" + Environment.NewLine + Environment.NewLine + " SafeMode does not provide IDE/UI support for Nmake targets but may provide " + Environment.NewLine + " a more accurate conversion for Nmake targets. Only specify -SafeMode if you " + Environment.NewLine + " experience issues during build steps that were previously performed in your " + Environment.NewLine + " project's Nmake targets." + Environment.NewLine + Environment.NewLine + " -Arm adds Arm as a valid target cpu architecture to the project's build " + Environment.NewLine + " configurations. The generated project will still require that the installed " + Environment.NewLine + " build environment and WDK support targeting Arm." + Environment.NewLine + Environment.NewLine + " Response (.Rsp) files are supported for specifying  command line parameters. " + Environment.NewLine + " Each parameters/switch should be specified on a separate line." + Environment.NewLine;

	private static int Main(string[] args)
	{
		ConversionSettings projectConversionSettings;
		if ((projectConversionSettings = ParseParams(args)) == null)
		{
			return Logger.ErrorLevel;
		}
		GenerateProject.Convert(projectConversionSettings);
		try
		{
			ResolveEventHandler value = AssemblyHelper.MyResolveEventHandler;
			//AppDomain.CurrentDomain.AssemblyResolve += value;
			//CollectTelemetryData();
			//AppDomain.CurrentDomain.AssemblyResolve -= value;
		}
		catch (Exception)
		{
		}
		return Logger.ErrorLevel;
	}

	private static ConversionSettings ParseParams(string[] args)
	{
		ConversionSettings conversionSettings = new ConversionSettings();
		conversionSettings.PrimaryInputFile = null;
		conversionSettings.SourcesFileList = null;
		if (args.Length < 1)
		{
			DisplayParsingError("Insufficient arguments");
			return null;
		}
		if (args[0].EndsWith(".rsp", StringComparison.OrdinalIgnoreCase))
		{
			if (!File.Exists(args[0]))
			{
				DisplayParsingError(".Rsp file {0} not found", args[0]);
				return null;
			}
			args = (from l in File.ReadAllLines(args[0])
					where !string.IsNullOrWhiteSpace(l)
					select l.Trim()).ToArray();
		}
		conversionSettings.PrimaryInputFile = args[0];
		if (!File.Exists(conversionSettings.PrimaryInputFile))
		{
			if (args[0].Contains("?"))
			{
				ShowUsage();
			}
			else
			{
				DisplayParsingError("Project file {0} does not exist", conversionSettings.PrimaryInputFile);
			}
			return null;
		}
		conversionSettings.PrimaryInputFile = Path.GetFullPath(conversionSettings.PrimaryInputFile);
		int num = 1;
		if (Path.GetFileName(conversionSettings.PrimaryInputFile).Equals("sources", StringComparison.OrdinalIgnoreCase))
		{
			conversionSettings.SourcesFileList = new List<string> { conversionSettings.PrimaryInputFile };
			int num2 = 1;
			while (num2 < args.Length && Path.GetFileName(args[num2]).Equals("sources", StringComparison.OrdinalIgnoreCase))
			{
				conversionSettings.SourcesFileList.Add(Path.GetFullPath(args[num2]));
				num = ++num2;
			}
		}
		for (int i = num; i < args.Length; i++)
		{
			if (args[i].StartsWith("-Log", StringComparison.OrdinalIgnoreCase))
			{
				if (!ParseLogFileOptions(args[i], conversionSettings))
				{
					return null;
				}
				continue;
			}
			if (args[i].StartsWith("-ConsoleLog", StringComparison.OrdinalIgnoreCase))
			{
				if (!ParseConsoleLogOptions(args[i], conversionSettings))
				{
					return null;
				}
				continue;
			}
			if (args[i].StartsWith("-Name", StringComparison.OrdinalIgnoreCase))
			{
				if (!ParseOverrideProjectName(args[i], conversionSettings))
				{
					return null;
				}
				continue;
			}
			if (args[i].StartsWith("-Package", StringComparison.OrdinalIgnoreCase))
			{
				if (!ParsePackageName(args[i], conversionSettings))
				{
					return null;
				}
				continue;
			}
			if (args[i].StartsWith("-Solution", StringComparison.OrdinalIgnoreCase))
			{
				if (!ParseSolutionFilePath(args[i], conversionSettings))
				{
					return null;
				}
				continue;
			}
			if (args[i].StartsWith("-SafeMode", StringComparison.OrdinalIgnoreCase))
			{
				conversionSettings.SafeMode = true;
				continue;
			}
			if (args[i].StartsWith("-Arm", StringComparison.OrdinalIgnoreCase))
			{
				conversionSettings.AddArmConfigurations = true;
				continue;
			}
			if (args[i].StartsWith("-NoPackageProject", StringComparison.OrdinalIgnoreCase))
			{
				conversionSettings.NoPackageProject = true;
				continue;
			}
			if (args[i].StartsWith("-NoSolution", StringComparison.OrdinalIgnoreCase))
			{
				conversionSettings.NoSolutionFile = true;
				continue;
			}
			DisplayParsingError("Unknown Switch {0}\n", args[i]);
			return null;
		}
		if (conversionSettings.NoSolutionFile && !string.IsNullOrEmpty(conversionSettings.PackageProjectFile))
		{
			DisplayParsingError("-NoSolution as well as -Package were both specified. Solution generation must be enabled if a package project is requested.");
			return null;
		}
		if (conversionSettings.NoSolutionFile && !string.IsNullOrEmpty(conversionSettings.SolutionFile))
		{
			DisplayParsingError("-NoSolution as well as -Solution were both specified");
			return null;
		}
		if (conversionSettings.NoPackageProject && !string.IsNullOrEmpty(conversionSettings.PackageProjectFile))
		{
			DisplayParsingError("-NoPackageProject and -Package cannot both specified at the same time.");
			return null;
		}
		if (!string.IsNullOrEmpty(conversionSettings.PackageProjectFile) && !Path.GetPathRoot(conversionSettings.PackageProjectFile).Equals(Path.GetPathRoot(conversionSettings.PrimaryInputFile)))
		{
			DisplayParsingError("Package project destination must be the same drive as {0}", conversionSettings.PrimaryInputFile);
			return null;
		}
		if (conversionSettings.SourcesFileList != null && conversionSettings.SourcesFileList.Count > 1 && !string.IsNullOrEmpty(conversionSettings.SolutionFile) && !string.IsNullOrEmpty(conversionSettings.OverrideProjectName))
		{
			DisplayParsingError("-Name and -Solution cannot both be specified if multiple sources files are to be batch converted.", conversionSettings.PrimaryInputFile);
			return null;
		}
		if (string.IsNullOrWhiteSpace(conversionSettings.TextLogName))
		{
			string directoryName = Path.GetDirectoryName(conversionSettings.PrimaryInputFile);
			conversionSettings.TextLogName = Path.Combine(directoryName, "Nmake2MSBuild_" + Path.GetFileName(conversionSettings.PrimaryInputFile) + ".log");
		}
		return conversionSettings;
	}

	private static void DisplayParsingError(string message, params object[] extraArgs)
	{
		ConsoleColor foregroundColor = Console.ForegroundColor;
		Console.ForegroundColor = ConsoleColor.Red;
		Console.Error.WriteLine(message, extraArgs);
		Console.ForegroundColor = foregroundColor;
		ShowUsage();
	}

	private static bool ParseSolutionFilePath(string arg, ConversionSettings conversionSettings)
	{
		if (Path.GetFileName(conversionSettings.PrimaryInputFile).Equals("dirs", StringComparison.OrdinalIgnoreCase))
		{
			DisplayParsingError("-Solution parameter is not supported for conversion of Dirs files. Use -Name instead to control the name of the solution.");
			return false;
		}
		if (!Path.GetFileName(conversionSettings.PrimaryInputFile).Equals("sources", StringComparison.OrdinalIgnoreCase))
		{
			DisplayParsingError("You may only specify an output solution file path when converting sources files.");
			return false;
		}
		int num = arg.IndexOf(':');
		string text = arg.Substring((num >= 0) ? (num + 1) : 0);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		conversionSettings.SolutionFile = Path.GetFullPath(text.Trim(new char[1] { '"' }));
		return true;
	}

	private static bool ParsePackageName(string arg, ConversionSettings conversionSettings)
	{
		int num = arg.IndexOf(':');
		string text = arg.Substring((num >= 0) ? (num + 1) : 0);
		if (!string.IsNullOrEmpty(text))
		{
			text = Path.GetFullPath(text.Trim(new char[1] { '"' }));
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		conversionSettings.PackageProjectFile = text;
		return true;
	}

	private static bool ParseOverrideProjectName(string arg, ConversionSettings conversionSettings)
	{
		int num = arg.IndexOf(':');
		string text = arg.Substring((num >= 0) ? (num + 1) : 0);
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		if (!Path.GetFileName(conversionSettings.PrimaryInputFile).Equals("sources", StringComparison.OrdinalIgnoreCase) && !Path.GetFileName(conversionSettings.PrimaryInputFile).Equals("dirs", StringComparison.OrdinalIgnoreCase))
		{
			DisplayParsingError("You may only specify an output project name when converting a sources or dirs file.");
			return false;
		}
		conversionSettings.OverrideProjectName = text.Trim();
		return true;
	}

	private static bool ParseLogFileOptions(string arg, ConversionSettings conversionSettings)
	{
		int num = arg.IndexOf(':');
		string text = arg.Substring((num >= 0) ? (num + 1) : 0);
		if (!text.Contains(":"))
		{
			DisplayParsingError("Invalid Console Log syntax. Expected at least 2 semicolons");
			return false;
		}
		string text2 = text.Substring(0, text.LastIndexOf(':'));
		string value = text.Substring(text.LastIndexOf(':') + 1);
		if (!string.IsNullOrEmpty(text2))
		{
			try
			{
				conversionSettings.TextLogName = Path.GetFullPath(text2);
			}
			catch (Exception ex)
			{
				DisplayParsingError("Invalid log file specified" + Environment.NewLine + ex.Message);
				return false;
			}
		}
		if (!string.IsNullOrWhiteSpace(value))
		{
			try
			{
				conversionSettings.TextTraceLevel = (SourceLevels)Enum.Parse(typeof(SourceLevels), value, ignoreCase: true);
			}
			catch
			{
				DisplayParsingError("Invalid Verbosity specified for Logging");
				return false;
			}
		}
		return true;
	}

	private static bool ParseConsoleLogOptions(string arg, ConversionSettings conversionSettings)
	{
		conversionSettings.ConsoleTraceLevel = SourceLevels.Error;
		string[] array = arg.Split(":".ToCharArray(), StringSplitOptions.None);
		if (array.Length > 1)
		{
			try
			{
				conversionSettings.ConsoleTraceLevel = (SourceLevels)Enum.Parse(typeof(SourceLevels), array[1], ignoreCase: true);
			}
			catch
			{
				DisplayParsingError("Invalid Verbosity specified for console logging");
				return false;
			}
		}
		return true;
	}

	private static void ShowUsage()
	{
		Console.Error.WriteLine(usageInfo);
	}
}
