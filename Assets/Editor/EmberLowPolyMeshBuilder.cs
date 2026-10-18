using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Emberlight.Editor
{
    /// <summary>仅编辑器使用的低模几何工具：平面法线、材质分组与单骨权重。</summary>
    internal sealed class EmberLowPolyMeshBuilder
    {
        readonly List<Vector3> vertices = new List<Vector3>(), normals = new List<Vector3>();
        readonly List<BoneWeight> weights = new List<BoneWeight>();
        readonly Dictionary<int, List<int>> triangles = new Dictionary<int, List<int>>();

        public Mesh Build(string name, bool skinned, out int[] materials)
        {
            var mesh = new Mesh { name = name, indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, vertices.Select(point => new Vector2(point.x, point.z)).ToList());
            materials = triangles.Keys.OrderBy(index => index).ToArray();
            mesh.subMeshCount = materials.Length;
            for (int i = 0; i < materials.Length; i++) mesh.SetTriangles(triangles[materials[i]], i);
            if (skinned) mesh.boneWeights = weights.ToArray();
            mesh.RecalculateBounds();
            return mesh;
        }

        public void Triangle(Vector3 a, Vector3 b, Vector3 c, int material, int bone = 0)
        {
            var normal = Vector3.Cross(b - a, c - a);
            if (normal.sqrMagnitude < .000000001f) return;
            normal.Normalize();
            if (!triangles.TryGetValue(material, out var indices)) triangles.Add(material, indices = new List<int>());
            foreach (var point in new[] { a, b, c })
            {
                indices.Add(vertices.Count);
                vertices.Add(point); normals.Add(normal);
                weights.Add(new BoneWeight { boneIndex0 = bone, weight0 = 1 });
            }
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int material, int bone = 0, bool bothSides = false)
        {
            Triangle(a, b, c, material, bone); Triangle(a, c, d, material, bone);
            if (!bothSides) return;
            Triangle(c, b, a, material, bone); Triangle(d, c, a, material, bone);
        }

        public void Box(Matrix4x4 matrix, Vector3 size, int material, int bone = 0)
        {
            var points = new[] { new Vector3(-1,-1,-1), new Vector3(1,-1,-1), new Vector3(1,1,-1), new Vector3(-1,1,-1),
                new Vector3(-1,-1,1), new Vector3(1,-1,1), new Vector3(1,1,1), new Vector3(-1,1,1) };
            for (int i = 0; i < points.Length; i++) points[i] = matrix.MultiplyPoint3x4(Vector3.Scale(points[i], size * .5f));
            var faces = new[] { 4,5,6,7, 1,0,3,2, 5,1,2,6, 0,4,7,3, 3,7,6,2, 0,1,5,4 };
            for (int i = 0; i < faces.Length; i += 4)
                Quad(points[faces[i]], points[faces[i+1]], points[faces[i+2]], points[faces[i+3]], material, bone);
        }

        public void Frustum(Matrix4x4 matrix, float bottom, float top, float height, int sides, int material, int bone = 0)
        {
            var low = matrix.MultiplyPoint3x4(Vector3.down * height * .5f);
            var high = matrix.MultiplyPoint3x4(Vector3.up * height * .5f);
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides, b = (i + 1) * Mathf.PI * 2 / sides;
                var p = matrix.MultiplyPoint3x4(new Vector3(Mathf.Cos(a) * bottom, -height * .5f, Mathf.Sin(a) * bottom));
                var q = matrix.MultiplyPoint3x4(new Vector3(Mathf.Cos(a) * top, height * .5f, Mathf.Sin(a) * top));
                var r = matrix.MultiplyPoint3x4(new Vector3(Mathf.Cos(b) * top, height * .5f, Mathf.Sin(b) * top));
                var s = matrix.MultiplyPoint3x4(new Vector3(Mathf.Cos(b) * bottom, -height * .5f, Mathf.Sin(b) * bottom));
                Quad(p, q, r, s, material, bone);
                Triangle(low, p, s, material, bone); Triangle(high, r, q, material, bone);
            }
        }

        public void Ellipsoid(Matrix4x4 matrix, Vector3 radius, int material, int bone = 0, int sides = 8, int rings = 5)
        {
            for (int row = 0; row < rings; row++) for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides, b = (i + 1) * Mathf.PI * 2 / sides;
                float low = -Mathf.PI * .5f + row * Mathf.PI / rings, high = low + Mathf.PI / rings;
                Quad(SpherePoint(matrix, radius, a, low), SpherePoint(matrix, radius, a, high),
                    SpherePoint(matrix, radius, b, high), SpherePoint(matrix, radius, b, low), material, bone);
            }
        }

        public void Ring(Matrix4x4 matrix, float rx, float ry, float width, int material, int bone = 0, int sides = 12)
        {
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides, b = (i + 1) * Mathf.PI * 2 / sides;
                Quad(Ellipse(matrix, rx, ry, a), Ellipse(matrix, rx, ry, b),
                    Ellipse(matrix, rx - width, ry - width, b), Ellipse(matrix, rx - width, ry - width, a), material, bone, true);
            }
        }

        public void Hood(Matrix4x4 matrix, int cloth, int trim, int bone)
        {
            const int sides = 12;
            var front = Matrix4x4.Translate(new Vector3(0, .08f, .30f));
            var outside = Matrix4x4.Translate(new Vector3(0, .08f, .12f));
            var back = Matrix4x4.Translate(new Vector3(0, .08f, -.18f));
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides, b = (i + 1) * Mathf.PI * 2 / sides;
                var p = Ellipse(matrix * front, .28f, .285f, a);
                var q = Ellipse(matrix * front, .28f, .285f, b);
                var r = Ellipse(matrix * outside, .36f, .37f, a);
                var s = Ellipse(matrix * outside, .36f, .37f, b);
                var u = Ellipse(matrix * back, .33f, .35f, a);
                var v = Ellipse(matrix * back, .33f, .35f, b);
                Quad(p, r, s, q, cloth, bone, true);
                Quad(r, u, v, s, cloth, bone, true);
                Triangle(u, matrix.MultiplyPoint3x4(new Vector3(0, .08f, -.31f)), v, cloth, bone);
            }
            Ring(matrix * front, .28f, .285f, .021f, trim, bone);
            Frustum(matrix * Matrix4x4.TRS(new Vector3(0, .38f, -.09f), Quaternion.Euler(-15, 0, 0), Vector3.one),
                .18f, .025f, .19f, 8, cloth, bone);
        }

        static Vector3 SpherePoint(Matrix4x4 matrix, Vector3 radius, float angle, float latitude) =>
            matrix.MultiplyPoint3x4(Vector3.Scale(new Vector3(Mathf.Cos(angle) * Mathf.Cos(latitude), Mathf.Sin(latitude),
                Mathf.Sin(angle) * Mathf.Cos(latitude)), radius));
        static Vector3 Ellipse(Matrix4x4 matrix, float rx, float ry, float angle) =>
            matrix.MultiplyPoint3x4(new Vector3(Mathf.Cos(angle) * rx, Mathf.Sin(angle) * ry, 0));
    }
}
