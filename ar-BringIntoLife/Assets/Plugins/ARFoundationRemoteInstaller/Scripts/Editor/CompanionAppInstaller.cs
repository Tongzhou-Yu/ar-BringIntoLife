using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.PackageManager;
using UnityEngine;


namespace ARFoundationRemote.Editor {
    [UsedImplicitly]
    public class CompanionAppInstaller: IPreprocessBuildWithReport, IPostprocessBuildWithReport {
        const string appName = "ARCompanion";
        
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) {
            if (isBuildingCompanionApp(report)) {
                applicationIdentifier = removeAppName(applicationIdentifier) + appName;
                PlayerSettings.productName = appName + removeAppName(PlayerSettings.productName);
                toggleDefine("AR_COMPANION", true);
            }
        }

        static string applicationIdentifier {
            get => PlayerSettings.GetApplicationIdentifier(activeBuildTargetGroup);
            set => PlayerSettings.SetApplicationIdentifier(activeBuildTargetGroup, value);
        }
        
        public void OnPostprocessBuild(BuildReport report) {
            if (isBuildingCompanionApp(report)) {
                PlayerSettings.productName = removeAppName(PlayerSettings.productName);
                applicationIdentifier = removeAppName(applicationIdentifier);
                toggleDefine("AR_COMPANION", false);

                if (report.summary.totalErrors == 0) {
                    Debug.Log(appName + " build succeeded.");
                }
            }
        }

        static void toggleDefine(string define, bool enable) {
            var buildTargetGroup = activeBuildTargetGroup;
            if (enable) {
                if (isDefineSet(define)) {
                    return;
                }

                var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, $"{defines};{define}");
            } else {
                if (!isDefineSet(define)) {
                    return;
                }

                var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup).Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
                PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, string.Join(";", defines.Where(d => d.Trim() != define).ToArray()));
            }
        }

        static BuildTargetGroup activeBuildTargetGroup => EditorUserBuildSettings.activeBuildTarget.ToBuildTargetGroup();

        static bool isDefineSet(string define) {
            return PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.activeBuildTarget.ToBuildTargetGroup()).Contains(define);
        }

        static bool isBuildingCompanionApp(BuildReport report) {
            var result = report.summary.outputPath.Contains(companionAppFolder);
            //Debug.Log("isBuildingCompanionApp " + result);
            return result;
        }

        static string removeAppName(string s) {
            return s.Replace(appName, "");
        }

        public static void Build(string optionalCompanionAppExtension) {
            build(buildOptions | BuildOptions.ShowBuiltPlayer, optionalCompanionAppExtension);
        }

        public static void BuildAndRun(string optionalCompanionAppExtension) {
            build(buildOptions | BuildOptions.AutoRunPlayer, optionalCompanionAppExtension);
        }

        static void build(BuildOptions options, string optionalCompanionAppExtension) {
            var listRequest = Client.List(true);
            ARFoundationRemoteInstaller.runRequest(listRequest, () => {
                if (listRequest.Status != StatusCode.Success) {
                    Debug.LogError("ARFoundationRemoteInstaller can't check installed packages.");
                    return;
                }

                if (!listRequest.Result.Any(_ => _.name == "com.kyrylokuzyk.arfoundationremote")) {
                    Debug.LogError("Please install " + ARFoundationRemoteInstaller.pluginName);
                    return;
                }
                
                var senderScenes = getSenderScenePaths().Select(_ => _.ToString()).ToArray();
                BuildPipeline.BuildPlayer(senderScenes, getInstallDirectory() + EditorUserBuildSettings.activeBuildTarget + getExtension(optionalCompanionAppExtension), EditorUserBuildSettings.activeBuildTarget, options);    
            });
        }

        static string getExtension(string optionalCompanionAppExtension) {
            switch (EditorUserBuildSettings.activeBuildTarget) {
                case BuildTarget.iOS:
                    return "";
                case BuildTarget.Android:
                    return ".apk";
                default:
                    if (string.IsNullOrEmpty(optionalCompanionAppExtension)) {
                        Debug.LogError("Please specify correct optionalCompanionAppExtension for your build target.");
                    } else {
                        Debug.Log("Using optionalCompanionAppExtension: " + optionalCompanionAppExtension);
                    }
                    return optionalCompanionAppExtension;
            }
        }

        /// Adding BuildOptions.AcceptExternalModificationsToPlayer will produce gradle build for android instead of apk
        static BuildOptions buildOptions {
            get {
                var result = BuildOptions.None;
                return result;
            }
        }

        static string getInstallDirectory() {
            return Directory.GetParent(Application.dataPath).FullName + "/" + companionAppFolder + "/";
        }

        static string companionAppFolder => "ARFoundationRemoteCompanionApp";

        static IEnumerable<FileInfo> getSenderScenePaths() {
            return new DirectoryInfo(Application.dataPath + "/Plugins/ARFoundationRemoteInstaller/Scenes/Senders")
                .GetFiles("*.unity");
        }

        public static void DeleteCompanionAppBuildFolder() {
            var path = getInstallDirectory();
            if (Directory.Exists(path)) {
                Directory.Delete(path, true);
            }
        }
    }
}
