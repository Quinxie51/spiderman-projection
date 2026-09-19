using UnityEditor;
using UnityEngine;
using SpiderProjection.Runtime;

namespace SpiderProjection.Editor
{
    internal static class SpiderProjectionDevCommands
    {
        [MenuItem("Tools/Spider Projection/Respawn Player %#r")]
        private static void RespawnPlayer()
        {
            var player = Object.FindFirstObjectByType<PlayerBrain>();
            if (player == null)
            {
                Debug.LogWarning("[SpiderProjection] No PlayerBrain found in the open scene.");
                return;
            }

            player.ResetPlayer();
            Debug.Log("[SpiderProjection] Player respawned.");
        }

        [MenuItem("Tools/Spider Projection/Respawn Player %#r", true)]
        private static bool ValidateRespawnPlayer()
        {
            return Application.isPlaying;
        }
    }
}
