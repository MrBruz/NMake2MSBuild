using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace Microsoft.DriverKit.NMakeConverter;

internal class InvokedTarget
{
	internal string TargetOutput { get; set; }

	internal string Condition { get; set; }

	internal string Pass { get; set; }

	internal string MsBuildAfterTargets => Pass.ToLowerInvariant() switch
	{
		"all" => string.Empty, 
		"0" => string.Empty, 
		"1" => "$(AfterBuildCompileTargets)", 
		"2" => "$(BuildLinkTargets)", 
		_ => throw new InvalidDataException(string.Format(CultureInfo.InvariantCulture, "Unknown Pass '{0}' detected on target '{1}'", new object[2] { Pass, TargetOutput })), 
	};

	internal string MsBuildBeforeTargets
	{
		get
		{
			switch (Pass.ToLowerInvariant())
			{
			case "all":
				Logger.TraceEvent(TraceEventType.Warning, null, "Use of NTTARGETFILES is deprecated. Target '{0}' will be converted to run only once during the build, during PreBuild", TargetOutput);
				return "$(BuildGenerateSourcesTargets)";
			case "0":
				return "$(BuildGenerateSourcesTargets)";
			case "1":
				return "$(BuildLinkTargets)";
			case "2":
				return "$(AfterBuildLinkTargets)";
			default:
				throw new InvalidDataException(string.Format(CultureInfo.InvariantCulture, "Unknown Pass '{0}' detected on target '{1}'", new object[2] { Pass, TargetOutput }));
			}
		}
	}

	internal InvokedTarget(string targetOutput, string condition, string pass)
	{
		TargetOutput = targetOutput;
		Condition = condition;
		Pass = pass;
	}
}
