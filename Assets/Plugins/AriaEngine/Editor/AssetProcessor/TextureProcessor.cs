using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AriaEditor
{
	class TextureProcessor : AssetPostprocessor
	{

		void OnPreprocessTexture()
		{
			if (!assetImporter.assetPath.StartsWith("Assets/") || !assetImporter.importSettingsMissing)
				return;
			if (assetImporter is not TextureImporter textureImporter)
				return;
			ImportSetting(textureImporter);
		}

		[MenuItem("Assets/AriaEngine/Reimport/Texture")]
		public static void ReimportTexutre()
		{
			var textures = Selection.GetFiltered<Texture>(SelectionMode.DeepAssets);
			if (textures == null || textures.Length <= 0)
				return;
			AssetDatabase.StartAssetEditing();
			try {
				foreach (var texture in textures) {
					var path = AssetDatabase.GetAssetPath(texture);
					var importer = TextureImporter.GetAtPath(path);
					if (importer is not TextureImporter textureImporter)
						continue;
					ImportSetting(textureImporter);
					AssetDatabase.SaveAssetIfDirty(importer);
				}
			}
			finally {
				AssetDatabase.StopAssetEditing();
				AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ImportRecursive);
			}
		}

		static void ImportSetting(TextureImporter textureImporter)
		{
			if (textureImporter == null)
				return;
			switch (textureImporter.textureType) {
				case TextureImporterType.Default:
				case TextureImporterType.Sprite:
				case TextureImporterType.Cursor:
					ImportDefault(textureImporter);
					break;
				case TextureImporterType.NormalMap:
					ImportNormalMap(textureImporter);
					break;
				case TextureImporterType.SingleChannel:
					break;
			}
		}

		static void ImportDefault(TextureImporter textureImporter)
		{
			PlatformSetting(textureImporter, "Standalone", TextureImporterFormat.BC7);
			PlatformSetting(textureImporter, "Android", TextureImporterFormat.ASTC_6x6);
			PlatformSetting(textureImporter, "iOS", TextureImporterFormat.ASTC_6x6);
		}

		static void ImportNormalMap(TextureImporter textureImporter)
		{
			PlatformSetting(textureImporter, "Standalone", TextureImporterFormat.BC5);
			PlatformSetting(textureImporter, "Android", TextureImporterFormat.ASTC_4x4);
			PlatformSetting(textureImporter, "iOS", TextureImporterFormat.ASTC_4x4);
		}

		static void PlatformSetting(TextureImporter textureImporter, string platformName, TextureImporterFormat format)
		{
			TextureImporterPlatformSettings platform = textureImporter.GetPlatformTextureSettings(platformName);
			platform.overridden = true;
			platform.format = format;
			platform.textureCompression = TextureImporterCompression.CompressedHQ;
			platform.compressionQuality = 100;
			textureImporter.SetPlatformTextureSettings(platform);
		}
	}
}
