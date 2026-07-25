using UnityEngine;

namespace SpiderProjection.Runtime
{
    public sealed class WallDemoRuntime : MonoBehaviour
    {
        [SerializeField] private PlayerBrain player;
        [SerializeField] private MusicDirector music;

        private void Start()
        {
            player ??= FindFirstObjectByType<PlayerBrain>();
            music ??= FindFirstObjectByType<MusicDirector>();
            music?.Bind(player);
        }
    }
}
