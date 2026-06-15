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

        private void OnEnable()
        {
            if (autoRegenerate)
            {
                Generate();
            }
        }

        private void OnValidate()
        {
            if (autoRegenerate)
            {
                Generate();
            }
        }

        [ContextMenu("Generate Cube Sphere")]
        public void Generate()
        {
            Mesh mesh = new Mesh { name = "Procedural Cube Sphere" };
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
                        uv[vertexIndex] = percent;
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
            GetComponent<MeshFilter>().sharedMesh = mesh;
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
