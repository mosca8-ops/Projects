set UnityHubLocation=C:\Program Files\Unity\Hub
set UnityVersion=2018.4.4f1

set JOB_NAME=Jacopo
set WORKSPACE=C:\Data\Projects\WEAVR\ProjectZero

echo Attempting to build %JOB_NAME% in Unity Version %UnityVersion%.
echo Workspace location: '%WORKSPACE%''
echo Unity Hub location: '%UnityHubLocation%''

echo Build of Editor DLL Started

rem "%UnityHubLocation%\Editor\%UnityVersion%\Editor\Unity.exe" -projectPath "%WORKSPACE%" -quit -nographics -batchmode -logFile "%WORKSPACE%\Logs\UnityEditor.log" -executeMethod UnityBuild.BuildPlatforms
rem "%UnityHubLocation%\Editor\%UnityVersion%\Editor\Unity.exe" -projectPath "%WORKSPACE%" -quit -nographics -batchmode -logFile "%WORKSPACE%\Logs\UnityEditor.log" -executeMethod TXT.WEAVR.Builder.UnityBuild.BuildEditor
"%UnityHubLocation%\Editor\%UnityVersion%\Editor\Unity.exe" -projectPath "%WORKSPACE%" -quit -nographics -batchmode -logFile "%WORKSPACE%\Logs\UnityEditor.log" -executeMethod TXT.WEAVR.Builder.UnityBuild.BuildWindowsOPS

echo Build of Editor DLL Ended

echo Build finished, see attached log for information!

set /p DUMMY=Hit ENTER to continue...