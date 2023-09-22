using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.DriverKit.NMakeConverter;

internal static class Logger
{
	private static TextWriterTraceListener textListener = null;

	private static ConsoleTraceListener consoleListener = null;

	private static ConsoleTraceListener consoleErrListener = null;

	private static StringNormalizer consoleStringNormalizer = new StringNormalizer();

	private static StringNormalizer textStringNormalizer = new StringNormalizer();

	private static Regex regLineFeed = new Regex("(?<!\\r)\\n", RegexOptions.Compiled);

	private static string textLogPath = null;

	private static int errorLevel = 0;

	public static int ErrorLevel => errorLevel;

	public static string TextLogPath => textLogPath;

	private static int GetConsoleBufferWidth()
	{
		try
		{
			return Console.BufferWidth;
		}
		catch (IOException)
		{
			return 500;
		}
	}

	public static void TraceEvent(TraceEventType eventType, int? id, string format, params object[] args)
	{
		StackFrame stackFrame = new StackFrame(1);
		string name = stackFrame.GetMethod().Name;
		string text = string.Empty;
		string text2 = string.Empty;
		if (args.Length > 0)
		{
			format = string.Format(CultureInfo.InvariantCulture, format, args);
		}
		switch (eventType)
		{
		case TraceEventType.Critical:
		case TraceEventType.Error:
			errorLevel = 2;
			break;
		case TraceEventType.Warning:
			if (errorLevel < 1)
			{
				errorLevel = 1;
			}
			break;
		}
		int id2 = (id.HasValue ? id.Value : 0);
		string text3 = ((!id.HasValue) ? string.Empty : id.ToString());
		int consoleBufferWidth = GetConsoleBufferWidth();
		bool flag = true;
		bool flag2 = true;
		foreach (TraceListener listener in Trace.Listeners)
		{
			if (listener.Filter != null && !listener.Filter.ShouldTrace(null, text, eventType, id2, null, null, null, null))
			{
				continue;
			}
			if (listener is ConsoleTraceListener)
			{
				if (flag)
				{
					text2 = consoleStringNormalizer.NormalizeLength(name, listener.IndentSize * listener.IndentLevel);
					flag = false;
				}
				string text4 = string.Concat(text2, " :\t", eventType, " ", text3, " : ");
				string text5 = "\n" + new string(' ', listener.IndentSize * listener.IndentLevel + text4.Length) + "\t";
				int num = consoleBufferWidth - text5.Length;
				string message;
				string message2;
				if (num > 0 && format.Length > num)
				{
					text5 = "\n    ";
					num = consoleBufferWidth - text5.Length;
					int num2 = 0;
					StringBuilder stringBuilder = new StringBuilder();
					string text6 = format;
					for (int i = 0; i < text6.Length; i++)
					{
						char value = text6[i];
						if (value.Equals('\n'))
						{
							num2 = 0;
						}
						if (num2++ == num)
						{
							stringBuilder.Append('\n');
							num2 = 1;
						}
						stringBuilder.Append(value);
					}
					format = stringBuilder.ToString();
					message = text4 + text5;
					message2 = format.Replace("\n", text5) + "\n";
				}
				else
				{
					message = text4 + "\n    ";
					message2 = format.Replace("\n", text5) + "\n";
				}
				Console.ForegroundColor = ConsoleColor.Cyan;
				listener.Write(message);
				Console.ResetColor();
				switch (eventType)
				{
				case TraceEventType.Critical:
					Console.ForegroundColor = ConsoleColor.Red;
					break;
				case TraceEventType.Error:
					Console.ForegroundColor = ConsoleColor.Red;
					break;
				case TraceEventType.Warning:
					Console.ForegroundColor = ConsoleColor.Yellow;
					break;
				}
				listener.Write(message2);
				Console.ResetColor();
			}
			else
			{
				if (flag2)
				{
					text = textStringNormalizer.NormalizeLength(name, listener.IndentSize * listener.IndentLevel);
					flag2 = false;
				}
				string text7 = string.Concat(text, ":\t", eventType, " ", text3, " : ");
				string newValue = "\n" + new string(' ', listener.IndentSize * listener.IndentLevel + text7.Length) + "\t";
				string input = text7 + format.Replace("\n", newValue);
				listener.WriteLine(regLineFeed.Replace(input, Environment.NewLine));
			}
		}
	}

	public static bool SetupTracing(string textLogName, SourceLevels textTraceLevel, SourceLevels consoleTraceLevel)
	{
		if (consoleTraceLevel != 0)
		{
			consoleErrListener = new ConsoleTraceListener(useErrorStream: true);
			consoleErrListener.Filter = new EventTypeFilter(SourceLevels.Error);
			Trace.Listeners.Add(consoleErrListener);
			consoleListener = new ConsoleTraceListener(useErrorStream: false);
			consoleListener.Filter = new EventTypeFilter(consoleTraceLevel & ~SourceLevels.Critical & ~SourceLevels.Error);
			Trace.Listeners.Add(consoleListener);
		}
		textLogPath = Path.GetFullPath(textLogName);
		if (textTraceLevel != 0)
		{
			try
			{
				if (!Directory.Exists(Path.GetDirectoryName(textLogPath)))
				{
					Directory.CreateDirectory(Path.GetDirectoryName(textLogPath));
				}
				StreamWriter writer = new StreamWriter(textLogPath, append: false);
				textListener = new TextWriterTraceListener(writer, "Text trace for NMake to MSBuild converter");
			}
			catch (UnauthorizedAccessException ex)
			{
				Console.Error.WriteLine(ex.Message);
				return false;
			}
			textListener.Filter = new EventTypeFilter(textTraceLevel);
			Trace.Listeners.Add(textListener);
		}
		DisableConsoleListenerIndent();
		return true;
	}

	private static void DisableConsoleListenerIndent()
	{
		foreach (TraceListener listener in Trace.Listeners)
		{
			if (listener is ConsoleTraceListener)
			{
				listener.IndentSize = 0;
			}
		}
	}

	public static void ClearTracing()
	{
		foreach (TraceListener listener in Trace.Listeners)
		{
			listener.Flush();
			listener.Close();
		}
		Trace.Listeners.Clear();
		if (textListener != null)
		{
			textListener.Close();
		}
	}
}
