On Error Resume Next
Set objFS = CreateObject("Scripting.FileSystemObject")
If objFS.FolderExists("c:\Program Files (x86)") = True Then
	MsgBox "32bit版ChaKi.NETをインストールしようとしています.  システムが64bitのようですので、64bit版(setup64.msi)を推奨します."
End If