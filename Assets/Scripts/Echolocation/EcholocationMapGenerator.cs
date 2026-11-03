using UnityEngine;

namespace EcholocationGame
{
    [ExecuteAlways]
    public class EcholocationMapGenerator : MonoBehaviour
    {
        [Header("Map Settings")]
        public bool autoBuildOnAwake = true;
        public Transform mapRoot;

        private void Awake()
        {
            if (autoBuildOnAwake)
            {
                EnsureCompleteSceneSetup();
            }
        }

        [ContextMenu("Rebuild Echolocation Map")]
        public void RebuildMap()
        {
            if (mapRoot != null)
            {
                if (Application.isPlaying) Destroy(mapRoot.gameObject);
                else DestroyImmediate(mapRoot.gameObject);
            }

            GameObject existing = GameObject.Find("EcholocationMap_Root");
            if (existing != null)
            {
                if (Application.isPlaying) Destroy(existing);
                else DestroyImmediate(existing);
            }

            BuildPrimitiveMap();
        }

        public void EnsureCompleteSceneSetup()
        {
            // 1. Ensure EcholocationManager exists
            EcholocationManager mgr = FindFirstObjectByType<EcholocationManager>();
            if (mgr == null)
            {
                mgr = gameObject.GetComponent<EcholocationManager>();
                if (mgr == null) mgr = gameObject.AddComponent<EcholocationManager>();
            }
            mgr.EnsureShaderAndMaterial();
            mgr.ConfigureBlackVisionEnvironment();

            // 2. Ensure Map exists
            if (mapRoot == null)
            {
                GameObject existing = GameObject.Find("EcholocationMap_Root");
                if (existing != null)
                {
                    mapRoot = existing.transform;
                }
            }

            if (mapRoot == null || mapRoot.childCount == 0)
            {
                BuildPrimitiveMap();
            }
            else
            {
                // Refresh all surfaces in existing map
                EcholocationSurface[] surfaces = mapRoot.GetComponentsInChildren<EcholocationSurface>(true);
                foreach (EcholocationSurface s in surfaces)
                {
                    s.ApplySetup();
                }
            }

            // 3. Ensure Player exists
            EnsurePlayerSetup();

            // 4. Ensure 1 Red-Outlined Enemy exists
            EnsureEnemySetup();
        }

        public void EnsureEnemySetup()
        {
            EcholocationEnemy enemy = FindFirstObjectByType<EcholocationEnemy>();
            if (enemy != null)
            {
                enemy.BuildRedOutlinedMonsterVisuals();
                return;
            }

            GameObject enemyObj = GameObject.Find("EcholocationEnemy_Red");
            if (enemyObj == null)
            {
                enemyObj = new GameObject("EcholocationEnemy_Red");
                enemyObj.transform.position = new Vector3(0f, 1.12f, 6.5f);
                enemyObj.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }

            CharacterController cc = enemyObj.GetComponent<CharacterController>();
            if (cc == null)
            {
                cc = enemyObj.AddComponent<CharacterController>();
                cc.height = 2.1f;
                cc.radius = 0.42f;
                cc.center = new Vector3(0f, 0.05f, 0f);
            }

            enemy = enemyObj.GetComponent<EcholocationEnemy>();
            if (enemy == null)
            {
                enemy = enemyObj.AddComponent<EcholocationEnemy>();
            }
            enemy.BuildRedOutlinedMonsterVisuals();
        }

        public void EnsurePlayerSetup()
        {
            EcholocationPlayerController player = FindFirstObjectByType<EcholocationPlayerController>();
            if (player != null) return;

            GameObject playerObj = GameObject.Find("EcholocationPlayer");
            if (playerObj == null)
            {
                playerObj = new GameObject("EcholocationPlayer");
                playerObj.transform.position = new Vector3(0f, 1.05f, -7.5f);
                playerObj.transform.rotation = Quaternion.identity;
            }

            CharacterController cc = playerObj.GetComponent<CharacterController>();
            if (cc == null)
            {
                cc = playerObj.AddComponent<CharacterController>();
                cc.height = 1.8f;
                cc.radius = 0.38f;
                cc.center = Vector3.zero;
            }

            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                mainCam = FindFirstObjectByType<Camera>();
            }
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                camObj.tag = "MainCamera";
                mainCam = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
            }

            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = Color.black;
            mainCam.nearClipPlane = 0.05f;
            mainCam.transform.SetParent(playerObj.transform, false);
            mainCam.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            mainCam.transform.localRotation = Quaternion.identity;

            player = playerObj.GetComponent<EcholocationPlayerController>();
            if (player == null)
            {
                player = playerObj.AddComponent<EcholocationPlayerController>();
            }
            player.playerCamera = mainCam.transform;
        }

        public void BuildPrimitiveMap()
        {
            GameObject rootObj = new GameObject("EcholocationMap_Root");
            mapRoot = rootObj.transform;

            // =========================================================================
            // 1. LANTAI & DINDING UTAMA (Planes / Large Wall Panels)
            // =========================================================================
            Transform wallsGroup = new GameObject("01_Lantai_Dan_Dinding_Plane").transform;
            wallsGroup.SetParent(mapRoot, false);

            // Main Floor (Plane / Thin Box)
            EcholocationMeshBuilder.CreateBox(
                "Lantai_Utama",
                new Vector3(0f, -0.1f, 6f),
                new Vector3(36f, 0.2f, 44f),
                Quaternion.identity,
                wallsGroup);

            // Outer Perimeter Walls (Dinding Plane / Panel)
            EcholocationMeshBuilder.CreateBox("Dinding_Selatan", new Vector3(0f, 2.2f, -16f), new Vector3(36f, 4.4f, 0.3f), Quaternion.identity, wallsGroup);
            EcholocationMeshBuilder.CreateBox("Dinding_Utara",   new Vector3(0f, 2.2f,  28f), new Vector3(36f, 4.4f, 0.3f), Quaternion.identity, wallsGroup);
            EcholocationMeshBuilder.CreateBox("Dinding_Barat",   new Vector3(-18f, 2.2f, 6f), new Vector3(0.3f, 4.4f, 44f), Quaternion.identity, wallsGroup);
            EcholocationMeshBuilder.CreateBox("Dinding_Timur",   new Vector3( 18f, 2.2f, 6f), new Vector3(0.3f, 4.4f, 44f), Quaternion.identity, wallsGroup);

            // Interior Dividing Walls (Corridor & Room Partitions)
            EcholocationMeshBuilder.CreateBox("Dinding_Sekat_Kiri",  new Vector3(-8.5f, 1.8f, -1.5f), new Vector3(0.3f, 3.6f, 11f), Quaternion.Euler(0f, 12f, 0f), wallsGroup);
            EcholocationMeshBuilder.CreateBox("Dinding_Sekat_Kanan", new Vector3( 8.5f, 1.8f, -1.5f), new Vector3(0.3f, 3.6f, 11f), Quaternion.Euler(0f, -12f, 0f), wallsGroup);
            EcholocationMeshBuilder.CreateBox("Dinding_Koridor_Tengah_A", new Vector3(-6.5f, 2.0f, 13f), new Vector3(11f, 4.0f, 0.3f), Quaternion.identity, wallsGroup);
            EcholocationMeshBuilder.CreateBox("Dinding_Koridor_Tengah_B", new Vector3( 7.5f, 2.0f, 13f), new Vector3(9f,  4.0f, 0.3f), Quaternion.identity, wallsGroup);

            // =========================================================================
            // 2. ZONA TENGAH / DEPAN PEMAIN (Screenshot 1 & 2 Landmarks: Balok + Segitiga)
            // =========================================================================
            Transform centerGroup = new GameObject("02_Objek_Balok_Cubes").transform;
            centerGroup.SetParent(mapRoot, false);

            // Landmark A (Front-Left): Wall block + Triangular Ramp in front (like Screenshot 1)
            EcholocationMeshBuilder.CreateBox(
                "Balok_Dinding_Depan",
                new Vector3(-2.2f, 1.15f, -1.2f),
                new Vector3(2.8f, 2.3f, 0.45f),
                Quaternion.Euler(0f, 8f, 0f),
                centerGroup);

            // Landmark B (Front-Right): Pair of Cubes (one grounded, one elevated/stacked like Screenshot 2)
            EcholocationMeshBuilder.CreateBox(
                "Balok_Besar_A",
                new Vector3(2.2f, 0.65f, -1.8f),
                new Vector3(1.3f, 1.3f, 1.3f),
                Quaternion.Euler(0f, -14f, 0f),
                centerGroup);

            EcholocationMeshBuilder.CreateBox(
                "Balok_Melayang_B",
                new Vector3(3.35f, 1.25f, -1.1f),
                new Vector3(1.15f, 1.15f, 1.15f),
                Quaternion.Euler(6f, 18f, -5f),
                centerGroup);

            // Additional Cubes (Balok) around the map
            EcholocationMeshBuilder.CreateBox("Balok_Pilar_1", new Vector3(-5.2f, 1.5f, 4.5f), new Vector3(1.5f, 3.0f, 1.5f), Quaternion.identity, centerGroup);
            EcholocationMeshBuilder.CreateBox("Balok_Pilar_2", new Vector3( 5.2f, 1.5f, 4.5f), new Vector3(1.5f, 3.0f, 1.5f), Quaternion.identity, centerGroup);
            EcholocationMeshBuilder.CreateBox("Balok_Tangga_1", new Vector3(-3.0f, 0.45f, 8.0f), new Vector3(1.8f, 0.9f, 1.8f), Quaternion.identity, centerGroup);
            EcholocationMeshBuilder.CreateBox("Balok_Tangga_2", new Vector3(-3.0f, 0.9f, 10.0f), new Vector3(1.8f, 1.8f, 1.8f), Quaternion.identity, centerGroup);
            EcholocationMeshBuilder.CreateBox("Balok_Platform_Atas", new Vector3(0f, 0.8f, 18.5f), new Vector3(6.5f, 1.6f, 4.5f), Quaternion.identity, centerGroup);
            EcholocationMeshBuilder.CreateBox("Balok_Rintangan_Utara_1", new Vector3(-9.0f, 1.0f, 20.0f), new Vector3(2.2f, 2.0f, 2.2f), Quaternion.Euler(0f, 25f, 0f), centerGroup);
            EcholocationMeshBuilder.CreateBox("Balok_Rintangan_Utara_2", new Vector3( 9.5f, 1.2f, 21.0f), new Vector3(2.4f, 2.4f, 1.6f), Quaternion.Euler(0f, -30f, 0f), centerGroup);

            // =========================================================================
            // 3. OBJEK SEGITIGA (Triangular Wedge Ramps & Pyramids)
            // =========================================================================
            Transform triangleGroup = new GameObject("03_Objek_Segitiga_Triangles").transform;
            triangleGroup.SetParent(mapRoot, false);

            // Triangular wedge right in front of Balok_Dinding_Depan (exactly like Screenshot 1!)
            EcholocationMeshBuilder.CreateTriangleRamp(
                "Segitiga_Ramp_Depan",
                new Vector3(-1.35f, 0.35f, -2.0f),
                new Vector3(1.9f, 0.7f, 1.3f),
                Quaternion.Euler(0f, -82f, 0f),
                triangleGroup);

            // Walkable Large Triangular Ramp leading up to the North Platform
            EcholocationMeshBuilder.CreateTriangleRamp(
                "Segitiga_Ramp_Besar_Tengah",
                new Vector3(0f, 0.8f, 14.25f),
                new Vector3(3.6f, 1.6f, 4.0f),
                Quaternion.identity,
                triangleGroup);

            // Side Triangular Ramp descending from the North Platform
            EcholocationMeshBuilder.CreateTriangleRamp(
                "Segitiga_Ramp_Samping",
                new Vector3(5.0f, 0.8f, 18.5f),
                new Vector3(3.5f, 1.6f, 2.8f),
                Quaternion.Euler(0f, -90f, 0f),
                triangleGroup);

            // Triangular Pyramids (Limas Segitiga)
            EcholocationMeshBuilder.CreatePyramid(
                "Segitiga_Limas_Kiri",
                new Vector3(-5.0f, 0.95f, -3.5f),
                new Vector3(1.9f, 1.9f, 1.9f),
                Quaternion.Euler(0f, 20f, 0f),
                triangleGroup);

            EcholocationMeshBuilder.CreatePyramid(
                "Segitiga_Limas_Kanan",
                new Vector3(4.8f, 1.1f, 8.5f),
                new Vector3(2.2f, 2.2f, 2.2f),
                Quaternion.Euler(0f, 45f, 0f),
                triangleGroup);

            EcholocationMeshBuilder.CreatePyramid(
                "Segitiga_Limas_AtasPlatform",
                new Vector3(0f, 2.4f, 18.5f),
                new Vector3(1.6f, 1.6f, 1.6f),
                Quaternion.Euler(0f, 15f, 0f),
                triangleGroup);

            // =========================================================================
            // 4. OBJEK BULAT (Spheres & Cylinders)
            // =========================================================================
            Transform roundGroup = new GameObject("04_Objek_Bulat_Spheres_Cylinders").transform;
            roundGroup.SetParent(mapRoot, false);

            // Spheres (Bola Besar di Map)
            EcholocationMeshBuilder.CreateSphere(
                "Bulat_Bola_1",
                new Vector3(0.4f, 0.85f, 3.2f),
                new Vector3(1.7f, 1.7f, 1.7f),
                roundGroup);

            EcholocationMeshBuilder.CreateSphere(
                "Bulat_Bola_2",
                new Vector3(5.5f, 1.1f, -4.5f),
                new Vector3(2.2f, 2.2f, 2.2f),
                roundGroup);

            EcholocationMeshBuilder.CreateSphere(
                "Bulat_Bola_3",
                new Vector3(-7.0f, 1.25f, 9.0f),
                new Vector3(2.5f, 2.5f, 2.5f),
                roundGroup);

            EcholocationMeshBuilder.CreateSphere(
                "Bulat_Bola_Utara",
                new Vector3(-4.5f, 1.0f, 23.0f),
                new Vector3(2.0f, 2.0f, 2.0f),
                roundGroup);

            // Cylinders (Tabung / Pilar Bulat)
            EcholocationMeshBuilder.CreateCylinder(
                "Bulat_Tabung_Pilar_Kiri",
                new Vector3(-3.8f, 1.6f, -7.0f),
                new Vector3(1.3f, 1.6f, 1.3f),
                Quaternion.identity,
                roundGroup);

            EcholocationMeshBuilder.CreateCylinder(
                "Bulat_Tabung_Pilar_Kanan",
                new Vector3(3.8f, 1.6f, -7.0f),
                new Vector3(1.3f, 1.6f, 1.3f),
                Quaternion.identity,
                roundGroup);

            EcholocationMeshBuilder.CreateCylinder(
                "Bulat_Tabung_Utara",
                new Vector3(4.5f, 1.8f, 23.0f),
                new Vector3(1.6f, 1.8f, 1.6f),
                Quaternion.identity,
                roundGroup);
        }
    }
}
