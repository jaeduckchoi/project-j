#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Editor.Art
{
    /// <summary>
    /// Authored sprite art를 경로 규칙에 따라 자동 import해 타일 정렬과 픽셀아트 표시를 일관되게 유지합니다.
    /// </summary>
    internal sealed class ArtSpriteImportPostprocessor : AssetPostprocessor
    {
        private const string ArtRoot = "Assets/Art/";
        private const string TilesRoot = ArtRoot + "Tiles/";
        private const string PropsRoot = ArtRoot + "Props/";
        private const string BuildingsRoot = ArtRoot + "Buildings/";
        private const string CharactersRoot = ArtRoot + "Characters/";
        private const string FxRoot = ArtRoot + "FX/";
        private const string UiRoot = ArtRoot + "UI/";

        private const int TilePixelsPerUnit = 64;

        public override uint GetVersion()
        {
            return 1;
        }

        private void OnPreprocessTexture()
        {
            TextureImporter importer = assetImporter as TextureImporter;
            if (importer == null)
            {
                return;
            }

            string normalizedAssetPath = NormalizeAssetPath(assetPath);
            if (!normalizedAssetPath.StartsWith(ArtRoot, StringComparison.Ordinal))
            {
                return;
            }

            ApplyBaseSpriteSettings(importer);

            if (normalizedAssetPath.StartsWith(TilesRoot, StringComparison.Ordinal))
            {
                ApplyTileSettings(importer, normalizedAssetPath);
                return;
            }

            if (normalizedAssetPath.StartsWith(PropsRoot, StringComparison.Ordinal)
                || normalizedAssetPath.StartsWith(BuildingsRoot, StringComparison.Ordinal))
            {
                ApplyWorldObjectSettings(importer);
                return;
            }

            if (normalizedAssetPath.StartsWith(CharactersRoot, StringComparison.Ordinal)
                || normalizedAssetPath.StartsWith(FxRoot, StringComparison.Ordinal))
            {
                ApplyFreeScaleSettings(importer, normalizedAssetPath);
                return;
            }

            if (normalizedAssetPath.StartsWith(UiRoot, StringComparison.Ordinal))
            {
                ApplyUiSettings(importer);
            }
        }

        private static void ApplyBaseSpriteSettings(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.mipmapEnabled = false;
        }

        private static void ApplyTileSettings(TextureImporter importer, string normalizedAssetPath)
        {
            ApplyPixelArtSpriteSettings(importer, TilePixelsPerUnit);

            if (!TryGetSourceTextureSize(importer, normalizedAssetPath, out int width, out int height))
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                return;
            }

            if (width == TilePixelsPerUnit && height == TilePixelsPerUnit)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                return;
            }

            if (!IsValidTileSheetSize(width, height))
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                LogWarning(
                    normalizedAssetPath,
                    $"Tiles 아트는 64x64 단위만 자동 슬라이스합니다. 현재 크기 {width}x{height}px 는 Grid 기준에 맞지 않아 Single로 유지합니다.");
                return;
            }

            importer.spriteImportMode = SpriteImportMode.Multiple;
            ApplyTileSlices(importer, normalizedAssetPath, width, height);
        }

        private static void ApplyWorldObjectSettings(TextureImporter importer)
        {
            ApplyPixelArtSpriteSettings(importer, TilePixelsPerUnit);
            importer.spriteImportMode = SpriteImportMode.Single;
        }

        private static void ApplyFreeScaleSettings(TextureImporter importer, string normalizedAssetPath)
        {
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            SetSpriteMeshType(importer, SpriteMeshType.FullRect);

            if (importer.spriteImportMode == SpriteImportMode.Multiple)
            {
                LogWarning(
                    normalizedAssetPath,
                    "Characters/FX 경로의 Multiple 스프라이트 시트는 프레임 폭을 알 수 없어 PPU를 자동 변경하지 않습니다.");
                return;
            }

            if (!TryGetSourceTextureSize(importer, normalizedAssetPath, out int width, out _))
            {
                return;
            }

            importer.spritePixelsPerUnit = Math.Max(1, Mathf.RoundToInt(width / 2f));
        }

        private static void ApplyUiSettings(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.mipmapEnabled = false;
        }

        private static void ApplyPixelArtSpriteSettings(TextureImporter importer, int pixelsPerUnit)
        {
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            SetSpriteMeshType(importer, SpriteMeshType.FullRect);
            importer.spritePixelsPerUnit = pixelsPerUnit;
        }

        private static void SetSpriteMeshType(TextureImporter importer, SpriteMeshType spriteMeshType)
        {
            TextureImporterSettings settings = new();
            importer.ReadTextureSettings(settings);
            if (settings.spriteMeshType == spriteMeshType)
            {
                return;
            }

            settings.spriteMeshType = spriteMeshType;
            importer.SetTextureSettings(settings);
        }

        private static bool TryGetSourceTextureSize(
            TextureImporter importer,
            string normalizedAssetPath,
            out int width,
            out int height)
        {
            width = 0;
            height = 0;

            try
            {
                importer.GetSourceTextureWidthAndHeight(out width, out height);
                return width > 0 && height > 0;
            }
            catch (Exception exception)
            {
                LogWarning(
                    normalizedAssetPath,
                    $"원본 텍스처 크기를 읽지 못해 import 규칙 일부를 건너뜁니다. {exception.GetType().Name}: {exception.Message}");
                return false;
            }
        }

        private static bool IsValidTileSheetSize(int width, int height)
        {
            return width % TilePixelsPerUnit == 0
                   && height % TilePixelsPerUnit == 0
                   && (width > TilePixelsPerUnit || height > TilePixelsPerUnit);
        }

        private static void ApplyTileSlices(
            TextureImporter importer,
            string normalizedAssetPath,
            int width,
            int height)
        {
            SpriteDataProviderFactories factories = new();
            factories.Init();

            ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
            if (dataProvider == null)
            {
                LogWarning(normalizedAssetPath, "Sprite data provider를 가져오지 못해 타일 슬라이스를 적용하지 못했습니다.");
                return;
            }

            dataProvider.InitSpriteEditorDataProvider();
            List<SpriteRect> spriteRects = BuildTileSpriteRects(normalizedAssetPath, width, height);
            dataProvider.SetSpriteRects(spriteRects.ToArray());

            ISpriteNameFileIdDataProvider nameFileIdProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            if (nameFileIdProvider != null)
            {
                List<SpriteNameFileIdPair> nameFileIdPairs = new(spriteRects.Count);
                foreach (SpriteRect spriteRect in spriteRects)
                {
                    nameFileIdPairs.Add(new SpriteNameFileIdPair(spriteRect.name, spriteRect.spriteID));
                }

                nameFileIdProvider.SetNameFileIdPairs(nameFileIdPairs);
            }

            dataProvider.Apply();
        }

        private static List<SpriteRect> BuildTileSpriteRects(string normalizedAssetPath, int width, int height)
        {
            string baseName = Path.GetFileNameWithoutExtension(normalizedAssetPath);
            int rows = height / TilePixelsPerUnit;
            int columns = width / TilePixelsPerUnit;

            List<SpriteRect> spriteRects = new(rows * columns);
            for (int row = 0; row < rows; row++)
            {
                int y = height - ((row + 1) * TilePixelsPerUnit);
                for (int column = 0; column < columns; column++)
                {
                    SpriteRect spriteRect = new()
                    {
                        name = $"{baseName}_r{row}_c{column}",
                        rect = new Rect(column * TilePixelsPerUnit, y, TilePixelsPerUnit, TilePixelsPerUnit),
                        alignment = SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                        border = Vector4.zero
                    };
                    spriteRects.Add(spriteRect);
                }
            }

            return spriteRects;
        }

        private static string NormalizeAssetPath(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : path.Replace('\\', '/');
        }

        private static void LogWarning(string normalizedAssetPath, string message)
        {
            Debug.LogWarning($"[ArtSpriteImportPostprocessor] {message}\nAsset: {normalizedAssetPath}");
        }
    }
}
#endif
