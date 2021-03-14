set unity_editor_path=%~1
set workspace=%~2
set netreactor_exe=%~3

if not exist "%workspace%\Secured" mkdir "%workspace%\Secured"

"%netreactor_exe%" -file "%workspace%\WEAVR\Essential\WEAVR.Essential.Runtime.dll" -satellite_assemblies "%workspace%\WEAVR\Essential\WEAVR.Essential.Editor.dll;%workspace%\WEAVR\Essential\Runtime\Core\Reflection\WEAVR.Essential.Runtime.Reflection.dll;%workspace%\WEAVR\Essential\Runtime\Core\Reflection\WEAVR.Essential.Runtime.ReflectionAOT.dll;%workspace%\WEAVR\Networking\WEAVR.Multiplayer.Runtime.dll;%workspace%\WEAVR\Cockpit\WEAVR.Cockpit.Runtime.dll;%workspace%\WEAVR\Cockpit\WEAVR.Cockpit.Editor.dll;%workspace%\WEAVR\Maintenance\WEAVR.Maintenance.Runtime.dll;%workspace%\WEAVR\Maintenance\WEAVR.Maintenance.Editor.dll;%workspace%\WEAVR\Simulation\WEAVR.Simulation.Runtime.dll;%workspace%\WEAVR\Simulation\WEAVR.Simulation.Editor.dll;%workspace%\WEAVR\WEAVR.Creator.dll;%workspace%\WEAVR\WEAVR.Packaging.dll;%unity_editor_path%\Data\Managed\UnityEngine.dll;%unity_editor_path%\Data\Managed\UnityEngine\UnityEngine.CoreModule.dll" -targetfile "%workspace%\Secured\<AssemblyFileName>" -mono 1 -antitamp 1 -anti_debug 1 -necrobit 1 -obfuscation 0 -stringencryption 1 -suppressildasm 1 -naming incremental -exclude_enums 1 -exclude_properties 1 -exclude_fields 1 -exclude_methods 1 -exclude_events 1 -exclude_types 1 -exclude_compiler_types 1 -exclude_serializable_types 1 -mapping_file 1
                
echo "Copying Encrypted Content"
move /Y "%workspace%\Secured\WEAVR.Essential.Runtime.dll" "%workspace%\WEAVR\Essential\WEAVR.Essential.Runtime.dll"
move /Y "%workspace%\Secured\WEAVR.Essential.Editor.dll" "%workspace%\WEAVR\Essential\WEAVR.Essential.Editor.dll"
move /Y "%workspace%\Secured\WEAVR.Essential.Runtime.Reflection.dll" "%workspace%\WEAVR\Essential\Runtime\Core\Reflection\WEAVR.Essential.Runtime.Reflection.dll"
move /Y "%workspace%\Secured\WEAVR.Essential.Runtime.ReflectionAOT.dll" "%workspace%\WEAVR\Essential\Runtime\Core\Reflection\WEAVR.Essential.Runtime.ReflectionAOT.dll"
move /Y "%workspace%\Secured\WEAVR.Multiplayer.Runtime.dll" "%workspace%\WEAVR\Networking\WEAVR.Multiplayer.Runtime.dll"
move /Y "%workspace%\Secured\WEAVR.Cockpit.Runtime.dll" "%workspace%\WEAVR\Cockpit\WEAVR.Cockpit.Runtime.dll"
move /Y "%workspace%\Secured\WEAVR.Cockpit.Editor.dll" "%workspace%\WEAVR\Cockpit\WEAVR.Cockpit.Editor.dll"
move /Y "%workspace%\Secured\WEAVR.Maintenance.Runtime.dll" "%workspace%\WEAVR\Maintenance\WEAVR.Maintenance.Runtime.dll"
move /Y "%workspace%\Secured\WEAVR.Maintenance.Editor.dll" "%workspace%\WEAVR\Maintenance\WEAVR.Maintenance.Editor.dll"
move /Y "%workspace%\Secured\WEAVR.Simulation.Runtime.dll" "%workspace%\WEAVR\Simulation\WEAVR.Simulation.Runtime.dll"
move /Y "%workspace%\Secured\WEAVR.Simulation.Editor.dll" "%workspace%\WEAVR\Simulation\WEAVR.Simulation.Editor.dll"
move /Y "%workspace%\Secured\WEAVR.Creator.dll" "%workspace%\WEAVR\WEAVR.Creator.dll"
move /Y "%workspace%\Secured\WEAVR.Packaging.dll" "%workspace%\WEAVR\WEAVR.Packaging.dll"