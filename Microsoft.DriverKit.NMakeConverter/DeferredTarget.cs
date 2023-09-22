using System.Collections.Generic;
using Microsoft.DriverKit.NMakeConverter.Commands;

namespace Microsoft.DriverKit.NMakeConverter;

internal class DeferredTarget
{
	internal TargetDefinition TargetDefinition;

	internal string Condition;

	internal List<string> DependsOnTargets;

	internal string Name;

	internal bool IsMakeFileTarget;

	internal DeferredTarget(TargetDefinition targetDefinition, string name, string condition, List<string> dependsOnTargets, bool isMakeFileTarget)
	{
		TargetDefinition = targetDefinition;
		Condition = condition;
		DependsOnTargets = dependsOnTargets;
		Name = name;
		IsMakeFileTarget = isMakeFileTarget;
	}
}
