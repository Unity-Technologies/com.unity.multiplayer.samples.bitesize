using UnityEngine;
using UnityEditor;

public class EveryplayPackageImport : AssetPostprocessor
{
	void OnPreprocessTexture()
	{
		// Don't compress Everyplay textures, makes importing faster
		if (assetPath.Contains("Plugins/Everyplay"))
		{
			TextureImporter textureImporter = (TextureImporter)assetImporter;
			if (textureImporter != null)
			{
				textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
			}
		}
	}
}
