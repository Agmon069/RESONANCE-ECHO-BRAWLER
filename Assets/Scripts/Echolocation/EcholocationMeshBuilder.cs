using System.Collections.Generic;
using UnityEngine;

namespace EcholocationGame
{
    public static class EcholocationMeshBuilder
    {
        private static Mesh _wedgeRampMesh;
        private static Mesh _pyramidMesh;

        public static Mesh GetOrCreateWedgeRampMesh()
        {
            if (_wedgeRampMesh != null) return _wedgeRampMesh;

            Mesh mesh = new Mesh { name = "Echolocation_WedgeRamp" };

            Vector3 blf = new Vector3(-0.5f, -0.5f, -0.5f);
            Vector3 brf = new Vector3( 0.5f, -0.5f, -0.5f);
            Vector3 blb = new Vector3(-0.5f, -0.5f,  0.5f);
            Vector3 brb = new Vector3( 0.5f, -0.5f,  0.5f);
            Vector3 tlb = new Vector3(-0.5f,  0.5f,  0.5f);
            Vector3 trb = new Vector3( 0.5f,  0.5f,  0.5f);

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vector4> uv2s = new List<Vector4>();
            List<int> tris = new List<int>();

            // Helper to add a Quad face (uses UV [0,1]^2 for 4 outer edges, uv2.w = 0)
            void AddQuad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
            {
                int start = verts.Count;
                verts.Add(v0); verts.Add(v1); verts.Add(v2); verts.Add(v3);
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(0f, 1f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(1f, 0f));
                uv2s.Add(new Vector4(1f, 1f, 1f, 0f));
                uv2s.Add(new Vector4(1f, 1f, 1f, 0f));
                uv2s.Add(new Vector4(1f, 1f, 1f, 0f));
                uv2s.Add(new Vector4(1f, 1f, 1f, 0f));
                tris.Add(start + 0); tris.Add(start + 1); tris.Add(start + 2);
                tris.Add(start + 0); tris.Add(start + 2); tris.Add(start + 3);
            }

            // Helper to add a Triangle face (uses barycentric uv2.xyz for 3 outer edges, uv2.w = 1)
            void AddTriangle(Vector3 v0, Vector3 v1, Vector3 v2)
            {
                int start = verts.Count;
                verts.Add(v0); verts.Add(v1); verts.Add(v2);
                uvs.Add(new Vector2(0.5f, 0.5f));
                uvs.Add(new Vector2(0.5f, 0.5f));
                uvs.Add(new Vector2(0.5f, 0.5f));
                uv2s.Add(new Vector4(1f, 0f, 0f, 1f));
                uv2s.Add(new Vector4(0f, 1f, 0f, 1f));
                uv2s.Add(new Vector4(0f, 0f, 1f, 1f));
                tris.Add(start + 0); tris.Add(start + 1); tris.Add(start + 2);
            }

            // 1. Sloped Top Quad
            AddQuad(blf, tlb, trb, brf);
            // 2. Back Vertical Quad
            AddQuad(brb, trb, tlb, blb);
            // 3. Bottom Horizontal Quad
            AddQuad(blb, blf, brf, brb);
            // 4. Left Triangle Side
            AddTriangle(blb, tlb, blf);
            // 5. Right Triangle Side
            AddTriangle(brf, trb, brb);

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetUVs(1, uv2s);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            _wedgeRampMesh = mesh;
            return _wedgeRampMesh;
        }

        public static Mesh GetOrCreatePyramidMesh()
        {
            if (_pyramidMesh != null) return _pyramidMesh;

            Mesh mesh = new Mesh { name = "Echolocation_Pyramid" };

            Vector3 blf = new Vector3(-0.5f, -0.5f, -0.5f);
            Vector3 brf = new Vector3( 0.5f, -0.5f, -0.5f);
            Vector3 blb = new Vector3(-0.5f, -0.5f,  0.5f);
            Vector3 brb = new Vector3( 0.5f, -0.5f,  0.5f);
            Vector3 apex = new Vector3(0f, 0.5f, 0f);

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vector4> uv2s = new List<Vector4>();
            List<int> tris = new List<int>();

            void AddQuad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3)
            {
                int start = verts.Count;
                verts.Add(v0); verts.Add(v1); verts.Add(v2); verts.Add(v3);
                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(0f, 1f));
                uvs.Add(new Vector2(1f, 1f));
                uvs.Add(new Vector2(1f, 0f));
                uv2s.Add(new Vector4(1f, 1f, 1f, 0f));
                uv2s.Add(new Vector4(1f, 1f, 1f, 0f));
                uv2s.Add(new Vector4(1f, 1f, 1f, 0f));
                uv2s.Add(new Vector4(1f, 1f, 1f, 0f));
                tris.Add(start + 0); tris.Add(start + 1); tris.Add(start + 2);
                tris.Add(start + 0); tris.Add(start + 2); tris.Add(start + 3);
            }

            void AddTriangle(Vector3 v0, Vector3 v1, Vector3 v2)
            {
                int start = verts.Count;
                verts.Add(v0); verts.Add(v1); verts.Add(v2);
                uvs.Add(new Vector2(0.5f, 0.5f));
                uvs.Add(new Vector2(0.5f, 0.5f));
                uvs.Add(new Vector2(0.5f, 0.5f));
                uv2s.Add(new Vector4(1f, 0f, 0f, 1f));
                uv2s.Add(new Vector4(0f, 1f, 0f, 1f));
                uv2s.Add(new Vector4(0f, 0f, 1f, 1f));
                tris.Add(start + 0); tris.Add(start + 1); tris.Add(start + 2);
            }

            // Base Quad
            AddQuad(blb, blf, brf, brb);
            // 4 Triangular Sides
            AddTriangle(blf, apex, brf);
            AddTriangle(brf, apex, brb);
            AddTriangle(brb, apex, blb);
            AddTriangle(blb, apex, blf);

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetUVs(1, uv2s);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            _pyramidMesh = mesh;
            return _pyramidMesh;
        }

        public static GameObject CreateBox(string name, Vector3 position, Vector3 scale, Quaternion rotation, Transform parent = null, float alwaysVisible = 0f)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = rotation;
            go.transform.localScale = scale;

            ConfigureSurface(go, EchoShapeMode.BoxOrPlane, EchoPrimitiveKind.ExistingMesh, alwaysVisible);
            return go;
        }

        public static GameObject CreateSphere(string name, Vector3 position, Vector3 scale, Transform parent = null, float alwaysVisible = 0f)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;

            ConfigureSurface(go, EchoShapeMode.Sphere, EchoPrimitiveKind.ExistingMesh, alwaysVisible);
            return go;
        }

        public static GameObject CreateCylinder(string name, Vector3 position, Vector3 scale, Quaternion rotation, Transform parent = null, float alwaysVisible = 0f)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = rotation;
            go.transform.localScale = scale;

            ConfigureSurface(go, EchoShapeMode.Cylinder, EchoPrimitiveKind.ExistingMesh, alwaysVisible);
            return go;
        }

        public static GameObject CreateTriangleRamp(string name, Vector3 position, Vector3 scale, Quaternion rotation, Transform parent = null, bool convexCollider = false, float alwaysVisible = 0f)
        {
            GameObject go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = rotation;
            go.transform.localScale = scale;

            Mesh mesh = GetOrCreateWedgeRampMesh();
            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            go.AddComponent<MeshRenderer>();

            MeshCollider mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.convex = convexCollider;

            ConfigureSurface(go, EchoShapeMode.TriangleOrRamp, EchoPrimitiveKind.TriangleRamp, alwaysVisible);
            return go;
        }

        public static GameObject CreatePyramid(string name, Vector3 position, Vector3 scale, Quaternion rotation, Transform parent = null, bool convexCollider = true, float alwaysVisible = 0f)
        {
            GameObject go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = rotation;
            go.transform.localScale = scale;

            Mesh mesh = GetOrCreatePyramidMesh();
            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            go.AddComponent<MeshRenderer>();

            MeshCollider mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            mc.convex = convexCollider;

            ConfigureSurface(go, EchoShapeMode.TriangleOrRamp, EchoPrimitiveKind.TrianglePyramid, alwaysVisible);
            return go;
        }

        public static void ConfigureSurface(GameObject go, EchoShapeMode mode, EchoPrimitiveKind kind = EchoPrimitiveKind.ExistingMesh, float alwaysVisible = 0f, float edgeWidth = 1.65f)
        {
            EcholocationSurface surf = go.GetComponent<EcholocationSurface>();
            if (surf == null)
            {
                surf = go.AddComponent<EcholocationSurface>();
            }
            surf.shapeMode = mode;
            surf.primitiveKind = kind;
            surf.alwaysVisibleEdge = alwaysVisible;
            surf.edgeWidth = edgeWidth;
            surf.ApplySetup();
        }
    }
}
