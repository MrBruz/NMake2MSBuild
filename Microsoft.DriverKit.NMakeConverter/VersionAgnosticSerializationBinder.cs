using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Microsoft.DriverKit.NMakeConverter;

internal class VersionAgnosticSerializationBinder : SerializationBinder
{
	public override Type BindToType(string assemblyName, string typeName)
	{
		Type type = Type.GetType(Assembly.CreateQualifiedName(assemblyName, typeName));
		if (type == null)
		{
			string simpleAssemblyName = assemblyName.Split(',').FirstOrDefault();
			if (string.IsNullOrEmpty(simpleAssemblyName))
			{
				throw new InvalidOperationException("Cannot retrieve assembly name from string '" + assemblyName + "'");
			}
			Assembly assembly2 = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault((Assembly assembly) => assembly.GetName().Name.Equals(simpleAssemblyName));
			if (assembly2 == null)
			{
				throw new InvalidOperationException("The requested assembly '" + simpleAssemblyName + "' was not found");
			}
			type = assembly2.GetType(typeName);
		}
		if (type == null)
		{
			Logger.TraceEvent(TraceEventType.Critical, null, "Cannot bind requested type  \"{0}, {1}\"", assemblyName, typeName);
		}
		return type;
	}
}