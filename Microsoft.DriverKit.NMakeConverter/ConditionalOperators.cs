#define TRACE
using System;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Microsoft.DriverKit.NMakeConverter;

internal static class ConditionalOperators
{
	public static string Not(string operand)
	{
		return string.Format(CultureInfo.InvariantCulture, "!({0})", new object[1] { EnsureIsBool(operand) });
	}

	public static string Defined(string operand)
	{
		return string.Format(CultureInfo.InvariantCulture, "'$({0})'!=''", new object[1] { operand });
	}

	public static string Exists(string operand)
	{
		return string.Format(CultureInfo.InvariantCulture, "Exists('{0}')", new object[1] { operand });
	}

	public static string Exist(string operand)
	{
		return Exists(operand);
	}

	public static string EnsureIsBool(string operand)
	{
		string[] array = new string[8] { "==", "!=", "<", ">", "<=", ">=", "&lt;", "&gt;" };
		string[] array2 = new string[2] { "Exists(.*)", "HasTrailingSlash(.*)" };
		if (operand.Trim().Equals("true", StringComparison.OrdinalIgnoreCase) || operand.Trim().Equals("false", StringComparison.OrdinalIgnoreCase))
		{
			return operand;
		}
		string[] array3 = array;
		foreach (string toFind in array3)
		{
			if (StringUtilities.FindFirstUnquotedIdxOf(toFind, operand, '\'') > -1)
			{
				return operand;
			}
		}
		string[] array4 = array2;
		foreach (string pattern in array4)
		{
			if (Regex.IsMatch(operand, pattern))
			{
				return operand;
			}
		}
		string text = string.Format(CultureInfo.InvariantCulture, "'{0}'!='' And {0}!=0", new object[1] { operand });
		Logger.TraceEvent(TraceEventType.Verbose, null, "Conditional Operand \"{0}\" coerced to boolean \"{1}\"", operand, text);
		return text;
	}

	public static string And(string left, string right)
	{
		return string.Format(CultureInfo.InvariantCulture, "({0}) And ({1})", new object[2]
		{
			EnsureIsBool(left),
			EnsureIsBool(right)
		});
	}

	public static string Or(string left, string right)
	{
		return string.Format(CultureInfo.InvariantCulture, "({0}) Or ({1})", new object[2]
		{
			EnsureIsBool(left),
			EnsureIsBool(right)
		});
	}

	public static string Equals(string left, string right)
	{
		if (IsValidDecimalOrHex(left) || IsValidDecimalOrHex(right))
		{
			Logger.TraceEvent(TraceEventType.Verbose, null, "Given comparison operands \"{0}\" and \"{1}\" assuming a numeric comparison", left, right);
			return string.Format(CultureInfo.InvariantCulture, "{0}=={1}", new object[2] { left, right });
		}
		Logger.TraceEvent(TraceEventType.Verbose, null, "Given comparison operands \"{0}\" and \"{1}\" assuming a string comparison", left, right);
		return string.Format(CultureInfo.InvariantCulture, "{0}=={1}", new object[2]
		{
			ConvertToMSBuildQuotes(left),
			ConvertToMSBuildQuotes(right)
		});
	}

	public static string NotEquals(string left, string right)
	{
		if (IsValidDecimalOrHex(left) || IsValidDecimalOrHex(right))
		{
			Logger.TraceEvent(TraceEventType.Verbose, null, "Given comparison operands \"{0}\" and \"{1}\" assuming a numeric comparison", left, right);
			return string.Format(CultureInfo.InvariantCulture, "{0}!={1}", new object[2] { left, right });
		}
		Logger.TraceEvent(TraceEventType.Verbose, null, "Given comparison operands \"{0}\" and \"{1}\" assuming a string comparison", left, right);
		return string.Format(CultureInfo.InvariantCulture, "{0}!={1}", new object[2]
		{
			ConvertToMSBuildQuotes(left),
			ConvertToMSBuildQuotes(right)
		});
	}

	public static string GreaterThanOrEqual(string left, string right)
	{
		return string.Format(CultureInfo.InvariantCulture, "{0}>={1}", new object[2] { left, right });
	}

	public static string LessThanOrEqual(string left, string right)
	{
		return string.Format(CultureInfo.InvariantCulture, "{0}<={1}", new object[2] { left, right });
	}

	public static string LessThan(string left, string right)
	{
		return string.Format(CultureInfo.InvariantCulture, "{0}<{1}", new object[2] { left, right });
	}

	public static string GreaterThan(string left, string right)
	{
		return string.Format(CultureInfo.InvariantCulture, "{0}>{1}", new object[2] { left, right });
	}

	private static bool IsValidDecimalOrHex(string expression)
	{
		try
		{
			decimal.Parse(expression, CultureInfo.InvariantCulture);
			return true;
		}
		catch (FormatException)
		{
			char[] trimChars = new char[2] { '0', 'x' };
			if (!int.TryParse(expression.TrimStart(trimChars), NumberStyles.HexNumber, null, out var _))
			{
				return false;
			}
		}
		return true;
	}

	public static string ConvertToMSBuildQuotes(string expression)
	{
		Trace.Indent();
		expression = expression.Replace("'", "%2C");
		expression = StringUtilities.UnQuote(expression);
		string text = string.Format(CultureInfo.InvariantCulture, "'{0}'", new object[1] { expression });
		Logger.TraceEvent(TraceEventType.Verbose, null, "NMake expression \"{0}\" converted to MSBuild quoted expression \"{1}\"", expression, text);
		Trace.Unindent();
		return text;
	}
}
