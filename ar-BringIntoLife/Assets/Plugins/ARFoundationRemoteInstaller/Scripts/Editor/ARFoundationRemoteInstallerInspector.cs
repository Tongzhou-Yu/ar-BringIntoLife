#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;


namespace ARFoundationRemote.Editor {
    [CustomEditor(typeof(ARFoundationRemoteInstaller), true), CanEditMultipleObjects]
    public class ARFoundationRemoteInstallerInspector : UnityEditor.Editor {
        public override void OnInspectorGUI() {
            DrawDefaultInspector();
            showMethodsInInspector(targets);
        }

        static void showMethodsInInspector(params Object[] targets) {
            var target = targets.First() as ARFoundationRemoteInstaller;
            Assert.IsNotNull(target);
            
            #if AR_FOUNDATION_REMOTE_INSTALLED
            GUILayout.Space(16);
            GUILayout.Label("AR Companion app", EditorStyles.boldLabel);
            if (GUILayout.Button("Install AR Companion app", new GUIStyle(GUI.skin.button) {fontStyle = FontStyle.Bold})) {
                execute(() => CompanionAppInstaller.BuildAndRun(target.optionalCompanionAppExtension));
            }
            if (GUILayout.Button("Build AR Companion and show in folder", new GUIStyle(GUI.skin.button))) {
                execute(() => CompanionAppInstaller.Build(target.optionalCompanionAppExtension));
            }
            if (GUILayout.Button("Open Plugin Settings")) {
                Selection.activeObject = ARFoundationRemote.Runtime.Settings.Instance;
            }
            if (GUILayout.Button("Delete AR Companion app build folder")) {
                execute(CompanionAppInstaller.DeleteCompanionAppBuildFolder);
            }
            #endif

            GUILayout.Space(16);
            GUILayout.Label(ARFoundationRemoteInstaller.pluginName, EditorStyles.boldLabel);
            if (GUILayout.Button("Install or Update Plugin")) {
                execute(ARFoundationRemoteInstaller.InstallPlugin);
            }
            #if AR_FOUNDATION_REMOTE_INSTALLED
            if (GUILayout.Button("Apply fixes")) {
                FixesForEditorSupport.Apply();
            }
            if (GUILayout.Button("Un-install Plugin", new GUIStyle(GUI.skin.button) {normal = {textColor = Color.red}})) {
                execute(ARFoundationRemoteInstaller.UnInstallPlugin);
            }
            #endif
        }

        static void execute(Action action) {
            action();
        }
    }
}
#endif // UNITY_EDITOR
