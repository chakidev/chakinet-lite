REM @ECHO OFF
 REM fix installer failure on XP
 REM cf. http://stackoverflow.com/questions/23978677/dirca-checkfx-return-value-3-vs-2013-deployment-project

C:
PUSHD "C:\Program Files (x86)\Microsoft Visual Studio\2019\Professional\Common7\IDE\CommonExtensions\Microsoft\VSI\bin"
IF EXIST  dpca_updated.txt GOTO :end
REN dpca.dll _dpca.dll
COPY /Y "C:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\Tools\Deployment\dpca.dll" .
ECHO > dpca_updated.txt
:end
POPD
