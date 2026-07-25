using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.TestTools;

namespace SpiderProjection.Editor
{
    [InitializeOnLoad]
    public static class SpiderProjectionTestRunner
    {
        private const string EditModeAssembly = "SpiderProjection.Tests.EditMode";
        private const string PlayModeAssembly = "SpiderProjection.Tests.PlayMode";

        static SpiderProjectionTestRunner()
        {
            TestRunnerApi.RegisterTestCallback(new ResultCallbacks(), 100);
        }

        [MenuItem("Tools/Spider Projection/Phase 4 - Run EditMode Tests")]
        public static void RunEditMode()
        {
            Run(TestMode.EditMode, EditModeAssembly, true);
        }

        [MenuItem("Tools/Spider Projection/Phase 4 - Run PlayMode Tests")]
        public static void RunPlayMode()
        {
            Run(TestMode.PlayMode, PlayModeAssembly, false);
        }

        private static void Run(TestMode mode, string assemblyName, bool synchronously)
        {
            SessionState.SetString(ResultKey(mode), "REQUESTED");
            SessionState.EraseString(FailureKey(mode));
            TestRunnerApi api = ScriptableObject.CreateInstance<TestRunnerApi>();
            ExecutionSettings settings = new ExecutionSettings(new Filter
            {
                testMode = mode,
                assemblyNames = new[] { assemblyName }
            })
            {
                runSynchronously = synchronously
            };
            string runId = api.Execute(settings);
            Debug.Log($"[SpiderProjectionTests] RUN_REQUESTED mode={mode} assembly={assemblyName} id={runId}");
        }

        public static string GetResult(TestMode mode)
        {
            string summary = SessionState.GetString(ResultKey(mode), "NOT_RUN");
            string failures = SessionState.GetString(FailureKey(mode), string.Empty);
            return string.IsNullOrEmpty(failures) ? summary : summary + "\n" + failures;
        }

        private static string ResultKey(TestMode mode)
        {
            return "SpiderProjection.Tests." + mode + ".Result";
        }

        private static string FailureKey(TestMode mode)
        {
            return "SpiderProjection.Tests." + mode + ".Failures";
        }

        private sealed class ResultCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun)
            {
                SessionState.SetString(
                    ResultKey(testsToRun.TestMode),
                    $"RUNNING tests={testsToRun.TestCaseCount}");
                Debug.Log(
                    $"[SpiderProjectionTests] RUN_STARTED mode={testsToRun.TestMode} tests={testsToRun.TestCaseCount}");
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                string summary =
                    $"RUN_COMPLETE mode={result.Test.TestMode} " +
                    $"passed={result.PassCount} failed={result.FailCount} " +
                    $"skipped={result.SkipCount} inconclusive={result.InconclusiveCount} " +
                    $"duration={result.Duration:F3}s";
                SessionState.SetString(ResultKey(result.Test.TestMode), summary);
                Debug.Log("[SpiderProjectionTests] " + summary);
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (!result.HasChildren && result.FailCount > 0)
                {
                    string failure =
                        $"TEST_FAILED name={result.FullName}\n{result.Message}\n{result.StackTrace}";
                    string key = FailureKey(result.Test.TestMode);
                    string existing = SessionState.GetString(key, string.Empty);
                    SessionState.SetString(
                        key,
                        string.IsNullOrEmpty(existing) ? failure : existing + "\n" + failure);
                    Debug.LogError("[SpiderProjectionTests] " + failure);
                }
            }
        }
    }
}
