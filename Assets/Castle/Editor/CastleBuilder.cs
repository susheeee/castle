using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace CastleGen
{
    /// <summary>
    /// Procedurally builds the block-castle scene: terraced grass base, curtain walls with
    /// crenellations, round towers with cone roofs, a gatehouse with a voussoir arch, the
    /// central keep under a big pyramid roof, outbuildings, hedges, path and scatter rocks.
    /// </summary>
    public static class CastleBuilder
    {
        const string Root = "Assets/Castle";
        const string MatDir = Root + "/Materials";
        const string MeshDir = Root + "/Meshes";
        const string ScenePath = "Assets/Scenes/Castle.unity";

        static readonly Dictionary<string, Material> Mats = new Dictionary<string, Material>();
        static readonly Dictionary<string, Mesh> Meshes = new Dictionary<string, Mesh>();
        static System.Random _rng;

        [MenuItem("Castle/Build Castle Scene", false, 0)]
        public static void BuildMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
        }

        public static void Build()
        {
            _rng = new System.Random(20260908);
            Mats.Clear();
            Meshes.Clear();

            EnsureFolder("Assets", "Castle");
            EnsureFolder(Root, "Materials");
            EnsureFolder(Root, "Meshes");
            EnsureFolder("Assets", "Scenes");

            CreateMaterials();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetUpEnvironment();

            var castle = new GameObject("Castle").transform;

            BuildTerrain(castle);
            BuildOuterWalls(castle);
            BuildTowers(castle);
            BuildGatehouse(castle);
            BuildKeep(castle);
            BuildOutbuildings(castle);
            BuildPathAndHedges(castle);
            BuildRocks(castle);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();

            Debug.Log("Castle built: " + ScenePath + " (" +
                      castle.GetComponentsInChildren<MeshRenderer>().Length + " parts)");
        }

        // ------------------------------------------------------------------ setup

        static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
                AssetDatabase.CreateFolder(parent, child);
        }

        static void CreateMaterials()
        {
            M("StoneA", 0xAD, 0xAE, 0xBC);
            M("StoneB", 0xBE, 0xBF, 0xCB);
            M("StoneC", 0x9B, 0x9D, 0xAD);
            M("StoneLight", 0xCF, 0xD0, 0xDA);
            M("Brown", 0x8C, 0x5C, 0x3E);
            M("BrownDark", 0x7B, 0x4A, 0x32);
            M("Orange", 0xE0, 0x8A, 0x5A);
            M("OrangeDark", 0xC9, 0x70, 0x4A);
            M("Cream", 0xF0, 0xE3, 0xC6);
            M("CreamDark", 0xDF, 0xCE, 0xAB);
            M("Grass", 0x63, 0xB6, 0x40);
            M("GrassDark", 0x51, 0x9C, 0x33);
            M("GrassDeep", 0x40, 0x81, 0x28);
            M("Wood", 0x8A, 0x4A, 0x2E);
            M("Rock", 0xB0, 0xB2, 0xBC);
            M("Dark", 0x3C, 0x3A, 0x42);
            M("PathOrange", 0xE0, 0x8A, 0x4A);
            M("PathTan", 0xE8, 0xB4, 0x5A);
            M("PathBrown", 0x8B, 0x5A, 0x3A);
            M("Flag", 0xB5, 0x53, 0x3A);
        }

        static void M(string name, int r, int g, int b)
        {
            string path = MatDir + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }

            var c = new Color(r / 255f, g / 255f, b / 255f, 1f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.06f);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.06f);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(mat);
            Mats[name] = mat;
        }

        static Material Mat(string name) { return Mats[name]; }

        static void SetUpEnvironment()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.86f, 0.88f, 0.93f);
            RenderSettings.ambientEquatorColor = new Color(0.70f, 0.71f, 0.75f);
            RenderSettings.ambientGroundColor = new Color(0.50f, 0.51f, 0.54f);
            RenderSettings.fog = false;

            var sunGo = new GameObject("Sun");
            sunGo.transform.rotation = Quaternion.Euler(46f, -34f, 0f);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.97f, 0.92f);
            sun.intensity = 1.5f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.55f;
            sun.shadowBias = 0.02f;
            RenderSettings.sun = sun;

            var fillGo = new GameObject("Fill Light");
            fillGo.transform.rotation = Quaternion.Euler(24f, 150f, 0f);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.85f, 0.89f, 1f);
            fill.intensity = 0.35f;
            fill.shadows = LightShadows.None;

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(-42f, 30f, -54f);
            var look = new Vector3(1.5f, 5.5f, 0.5f) - camGo.transform.position;
            camGo.transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.93f, 0.93f, 0.94f);
            cam.fieldOfView = 30f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 400f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<CastleOrbitCamera>();
        }

        // ------------------------------------------------------- geometry helpers

        static GameObject Attach(GameObject go, Transform parent, string name, Material mat)
        {
            go.name = name;
            go.transform.SetParent(parent, true);
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.isStatic = true;
            return go;
        }

        /// <summary>Axis-aligned box given its centre and full size.</summary>
        static GameObject Box(Transform parent, string name, Vector3 centre, Vector3 size, Material mat, float yaw)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = centre;
            go.transform.localScale = size;
            if (!Mathf.Approximately(yaw, 0f)) go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            return Attach(go, parent, name, mat);
        }

        static GameObject Box(Transform parent, string name, Vector3 centre, Vector3 size, Material mat)
        {
            return Box(parent, name, centre, size, mat, 0f);
        }

        /// <summary>Box specified by its extents, the way walls and floors are laid out.</summary>
        static GameObject BoxFromTo(Transform parent, string name,
            float x0, float x1, float y0, float y1, float z0, float z1, Material mat)
        {
            return Box(parent, name,
                new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, (z0 + z1) * 0.5f),
                new Vector3(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0), Mathf.Abs(z1 - z0)), mat);
        }

        static GameObject Cylinder(Transform parent, string name, Vector3 baseCentre, float radius, float height, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.transform.position = baseCentre + new Vector3(0f, height * 0.5f, 0f);
            go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            return Attach(go, parent, name, mat);
        }

        static Mesh CachedMesh(string key, System.Func<Mesh> factory)
        {
            Mesh m;
            if (Meshes.TryGetValue(key, out m)) return m;

            string path = MeshDir + "/" + key + ".asset";
            m = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (m == null)
            {
                m = factory();
                AssetDatabase.CreateAsset(m, path);
            }
            Meshes[key] = m;
            return m;
        }

        static GameObject Custom(Transform parent, string name, Mesh mesh, Vector3 pos, Material mat, float yaw)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            if (!Mathf.Approximately(yaw, 0f)) go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            go.isStatic = true;
            return go;
        }

        static GameObject Cone(Transform parent, string name, Vector3 baseCentre, float radius, float height, Material mat)
        {
            string key = string.Format("cone_{0:0.00}_{1:0.00}", radius, height);
            var mesh = CachedMesh(key, delegate { return CastleMeshFactory.Cone(radius, height); });
            return Custom(parent, name, mesh, baseCentre, mat, 0f);
        }

        static GameObject Pyramid(Transform parent, string name, Vector3 baseCentre, float w, float d, float h, Material mat)
        {
            string key = string.Format("pyr_{0:0.00}_{1:0.00}_{2:0.00}", w, d, h);
            var mesh = CachedMesh(key, delegate { return CastleMeshFactory.Pyramid(w, d, h); });
            return Custom(parent, name, mesh, baseCentre, mat, 0f);
        }

        static GameObject Gable(Transform parent, string name, Vector3 baseCentre, float length, float width, float h, Material mat, float yaw)
        {
            string key = string.Format("gable_{0:0.00}_{1:0.00}_{2:0.00}", length, width, h);
            var mesh = CachedMesh(key, delegate { return CastleMeshFactory.GableRoof(length, width, h); });
            return Custom(parent, name, mesh, baseCentre, mat, yaw);
        }

        static float Rand(float a, float b) { return a + (float)_rng.NextDouble() * (b - a); }

        static Material RandStone()
        {
            double r = _rng.NextDouble();
            if (r < 0.34) return Mat("StoneA");
            if (r < 0.68) return Mat("StoneB");
            return Mat("StoneC");
        }

        // ------------------------------------------------------------------ walls

        const float WallSeam = 0.045f;

        /// <summary>
        /// Lays a curtain wall between two centreline points as staggered courses of
        /// individual blocks, optionally topped with merlons.
        /// </summary>
        static void WallRun(Transform parent, string name, Vector3 a, Vector3 b,
            float thickness, float height, bool crenellate, Material tint)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);

            Vector3 dir = b - a;
            float length = dir.magnitude;
            if (length < 0.01f) return;
            dir /= length;
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;

            int courses = Mathf.Max(1, Mathf.RoundToInt(height / 1.75f));
            float courseH = height / courses;

            for (int c = 0; c < courses; c++)
            {
                float y0 = c * courseH;
                float t = 0f;
                bool stagger = (c % 2) == 1;
                while (t < length - 0.01f)
                {
                    float seg = Rand(2.2f, 3.8f);
                    if (t < 0.001f && stagger) seg *= 0.55f;
                    seg = Mathf.Min(seg, length - t);
                    if (length - (t + seg) < 0.9f) seg = length - t; // avoid slivers

                    Vector3 mid = a + dir * (t + seg * 0.5f) + new Vector3(0f, y0 + courseH * 0.5f, 0f);
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.transform.position = mid;
                    go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                    go.transform.localScale = new Vector3(thickness, courseH - WallSeam, seg - WallSeam);
                    Attach(go, group, "Block", tint != null ? tint : RandStone());

                    t += seg;
                }
            }

            if (crenellate) Crenellations(group, a, b, dir, thickness, height);
        }

        static void Crenellations(Transform parent, Vector3 a, Vector3 b, Vector3 dir, float thickness, float height)
        {
            float length = (b - a).magnitude;
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            const float merlon = 0.95f, gap = 0.8f, mh = 0.85f;
            float pitch = merlon + gap;
            int count = Mathf.Max(1, Mathf.FloorToInt((length + gap) / pitch));
            float used = count * pitch - gap;
            float start = (length - used) * 0.5f;

            for (int i = 0; i < count; i++)
            {
                float t = start + i * pitch + merlon * 0.5f;
                Vector3 mid = a + dir * t + new Vector3(0f, height + mh * 0.5f, 0f);
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.position = mid;
                go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                go.transform.localScale = new Vector3(thickness * 0.92f, mh, merlon);
                Attach(go, parent, "Merlon", RandStone());
            }
        }

        /// <summary>Window slit: a dark panel sitting just proud of a wall face.</summary>
        static void Window(Transform parent, Vector3 faceCentre, Vector3 normal, float w, float h)
        {
            var size = new Vector3(
                Mathf.Abs(normal.x) > 0.5f ? 0.12f : w,
                h,
                Mathf.Abs(normal.z) > 0.5f ? 0.12f : w);
            Box(parent, "Window", faceCentre + normal * 0.03f, size, Mat("Dark"));
        }

        static void Stairs(Transform parent, string name, Vector3 bottomCentre, Vector3 dir,
            float width, float run, float rise, int steps, Material mat)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            float stepRun = run / steps, stepRise = rise / steps;

            for (int i = 0; i < steps; i++)
            {
                float h = stepRise * (i + 1);
                Vector3 c = bottomCentre + dir * (stepRun * (i + 0.5f)) + new Vector3(0f, h * 0.5f, 0f);
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.position = c;
                go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                go.transform.localScale = new Vector3(width, h, stepRun);
                Attach(go, group, "Step", mat);
            }
        }

        // -------------------------------------------------------------- the build

        // Curtain wall footprint.
        const float WX = 15f;   // outer wall extent in x, either side of the origin
        const float WZ = 10f;   // outer wall extent in z
        const float WT = 1.8f;  // wall thickness
        const float GateX = -1.8f, GateHalf = 3.2f;

        /// <summary>Top surface height of the grass terrace under a given z.</summary>
        static float GroundY(float z)
        {
            if (z > -24f) return 0f;
            if (z > -28f) return -0.55f;
            return -1.3f;
        }

        static void BuildTerrain(Transform parent)
        {
            var g = new GameObject("Terrain").transform;
            g.SetParent(parent, false);

            // Three grass terraces, largest and lowest first, with a lawn in front of the gate.
            BoxFromTo(g, "Terrace_Low", -31f, 34f, -2.6f, -1.3f, -32f, 24f, Mat("GrassDeep"));
            BoxFromTo(g, "Terrace_Mid", -27f, 30f, -1.3f, -0.55f, -28f, 21f, Mat("GrassDark"));
            BoxFromTo(g, "Terrace_Top", -23f, 26f, -0.55f, 0f, -24f, 18f, Mat("Grass"));

            // Courtyard floor inside the curtain wall.
            BoxFromTo(g, "Courtyard", -WX + WT * 0.4f, WX - WT * 0.4f, -0.1f, 0.12f,
                -WZ + WT * 0.4f, WZ - WT * 0.4f, Mat("StoneLight"));

            // Raised plinth the keep stands on.
            BoxFromTo(g, "Keep_Plinth", -7.5f, 9f, 0.12f, 3.2f, -1.5f, 8.6f, Mat("StoneA"));
            BoxFromTo(g, "Keep_Plinth_Trim", -7.5f, 9f, 3.2f, 3.5f, -1.5f, 8.6f, Mat("StoneB"));

            // Stair up to the plinth, on the east side of the keep.
            Stairs(g, "Keep_Stairs", new Vector3(11.1f, 0.12f, 1.4f), Vector3.forward, 2.3f, 5.2f, 3.38f, 11, Mat("StoneLight"));
            BoxFromTo(g, "Stair_Landing", 9f, 12.3f, 0.12f, 3.5f, 6.6f, 8.6f, Mat("StoneB"));
        }

        static void BuildOuterWalls(Transform parent)
        {
            var g = new GameObject("Curtain Walls").transform;
            g.SetParent(parent, false);

            float fz = -WZ + WT * 0.5f;
            float bz = WZ - WT * 0.5f;
            float lx = -WX + WT * 0.5f;
            float rx = WX - WT * 0.5f;

            // Front wall, split around the gatehouse.
            WallRun(g, "Front_West", new Vector3(lx, 0f, fz), new Vector3(GateX - GateHalf, 0f, fz), WT, 5.0f, true, null);
            WallRun(g, "Front_East", new Vector3(GateX + GateHalf, 0f, fz), new Vector3(rx, 0f, fz), WT, 4.4f, true, null);

            // Sides and back. The east run is lower and lighter, as in the reference.
            WallRun(g, "West", new Vector3(lx, 0f, fz), new Vector3(lx, 0f, bz), WT, 4.8f, true, null);
            WallRun(g, "East", new Vector3(rx, 0f, fz), new Vector3(rx, 0f, bz), WT, 4.2f, true, Mat("StoneLight"));
            WallRun(g, "Back", new Vector3(lx, 0f, bz), new Vector3(rx, 0f, bz), WT, 4.8f, true, null);

            // Wall-walk along the inner face of the east wall, reached by the stair.
            BoxFromTo(g, "East_Walk", rx - WT * 0.5f - 1.5f, rx - WT * 0.5f, 3.2f, 3.5f, -8f, 8.6f, Mat("StoneLight"));
        }

        static void BuildTowers(Transform parent)
        {
            var g = new GameObject("Towers").transform;
            g.SetParent(parent, false);

            Tower(g, "Tower_FrontWest", -15f, -10f, 1.6f, 9.4f);
            Tower(g, "Tower_GateWest", -5.8f, -9.6f, 1.15f, 8.2f);
            Tower(g, "Tower_GateEast", 2.4f, -9.6f, 1.35f, 9.0f);
            Tower(g, "Tower_FrontEast", 10.8f, -9.6f, 1.3f, 9.8f);
            Tower(g, "Tower_East", 15f, 2.0f, 1.15f, 7.8f);
            Tower(g, "Tower_West", -15f, 6.5f, 1.1f, 7.6f);
            Tower(g, "Tower_BackWest", -6.0f, 9.8f, 1.05f, 7.4f);
        }

        static void Tower(Transform parent, string name, float x, float z, float radius, float height)
        {
            var g = new GameObject(name).transform;
            g.SetParent(parent, false);
            var basePos = new Vector3(x, -0.4f, z);

            Cylinder(g, "Shaft", basePos, radius, height, Mat("Brown"));
            Cylinder(g, "Corbel", basePos + new Vector3(0f, height - 0.35f, 0f), radius * 1.1f, 0.45f, Mat("BrownDark"));
            Cone(g, "Roof", basePos + new Vector3(0f, height + 0.05f, 0f), radius * 1.28f, radius * 2.95f, Mat("BrownDark"));
        }

        static void BuildGatehouse(Transform parent)
        {
            var g = new GameObject("Gatehouse").transform;
            g.SetParent(parent, false);

            float x0 = GateX - GateHalf, x1 = GateX + GateHalf;
            float z0 = -WZ - 0.3f, z1 = -WZ + 3.9f;
            const float top = 8.2f;
            const float openHalf = 1.45f;   // half width of the passage
            const float springer = 2.5f;    // height the arch springs from

            // Piers either side of the passage, built as courses.
            for (int c = 0; c < 5; c++)
            {
                float y0 = c * 1.64f;
                if (y0 >= top) break;
                float y1 = Mathf.Min(top, y0 + 1.64f);
                BoxFromTo(g, "Pier_West", x0, GateX - openHalf, y0, y1 - WallSeam, z0, z1, RandStone());
                BoxFromTo(g, "Pier_East", GateX + openHalf, x1, y0, y1 - WallSeam, z0, z1, RandStone());
            }

            // Spandrel above the arch.
            BoxFromTo(g, "Lintel", GateX - openHalf, GateX + openHalf, springer + openHalf + 0.35f, top, z0, z1, Mat("StoneB"));

            // Dark passage interior, so the gate reads as an opening.
            BoxFromTo(g, "Passage", GateX - openHalf, GateX + openHalf, 0f, springer + openHalf, z0 + 0.45f, z1 - 0.45f, Mat("Dark"));

            // Voussoirs: cream blocks swept around the arch on both faces of the passage.
            const int voussoirs = 9;
            for (int face = 0; face < 2; face++)
            {
                float fz = face == 0 ? z0 + 0.24f : z1 - 0.24f;
                for (int i = 0; i < voussoirs; i++)
                {
                    float a = Mathf.PI * (i + 0.5f) / voussoirs;
                    const float r = openHalf + 0.3f;
                    var pos = new Vector3(GateX - Mathf.Cos(a) * r, springer + Mathf.Sin(a) * r, fz);
                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.transform.position = pos;
                    go.transform.rotation = Quaternion.Euler(0f, 0f, 90f - a * Mathf.Rad2Deg);
                    go.transform.localScale = new Vector3(0.78f, 0.68f, 0.52f);
                    Attach(go, g, "Voussoir", i % 2 == 0 ? Mat("Cream") : Mat("CreamDark"));
                }

                // Imposts the arch springs from.
                Box(g, "Impost", new Vector3(GateX - openHalf - 0.15f, springer * 0.5f, fz), new Vector3(0.68f, springer, 0.52f), Mat("Cream"));
                Box(g, "Impost", new Vector3(GateX + openHalf + 0.15f, springer * 0.5f, fz), new Vector3(0.68f, springer, 0.52f), Mat("Cream"));
            }

            // Window slits above the gate.
            for (int i = -1; i <= 1; i++)
                Window(g, new Vector3(GateX + i * 1.55f, 5.6f, z0), Vector3.back, 0.55f, 0.55f);

            float mz = (z0 + z1) * 0.5f;
            Crenellations(g, new Vector3(x0, 0f, mz), new Vector3(x1, 0f, mz), Vector3.right, z1 - z0 - 0.5f, top);
        }

        static void BuildKeep(Transform parent)
        {
            var g = new GameObject("Keep").transform;
            g.SetParent(parent, false);

            const float plinth = 3.5f;
            const float x0 = -5.6f, x1 = 4.6f, z0 = 0.2f, z1 = 7.6f;

            // Main mass, laid up as courses of blocks so the seams show.
            float y = plinth;
            int course = 0;
            while (y < plinth + 7.6f)
            {
                float h = Mathf.Min(1.9f, plinth + 7.6f - y);
                float split = Mathf.Lerp(x0, x1, course % 2 == 0 ? 0.38f : 0.62f);
                BoxFromTo(g, "Keep_Block", x0, split, y, y + h - WallSeam, z0, z1, RandStone());
                BoxFromTo(g, "Keep_Block", split, x1, y, y + h - WallSeam, z0, z1, RandStone());
                y += h;
                course++;
            }

            // Recessed upper storey and the big pyramid roof.
            BoxFromTo(g, "Keep_Upper", x0 + 0.8f, x1 - 0.8f, y, y + 2.4f, z0 + 0.7f, z1 - 0.7f, Mat("StoneB"));
            float roofBase = y + 2.4f;
            Pyramid(g, "Keep_Roof", new Vector3((x0 + x1) * 0.5f, roofBase, (z0 + z1) * 0.5f),
                (x1 - x0) + 0.9f, (z1 - z0) + 0.9f, 6.4f, Mat("Orange"));

            // Window band on the front face of the keep.
            for (int i = -1; i <= 1; i++)
                Window(g, new Vector3(-0.5f + i * 1.5f, plinth + 4.9f, z0), Vector3.back, 0.6f, 0.6f);

            // Lower east wing.
            BoxFromTo(g, "Wing_Lower", x1, 8.6f, plinth, plinth + 2.6f, z0 + 0.6f, z1 - 1.4f, Mat("StoneA"));
            BoxFromTo(g, "Wing_Upper", x1, 8.2f, plinth + 2.6f, plinth + 4.6f, z0 + 0.6f, z1 - 1.9f, Mat("StoneB"));
            for (int i = 0; i < 3; i++)
                Window(g, new Vector3(x1 + 0.9f + i * 1.15f, plinth + 3.6f, z0 + 0.6f), Vector3.back, 0.45f, 0.55f);

            // West shoulder block.
            BoxFromTo(g, "Shoulder_West", -7.4f, x0, plinth, plinth + 3.4f, z0 + 0.4f, z1 - 1.2f, Mat("StoneA"));

            // Chimneys and turrets poking through the roof line.
            Cylinder(g, "Chimney_Cream", new Vector3(-4.4f, roofBase - 2.6f, 6.6f), 0.72f, 6.4f, Mat("Cream"));
            Cylinder(g, "Chimney_Orange", new Vector3(3.4f, roofBase - 1.4f, 5.4f), 0.62f, 4.4f, Mat("Orange"));
            Cylinder(g, "Chimney_Small", new Vector3(6.4f, plinth + 4.6f, 3.4f), 0.62f, 3.2f, Mat("Cream"));

            // Flag tower rising behind the roof.
            var f = new GameObject("Flag Tower").transform;
            f.SetParent(g, false);
            const float tx0 = 4.8f, tx1 = 7.2f, tz0 = 4.6f, tz1 = 7.0f;
            for (int i = 0; i < 5; i++)
            {
                float ty = plinth + i * 2.6f;
                BoxFromTo(f, "Shaft", tx0, tx1, ty, ty + 2.6f - WallSeam, tz0, tz1, RandStone());
            }
            float capY = plinth + 5 * 2.6f;
            BoxFromTo(f, "Cap", tx0, tx1, capY, capY + 2.7f, tz0, tz1, Mat("Orange"));
            BoxFromTo(f, "Cap_Top", tx0 + 0.35f, tx1 - 0.35f, capY + 2.7f, capY + 3.9f, tz0 + 0.35f, tz1 - 0.35f, Mat("OrangeDark"));
            Cylinder(f, "Pole", new Vector3((tx0 + tx1) * 0.5f, capY + 3.9f, (tz0 + tz1) * 0.5f), 0.085f, 3.6f, Mat("Cream"));
            Box(f, "Banner", new Vector3((tx0 + tx1) * 0.5f + 0.95f, capY + 6.3f, (tz0 + tz1) * 0.5f), new Vector3(1.7f, 1.05f, 0.09f), Mat("Flag"));
        }

        static void BuildOutbuildings(Transform parent)
        {
            var g = new GameObject("Outbuildings").transform;
            g.SetParent(parent, false);

            // Cream house tucked against the west wall, its roof clearing the parapet.
            House(g, "House_West", new Vector3(-10.6f, 0.12f, 5.4f), 5.2f, 4.0f, 4.4f);

            // Walled yard on the grass east of the castle, clear of the curtain wall.
            var yard = new GameObject("East Yard").transform;
            yard.SetParent(g, false);
            House(yard, "House_East", new Vector3(19f, 0f, -5.6f), 4.6f, 3.8f, 3.6f);

            const float fh = 1.55f, ft = 0.6f;
            BoxFromTo(yard, "Fence_South", 16f, 25f, 0f, fh, -8f, -8f + ft, Mat("Wood"));
            BoxFromTo(yard, "Fence_East", 25f - ft, 25f, 0f, fh, -8f, 2f, Mat("Wood"));
            BoxFromTo(yard, "Fence_North", 16f, 25f, 0f, fh, 2f - ft, 2f, Mat("Wood"));
            BoxFromTo(yard, "Fence_West", 16f, 16f + ft, 0f, fh, -8f, -3.2f, Mat("Wood"));
            BoxFromTo(yard, "Store", 19.6f, 24.2f, 0f, 2.2f, -1.8f, 1.2f, Mat("Cream"));
            BoxFromTo(yard, "Store_Cap", 19.4f, 24.4f, 2.2f, 2.55f, -2f, 1.4f, Mat("CreamDark"));
        }

        static void House(Transform parent, string name, Vector3 basePos, float w, float d, float wallH)
        {
            var g = new GameObject(name).transform;
            g.SetParent(parent, false);

            Box(g, "Walls", basePos + new Vector3(0f, wallH * 0.5f, 0f), new Vector3(w, wallH, d), Mat("Cream"));
            Gable(g, "Roof", basePos + new Vector3(0f, wallH, 0f), w + 0.7f, d + 0.7f, d * 0.55f, Mat("Orange"), 0f);
            Window(g, basePos + new Vector3(-w * 0.24f, wallH * 0.58f, -d * 0.5f), Vector3.back, 0.6f, 0.6f);
            Window(g, basePos + new Vector3(w * 0.24f, wallH * 0.58f, -d * 0.5f), Vector3.back, 0.6f, 0.6f);
            Box(g, "Door", basePos + new Vector3(0f, 0.75f, -d * 0.5f - 0.03f), new Vector3(0.85f, 1.5f, 0.12f), Mat("Wood"));
        }

        static void BuildPathAndHedges(Transform parent)
        {
            var g = new GameObject("Grounds").transform;
            g.SetParent(parent, false);

            // Approach path, stepping down each terrace as it leaves the gate.
            var pathMats = new[] { Mat("PathOrange"), Mat("PathTan"), Mat("PathBrown"), Mat("PathTan") };
            const float depth = 2.1f;
            for (int i = 0; i < 9; i++)
            {
                float z = -WZ - 1.2f - depth * (i + 0.5f);
                float w = 3.4f + i * 0.42f;
                Box(g, "Path_" + i, new Vector3(GateX - i * 0.3f, GroundY(z) + 0.09f, z),
                    new Vector3(w, 0.18f, depth), pathMats[i % pathMats.Length]);
            }

            // Clipped hedge maze on the front lawn, east of the path.
            var hedge = new GameObject("Hedges").transform;
            hedge.SetParent(g, false);
            var plan = new[]
            {
                "XXXXXXXXX",
                "X...X...X",
                "X.XXX.X.X",
                "X.....X.X",
                "XXX.XXX.X",
                "X.......X",
            };
            const float cell = 1.5f;
            const float ox = 6f, oz = -22f;
            for (int r = 0; r < plan.Length; r++)
            {
                for (int c = 0; c < plan[r].Length; c++)
                {
                    if (plan[r][c] != 'X') continue;
                    Box(hedge, "Hedge", new Vector3(ox + c * cell, 0.5f, oz + r * cell),
                        new Vector3(cell * 0.96f, 1.05f, cell * 0.96f), Mat("GrassDark"));
                }
            }
        }

        static void BuildRocks(Transform parent)
        {
            var g = new GameObject("Rocks").transform;
            g.SetParent(parent, false);

            // Rocky skirt hugging the rim of the lower terraces, never on the top lawn.
            for (int i = 0; i < 130; i++)
            {
                float x, z;
                if (_rng.NextDouble() < 0.5)
                {
                    x = _rng.NextDouble() < 0.45 ? Rand(-33f, -23f) : Rand(24f, 35f);
                    z = Rand(-33f, 24f);
                }
                else
                {
                    x = Rand(-33f, 35f);
                    z = _rng.NextDouble() < 0.6 ? Rand(-34f, -23f) : Rand(18f, 25f);
                }

                // Only the outermost terrace and the ground beyond it get rocks.
                if (x > -27f && x < 30f && z > -28f && z < 21f) continue;

                bool onLow = x > -31f && x < 34f && z > -32f && z < 24f;
                float top = onLow ? -1.3f : -2.6f;

                float s = Rand(0.85f, 2.3f);
                var size = new Vector3(s * Rand(0.75f, 1.35f), s * Rand(0.6f, 1.5f), s * Rand(0.75f, 1.35f));
                var pos = new Vector3(x, top - Rand(0.35f, 1.1f) + size.y * 0.5f, z);

                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.position = pos;
                go.transform.rotation = Quaternion.Euler(Rand(-9f, 9f), Rand(0f, 360f), Rand(-9f, 9f));
                go.transform.localScale = size;
                Attach(go, g, "Rock", Mat("Rock"));
            }
        }
    }
}
