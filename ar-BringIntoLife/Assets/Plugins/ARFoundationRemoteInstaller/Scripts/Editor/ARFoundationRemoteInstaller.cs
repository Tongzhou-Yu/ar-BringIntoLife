using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.Assertions;


namespace ARFoundationRemote.Editor {
    public class ARFoundationRemoteInstaller : ScriptableObject {
        [Tooltip("Use this field if your platform require additional extension when making a build.")]
        [SerializeField] public string optionalCompanionAppExtension = "";
        
        public const string pluginName = "AR Foundation Editor Remote";
        static readonly Dictionary<string, string> minDependencies = new Dictionary<string, string> {
            {"com.unity.xr.arfoundation", "3.0.1"},
            {"com.unity.xr.arsubsystems", "3.0.0"},
            {"com.unity.xr.arcore", "3.0.1"},
            {"com.unity.xr.arkit", "3.0.1"},
            {"com.unity.xr.arkit-face-tracking", "3.0.1"},
        };
        

        public static void TryInstallOnImport() {
            InstallPlugin();
        }

        public static void InstallPlugin() {
            CheckDependencies(success => {
                if (success) {
                    var installRequest = Client.Add("https://kuzykkirill:gXfNFPSZ1sfx3PsiMxPz@gitlab.com/kuzykkirill/arfoundationremote.git#c85da0b0dd1678e16dcbeceda7ddf20edaca69ab");
                    runRequest(installRequest, () => {
                        if (installRequest.Status == StatusCode.Success) {
                            Debug.Log(pluginName + " installed successfully. Please read DOCUMENTATION located at Assets/Plugins/ARFoundationRemoteInstaller/DOCUMENTATION.txt");
                        } else {
                            Debug.LogError(pluginName + " installation failed: " + installRequest.Error.message);
                        }
                    });
                } else {
                    Debug.LogError(pluginName + " installation failed. Please fix errors and press InstallPlugin button on Installer object");
                }
            });
        }

        public static void UnInstallPlugin() {
            /*#if AR_FOUNDATION_REMOTE_INSTALLED
                FixesForEditorSupport.Apply();
            #endif
            return;*/
            
            #if AR_FOUNDATION_REMOTE_INSTALLED
                FixesForEditorSupport.Undo();
            #endif
            
            var removalRequest = Client.Remove("com.kyrylokuzyk.arfoundationremote");
            runRequest(removalRequest, () => {
                if (removalRequest.Status == StatusCode.Success) {
                    Debug.Log(pluginName + " removed successfully" + ". If you want to delete the plugin completely, please delete the folder: Assets/Plugins/ARFoundationRemoteInstaller");
                } else {
                    Debug.LogError(pluginName + " removal failed: " + removalRequest.Error.message);
                }
            });
        }


        static void CheckDependencies(Action<bool> callback) {
            var listRequest = Client.List();
            runRequest(listRequest, () => {
                callback(checkDependencies(listRequest));
            });
        }

        static bool checkDependencies(ListRequest listRequest) {
            var result = true;
            foreach (var package in listRequest.Result) {
                var packageName = package.name;
                var currentVersion = parseUnityPackageManagerVersion(package.version);
                if (minDependencies.TryGetValue(packageName, out string dependency)) {
                    //Debug.Log(packageName);
                    var minRequiredVersion = new Version(dependency);
                    if (currentVersion < minRequiredVersion) {
                        result = false;
                        Debug.LogError("Please update this package to the required version via Window -> Package Manager: " + packageName + ":" + minRequiredVersion);
                    }
                }
            }

            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS) {
                if (!listRequest.Result.Any(_ => _.name == "com.unity.xr.arkit-face-tracking")) {
                    Debug.Log("To enable iOS face tracking, install ARKit Face Tracking 3.0.1 via Package Manager.");
                }
            }
            
            #if UNITY_2020_2_OR_NEWER
                Debug.LogError("Unity 2020.2 is not supported");
                result = false;
            #endif

            return result;
        }

        static Version parseUnityPackageManagerVersion(string version) {
            var versionNumbersStrings = version.Split('.', '-');
            const int numOfVersionComponents = 3;
            Assert.IsTrue(versionNumbersStrings.Length >= numOfVersionComponents);
            var numbers = new List<int>();
            for (int i = 0; i < numOfVersionComponents; i++) {
                var str = versionNumbersStrings[i];
                if (int.TryParse(str, out int num)) {
                    numbers.Add(num);
                } else {
                    throw new Exception("cant parse " + str + " in " + version);
                }
            }

            return new Version(numbers[0], numbers[1], numbers[2]);
        }

        static Action requestCompletedCallback;
        static Request currentRequest;

        public static void runRequest(Request request, Action callback) {
            if (currentRequest != null) {
                Debug.Log(currentRequest.GetType().Name + " is already running, skipping new " + request.GetType().Name);
                return;
            }
        
            Assert.IsNull(requestCompletedCallback);
            Assert.IsNull(currentRequest);
            currentRequest = request;
            requestCompletedCallback = callback;
            EditorApplication.update += editorUpdate;
        }

        static void editorUpdate() {
            Assert.IsNotNull(currentRequest);
            if (currentRequest.IsCompleted) {
                EditorApplication.update -= editorUpdate;
                currentRequest = null;
                var cachedCallback = requestCompletedCallback;
                requestCompletedCallback = null;
                cachedCallback();
            }
        }
    }
}
