using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

namespace Microsoft.DriverKit.Sqm;

internal static class AssemblyHelper
{
	internal static string GetSqmRoot()
	{
		string text = null;
		try
		{
			RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey("SOFTWARE\\Microsoft\\Windows Kits\\WDK");
			if (registryKey != null)
			{
				try
				{
					object value = registryKey.GetValue("WDKContentRoot");
					if (value != null)
					{
						text = Path.Combine(value.ToString(), "bin\\");
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
				}
				finally
				{
					registryKey.Close();
				}
			}
		}
		catch (Exception ex2)
		{
			Console.WriteLine(ex2.Message);
		}
		if (!Directory.Exists(text))
		{
			throw new DirectoryNotFoundException(string.Format(CultureInfo.InvariantCulture, "Directory \"{0}\" not found", new object[1] { text }));
		}
		return text;
	}

	[SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods", Scope = "member", Target = "Microsoft.DriverKit.Build.Tasks.AssemblyHelper.#MyResolveEventHandler(System.Object,System.ResolveEventArgs)", MessageId = "System.Reflection.Assembly.LoadFrom")]
	internal static Assembly MyResolveEventHandler(object sender, ResolveEventArgs args)
	{
		try
		{
			string sqmRoot = GetSqmRoot();
			string assemblyFile = "";
			Assembly executingAssembly = Assembly.GetExecutingAssembly();
			AssemblyName[] referencedAssemblies = executingAssembly.GetReferencedAssemblies();
			AssemblyName[] array = referencedAssemblies;
			foreach (AssemblyName assemblyName in array)
			{
				if (assemblyName.FullName.Substring(0, assemblyName.FullName.IndexOf(",", StringComparison.OrdinalIgnoreCase)) == args.Name.Substring(0, args.Name.IndexOf(",", StringComparison.OrdinalIgnoreCase)))
				{
					assemblyFile = sqmRoot + args.Name.Substring(0, args.Name.IndexOf(",", StringComparison.OrdinalIgnoreCase)) + ".dll";
					break;
				}
			}
			return Assembly.LoadFrom(assemblyFile);
		}
		catch (Exception)
		{
			return null;
		}
	}
}
