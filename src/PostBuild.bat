cd %~DP0
cd ..\bin\Release
copy ..\..\src\Entity\Readers\ReaderDefs.xml .
copy ..\..\src\Entity\Readers\ReaderDefs.xsd .
copy ..\..\src\Entity\Readers\ReaderDefs.xml ..\Debug
copy ..\..\src\Entity\Readers\ReaderDefs.xsd ..\Debug
del ..\..\src\_current\temp.zip
..\..\src\ChaKi.net\MakeExZIP.VBS ..\..\src\_current\temp.zip ChaKi.NET.exe DepEditSLA.dll CreateCorpusSLA.exe ExportCorpus.exe TagSetDefinitionEditor.exe Timings.exe ChaKiService.dll ServiceInterface.dll ChaKiEntity.dll ChaKiCommon.dll MSSQLUserFunc.dll OpenFileFolderDialog.dll TextFormatter.exe AttachBib.exe ExportCorpus.exe ReaderDefs.xml ReaderDefs.xsd
copy ..\..\src\_current\temp.zip ..\..\src\_current\chakinet.zip
del ..\..\src\_current\temp.zip

del ..\..\src\_current\temp_x64.zip
..\..\src\ChaKi.net\MakeExZIP.VBS ..\..\src\_current\temp_x64.zip x64\OpenFileFolderDialog.dll "x64\System.Data.SQLite.dll"
copy ..\..\src\_current\temp_x64.zip ..\..\src\_current\x64.zip
del ..\..\src\_current\temp_x64.zip

cd ..\..\src
