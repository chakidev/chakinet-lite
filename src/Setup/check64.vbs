On Error Resume Next
Set objFS = CreateObject("Scripting.FileSystemObject")
If objFS.FolderExists("c:\Program Files (x86)") = True Then
	MsgBox "32bit��ChaKi.NET���C���X�g�[�����悤�Ƃ��Ă��܂�.  �V�X�e����64bit�̂悤�ł��̂ŁA64bit��(setup64.msi)�𐄏����܂�."
End If