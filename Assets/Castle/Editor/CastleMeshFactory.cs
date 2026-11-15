using System.Collections.Generic;
using UnityEngine;

namespace CastleGen
{
    /// <summary>
    /// Flat-shaded primitive meshes Unity does not ship with: cones, square pyramids
    /// and triangular prisms (gable roofs). Everything is built with unshared vertices
    /// so the faces read as crisp facets, matching the toy-block look of the reference.
    /// </summary>
    public static class CastleMeshFactory
    {
        // Base sits on y = 0, apex at y = height, centred on the origin in XZ.
        public static Mesh Cone(float radius, float height, int segments = 28)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var apex = new Vector3(0f, height, 0f);
            var centre = Vector3.zero;

            for (int i = 0; i < segments; i++)
            {
                float a0 = (i / (float)segments) * Mathf.PI * 2f;
                float a1 = ((i + 1) / (float)segments) * Mathf.PI * 2f;
                var p0 = new Vector3(Mathf.Cos(a0) * radius, 0f, Mathf.Sin(a0) * radius);
                var p1 = new Vector3(Mathf.Cos(a1) * radius, 0f, Mathf.Sin(a1) * radius);

                AddTri(verts, tris, apex, p1, p0);   // side, outward facing
                AddTri(verts, tris, centre, p0, p1); // base cap, downward facing
            }

            return Finish(verts, tris, "Cone");
        }

        // Square-base pyramid. Base on y = 0 centred on the origin, apex at y = height.
        public static Mesh Pyramid(float width, float depth, float height)
        {
            float hw = width * 0.5f, hd = depth * 0.5f;
            var c = new[]
            {
                new Vector3(-hw, 0f, -hd),
                new Vector3( hw, 0f, -hd),
                new Vector3( hw, 0f,  hd),
                new Vector3(-hw, 0f,  hd),
            };
            var apex = new Vector3(0f, height, 0f);

            var verts = new List<Vector3>();
            var tris = new List<int>();
            for (int i = 0; i < 4; i++)
                AddTri(verts, tris, apex, c[(i + 1) % 4], c[i]);

            AddTri(verts, tris, c[0], c[1], c[2]);
            AddTri(verts, tris, c[0], c[2], c[3]);

            return Finish(verts, tris, "Pyramid");
        }

        // Gable roof: ridge runs along X, triangular cross section in ZY, base on y = 0.
        public static Mesh GableRoof(float length, float width, float height)
        {
            float hl = length * 0.5f, hw = width * 0.5f;
            var a = new Vector3(-hl, 0f, -hw);
            var b = new Vector3(-hl, 0f,  hw);
            var c = new Vector3( hl, 0f, -hw);
            var d = new Vector3( hl, 0f,  hw);
            var A = new Vector3(-hl, height, 0f);
            var B = new Vector3( hl, height, 0f);

            var verts = new List<Vector3>();
            var tris = new List<int>();

            AddQuad(verts, tris, a, A, B, c); // slope facing -Z
            AddQuad(verts, tris, d, B, A, b); // slope facing +Z
            AddTri(verts, tris, A, a, b);     // gable end at -X
            AddTri(verts, tris, B, d, c);     // gable end at +X
            AddQuad(verts, tris, a, c, d, b); // underside

            return Finish(verts, tris, "GableRoof");
        }

        static void AddTri(List<Vector3> verts, List<int> tris, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            int i = verts.Count;
            verts.Add(p0); verts.Add(p1); verts.Add(p2);
            tris.Add(i); tris.Add(i + 1); tris.Add(i + 2);
        }

        static void AddQuad(List<Vector3> verts, List<int> tris, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            AddTri(verts, tris, p0, p1, p2);
            AddTri(verts, tris, p0, p2, p3);
        }

        static Mesh Finish(List<Vector3> verts, List<int> tris, string name)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            // Cheap planar UVs; the castle is untextured but this keeps the mesh well formed.
            var uv = new Vector2[verts.Count];
            for (int i = 0; i < verts.Count; i++) uv[i] = new Vector2(verts[i].x, verts[i].z);
            mesh.uv = uv;

            return mesh;
        }
    }
}
