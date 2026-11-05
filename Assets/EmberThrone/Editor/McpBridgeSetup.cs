using UnityEditor;
using UnityEngine;

namespace EmberThrone.EditorTools
{
    /// <summary>
    /// Makes the MCP for Unity bridge start by itself on this machine, so an AI agent can drive the
    /// editor without anyone clicking through its window. Loopback only (package default).
    /// The preference keys are the package's own; see Packages/manifest.json for the pinned version.
    /// </summary>
    [InitializeOnLoad]
    static class McpBridgeSetup
    {
        const string UseHttp = "MCPForUnity.UseHttpTransport";
        const string Scope = "MCPForUnity.HttpTransportScope";
        const string AutoStart = "MCPForUnity.AutoStartOnLoad";

        static McpBridgeSetup()
        {
            if (Application.isBatchMode || EditorPrefs.GetBool(AutoStart, false)) return;
            EditorPrefs.SetBool(UseHttp, true);
            EditorPrefs.SetString(Scope, "local");
            EditorPrefs.SetBool(AutoStart, true);
            Debug.Log("[EmberThrone] MCP bridge auto-start enabled (localhost:8080).");
            // The package reads the preference while loading, so it needs one more reload to see it.
            EditorApplication.delayCall += EditorUtility.RequestScriptReload;
        }
    }
}
