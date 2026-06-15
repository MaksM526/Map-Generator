using UnityEngine;

namespace MapGenerator.ProceduralWorld
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class CubeSphereWorld : MonoBehaviour
    {
        [SerializeField, Range(2, 256)] private int gridSize = 32;
        [SerializeField, Min(0.01f)] private float radius = 10f;
        [SerializeField] private bool autoRegenerate = true;

        private const string GeneratedMeshName = "Procedural Cube Sphere";

        private bool _needsRegenerate;
        private Mesh _generatedMesh;

        private void OnEnable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update += HandleEditorUpdate;
#endif
            if (autoRegenerate)
            {
                Generate();
            }
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= HandleEditorUpdate;
#endif
            CleanupGeneratedMesh();
        }

        private void OnValidate()
        {
            if (autoRegenerate)
            {
                _needsRegenerate = true;
            }
        }

#if UNITY_EDITOR
        private void HandleEditorUpdate()
        {
            if (!_needsRegenerate)
            {
                return;
            }

            _needsRegenerate = false;
            Generate();
        }
#endif

        [ContextMenu("Generate Cube Sphere")]
        public void Generate()
        {
            Mesh mesh = new Mesh { name = GeneratedMeshName };
            int verticesPerFace = (gridSize + 1) * (gridSize + 1);
            Vector3[] vertices = new Vector3[verticesPerFace * 6];
            Vector3[] normals = new Vector3[vertices.Length];
            Vector2[] uv = new Vector2[vertices.Length];
            int[] triangles = new int[gridSize * gridSize * 6 * 6];

            int vertexIndex = 0;
            int triangleIndex = 0;
            for (int face = 0; face < 6; face++)
            {
                Vector3 localUp = FaceNormal(face);
                Vector3 axisA = new Vector3(localUp.y, localUp.z, localUp.x);
                Vector3 axisB = Vector3.Cross(localUp, axisA);

                for (int y = 0; y <= gridSize; y++)
                {
                    for (int x = 0; x <= gridSize; x++)
                    {
                        Vector2 percent = new Vector2(x, y) / gridSize;
                        Vector3 pointOnCube = localUp + (percent.x - 0.5f) * 2f * axisA + (percent.y - 0.5f) * 2f * axisB;
                        Vector3 pointOnSphere = pointOnCube.normalized;
                        vertices[vertexIndex] = pointOnSphere * radius;
                        normals[vertexIndex] = pointOnSphere;
                        uv[vertexIndex] = GetFaceAtlasUv(face, percent);
                        vertexIndex++;
                    }
                }

                int faceVertexStart = face * verticesPerFace;
                for (int y = 0; y < gridSize; y++)
                {
                    for (int x = 0; x < gridSize; x++)
                    {
                        int i = faceVertexStart + x + y * (gridSize + 1);
                        triangles[triangleIndex++] = i;
                        triangles[triangleIndex++] = i + gridSize + 1;
                        triangles[triangleIndex++] = i + 1;
                        triangles[triangleIndex++] = i + 1;
                        triangles[triangleIndex++] = i + gridSize + 1;
                        triangles[triangleIndex++] = i + gridSize + 2;
                    }
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();

            MeshFilter meshFilter = GetComponent<MeshFilter>();
            Mesh previousMesh = meshFilter.sharedMesh;
            bool shouldDestroyPreviousMesh = IsGeneratedMesh(previousMesh);
            meshFilter.sharedMesh = mesh;
            _generatedMesh = mesh;

            if (shouldDestroyPreviousMesh)
            {
                DestroyGeneratedMesh(previousMesh);
            }
        }

        private void CleanupGeneratedMesh()
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter.sharedMesh == _generatedMesh)
            {
                meshFilter.sharedMesh = null;
            }

            DestroyGeneratedMesh(_generatedMesh);
        }

        private void DestroyGeneratedMesh(Mesh mesh)
        {
            if (!IsGeneratedMesh(mesh))
            {
                return;
            }

            if (mesh == _generatedMesh)
            {
                _generatedMesh = null;
            }
            if (Application.isPlaying)
            {
                Destroy(mesh);
            }
            else
            {
                DestroyImmediate(mesh);
            }
        }

        private bool IsGeneratedMesh(Mesh mesh)
        {
            if (mesh == null)
            {
                return false;
            }

            if (mesh == _generatedMesh)
            {
                return true;
            }

            return mesh.name == GeneratedMeshName && !IsPersistentAsset(mesh);
        }

        private static bool IsPersistentAsset(Mesh mesh)
        {
#if UNITY_EDITOR
            return UnityEditor.EditorUtility.IsPersistent(mesh);
#else
            return false;
#endif
        }

        private static Vector2 GetFaceAtlasUv(int face, Vector2 percent)
        {
            const int atlasColumns = 3;
            const int atlasRows = 2;

            int atlasX = face % atlasColumns;
            int atlasY = face / atlasColumns;
            Vector2 tileSize = new Vector2(1f / atlasColumns, 1f / atlasRows);

            return new Vector2(
                (atlasX + percent.x) * tileSize.x,
                (atlasY + percent.y) * tileSize.y);
        }

        private static Vector3 FaceNormal(int face)
        {
            switch (face)
            {
                case 0: return Vector3.up;
                case 1: return Vector3.down;
                case 2: return Vector3.left;
                case 3: return Vector3.right;
                case 4: return Vector3.forward;
                default: return Vector3.back;
            }
        }
    }
}
