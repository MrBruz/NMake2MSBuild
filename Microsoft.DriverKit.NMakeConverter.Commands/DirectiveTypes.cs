namespace Microsoft.DriverKit.NMakeConverter.Commands;

public enum DirectiveTypes
{
	If,
	Ifndef,
	Ifdef,
	Elseif,
	Elseifdef,
	Elseifndef,
	Else,
	Endif,
	Undef,
	IncludeFile,
	DotDirective,
	Message,
	Error,
	InferenceRuleDefinition,
	MacroDefinition,
	TargetDefinition,
	None
}
