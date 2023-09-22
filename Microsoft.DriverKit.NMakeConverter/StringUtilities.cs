using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Microsoft.DriverKit.NMakeConverter;

internal static class StringUtilities
{
	public static int FindFirstUngroupedIdxOf(string toFind, string expression, char groupStartChar = '(', char groupEndChar = ')', char quoteChar = '"', int minAllowdMatchIdx = 0)
	{
		if (string.IsNullOrEmpty(expression))
		{
			return -1;
		}
		bool flag = false;
		int num = 0;
		for (int i = 0; i < expression.Length; i++)
		{
			if (expression[i].Equals(quoteChar))
			{
				flag = !flag;
			}
			if (!flag)
			{
				if (expression[i].Equals(groupStartChar))
				{
					num++;
				}
				if (expression[i].Equals(groupEndChar))
				{
					num--;
				}
				if (num == 0 && expression.Substring(i).StartsWith(toFind, StringComparison.OrdinalIgnoreCase) && i >= minAllowdMatchIdx)
				{
					return i;
				}
			}
		}
		return -1;
	}

	public static string RemoveOuterMostParantheses(string expression, char groupStartChar = '(', char groupEndChar = ')', char quoteChar = '"')
	{
		expression = expression.Trim();
		if (string.IsNullOrEmpty(expression))
		{
			return expression;
		}
		if (expression[0].Equals(groupStartChar) && expression[expression.Length - 1].Equals(groupEndChar))
		{
			string expression2 = expression.Substring(1, expression.Length - 2);
			if (!ContainsInvalidGrouping(expression2, groupStartChar, groupEndChar, quoteChar))
			{
				return RemoveOuterMostParantheses(expression2);
			}
		}
		return expression;
	}

	public static bool ContainsInvalidGrouping(string expression, char groupStartChar = '(', char groupEndChar = ')', char quoteChar = '"')
	{
		int num = 0;
		int num2 = 0;
		while (num2 < expression.Length)
		{
			int num3 = FindFirstUnquotedIdxOf(groupStartChar.ToString(), expression.Substring(num2), quoteChar);
			int num4 = FindFirstUnquotedIdxOf(groupEndChar.ToString(), expression.Substring(num2), quoteChar);
			if (num3 < 0 && num4 < 0)
			{
				return false;
			}
			if (num3 < 0 || (num4 < num3 && num4 >= 0))
			{
				num--;
				num2 += num4 + 1;
			}
			else
			{
				num++;
				num2 += num3 + 1;
			}
			if (num < 0)
			{
				return true;
			}
		}
		if (num != 0)
		{
			return true;
		}
		return false;
	}

	public static int FindFirstUnquotedIdxOf(string toFind, string expression, char quoteChar = '"', int minAllowdMatchIdx = 0)
	{
		bool flag = false;
		for (int i = 0; i < expression.Length; i++)
		{
			if (expression[i].Equals(quoteChar))
			{
				flag = !flag;
			}
			if (!flag && expression.Substring(i).StartsWith(toFind, StringComparison.OrdinalIgnoreCase) && i >= minAllowdMatchIdx)
			{
				return i;
			}
		}
		return -1;
	}

	public static bool IsQuoted(int idx, string expression, char quoteChar = '"')
	{
		return idx != FindFirstUnquotedIdxOf(expression[idx].ToString(), expression, quoteChar, idx - 1);
	}

	public static string UnQuote(string expression, char quoteChar = '"')
	{
		expression = expression.Trim();
		if (expression.Length <= 1)
		{
			return expression;
		}
		if (expression.StartsWith(quoteChar.ToString(), StringComparison.Ordinal) && expression.EndsWith(quoteChar.ToString(), StringComparison.Ordinal))
		{
			return expression.Substring(1, expression.Length - 2);
		}
		return expression;
	}

	[SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
	public static string QuoteIfNeeded(string expression, char quoteChar = '"')
	{
		if (expression.Contains(" ") && !IsQuoted(1, expression))
		{
			return string.Format(CultureInfo.InvariantCulture, "\"{0}\"", new object[1] { expression.Trim() });
		}
		return expression;
	}

	public static List<string> ExpandDelimitedStringToArray(string includesString, string delimiter = ";")
	{
		List<string> list = new List<string>();
		string text = includesString;
		int num;
		while ((num = FindFirstUnquotedIdxOf(";", text)) >= 0)
		{
			string text2 = text.Substring(0, num).Trim();
			if (!string.IsNullOrEmpty(text2))
			{
				list.Add(text2);
			}
			text = ((num >= text.Length - 1) ? string.Empty : text.Substring(num + 1));
		}
		if (!string.IsNullOrWhiteSpace(text))
		{
			list.Add(text.Trim());
		}
		return list;
	}

	public static string GetCommonPrefix(IEnumerable<string> strings, StringComparer stringComparer)
	{
		if (strings.Count() <= 1)
		{
			return strings.FirstOrDefault();
		}
		int num = strings.Select((string e) => e.Length).Min();
		int commonPrefixLength = 0;
		for (commonPrefixLength = 0; commonPrefixLength < num && strings.All((string str) => stringComparer.Compare(str[commonPrefixLength], strings.First()[commonPrefixLength]) == 0); commonPrefixLength++)
		{
		}
		return strings.First().Substring(0, commonPrefixLength);
	}

	public static string GetCommonDirectoryPath(IEnumerable<string> pathsToFiles)
	{
		IEnumerable<string> strings = pathsToFiles.Select((string p) => Path.GetDirectoryName(p).TrimEnd(new char[1] { '\\' }) + "\\");
		string commonPrefix = GetCommonPrefix(strings, StringComparer.OrdinalIgnoreCase);
		int num = commonPrefix.LastIndexOf('\\');
		if (num <= 0)
		{
			return string.Empty;
		}
		return commonPrefix.Substring(0, num);
	}
}
