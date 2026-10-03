// =====================================================================
//  ColorRPGWeaponsImport.cs - setari de import pentru armele 3D CC0
//  (KayKit Fantasy Weapons Bits + Quaternius Modular Weapons) din
//  Resources/Models/Weapons: fara camere / lumini / animatii, textura max 512.
// =====================================================================
using UnityEditor;

namespace ColorRPG.EditorTools
{
    public class ColorRPGWeaponsImport : AssetPostprocessor
    {
        const string Dir = "Assets/ColorRPG/Resources/Models/Weapons";

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Dir)) return;
            var mi = (ModelImporter)assetImporter;
            mi.importCameras = false;
            mi.importLights = false;
            mi.animationType = ModelImporterAnimationType.None;
            mi.importAnimation = false;
            mi.materialSearch = ModelImporterMaterialSearch.Local;
            mi.isReadable = true;   // varfurile se citesc ca sa orientam arma automat
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Dir)) return;
            var ti = (TextureImporter)assetImporter;
            ti.maxTextureSize = 512;
            ti.mipmapEnabled = true;
        }
    }
}
