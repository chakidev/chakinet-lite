Option Explicit

Class AssemblyInfo
	Public Name
	Public Guid
	Public FilePath
End Class

Dim AssemblyList(15)
Dim objRegExp, objFSO, objStream, wshShell, objSvnCommand
Dim versionStr, lastVersionStr
Dim info
Const ForReading = 1, ForWriting = 2, ForAppending = 8

'=============================================================
Const CompanyStr = "NAIST/Sowa Research Co.,Ltd."
Const CopyrightStr = "(C) 2011-2021"
Const CurrentVersion = "3.16"
'=============================================================
Set AssemblyList(0) = New AssemblyInfo
With AssemblyList(0)
  .Name = "ChaKi"
  .Guid = "d1df69eb-c99c-402d-926d-692b04246138"
  .FilePath = ".\ChaKi.NET\Properties\AssemblyInfo.cs"
End With
Set AssemblyList(1) = New AssemblyInfo
With AssemblyList(1)
  .Name = "ChaKiCommon"
  .Guid = "9d33440e-e7f2-42c7-8378-9f0be35331bf"
  .FilePath = ".\ChaKiCommon\Properties\AssemblyInfo.cs"
End With
Set AssemblyList(2) = New AssemblyInfo
With AssemblyList(2)
  .Name = "CreateCorpusSLA"
  .Guid = "fb51b563-a39e-4573-be80-e9b6770b7192"
  .FilePath = ".\CreateCorpusSLA\Properties\AssemblyInfo.cs"
End With
Set AssemblyList(3) = New AssemblyInfo
With AssemblyList(3)
  .Name = "DepEditSLA"
  .Guid = "091cab4f-1bf6-4b9c-b72c-7ad7f0ee336f"
  .FilePath = ".\DepEditSLA\Properties\AssemblyInfo.cs"
End With
Set AssemblyList(4) = New AssemblyInfo
With AssemblyList(4)
  .Name = "ChaKiEntity"
  .Guid = "36aa90b2-bbfc-40ad-aed1-022ebfd57ec4"
  .FilePath = ".\Entity\Properties\AssemblyInfo.cs"
End With
Set AssemblyList(5) = New AssemblyInfo
With AssemblyList(5)
  .Name = "MSSQLUserFunc"
  .Guid = "30c4bace-8545-413a-ac89-8aba73011a20"
  .FilePath = ".\MSSQLUserFunc\Properties\AssemblyInfo.cs"
End With
Set AssemblyList(6) = New AssemblyInfo
With AssemblyList(6)
  .Name = "ChaKiService"
  .Guid = "096948c7-c72d-4586-8efe-3aa247b7b3aa"
  .FilePath = ".\Service\Properties\AssemblyInfo.cs"
End With
Set AssemblyList(7) = New AssemblyInfo
With AssemblyList(7)
  .Name = "ChaKiServiceInterface"
  .Guid = "11c1dac2-36cb-4ff8-aff2-09a5a72d7841"
  .FilePath = ".\ServiceInterface\Properties\AssemblyInfo.cs"
End With
Set AssemblyList(8) = New AssemblyInfo
With AssemblyList(8)
  .Name = "TextFormatter"
  .Guid = "f3b146a9-36b0-4951-bc76-3795e4cb80d5"
  .FilePath = ".\TextFormatter\Properties\AssemblyInfo.cs"
End With
Set AssemblyList(9) = New AssemblyInfo
With AssemblyList(9)
  .Name = "AttachBib"
  .Guid = "a3543add-0b99-4367-a32d-0d733e79fd93"
  .FilePath = ".\AttachBib\Properties\AssemblyInfo.cs"
End With
Set AssemblyList(10) = New AssemblyInfo
With AssemblyList(10)
  .Name = "ExportCorpus"
  .Guid = "e191a153-16c4-4110-8597-9d0ca10a0551"
  .FilePath = ".\ExportCorpus\Properties\AssemblyInfo.cs"
End With
Set AssemblyList(11) = New AssemblyInfo
With AssemblyList(11)
  .Name = "NativeCabochaTypes"
  .Guid = "9d902716-21e0-4bd3-8780-8c624ccd14fc"
  .FilePath = ".\NativeCabochaTypes\Properties\AssemblyInfo.cs"
End With
Set AssemblyList(12) = New AssemblyInfo
With AssemblyList(12)
  .Name = "DocEdit"
  .Guid = "a0c4cd24-d824-4d6e-9165-a8eb65d15784"
  .FilePath = ".\DocEdit\Properties\AssemblyInfo.cs"
End With
Set AssemblyList(13) = New AssemblyInfo
With AssemblyList(13)
  .Name = "Timings"
  .Guid = "ff6652ed-b932-466b-944b-ce88d698979b"
  .FilePath = ".\Timings\Properties\AssemblyInfo.cs"
End With
Set AssemblyList(14) = New AssemblyInfo
With AssemblyList(14)
  .Name = "Text2Corpus"
  .Guid = "a8cf8403-eb88-418f-bf54-56aeaef39268"
  .FilePath = ".\Text2Corpus\Properties\AssemblyInfo.cs"
End With
Set AssemblyList(15) = New AssemblyInfo
With AssemblyList(15)
  .Name = "ImportWordRelation"
  .Guid = "6a95808a-d1e3-47de-bb62-7ed7a281ac0b"
  .FilePath = ".\ImportWordRelation\Properties\AssemblyInfo.cs"
End With
'=============================================================
'MsgBox "CreateAssemblyInfo.vbs"
' SubversionÇ©ÇÁHEAD Revisionî‘çÜÇìæÇÈ
Set wshShell = CreateObject("WScript.Shell")
Set objSvnCommand = wshShell.Exec("svnversion ..\..\ChaKi.NET\src")
versionStr = objSvnCommand.StdOut.ReadAll
Set objRegExp = new regexp
objRegExp.Pattern = "(\d+:)?(\d+)M?"
objRegExp.Global = True
versionStr = Replace(objRegExp.Replace(versionStr,"$2"), vbNewLine,"")

On Error Resume Next
Set objFSO = WScript.CreateObject("Scripting.FileSystemObject")
Set objStream = objFSO.OpenTextFile("LastVer.txt", ForReading, True)
lastVersionStr = objStream.ReadLine()
objStream.Close

If true Then
	MsgBox "Updating AssemblyInfo's : " + versionStr
	' AssemblyInfo.csÇê∂ê¨
	For Each info in AssemblyList
	  Set objStream = objFSO.OpenTextFile(info.FilePath, ForWriting, True)
	  
	  objStream.WriteLine "// Created  automatically by CreateAssemblyInfo.vbs."
	  objStream.WriteLine "using System.Reflection;"
	  objStream.WriteLine "using System.Runtime.CompilerServices;"
	  objStream.WriteLine "using System.Runtime.InteropServices;"
	  objStream.WriteLine "[assembly: AssemblyTitle(""" + info.Name + """)]"
	  objStream.WriteLine "[assembly: AssemblyDescription("""")]"
	  objStream.WriteLine "[assembly: AssemblyConfiguration("""")]"
	  objStream.WriteLine "[assembly: AssemblyCompany(""" + CompanyStr + """)]"
	  objStream.WriteLine "[assembly: AssemblyProduct(""" + info.Name + """)]"
	  objStream.WriteLine "[assembly: AssemblyCopyright(""" + CopyrightStr + """)]"
	  objStream.WriteLine "[assembly: AssemblyTrademark("""")]"
	  objStream.WriteLine "[assembly: AssemblyCulture("""")]"
	  objStream.WriteLine "[assembly: ComVisible(false)]"
	  objStream.WriteLine "[assembly: Guid(""" + info.Guid + """)]"
	  objStream.WriteLine "[assembly: AssemblyVersion(""" + CurrentVersion + "." + versionStr + ".0"")]"
	  objStream.WriteLine "[assembly: AssemblyFileVersion(""" + CurrentVersion + "." + versionStr + ".0"")]"
	  If info.Name = "ChaKiService" Then
	    objStream.WriteLine "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""ServiceTest"")]"
	  End If
	  
	  objStream.Close
	Next
	Set objStream = objFSO.OpenTextFile("LastVer.txt", ForWriting, True)
	objStream.WriteLine versionStr
	objStream.Close

End If
