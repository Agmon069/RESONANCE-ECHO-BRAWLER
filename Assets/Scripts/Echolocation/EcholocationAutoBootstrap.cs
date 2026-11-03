using UnityEngine;
using UnityEngine.SceneManagement;

namespace EcholocationGame
{
    public static class EcholocationAutoBootstrap
    {
        public const string EcholocationSceneName = "SampleScene";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnAfterSceneLoad()
        {
            Scene activeScene = SceneManager.GetActiveScene();

            // Hanya jalankan game Echolocation di scene "SampleScene" (atau jika memang sudah ada EcholocationMapGenerator di scene tersebut).
            // Jangan pernah memunculkan objek Echolocation di scene lain (seperti scene dari package dosen).
            if (activeScene.name != EcholocationSceneName)
            {
                return;
            }

            EcholocationMapGenerator mapGen = Object.FindFirstObjectByType<EcholocationMapGenerator>();
            if (mapGen == null)
            {
                GameObject sysObj = new GameObject("EcholocationSystem");
                sysObj.AddComponent<EcholocationManager>();
                mapGen = sysObj.AddComponent<EcholocationMapGenerator>();
            }

            mapGen.EnsureCompleteSceneSetup();
        }
    }
}
