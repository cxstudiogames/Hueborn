// =====================================================================
//  ColorRPGIcon.cs - iconita jocului (Assets/ColorRPG/Icon/hueborn_icon.png)
//  se pune automat in Player Settings (PC + Android) la deschiderea
//  proiectului si inainte de fiecare build.
// =====================================================================
using UnityEditor;
using UnityEngine;

namespace ColorRPG.EditorTools
{
    [InitializeOnLoad]
    public static class ColorRPGIcon
    {
        public const string Path = "Assets/ColorRPG/Icon/hueborn_icon.png";

        static ColorRPGIcon() { EditorApplication.delayCall += () => Apply(false); }

        /// <summary>Seteaza iconita implicita a aplicatiei (daca nu e deja pusa).</summary>
        public static void Apply(bool log = true)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Path);
            if (tex == null) return;
            var cur = PlayerSettings.GetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, IconKind.Any);
            if (cur != null && cur.Length > 0 && cur[0] == tex) return;
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { tex }, IconKind.Any);
            AssetDatabase.SaveAssets();
            if (log) Debug.Log("[ColorRPG] App icon set: " + Path);
        }

        [MenuItem("ColorRPG/Apply Game Icon", false, 95)]
        static void Menu() { Apply(true); }
    }
}
