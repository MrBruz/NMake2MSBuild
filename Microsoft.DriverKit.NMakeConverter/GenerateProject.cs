using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Microsoft.DriverKit.MakeSolution;

namespace Microsoft.DriverKit.NMakeConverter;

public static class GenerateProject
{
	private static IConversionSettings conversionSettings;

	public static IConversionSettings ConversionSettings => conversionSettings;

	public static string Convert(IConversionSettings projectConversionSettings)
	{
		conversionSettings = projectConversionSettings;
		try
		{
			if (!Logger.SetupTracing(conversionSettings.TextLogName, conversionSettings.TextTraceLevel, conversionSettings.ConsoleTraceLevel) || !FileConverter.RequiredFilesArePresent())
			{
				return null;
			}
			Logger.TraceEvent(TraceEventType.Verbose, null, DateTime.Now.ToString());
			DependencyScanner.ClearCache();
			if (!Path.GetFileName(conversionSettings.PrimaryInputFile).Equals("dirs", StringComparison.OrdinalIgnoreCase))
			{
				if (conversionSettings.SourcesFileList != null && conversionSettings.SourcesFileList.Count > 1)
				{
					string result = null;
					Logger.TraceEvent(TraceEventType.Information, null, "Batch converting the following sources:\n  {0}", string.Join(Environment.NewLine + "  ", conversionSettings.SourcesFileList));
					List<string> list = new List<string>();
					foreach (string sourcesFile in conversionSettings.SourcesFileList)
					{
						try
						{
							string item = FileConverter.Convert(sourcesFile, sourcesFile + ".props", !conversionSettings.SafeMode);
							list.Add(item);
						}
						catch (Exception ex)
						{
							Logger.TraceEvent(TraceEventType.Critical, null, "Critical failure while converting '{0}', skipping conversion. Error:\nMessage:{1}", sourcesFile, ex.Message);
							Logger.TraceEvent(TraceEventType.Verbose, null, "StackTrace=\n{0}", ex.StackTrace);
						}
					}
					if (conversionSettings.NoSolutionFile)
					{
						Logger.TraceEvent(TraceEventType.Information, null, "========== Project Conversion Finished: {0} Projects converted ==========", conversionSettings.SourcesFileList.Count);
					}
					else if (list.Count > 0)
					{
						string text = conversionSettings.SolutionFile;
						if (string.IsNullOrWhiteSpace(text))
						{
							string text2 = conversionSettings.OverrideProjectName;
							if (string.IsNullOrWhiteSpace(text2))
							{
								text2 = Path.GetFileNameWithoutExtension(list[0]);
							}
							text2 += ".sln";
							string commonDirectoryPath = StringUtilities.GetCommonDirectoryPath(list);
							text = ((!string.IsNullOrWhiteSpace(commonDirectoryPath)) ? Path.Combine(commonDirectoryPath, text2) : Path.Combine(Path.GetDirectoryName(list[0]), text2));
						}
						if (!conversionSettings.NoPackageProject)
						{
							string text3 = conversionSettings.PackageProjectFile;
							if (string.IsNullOrEmpty(text3))
							{
								text3 = PackageProjectGenerator.GetDefaultPackageFileNameFor(text);
							}
							if (PackageProjectGenerator.CreatePackage(list.ToArray(), text3))
							{
								list.Add(text3);
								list.Reverse();
							}
						}
						if (SolutionGenerator.CreateSolutionIncludingDependencies(list.ToArray(), text, generateSolutionEvenIfNoDependenciesExist: true))
						{
							result = text;
							Logger.TraceEvent(TraceEventType.Information, null, "========== Project Conversion Finished: 1 Solution created with {0} Projects ==========", list.Count);
						}
					}
					Logger.TraceEvent(TraceEventType.Verbose, null, DateTime.Now.ToString());
					Logger.ClearTracing();
					return result;
				}
				string convertedFile = conversionSettings.PrimaryInputFile + ".props";
				string text4 = FileConverter.Convert(conversionSettings.PrimaryInputFile, convertedFile, !conversionSettings.SafeMode, conversionSettings.OverrideProjectName);
				string result2 = text4;
				if (Path.GetFileName(conversionSettings.PrimaryInputFile).Equals("sources", StringComparison.OrdinalIgnoreCase))
				{
					bool flag = false;
					if (!conversionSettings.NoSolutionFile)
					{
						string text5 = conversionSettings.SolutionFile;
						if (string.IsNullOrWhiteSpace(text5))
						{
							string text6 = conversionSettings.OverrideProjectName;
							if (string.IsNullOrWhiteSpace(text6))
							{
								text6 = Path.GetFileNameWithoutExtension(text4);
							}
							text6 += ".sln";
							text5 = Path.Combine(Path.GetDirectoryName(text4), text6);
						}
						string text7 = text5;
						string text8 = conversionSettings.PackageProjectFile;
						if (conversionSettings.NoPackageProject)
						{
							text8 = null;
						}
						else if (string.IsNullOrEmpty(text8))
						{
							text8 = PackageProjectGenerator.GetDefaultPackageFileNameFor(text7);
						}
						if (SolutionGenerator.CreateSolutionIncludingDependencies(new string[1] { text4 }, text7, generateSolutionEvenIfNoDependenciesExist: true, out var finalProjectsInSolution, text8))
						{
							flag = true;
							result2 = text7;
							Logger.TraceEvent(TraceEventType.Information, null, "========== Project Conversion Finished: 1 Solution created with {0} Projects ==========", finalProjectsInSolution.Length);
						}
					}
					if (!flag)
					{
						Logger.TraceEvent(TraceEventType.Information, null, "========== Project Conversion Finished: 1 Project converted ==========");
					}
				}
				Logger.TraceEvent(TraceEventType.Verbose, null, DateTime.Now.ToString());
				Logger.ClearTracing();
				return result2;
			}
			Logger.TraceEvent(TraceEventType.Information, null, "Converting all children of DIRS file {0}", conversionSettings.PrimaryInputFile);
			Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			TraverseDirs traverseDirs = new TraverseDirs();
			List<string> traversalProjects = traverseDirs.GetTraversalProjects(conversionSettings.PrimaryInputFile);
			foreach (string item2 in traversalProjects)
			{
				try
				{
					string text9 = FileConverter.Convert(item2, item2 + ".props", !conversionSettings.SafeMode);
					dictionary.Add(item2, text9);
					if (!conversionSettings.NoSolutionFile)
					{
						string solutionFile = Path.Combine(Path.GetDirectoryName(text9), Path.GetFileNameWithoutExtension(text9) + ".sln");
						SolutionGenerator.CreateSolutionIncludingDependencies(new string[1] { text9 }, solutionFile);
					}
				}
				catch (Exception ex2)
				{
					Logger.TraceEvent(TraceEventType.Critical, null, "Critical failure while converting '{0}', skipping conversion. Error:\nMessage:{1}", item2, ex2.Message);
					Logger.TraceEvent(TraceEventType.Verbose, null, "StackTrace=\n{0}", ex2.StackTrace);
				}
			}
			Logger.TraceEvent(TraceEventType.Information, null, "Processed {0} projects spanned by the specified dirs file. Attempting to generate corresponding project solutions", traversalProjects.Count);
			try
			{
				int num = 0;
				int num2 = 0;
				string result3 = null;
				Dictionary<string, List<string>> traversalMap = traverseDirs.GetTraversalMap(conversionSettings.PrimaryInputFile);
				foreach (string key in traversalMap.Keys)
				{
					List<string> list2 = traversalMap[key];
					List<string> list3 = new List<string>();
					string text10 = Path.Combine(Path.GetDirectoryName(key), conversionSettings.DirSolutionFileName);
					if (!string.IsNullOrWhiteSpace(conversionSettings.OverrideProjectName) && object.Equals(key, conversionSettings.PrimaryInputFile))
					{
						text10 = Path.Combine(Path.GetDirectoryName(key), conversionSettings.OverrideProjectName + ".sln");
					}
					if (Path.GetDirectoryName(key).Equals(Path.GetDirectoryName(conversionSettings.PrimaryInputFile), StringComparison.OrdinalIgnoreCase))
					{
						result3 = text10;
					}
					foreach (string item3 in list2)
					{
						if (!dictionary.ContainsKey(item3))
						{
							Logger.TraceEvent(TraceEventType.Error, null, "Did not find VcxProj associated with file \"{0}\", hence the corresponding project will not be added to solution \"{1}\"", item3, text10);
						}
						else
						{
							list3.Add(dictionary[item3]);
						}
					}
					if (!conversionSettings.NoSolutionFile)
					{
						List<string> list4 = list3;
						if (Path.GetFullPath(key) == Path.GetFullPath(conversionSettings.PrimaryInputFile) && !conversionSettings.NoPackageProject)
						{
							string text11 = conversionSettings.PackageProjectFile;
							if (string.IsNullOrEmpty(text11))
							{
								text11 = PackageProjectGenerator.GetDefaultPackageFileNameFor(key);
							}
							if (PackageProjectGenerator.CreatePackage(list3.ToArray(), text11))
							{
								list4.Add(text11);
								list4.Reverse();
							}
						}
						SolutionGenerator.CreateSolutionIncludingDependencies(list4.ToArray(), text10, generateSolutionEvenIfNoDependenciesExist: true);
					}
					num++;
					num2 += list3.Count;
				}
				Logger.TraceEvent(TraceEventType.Information, null, "========== Conversion Finished: {0} Solutions and {1} Projects created ==========", num, num2);
				Logger.TraceEvent(TraceEventType.Verbose, null, DateTime.Now.ToString());
				Logger.ClearTracing();
				return result3;
			}
			catch (Exception ex3)
			{
				Logger.TraceEvent(TraceEventType.Critical, null, "Critical failure while generating .Sln files for '{0}', skipping solution generation. Error:\nMessage:{1}", conversionSettings.PrimaryInputFile, ex3.Message);
				Logger.TraceEvent(TraceEventType.Verbose, null, "StackTrace=\n{0}", ex3.StackTrace);
			}
		}
		catch (Exception ex4)
		{
			Console.Error.WriteLine(string.Format(CultureInfo.InvariantCulture, "Catastrophic failure while converting '{0}':\nMessage:{1}\n StackTrace=\n{2}", new object[3] { conversionSettings.PrimaryInputFile, ex4.Message, ex4.StackTrace }));
		}
		return null;
	}
}
