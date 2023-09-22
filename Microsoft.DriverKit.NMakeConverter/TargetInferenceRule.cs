using System.Collections.Generic;
using Microsoft.DriverKit.NMakeConverter.Commands;

namespace Microsoft.DriverKit.NMakeConverter;

internal class TargetInferenceRule : InferenceRule
{
	public TargetInferenceRule(string FromExtension, string ToExtension, string[] Commands, string FromPath = "", string ToPath = "")
	{
		base.ToExtension = ToExtension;
		base.FromExtension = FromExtension;
		base.ToPath = ToPath;
		base.FromPath = FromPath;
		base.Commands = new List<string>(Commands);
	}
}
