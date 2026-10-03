// =====================================================================
//  ColorRPGNaturePack.cs - pachetul de natura "Stylized Nature MegaKit"
//  (Quaternius, CC0) din Resources/Models/Megakit:
//   * texturile max 1024 (telefon), normal map-urile setate corect
//   * ColorRPG > Nature > Setup Materials: materiale URP proprii (MK_*):
//     frunze / iarba / flori cu transparenta taiata (alpha clip) si
//     doua fete, scoarta cu relief (normal map), GPU instancing
//   * Promo/naturesetup.flag  -> Setup Materials automat
//   * Promo/rebuildworld.flag -> backup + Rebuild World In Scene automat (la urmatoarea recompilare)
// =====================================================================
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ColorRPG.EditorTools
{
    public class ColorRPGNaturePackImport : AssetPostprocessor
    {
        const string Dir = "Assets/ColorRPG/Resources/Models/Megakit";

        void OnPreprocessTexture()
        {
            var ti = (TextureImporter)assetImporter;
            if (assetPath.StartsWith(Dir + "/Textures"))
            {
                ti.maxTextureSize = 1024;
                // doar *_Normal.png e normal map ("Leaves_NormalTree_C" / "Bark_NormalTree" sunt culori!)
                ti.textureType = assetPath.EndsWith("_Normal.png") ? TextureImporterType.NormalMap : TextureImporterType.Default;
                ti.mipmapEnabled = true;
            }
            else if (assetPath.StartsWith("Assets/ColorRPG/Resources/Textures/"))
            {
                ti.maxTextureSize = 512;
                ti.wrapMode = TextureWrapMode.Repeat;
                ti.anisoLevel = 4;
            }
        }

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Dir)) return;
            var mi = (ModelImporter)assetImporter;
            mi.importCameras = false;
            mi.importLights = false;
            mi.animationType = ModelImporterAnimationType.None;
            mi.importAnimation = false;
            mi.materialSearch = ModelImporterMaterialSearch.RecursiveUp;
            // Mesh LOD (Unity 6): copacii / tufele / pietrele mari primesc variante simplificate
            // pentru distanta (LOD0 = complet, LOD1 ~ 1/2, LOD2 ~ 1/4 din triunghiuri)
            if (WantsLod(assetPath)) { mi.generateMeshLods = true; mi.maximumMeshLod = MaxLod; }
        }

        public const int MaxLod = 2;

        /// <summary>Modelele care merita LOD (destule triunghiuri si multe pe harta). Pietricelele / iarba / florile nu.</summary>
        public static bool WantsLod(string path)
        {
            if (!path.StartsWith(Dir) || !path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) return false;
            string n = Path.GetFileNameWithoutExtension(path);
            return n.StartsWith("CommonTree") || n.StartsWith("TwistedTree") || n.StartsWith("Pine_") || n.StartsWith("DeadTree") ||
                   n.StartsWith("Bush_Common") || n.StartsWith("Rock_Medium") || n.StartsWith("RockPath");
        }
    }

    [InitializeOnLoad]
    public static class ColorRPGNaturePack
    {
        const string Dir = "Assets/ColorRPG/Resources/Models/Megakit";
        const string MatDir = Dir + "/Materials";
        static string SetupFlag { get { return Path.Combine(PromoDirector.Folder, "naturesetup.flag"); } }
        static string RebuildFlag { get { return Path.Combine(PromoDirector.Folder, "rebuildworld.flag"); } }

        static ColorRPGNaturePack()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
                FixTextureTypes();
                EnsureMeshLods();   // inainte de rebuild: scena noua foloseste deja plasele cu LOD
                if (File.Exists(SetupFlag)) { try { File.Delete(SetupFlag); } catch (System.Exception) { } SetupMaterials(); return; }
                if (File.Exists(RebuildFlag))
                {
                    try { File.Delete(RebuildFlag); } catch (System.Exception) { }
                    if (ColorRPGSceneBuilder.WorldFinal)
                    {
                        Debug.LogWarning("[ColorRPG] rebuildworld.flag ignored: the world is FINAL (manual edits are kept).");
                        File.WriteAllText(Path.Combine(PromoDirector.Folder, "Logs", "rebuild_result.txt"), "time: " + System.DateTime.Now + "\nSKIPPED - world is FINAL");
                        return;
                    }
                    var sc = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
                    if (!string.IsNullOrEmpty(sc.path))
                    {
                        string bdir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "_Backups", "Scenes"));
                        Directory.CreateDirectory(bdir);
                        if (sc.isDirty) UnityEditor.SceneManagement.EditorSceneManager.SaveScene(sc);
                        File.Copy(Path.GetFullPath(sc.path), Path.Combine(bdir, sc.name + "_BEFORE_NATURE_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity"), true);
                    }
                    ColorRPGSceneBuilder.Rebuild();
                    File.WriteAllText(Path.Combine(PromoDirector.Folder, "Logs", "rebuild_result.txt"), "time: " + System.DateTime.Now + "\nworld v" + GameData.WorldVersion + " rebuilt");
                }
            };
        }

        /// <summary>Reimporta (o singura data) modelele care trebuie sa aiba Mesh LOD, dar au fost importate fara.
        /// Scrie rezultatul in Promo/Logs/meshlod.txt (triunghiuri pe fiecare LOD).</summary>
        static void EnsureMeshLods()
        {
            var todo = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { Dir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!ColorRPGNaturePackImport.WantsLod(path)) continue;
                var mi = AssetImporter.GetAtPath(path) as ModelImporter;
                if (mi != null && (!mi.generateMeshLods || mi.maximumMeshLod != ColorRPGNaturePackImport.MaxLod)) todo.Add(path);
            }
            if (todo.Count == 0) return;
            AssetDatabase.StartAssetEditing();
            try { foreach (var p in todo) AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate); }
            finally { AssetDatabase.StopAssetEditing(); }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            var log = new System.Text.StringBuilder("time: " + System.DateTime.Now + "\nMesh LOD reimport: " + todo.Count + " models\n");
            foreach (var p in todo)
            {
                log.Append(Path.GetFileNameWithoutExtension(p)).Append(":");
                foreach (var m in AssetDatabase.LoadAllAssetsAtPath(p).OfType<Mesh>())
                {
                    log.Append("  ").Append(m.name).Append(" lods=").Append(m.lodCount).Append(" tris=");
                    for (int l = 0; l < m.lodCount; l++)
                    {
                        long idx = 0;
                        for (int sm = 0; sm < m.subMeshCount; sm++) idx += m.GetLod(sm, l).indexCount;
                        log.Append(l == 0 ? "" : "/").Append(idx / 3);
                    }
                    if (m.lodCount == 0) { long ic = 0; for (int sm = 0; sm < m.subMeshCount; sm++) ic += m.GetIndexCount(sm); log.Append(ic / 3); }
                }
                log.Append("\n");
            }
            Directory.CreateDirectory(Path.Combine(PromoDirector.Folder, "Logs"));
            File.WriteAllText(Path.Combine(PromoDirector.Folder, "Logs", "meshlod.txt"), log.ToString());
            Debug.Log("[ColorRPG] Mesh LOD: reimported " + todo.Count + " nature models (see Promo/Logs/meshlod.txt).");
        }

        /// <summary>Texturile de culoare importate gresit ca normal map (frunzele CommonTree apareau ca patrate albe).</summary>
        static void FixTextureTypes()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Dir + "/Textures" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null || path.EndsWith("_Normal.png") || ti.textureType != TextureImporterType.NormalMap) continue;
                ti.textureType = TextureImporterType.Default;
                ti.sRGBTexture = true;
                ti.SaveAndReimport();
                Debug.Log("[ColorRPG] Nature: fixed texture type " + path);
            }
        }

        static bool IsCutout(string texName)
        {
            string n = texName.ToLowerInvariant();
            return n.Contains("leaf") || n.Contains("leaves") || n.Contains("grass") || n.Contains("flower");
        }

        [MenuItem("ColorRPG/Nature/Setup Materials", false, 90)]
        public static void SetupMaterials()
        {
            Directory.CreateDirectory(MatDir);
            var log = new System.Text.StringBuilder("time: " + System.DateTime.Now + "\n");
            var made = new Dictionary<string, Material>();
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { Dir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mi = AssetImporter.GetAtPath(path) as ModelImporter;
                if (mi == null) continue;
                var embedded = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().ToList();
                var line = new System.Text.StringBuilder(Path.GetFileNameWithoutExtension(path) + ":");
                foreach (var src in embedded)
                {
                    Texture tex = src.HasProperty("_BaseMap") ? src.GetTexture("_BaseMap") : null;
                    if (tex == null && src.HasProperty("_MainTex")) tex = src.GetTexture("_MainTex");
                    string key = src.name + (tex != null ? "_" + tex.name : "");
                    Material mk;
                    if (!made.TryGetValue(key, out mk))
                    {
                        string mp = MatDir + "/MK_" + Clean(key) + ".mat";
                        mk = AssetDatabase.LoadAssetAtPath<Material>(mp);
                        if (mk == null) { mk = new Material(lit); AssetDatabase.CreateAsset(mk, mp); }
                        mk.shader = lit;
                        mk.name = "MK_" + Clean(key);
                        mk.SetColor("_BaseColor", Color.white);
                        if (tex != null) mk.SetTexture("_BaseMap", tex);
                        mk.SetFloat("_Smoothness", 0.08f);
                        mk.SetFloat("_Metallic", 0f);
                        string tn = tex != null ? tex.name : key;
                        // relief pe scoarta (textura *_Normal langa cea de baza)
                        var nrm = AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "/Textures/" + tn + "_Normal.png");
                        if (nrm != null) { mk.SetTexture("_BumpMap", nrm); mk.EnableKeyword("_NORMALMAP"); }
                        if (IsCutout(tn))
                        {
                            mk.SetFloat("_AlphaClip", 1f);
                            mk.SetFloat("_Cutoff", 0.2f);   // ca in pachetul original (frunzele moi nu dispar)
                            mk.EnableKeyword("_ALPHATEST_ON");
                            mk.SetFloat("_Cull", 0f);   // frunzele se vad din ambele parti
                            mk.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                            mk.SetOverrideTag("RenderType", "TransparentCutout");
                        }
                        mk.enableInstancing = true;
                        EditorUtility.SetDirty(mk);
                        made[key] = mk;
                    }
                    mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), src.name), mk);
                    line.Append(" ").Append(src.name).Append("=").Append(tex != null ? tex.name : "NO TEXTURE");
                }
                log.AppendLine(line.ToString());
                mi.SaveAndReimport();
            }
            // si materialele facute deja (a doua rulare): pragul de transparenta corect
            foreach (var g2 in AssetDatabase.FindAssets("t:Material", new[] { MatDir }))
            {
                var m2 = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g2));
                if (m2 != null && m2.HasProperty("_AlphaClip") && m2.GetFloat("_AlphaClip") > 0.5f) { m2.SetFloat("_Cutoff", 0.2f); EditorUtility.SetDirty(m2); }
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory(Path.Combine(PromoDirector.Folder, "Logs"));
            File.WriteAllText(Path.Combine(PromoDirector.Folder, "Logs", "nature_setup.txt"), log.ToString());
            Debug.Log("[ColorRPG] Nature pack: " + made.Count + " materials set up");
        }

        static string Clean(string n)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char c in n) sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
            return sb.ToString();
        }
    }
}
