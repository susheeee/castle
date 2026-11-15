using System.IO;
using UnityEditor;
using UnityEngine;

namespace CastleGen
{
    /// <summary>
    /// Runs the castle build once, the first time the editor compiles after a
    /// "BuildCastle.flag" file appears in the project root. The flag is deleted up front
    /// so the build never repeats on later domain reloads. Rebuild any time from
    /// Castle > Build Castle Scene.
    /// </summary>
    [InitializeOnLoad]
    static class CastleAutoBuild
    {
        static string FlagPath
        {
            get { return Path.Combine(Directory.GetParent(Application.dataPath).FullName, "BuildCastle.flag"); }
        }

        static CastleAutoBuild()
        {
            if (!File.Exists(FlagPath)) return;

            try { File.Delete(FlagPath); }
            catch (IOException) { return; }

            EditorApplication.delayCall += () =>
            {
                Debug.Log("BuildCastle.flag found - building the castle scene.");
                CastleBuilder.BuildMenu(); // prompts to save the open scene first
            };
        }
    }
}
