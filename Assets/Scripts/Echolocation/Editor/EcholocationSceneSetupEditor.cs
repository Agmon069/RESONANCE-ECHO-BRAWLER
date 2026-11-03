#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EcholocationGame.Editor
{
    [InitializeOnLoad]
    public static class EcholocationSceneSetupEditor
    {
        private const string TargetEcholocationSceneName = "SampleScene";

        static EcholocationSceneSetupEditor()
        {
            EditorApplication.delayCall += OnEditorDelayCall;
        }

        private static void OnEditorDelayCall()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid()) return;

            // Jika scene yang sedang dibuka BUKAN SampleScene (misal scene dari package dosen),
            // pastikan tidak ada objek Echolocation yang ikut masuk ke scene tersebut.
            if (activeScene.name != TargetEcholocationSceneName)
            {
                if (GameObject.Find("EcholocationSystem") != null ||
                    GameObject.Find("EcholocationMap_Root") != null ||
                    GameObject.Find("EcholocationPlayer") != null ||
                    GameObject.Find("EcholocationEnemy_Red") != null)
                {
                    RemoveEcholocationFromCurrentScene();
                }
                return;
            }

            // Hanya auto-setup di SampleScene
            EcholocationMapGenerator existingGen = Object.FindFirstObjectByType<EcholocationMapGenerator>();
            if (existingGen == null)
            {
                SetupEcholocationScene();
            }
            else
            {
                existingGen.EnsureCompleteSceneSetup();
            }
        }

        [MenuItem("Tools/Echolocation/Setup Scene & Rebuild Map (SampleScene)")]
        public static void SetupEcholocationScene()
        {
            EcholocationMapGenerator mapGen = Object.FindFirstObjectByType<EcholocationMapGenerator>();
            if (mapGen == null)
            {
                GameObject sysObj = GameObject.Find("EcholocationSystem");
                if (sysObj == null)
                {
                    sysObj = new GameObject("EcholocationSystem");
                }

                if (sysObj.GetComponent<EcholocationManager>() == null)
                {
                    sysObj.AddComponent<EcholocationManager>();
                }

                mapGen = sysObj.AddComponent<EcholocationMapGenerator>();
            }

            mapGen.RebuildMap();
            mapGen.EnsureCompleteSceneSetup();

            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();
            }

            Debug.Log("[Echolocation] Scene setup complete!");
        }

        [MenuItem("Tools/Echolocation/Remove Echolocation From Current Scene")]
        public static void RemoveEcholocationFromCurrentScene()
        {
            // Lepaskan Main Camera dari EcholocationPlayer jika sempat menjadi child-nya
            GameObject playerObj = GameObject.Find("EcholocationPlayer");
            if (playerObj != null)
            {
                Camera cam = playerObj.GetComponentInChildren<Camera>(true);
                if (cam != null)
                {
                    Transform handVm = cam.transform.Find("HandViewModel");
                    if (handVm != null)
                    {
                        Object.DestroyImmediate(handVm.gameObject);
                    }
                    cam.transform.SetParent(null, true);
                    cam.clearFlags = CameraClearFlags.Skybox;
                }
                Object.DestroyImmediate(playerObj);
            }

            GameObject enemyObj = GameObject.Find("EcholocationEnemy_Red");
            if (enemyObj != null) Object.DestroyImmediate(enemyObj);

            GameObject mapObj = GameObject.Find("EcholocationMap_Root");
            if (mapObj != null) Object.DestroyImmediate(mapObj);

            GameObject sysObj = GameObject.Find("EcholocationSystem");
            if (sysObj != null) Object.DestroyImmediate(sysObj);

            // Nyalakan kembali Directional Light di scene tersebut
            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light l in lights)
            {
                l.enabled = true;
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.reflectionIntensity = 1f;
            Material defaultSkybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
            if (defaultSkybox != null)
            {
                RenderSettings.skybox = defaultSkybox;
            }

            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }

            Debug.Log("[Echolocation] Objek Echolocation telah dibersihkan dari scene ini.");
        }
    }
}
#endif
