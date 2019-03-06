#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class WorkAroundLeapMotionBuildBreakOnNetBackend : IPostprocessBuildWithReport
{
    int IOrderedCallback.callbackOrder => 0;

    void IPostprocessBuildWithReport.OnPostprocessBuild(BuildReport report)
    {
        OnPostprocessBuild(report.summary.outputPath);
    }

    //[MenuItem("TempTestBuildStep/WorkAroundLeapMotionBuildBreakOnNetBackend")] // Uncomment this attribute to temporarily run tests from the editor menu.
    private static void TempTest()
    {
        OnPostprocessBuild(Path.GetFullPath($"{Application.dataPath}/../Builds/01"));
    }

    private static void OnPostprocessBuild(string outputPath)
    {
        if (false
            || (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WSAPlayer)
            || (PlayerSettings.GetScriptingBackend(BuildTargetGroup.WSA) != ScriptingImplementation.WinRTDotNET)
            || (!EditorUserBuildSettings.wsaGenerateReferenceProjects)
            )
        {
            Debug.Log("WorkAroundLeapMotionBuildBreakOnNetBackend is being skipped, since it's not necessary for this build configuration.");
            return;
        }

        Debug.Log("Starting WorkAroundLeapMotionBuildBreakOnNetBackend...");

        var storeAppDirectoryPath = outputPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        RemoveLeapMotionFromSolution(storeAppDirectoryPath);
        RemoveLeapMotionFromProjects(storeAppDirectoryPath);
        RemoveLeapMotionFromAssemblyConverterArgs(storeAppDirectoryPath);
        MoveLeapMotionProjectDirectory(storeAppDirectoryPath);

        Debug.Log("WorkAroundLeapMotionBuildBreakOnNetBackend completed.");
    }

    private static void RemoveLeapMotionFromSolution(string storeAppDirectoryPath)
    {
        var solutionFilePath = Path.Combine(storeAppDirectoryPath, $"{PlayerSettings.productName}.sln");

        Debug.Log($"Commenting out LeapMotion project references in main solution file \"{solutionFilePath}\" ...");

        var solutionFileText = File.ReadAllText(solutionFilePath);
        var originalSolutionFileText = solutionFileText;


        // Find the project entry and determine the project guid. Example line:
        // 
        // Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "LeapMotion", "D:\e\AnalogInternalMrtk\Builds\01\GeneratedProjects\UWP\LeapMotion\LeapMotion.csproj", "{8a78583f-58a3-4318-9f87-6dab2cff7af8}"
        //

        var projectDefinitionPattern = @"(?i)Project\(""\{[0-9A-F-]{36}}""\) = ""LeapMotion"", ""[^""]+"", ""(?<projectGuid>\{[0-9A-F-]{36}})""";

        var projectGuidMatch = Regex.Match(solutionFileText, projectDefinitionPattern);
        var projectGuidGroup = projectGuidMatch.Groups["projectGuid"];

        if (!(projectGuidMatch.Success && projectGuidGroup.Success))
        {
            Debug.LogError($"Couldn't find project line to determine LeapMotion project guid in main solution file \"{solutionFilePath}\" .");
            return;
        }


        // Find and comment out any lines that aren't already commented out and contain the project guid. Example lines:
        //
        // 		{8a78583f-58a3-4318-9f87-6dab2cff7af8} = {8a78583f-58a3-4318-9f87-6dab2cff7af8}
        //
        // 		{8a78583f-58a3-4318-9f87-6dab2cff7af8}.Debug|x86.ActiveCfg = Debug|x86
        // 		{8a78583f-58a3-4318-9f87-6dab2cff7af8}.Debug|x86.Build.0 = Debug|x86
        // 		{8a78583f-58a3-4318-9f87-6dab2cff7af8}.Release|x86.ActiveCfg = Release|x86
        // 		{8a78583f-58a3-4318-9f87-6dab2cff7af8}.Release|x86.Build.0 = Release|x86
        // 		{8a78583f-58a3-4318-9f87-6dab2cff7af8}.Master|x86.ActiveCfg = Master|x86
        // 		{8a78583f-58a3-4318-9f87-6dab2cff7af8}.Master|x86.Build.0 = Master|x86
        // 		{8a78583f-58a3-4318-9f87-6dab2cff7af8}.Debug|x64.ActiveCfg = Debug|x64
        // 		{8a78583f-58a3-4318-9f87-6dab2cff7af8}.Debug|x64.Build.0 = Debug|x64
        // 		{8a78583f-58a3-4318-9f87-6dab2cff7af8}.Release|x64.ActiveCfg = Release|x64
        // 		{8a78583f-58a3-4318-9f87-6dab2cff7af8}.Release|x64.Build.0 = Release|x64
        // 		{8a78583f-58a3-4318-9f87-6dab2cff7af8}.Master|x64.ActiveCfg = Master|x64
        // 		{8a78583f-58a3-4318-9f87-6dab2cff7af8}.Master|x64.Build.0 = Master|x64
        // 		{8a78583f-58a3-4318-9f87-6dab2cff7af8}.Debug|ARM.ActiveCfg = Debug|ARM
        // 		{8a78583f-58a3-4318-9f87-6dab2cff7af8}.Debug|ARM.Build.0 = Debug|ARM
        // 		{8a78583f-58a3-4318-9f87-6dab2cff7af8}.Release|ARM.ActiveCfg = Release|ARM
        // 		{8a78583f-58a3-4318-9f87-6dab2cff7af8}.Release|ARM.Build.0 = Release|ARM
        // 		{8a78583f-58a3-4318-9f87-6dab2cff7af8}.Master|ARM.ActiveCfg = Master|ARM
        // 		{8a78583f-58a3-4318-9f87-6dab2cff7af8}.Master|ARM.Build.0 = Master|ARM
        //

        solutionFileText = Regex.Replace(
            solutionFileText,
            $@"(?im)^([^#].*{Regex.Escape(projectGuidGroup.Value)})",
            "# Automatically commented out to work around build break - $1"
            );


        // Find and comment out the "EndProject" tag that immediately follows the project definition line. Example lines:
        //
        // Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "LeapMotion", "D:\e\AnalogInternalMrtk\Builds\01\GeneratedProjects\UWP\LeapMotion\LeapMotion.csproj", "{8a78583f-58a3-4318-9f87-6dab2cff7af8}"
        // EndProject
        //

        solutionFileText = Regex.Replace(
            solutionFileText,
            $@"(?i)({projectDefinitionPattern}.*\r?\n)(EndProject)",
            "$1# Automatically commented out to work around build break - $2"
            );


        // Write the text back out:

        if (solutionFileText == originalSolutionFileText)
        {
            Debug.Log($"No need to comment out LeapMotion project references, since it's already done in main solution file \"{solutionFilePath}\" .");
        }
        else
        {
            File.WriteAllText(solutionFilePath, solutionFileText);
            Debug.Log($"Successfully commented out LeapMotion project references in main solution file \"{solutionFilePath}\" .");
        }
    }

    private static void RemoveLeapMotionFromProjects(string storeAppDirectoryPath)
    {
        var generatedProjectsBaseDirectoryPath = Path.Combine(storeAppDirectoryPath, "GeneratedProjects", "UWP");

        var mainProjectFilePath = Path.Combine(storeAppDirectoryPath, PlayerSettings.productName, $"{PlayerSettings.productName}.csproj");
        var automaticAssemblyProjectFilePath = Path.Combine(generatedProjectsBaseDirectoryPath, "Assembly-CSharp", "Assembly-CSharp.csproj");

        var referencersOfLeapMotionProjectFilePaths = new List<string>
        {
            mainProjectFilePath,
            Path.Combine(generatedProjectsBaseDirectoryPath, "Microsoft.MixedReality.Toolkit", "Microsoft.MixedReality.Toolkit.csproj"),
            Path.Combine(generatedProjectsBaseDirectoryPath, "Microsoft.MixedReality.Toolkit.Services.InputSystem", "Microsoft.MixedReality.Toolkit.Services.InputSystem.csproj"),
        };

        if (File.Exists(automaticAssemblyProjectFilePath))
        {
            referencersOfLeapMotionProjectFilePaths.Add(automaticAssemblyProjectFilePath);
        }


        foreach (var projectFilePath in referencersOfLeapMotionProjectFilePaths)
        {
            Debug.Log($"Removing LeapMotion references from project file \"{projectFilePath}\" ...");

            var projectFileText = File.ReadAllText(projectFilePath);
            var originalProjectFileText = projectFileText;


            // Get rid of LeapMotion path in SerializationWeaver args:

            projectFileText = projectFileText.Replace(
                @"&quot;-additionalAssemblyPath=$(UnityWSASolutionDir)GeneratedProjects\UWP\LeapMotion\bin\$(PlatformName)\$(ConfigurationName)&quot;",
                string.Empty
                );


            // Comment out project reference.  Note that newlines often get messed up around this reference, so we have to be a bit forgiving on the match.  Example:
            //
            //     <ProjectReference Include="D:\e\AnalogInternalMrtk\Builds\01\GeneratedProjects\UWP\LeapMotion\LeapMotion.csproj">
            //       <Project>{8a78583f-58a3-4318-9f87-6dab2cff7af8}</Project>
            //       <Name>LeapMotion</Name>
            //     </ProjectReference>
            //

            projectFileText = Regex.Replace(
                projectFileText,
                @"(?is)(?<!<!--)<ProjectReference Include=""[^""]+\\LeapMotion\.csproj"">.*?</ProjectReference>",
                @"<!-- Automatically commented out to work around build break --><!--$0-->"
                );


            if (projectFilePath == mainProjectFilePath)
            {
                // Comment out Copy files.  Example:
                //
                //         <Copy SourceFiles="$(UnityWSASolutionDir)\GeneratedProjects\UWP\LeapMotion\bin\$(PlatformName)\$(ConfigurationName)\LeapMotion.dll" DestinationFiles="$(ProjectDir)LeapMotion.dll" />
                //         <Copy SourceFiles="$(UnityWSASolutionDir)\GeneratedProjects\UWP\LeapMotion\bin\$(PlatformName)\$(ConfigurationName)\LeapMotion.pdb" DestinationFiles="$(ProjectDir)LeapMotion.pdb" Condition="Exists('$(UnityWSASolutionDir)\GeneratedProjects\UWP\LeapMotion\bin\$(PlatformName)\$(ConfigurationName)\LeapMotion.pdb')" />
                //

                projectFileText = Regex.Replace(
                    projectFileText,
                    @"(?i)(?<!<!--)<Copy SourceFiles=""[^""]+\\LeapMotion\.(dll|pdb)""[^>]*/>",
                    @"<!-- Automatically commented out to work around build break --><!--$0-->"
                    );


                // Comment out AppxPackagePayload Remove lines and Include blocks.  Example:
                //
                //             <AppxPackagePayload Remove="@(AppxPackagePayload)" Condition="'%(TargetPath)' == 'LeapMotion.dll'" />
                //             <AppxPackagePayload Include="$(ProjectDir)LeapMotion.dll">
                //                 <TargetPath>LeapMotion.dll</TargetPath>
                //             </AppxPackagePayload>
                //             <AppxPackagePayload Remove="@(AppxPackagePayload)" Condition="'%(TargetPath)' == 'LeapMotion.pdb'" />
                //             <AppxPackagePayload Include="$(ProjectDir)LeapMotion.pdb">
                //                 <TargetPath>LeapMotion.pdb</TargetPath>
                //             </AppxPackagePayload>
                //

                projectFileText = Regex.Replace(
                    projectFileText,
                    @"(?i)(?<!<!--)<AppxPackagePayload Remove=[^>]*'LeapMotion\.(dll|pdb)'[^>]*/>",
                    @"<!-- Automatically commented out to work around build break --><!--$0-->"
                    );

                projectFileText = Regex.Replace(
                    projectFileText,
                    @"(?is)(?<!<!--)<AppxPackagePayload Include=""\$\(ProjectDir\)LeapMotion\.(dll|pdb)"">.*?</AppxPackagePayload>",
                    @"<!-- Automatically commented out to work around build break --><!--$0-->"
                    );
            }


            // Write the text back out:

            if (projectFileText == originalProjectFileText)
            {
                Debug.Log($"No need to remove LeapMotion references, since it's already done in project file \"{projectFilePath}\" .");
            }
            else
            {
                File.WriteAllText(projectFilePath, projectFileText);
                Debug.Log($"Successfully removed LeapMotion references from project file \"{projectFilePath}\" .");
            }
        }
    }

    private static void RemoveLeapMotionFromAssemblyConverterArgs(string storeAppDirectoryPath)
    {
        var assemblyConverterArgsFilePath = Path.Combine(storeAppDirectoryPath, PlayerSettings.productName, "AssemblyConverterArgs.txt");

        Debug.Log($"Removing LeapMotion from assembly converter args file \"{assemblyConverterArgsFilePath}\" ...");

        var assemblyConverterArgsFileText = File.ReadAllText(assemblyConverterArgsFilePath);
        var originalAssemblyConverterArgsFileText = assemblyConverterArgsFileText;


        // Replace the "LeapMotion" line if it's there:

        assemblyConverterArgsFileText = Regex.Replace(
            assemblyConverterArgsFileText,
            @"(?i)\bLeapMotion\.dll(\r?\n)?",
            string.Empty
            );


        // Write the text back out:

        if (assemblyConverterArgsFileText == originalAssemblyConverterArgsFileText)
        {
            Debug.Log($"No need to remove LeapMotion, since it's already done in assembly converter args file \"{assemblyConverterArgsFilePath}\" .");
        }
        else
        {
            File.WriteAllText(assemblyConverterArgsFilePath, assemblyConverterArgsFileText);
            Debug.Log($"Successfully removed LeapMotion from assembly converter args file \"{assemblyConverterArgsFilePath}\" .");
        }
    }

    private static void MoveLeapMotionProjectDirectory(string storeAppDirectoryPath)
    {
        var originalLeapMotionProjectDirectoryPath = Path.Combine(storeAppDirectoryPath, "GeneratedProjects", "UWP", "LeapMotion");
        var targetLeapMotionProjectDirectoryPath = $"{originalLeapMotionProjectDirectoryPath}.movedForBuildBreakWorkaround";

        Debug.Log($"Moving LeapMotion project directory (to work around build break) from \"{originalLeapMotionProjectDirectoryPath}\" to \"{targetLeapMotionProjectDirectoryPath}\" ...");

        if (Directory.Exists(originalLeapMotionProjectDirectoryPath))
        {
            Directory.Move(originalLeapMotionProjectDirectoryPath, targetLeapMotionProjectDirectoryPath);

            Debug.Log($"Successfully moved LeapMotion project directory (to work around build break) from \"{originalLeapMotionProjectDirectoryPath}\" to \"{targetLeapMotionProjectDirectoryPath}\" .");
        }
        else if (Directory.Exists(targetLeapMotionProjectDirectoryPath))
        {
            Debug.Log($"No need to move LeapMotion project directory (to work around build break). It has already been moved to \"{targetLeapMotionProjectDirectoryPath}\" .");
        }
        else
        {
            Debug.LogError($"Please investigate! The LeapMotion project directory was not found at \"{originalLeapMotionProjectDirectoryPath}\" or \"{targetLeapMotionProjectDirectoryPath}\" . This"
                + $" postprocess step may no longer be necessary and should be removed, or it may be failing to do its job."
                );
        }
    }
}

#endif
