using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Palsoul.Editor
{
    /// <summary>Runs the integration suite from the Editor menu or a local validation request.</summary>
    [InitializeOnLoad]
    public static class PrototypeValidation
    {
        private const string Request = "Temp/palsoul-validation.request";
        private static TestRunnerApi runner;
        private static bool running;
        static PrototypeValidation() { EditorApplication.update += Poll; }
        private static void Poll()
        {
            if (!File.Exists(Request) || running || EditorApplication.isCompiling
                || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            string request = File.ReadAllText(Request).Trim();
            File.Delete(Request);
            Directory.CreateDirectory("Logs");
            var scene = EditorSceneManager.GetActiveScene();
            File.WriteAllText("Logs/editor-status.txt", $"Scene: {scene.path}\nDirty: {scene.isDirty}\nRequest: {request}\n");
            if (request == "status") return;
            if (scene.isDirty)
            {
                File.AppendAllText("Logs/editor-status.txt", "Validation deferred: save the current scene first.\n");
                return;
            }
            Run();
        }
        [MenuItem("Palsoul/Validate Prototype")]
        public static void Run()
        {
            PrototypeBuilder.Build();
            runner = ScriptableObject.CreateInstance<TestRunnerApi>();
            runner.RegisterCallbacks(new Results());
            running = true;
            runner.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode, assemblyNames = new[] { "Palsoul.EditModeTests" } }));
        }
        private class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                Directory.CreateDirectory("Logs");
                TestRunnerApi.SaveResultToFile(result, "Logs/test-results.xml");
                File.WriteAllText("Logs/validation-summary.txt", $"Passed: {result.PassCount}\nFailed: {result.FailCount}\nSkipped: {result.SkipCount}\n");
                running = false;
            }
        }
    }
}
