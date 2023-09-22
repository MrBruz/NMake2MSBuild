namespace Microsoft.DriverKit.NMakeConverter.Commands;

public delegate SourcesDirective ParseIfApplies(string nmakeLine, out bool waitingForMoreLines, ConditionBlock conditionBlock);
