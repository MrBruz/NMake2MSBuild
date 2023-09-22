#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.DriverKit.NMakeConverter;

internal static class ConditionParser
{
	private delegate string UnaryOperator(string operand);

	private delegate string BinaryOperator(string leftOperand, string rightOperand);

	private static Dictionary<string, UnaryOperator> UnaryOperators = new Dictionary<string, UnaryOperator>();

	private static Dictionary<string, BinaryOperator> BinaryOperators = new Dictionary<string, BinaryOperator>();

	private static bool initialized = false;

	private static void InitializeConditionParser()
	{
		UnaryOperators.Add("!", ConditionalOperators.Not);
		UnaryOperators.Add("Defined", ConditionalOperators.Defined);
		UnaryOperators.Add("Exists", ConditionalOperators.Exists);
		UnaryOperators.Add("Exist", ConditionalOperators.Exist);
		UnaryOperators.Add("EnsureIsBool", ConditionalOperators.EnsureIsBool);
		BinaryOperators.Add("||", ConditionalOperators.Or);
		BinaryOperators.Add("&&", ConditionalOperators.And);
		BinaryOperators.Add("==", ConditionalOperators.Equals);
		BinaryOperators.Add(">=", ConditionalOperators.GreaterThanOrEqual);
		BinaryOperators.Add("<=", ConditionalOperators.LessThanOrEqual);
		BinaryOperators.Add("!=", ConditionalOperators.NotEquals);
		BinaryOperators.Add(">", ConditionalOperators.GreaterThan);
		BinaryOperators.Add("<", ConditionalOperators.LessThan);
		Logger.TraceEvent(TraceEventType.Verbose, null, "Conditional parser initialized with unary operators : {0}", string.Join("  ", UnaryOperators.Keys));
		Logger.TraceEvent(TraceEventType.Verbose, null, "Conditional parser initialized with binary operators: {0}", string.Join("  ", BinaryOperators.Keys));
		initialized = true;
	}

	public static string ConvertToMSBuildSyntax(string expression)
	{
		Trace.Indent();
		if (!initialized)
		{
			InitializeConditionParser();
		}
		string expression2 = string.Format(CultureInfo.InvariantCulture, "EnsureIsBool({0})", new object[1] { expression });
		string text = ConvertConditionToMsBuildSyntax(expression2);
		Logger.TraceEvent(TraceEventType.Verbose, null, "Conditional NMake expression \"{0}\" was converted to MSBuild expression \"{1}\"", expression, text);
		Trace.Unindent();
		return text;
	}

	private static string ConvertConditionToMsBuildSyntax(string expression)
	{
		expression = StringUtilities.RemoveOuterMostParantheses(expression);
		string text = ConvertIfUnaryOperatorFirstOrNoOperator(expression);
		if (text != null)
		{
			return text;
		}
		return ConvertIfBinaryOperatorFirst(expression);
	}

	private static string ConvertIfUnaryOperatorFirstOrNoOperator(string expression)
	{
		expression = StringUtilities.RemoveOuterMostParantheses(expression);
		string text = null;
		int num = -1;
		foreach (string key in BinaryOperators.Keys)
		{
			int num2 = StringUtilities.FindFirstUngroupedIdxOf(key, expression);
			if ((num2 < num || num < 0) && num2 >= 0)
			{
				num = num2;
				text = key;
			}
		}
		foreach (string key2 in UnaryOperators.Keys)
		{
			if (expression.StartsWith(key2, StringComparison.OrdinalIgnoreCase))
			{
				string expression2;
				if (text != null)
				{
					expression2 = expression.Substring(key2.Length, num - key2.Length);
					string expression3 = expression.Substring(num + text.Length);
					return BinaryOperators[text](UnaryOperators[key2](ConvertConditionToMsBuildSyntax(expression2)), ConvertConditionToMsBuildSyntax(expression3));
				}
				expression2 = expression.Substring(key2.Length);
				return UnaryOperators[key2](ConvertConditionToMsBuildSyntax(expression2));
			}
		}
		if (text == null)
		{
			return expression;
		}
		return null;
	}

	private static string ConvertIfBinaryOperatorFirst(string expression)
	{
		expression = StringUtilities.RemoveOuterMostParantheses(expression);
		foreach (string key in BinaryOperators.Keys)
		{
			int num = StringUtilities.FindFirstUngroupedIdxOf(key, expression);
			if (num > -1)
			{
				string expression2 = expression.Substring(0, num);
				string expression3 = expression.Substring(num + key.Length);
				return BinaryOperators[key](ConvertConditionToMsBuildSyntax(expression2), ConvertConditionToMsBuildSyntax(expression3));
			}
		}
		return null;
	}
}
