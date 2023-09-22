using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: CLSCompliant(true)]
[assembly: AssemblyProduct("Microsoft (R) Windows (R) Operating System")]
[assembly: AssemblyFileVersion("6.2.9200.16384")]
[assembly: ComVisible(false)]
[assembly: AssemblyCompany("Microsoft Corporation")]
[assembly: AssemblyCopyright("Copyright (c) Microsoft Corporation. All rights reserved.")]
[assembly: AssemblyTitle("NMake2MSBuild")]
[assembly: AssemblyDescription("Command Line tool for converting sources file to VcxProj files.")]
[assembly: AssemblyVersion("6.2.0.0")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.DriverKit.NMakeConverter.StringUtilities.#ExpandDelimitedStringToArray(System.String,System.String)")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope = "member", Target = "Microsoft.DriverKit.NMakeConverter.Commands.DirectiveTypes.#Ifndef", MessageId = "Ifndef")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope = "member", Target = "Microsoft.DriverKit.NMakeConverter.Commands.DirectiveTypes.#Elseifdef", MessageId = "Elseifdef")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope = "member", Target = "Microsoft.DriverKit.NMakeConverter.Commands.DirectiveTypes.#Elseifndef", MessageId = "Elseifndef")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope = "member", Target = "Microsoft.DriverKit.NMakeConverter.Commands.DirectiveTypes.#Undef", MessageId = "Undef")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope = "member", Target = "Microsoft.DriverKit.NMakeConverter.Commands.DirectiveTypes.#Elseif", MessageId = "Elseif")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope = "member", Target = "Microsoft.DriverKit.NMakeConverter.Commands.SourcesDirective.#ParseIfApplies(System.String,System.Boolean&)", MessageId = "nmake")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope = "member", Target = "Microsoft.DriverKit.NMakeConverter.Commands.SourcesDirective.#ParseIfApplies(System.String,System.Boolean&,Microsoft.DriverKit.NMakeConverter.ConditionBlock)", MessageId = "nmake")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope = "type", Target = "Microsoft.DriverKit.NMakeConverter.Commands.ParseIfApplies", MessageId = "nmake")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.DriverKit.NMakeConverter.Commands.DotDirective.#get_NmakeLine()")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.DriverKit.NMakeConverter.Commands.Conditional.#get_nMakeCondition()")]
[module: SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Scope = "member", Target = "Microsoft.DriverKit.NMakeConverter.PropertyPageSupport.TargetCommandsParser.#ConvertNmakeCommands(System.String[],System.String,System.String,Microsoft.Build.Evaluation.Project&)", MessageId = "Nmake")]
[module: SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Scope = "member", Target = "Microsoft.DriverKit.NMakeConverter.PropertyPageSupport.ToolSwitch.#.ctor(Microsoft.DriverKit.NMakeConverter.PropertyPageSupport.ToolSwitchType,System.String,System.String)")]
[module: SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods", Scope = "member", Target = "Microsoft.DriverKit.NMakeConverter.PropertyPageSupport.ToolSwitch.#Type")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.DriverKit.NMakeConverter.PropertyPageSupport.SwitchInfo.#set_Metadata(System.String)")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.DriverKit.NMakeConverter.PropertyPageSupport.SwitchInfo.#set_Type(Microsoft.DriverKit.NMakeConverter.PropertyPageSupport.ToolSwitchType)")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.DriverKit.NMakeConverter.PropertyPageSupport.SwitchInfo.#set_AssociatedSwitch(System.String)")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.DriverKit.NMakeConverter.PropertyPageSupport.SwitchInfo.#set_Flag(System.String)")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.DriverKit.NMakeConverter.PropertyPageSupport.ToolSwitch.#set_Type(Microsoft.DriverKit.NMakeConverter.PropertyPageSupport.ToolSwitchType)")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.DriverKit.NMakeConverter.PropertyPageSupport.ToolSwitch.#set_Name(System.String)")]
[module: SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope = "member", Target = "Microsoft.DriverKit.NMakeConverter.PropertyPageSupport.ToolSwitch.#set_AssociatedSwitch(System.String)")]
