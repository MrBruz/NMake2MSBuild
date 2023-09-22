using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.DriverKit.NMakeConverter;

internal class TraverseDirs
{
	private List<string> m_TraversalProjects = new List<string>();

	private static string ChooseProjectFile(string targetDir, out bool isTraversal)
	{
		if (string.IsNullOrEmpty(targetDir) || !Directory.Exists(targetDir))
		{
			Logger.TraceEvent(TraceEventType.Warning, null, "The directory associated with the entry \"{0}\" in dirs, does not exist on disk", targetDir);
			isTraversal = false;
			return null;
		}
		if (File.Exists(Path.Combine(targetDir, "dirs")))
		{
			isTraversal = true;
			return Path.Combine(targetDir, "dirs");
		}
		if (File.Exists(Path.Combine(targetDir, "sources")))
		{
			isTraversal = false;
			return Path.Combine(targetDir, "sources");
		}
		isTraversal = false;
		return null;
	}

	private void PopulateTraversalProjects(string dirsFile)
	{
		Stack<string> stack = new Stack<string>();
		stack.Push(Path.GetDirectoryName(dirsFile));
		bool isTraversal = false;
		while (stack.Count != 0)
		{
			string text = ChooseProjectFile(stack.Pop(), out isTraversal);
			if (isTraversal)
			{
				DirsParser dirsParser;
				try
				{
					dirsParser = new DirsParser(text);
				}
				catch (Exception ex)
				{
					Logger.TraceEvent(TraceEventType.Error, null, "Failed to parse DIRS file \"{0}\": \"{1}\"", dirsFile, ex.ToString());
					throw;
				}
				List<string> list = dirsParser.Parse();
				list.Reverse();
				foreach (string item in list)
				{
					stack.Push(Path.Combine(Path.GetDirectoryName(text), item));
				}
			}
			else if (!string.IsNullOrEmpty(text))
			{
				m_TraversalProjects.Add(text);
			}
		}
	}

	private List<string> GetAllDirsFiles(string dirsFile)
	{
		Stack<string> stack = new Stack<string>();
		stack.Push(Path.GetDirectoryName(dirsFile));
		List<string> list = new List<string>();
		bool isTraversal = false;
		while (stack.Count != 0)
		{
			string text = ChooseProjectFile(stack.Pop(), out isTraversal);
			if (!isTraversal)
			{
				continue;
			}
			list.Add(text);
			DirsParser dirsParser;
			try
			{
				dirsParser = new DirsParser(text);
			}
			catch (Exception ex)
			{
				Logger.TraceEvent(TraceEventType.Error, null, "Failed to parse DIRS file \"{0}\": \"{1}\"", dirsFile, ex.ToString());
				throw;
			}
			List<string> list2 = dirsParser.Parse();
			list2.Reverse();
			foreach (string item in list2)
			{
				stack.Push(Path.Combine(Path.GetDirectoryName(text), item));
			}
		}
		return list;
	}

	public Dictionary<string, List<string>> GetTraversalMap(string dirsFile)
	{
		Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>();
		List<string> list = null;
		list = GetAllDirsFiles(dirsFile);
		foreach (string item in list)
		{
			List<string> list2 = new List<string>();
			TraverseDirs traverseDirs = new TraverseDirs();
			list2 = traverseDirs.GetTraversalProjects(item);
			dictionary.Add(item, list2);
		}
		return dictionary;
	}

	public List<string> GetTraversalProjects(string dirsFile)
	{
		PopulateTraversalProjects(dirsFile);
		return m_TraversalProjects;
	}
}
