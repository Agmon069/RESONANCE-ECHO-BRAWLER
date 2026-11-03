using System.Collections.Generic;
using UnityEngine;

namespace EcholocationGame
{
    public enum EchoPrimitiveKind
    {
        ExistingMesh = 0,
        TriangleRamp = 1,
        TrianglePyramid = 2
    }

    [ExecuteAlways]
    [RequireComponent(typeof(Renderer))]
    public class EcholocationSurface : MonoBehaviour
    {
        public EchoShapeMode shapeMode = EchoShapeMode.BoxOrPlane;
        public EchoPrimitiveKind primitiveKind = EchoPrimitiveKind.ExistingMesh;

        [Range(0f, 1f)]
        public float alwaysVisibleEdge = 0f;

        [Range(0.5f, 4f)]
        public float edgeWidth = 1.65f;

        public Color edgeColor = Color.white;

        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;

        private static readonly int ShapeModeId = Shader.PropertyToID("_ShapeMode");
        private static readonly int AlwaysVisibleId = Shader.PropertyToID("_AlwaysVisibleEdge");
        private static readonly int EdgeWidthId = Shader.PropertyToID("_EdgeWidth");
        private static readonly int EdgeColorId = Shader.PropertyToID("_EdgeColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            ApplySetup();
        }

        private void OnEnable()
        {
            ApplySetup();
        }

        private void OnValidate()
        {
            ApplySetup();
        }

        public void ApplySetup()
        {
            if (primitiveKind == EchoPrimitiveKind.TriangleRamp)
            {
                shapeMode = EchoShapeMode.TriangleOrRamp;
                EnsureProceduralMesh(EcholocationMeshBuilder.GetOrCreateWedgeRampMesh());
            }
            else if (primitiveKind == EchoPrimitiveKind.TrianglePyramid)
            {
                shapeMode = EchoShapeMode.TriangleOrRamp;
                EnsureProceduralMesh(EcholocationMeshBuilder.GetOrCreatePyramidMesh());
            }

            if (_renderer == null)
            {
                _renderer = GetComponent<Renderer>();
            }
            if (_renderer == null) return;

            Shader shader = Shader.Find("Custom/Echolocation");
            if (shader != null && (_renderer.sharedMaterial == null || _renderer.sharedMaterial.shader != shader))
            {
                if (EcholocationManager.Instance != null)
                {
                    _renderer.sharedMaterial = EcholocationManager.Instance.GetOrCreateMaterial();
                }
                else
                {
                    Material mat = new Material(shader)
                    {
                        name = "Echolocation_RuntimeMat",
                        enableInstancing = true
                    };
                    _renderer.sharedMaterial = mat;
                }
            }

            if (_propBlock == null)
            {
                _propBlock = new MaterialPropertyBlock();
            }

            _renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetFloat(ShapeModeId, (float)shapeMode);
            _propBlock.SetFloat(AlwaysVisibleId, alwaysVisibleEdge);
            _propBlock.SetFloat(EdgeWidthId, edgeWidth);
            _propBlock.SetColor(EdgeColorId, edgeColor);
            _propBlock.SetColor(BaseColorId, Color.black);
            _renderer.SetPropertyBlock(_propBlock);
        }

        private void EnsureProceduralMesh(Mesh mesh)
        {
            MeshFilter mf = GetComponent<MeshFilter>();
            if (mf == null) mf = gameObject.AddComponent<MeshFilter>();
            if (mf.sharedMesh != mesh)
            {
                mf.sharedMesh = mesh;
            }

            MeshCollider mc = GetComponent<MeshCollider>();
            if (mc != null && mc.sharedMesh != mesh)
            {
                mc.sharedMesh = mesh;
            }
        }
    }
}
