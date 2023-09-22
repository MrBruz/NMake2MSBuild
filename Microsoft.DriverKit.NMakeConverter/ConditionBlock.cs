using System;

namespace Microsoft.DriverKit.NMakeConverter;

[Serializable]
public class ConditionBlock
{
	public ConditionBlock ParentConditionBlock { get; set; }

	public bool NegateParentCondition { get; set; }

	public string Condition { get; set; }

	public ConditionBlock(ConditionBlock parentConditionBlock)
	{
		ParentConditionBlock = parentConditionBlock;
		NegateParentCondition = false;
		Condition = null;
	}

	public ConditionBlock(ConditionBlock parentConditionBlock, bool negateParentCondition)
	{
		ParentConditionBlock = parentConditionBlock;
		NegateParentCondition = negateParentCondition;
		Condition = null;
	}

	public ConditionBlock(string condition)
	{
		ParentConditionBlock = null;
		Condition = condition;
		NegateParentCondition = false;
	}

	public ConditionBlock(string condition, bool negateParentCondition)
	{
		ParentConditionBlock = null;
		Condition = condition;
		NegateParentCondition = negateParentCondition;
	}

	public static string ExtractCondition(ConditionBlock block)
	{
		if (block == null)
		{
			return string.Empty;
		}
		if (block.ParentConditionBlock != null)
		{
			if (block.NegateParentCondition)
			{
				if (string.IsNullOrWhiteSpace(block.Condition))
				{
					return ConditionalOperators.Not(ExtractCondition(block.ParentConditionBlock));
				}
				return ConditionalOperators.And(block.Condition, ConditionalOperators.Not(ExtractCondition(block.ParentConditionBlock)));
			}
			return ConditionalOperators.And(block.Condition, ExtractCondition(block.ParentConditionBlock));
		}
		return block.Condition;
	}
}
