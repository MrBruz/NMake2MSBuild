using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.DriverKit.NMakeConverter;

internal class DirsParser
{
	private string m_DirsFile;

	internal DirsParser(string dirsFile)
	{
		if (string.IsNullOrEmpty(dirsFile))
		{
			throw new ArgumentNullException("dirsFile", "Must provide a File path to a dirs file");
		}
		if (!File.Exists(dirsFile))
		{
			throw new FileNotFoundException("dirsFile", dirsFile);
		}
		m_DirsFile = dirsFile;
	}

	internal List<string> Parse()
	{
		List<string> list = new List<string>();
		string text = null;
		Regex regex = new Regex("\\s*(DIRS\\s*=)?(\\s*\\\\?\\s*(?<dir>[^\\s\\\\{=#]+)({[^}]*})?)*", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.CultureInvariant);
		StreamReader streamReader = new StreamReader(m_DirsFile);
		while ((text = streamReader.ReadLine()) != null)
		{
			if (string.IsNullOrEmpty(text) || text.TrimStart(new char[1] { ' ' }).StartsWith("#", StringComparison.OrdinalIgnoreCase) || !regex.IsMatch(text))
			{
				continue;
			}
			Match match = regex.Match(text);
			for (int i = 0; i < match.Groups[1].Captures.Count; i++)
			{
				string value = match.Groups[1].Captures[i].Value;
				if (!string.IsNullOrEmpty(value))
				{
					list.Add(value);
				}
			}
		}
		return list;
	}
}
