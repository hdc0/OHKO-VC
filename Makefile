CSC = csc

SOURCES = \
	GtaVersions.cs \
	ListViewItemComparer.cs \
	LocalProcess.cs \
	OhkoVC.cs \
	ProcessSelectionForm.cs \
	Program.cs \
	WinApi.cs

ohko_vc.exe: $(SOURCES)
	$(CSC) $(CSFLAGS) /optimize+ /out:$@ /platform:x86 /target:winexe $(SOURCES)
