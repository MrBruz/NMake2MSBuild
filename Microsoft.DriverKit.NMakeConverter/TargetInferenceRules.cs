using System.Collections.Generic;
using Microsoft.DriverKit.NMakeConverter.Commands;

namespace Microsoft.DriverKit.NMakeConverter;

internal class TargetInferenceRules
{
	public List<InferenceRule> mRules = new List<InferenceRule>();

	public List<InferenceRule> Rules => mRules;

	public TargetInferenceRules()
	{
		AddRule(new TargetInferenceRule("asm", "exe", new string[1] { "$(AS) $(AFLAGS) %24%2A.asm" }));
		AddRule(new TargetInferenceRule("asm", "obj", new string[1] { "$(AS) $(AFLAGS) /c %24%2A.asm" }));
		AddRule(new TargetInferenceRule("c", "exe", new string[1] { "$(CC) $(CFLAGS) %24%2A.c" }));
		AddRule(new TargetInferenceRule("c", "obj", new string[1] { "$(CC) $(CFLAGS) /c %24%2A.c" }));
		AddRule(new TargetInferenceRule("cpp", "exe", new string[1] { "$(CPP) $(CPPFLAGS) %24%2A.cpp" }));
		AddRule(new TargetInferenceRule("cpp", "obj", new string[1] { "$(CPP) $(CPPFLAGS) /c %24%2A.cpp" }));
		AddRule(new TargetInferenceRule("cxx", "exe", new string[1] { "$(CXX) $(CXXFLAGS) %24%2A.cxx" }));
		AddRule(new TargetInferenceRule("cxx", "obj", new string[1] { "$(CXX) $(CXXFLAGS) /c %24%2A.cxx" }));
		AddRule(new TargetInferenceRule("bas", "obj", new string[1] { "$(BC) $(BFLAGS) %24%2A.bas;" }));
		AddRule(new TargetInferenceRule("cbl", "exe", new string[1] { "$(COBOL) $(COBFLAGS) %24%2A.cbl, %24%2A.exe;" }));
		AddRule(new TargetInferenceRule("cbl", "obj", new string[1] { "$(COBOL) $(COBFLAGS) %24%2A.cbl;" }));
		AddRule(new TargetInferenceRule("f", "exe", new string[1] { "$(FOR) $(FFLAGS) %24%2A.f" }));
		AddRule(new TargetInferenceRule("f", "obj", new string[1] { "$(FOR) /c $(FFLAGS) %24%2A.f" }));
		AddRule(new TargetInferenceRule("f90", "exe", new string[1] { "$(FOR) $(FFLAGS) %24%2A.f90" }));
		AddRule(new TargetInferenceRule("f90", "obj", new string[1] { "$(FOR) /c $(FFLAGS) %24%2A.f90" }));
		AddRule(new TargetInferenceRule("for", "exe", new string[1] { "$(FOR) $(FFLAGS) %24%2A.for" }));
		AddRule(new TargetInferenceRule("for", "obj", new string[1] { "$(FOR) /c $(FFLAGS) %24%2A.for" }));
		AddRule(new TargetInferenceRule("pas", "exe", new string[1] { "$(PASCAL) $(PFLAGS) %24%2A.pas" }));
		AddRule(new TargetInferenceRule("pas", "obj", new string[1] { "$(PASCAL) /c $(PFLAGS) %24%2A.pas" }));
		AddRule(new TargetInferenceRule("rc", "res", new string[1] { "$(RC) $(RFLAGS) /r %24%2A" }));
		AddRule(new TargetInferenceRule("mof", "bmf", new string[4] { "if not exist %24< copy  \"$(OBJ_PATH)\\%24%28<F%29\" %24<", "mofcomp -Amendment:ms_409 -MFL:$(O)\\MFL.MFL -MOF:$(O)\\MOF.MOF %24<", "wmimofck -y$(O)\\MOF.MOF -z$(O)\\MFL.MFL $(O)\\MOFMFL.MOF", "mofcomp -B:%24%40 $(O)\\MOFMFL.MOF" }));
	}

	public void AddRule(InferenceRule rule)
	{
		mRules.Add(rule);
	}
}
