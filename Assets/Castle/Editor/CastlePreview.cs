using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CastleGen
{
    /// <summary>
    /// Renders the scene camera to a PNG. Handy for eyeballing the castle without
    /// entering play mode, and used by the batch-mode build to produce a preview.
    /// </summary>
    public static class CastlePreview
    {
        [MenuItem("Castle/Render Preview PNG", false, 20)]
        public static void RenderMenu()
        {
            string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "castle_preview.png");
            Render(path, 1920, 1080);
            Debug.Log("Preview written to " + path);
            EditorUtility.RevealInFinder(path);
        }

        public static void Render(string path, int width, int height)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("No camera tagged MainCamera in the scene.");
                return;
            }

            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 8
            };

            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;

            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;

            File.WriteAllBytes(path, tex.EncodeToPNG());

            UnityEngine.Object.DestroyImmediate(tex);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }

        /// <summary>
        /// Batch entry point: -executeMethod CastleGen.CastlePreview.BuildAndRender
        /// Optional args: -castleOut &lt;png path&gt; -castleView &lt;x,y,z,tx,ty,tz&gt;
        /// </summary>
        public static void BuildAndRender()
        {
            string outPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "castle_preview.png");
            string view = null;
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-castleOut") outPath = args[i + 1];
                if (args[i] == "-castleView") view = args[i + 1];
            }

            CastleBuilder.Build();

            if (!string.IsNullOrEmpty(view))
            {
                var p = view.Split(',');
                if (p.Length == 6)
                {
                    var cam = Camera.main;
                    var pos = new Vector3(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]));
                    var tgt = new Vector3(float.Parse(p[3]), float.Parse(p[4]), float.Parse(p[5]));
                    cam.transform.position = pos;
                    cam.transform.rotation = Quaternion.LookRotation((tgt - pos).normalized, Vector3.up);
                }
            }

            Render(outPath, 1920, 1080);
            Debug.Log("Preview written to " + outPath);
        }
    }
}
