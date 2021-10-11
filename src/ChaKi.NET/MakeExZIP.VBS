'<job>
'<object id="ie" classid="clsid:C08AFD90-F2A1-11D1-8455-00A0C91F3880" />
'<script language="vbscript">
' MakeExZIP.VBS V1.07 (C) yoshioka.teruo@nifty.com 2008-06-23
' MakeExZIP.VBS makes ZIP file in command line.

Option Explicit
Const Usage="Usage: CScript.exe MakeExZIP.VBS ZIPfile files..."
Dim ZIPfile
Dim fso
Dim ie	'WSFÇ≈äOÇ∑
Dim Shell
Dim zFolder
Dim k
Dim Path
Dim FileName
Dim zFolderItem
Dim tFolderName
Dim tFolder
Dim sFolderItem
Dim Count

If WScript.Arguments.Count<2 Then
  WScript.Echo Usage
  WScript.Quit
End If
ZIPfile=WScript.Arguments.Item(0)
Set fso=CreateObject("Scripting.FileSystemObject")
If UCase(fso.GetExtensionName(ZIPfile))<>"ZIP" Then
  WScript.Echo "Invalid Extension Name -",ZIPfile
  WScript.Quit
End If
ZIPfile=fso.GetAbsolutePathName(ZIPfile)
If fso.FileExists(ZIPfile) Then
'IE7à»ç~Ç‡â¬
Set Shell=CreateObject("Shell.Application")
ZIPfile=Shell.NameSpace(ZIPfile).Self.Path
Shell.ShellExecute ZIPfile,,,,0
Do
  For Each ie In Shell.Windows()
    If ie.Visible Then
    ElseIf InStr(TypeName(ie.Document),"IShellFolderViewDual") Then
      If ie.Document.Folder.Self.Path=ZIPfile Then Exit Do
    End If
  Next
  WScript.Sleep 100
Loop
'Set ie=CreateObject("InternetExplorer.Application")	'IE7à»ç~É_ÉÅ
'Set ie=GetObject("new:{C08AFD90-F2A1-11D1-8455-00A0C91F3880}")	'IE7à»ç~Ç‡â¬
'ie.Navigate ZIPfile
  Do While ie.Busy Or ie.ReadyState<>4
    WScript.Sleep 100
  Loop
  Set Shell=ie.Document.Application
  Set zFolder=ie.Document.Folder
Else
  fso.CreateTextFile(ZIPfile,False).Write "PK" & Chr(5) & Chr(6) & String(18,0)
  Set Shell=CreateObject("Shell.Application")
  Set zFolder=Shell.NameSpace(ZIPfile)
End If
For k=1 To WScript.Arguments.Count-1
  Path=WScript.Arguments.Item(k)
  FileName=fso.GetFileName(Path)
  DIm absPath
  absPath = fso.GetParentFolderName( fso.GetAbsolutePathName(Path) )
  Set sFolderItem=Shell.NameSpace(absPath).Items().Item(FileName)
  If sFolderItem Is Nothing Then
    WScript.Echo Path,"- Not Found."
    Exit For
  End If
  Set zFolderItem=zFolder.ParseName(FileName)
  If Not zFolderItem Is Nothing Then
    If IsEmpty(tFolderName) Then
      tFolderName=fso.BuildPath(fso.GetParentFolderName(ZIPfile),fso.GetTempName())
      fso.CreateFolder tFolderName
      Set tFolder=Shell.NameSpace(tFolderName)
    End If
    Count=zFolder.Items().Count
'    tFolder.MoveHere zFolderItem
    zFolderItem.InvokeVerb "cut"
    tFolder.Self.InvokeVerb "paste"
    Do While zFolder.Items().Count=Count
'      WScript.Echo 2,zFolder.Items().Count,Count
      WScript.Sleep 100
    Loop
  End If
  Count=zFolder.Items().Count
  zFolder.CopyHere sFolderItem
  Do While zFolder.Items().Count=Count
'    WScript.Echo 3,zFolder.Items().Count,Count
    WScript.Sleep 100
  Loop
Next
Set tFolder=Nothing
Set Shell=Nothing
If Not IsEmpty(ie) Then ie.Quit
Do While TypeName(ie)="IWebBrowser2"
  WScript.Sleep 100
Loop
'MsgBox "IE ended."
If Not IsEmpty(tFolderName) Then
  fso.DeleteFolder tFolderName
End If
WScript.Quit
'</script>
'</job>
