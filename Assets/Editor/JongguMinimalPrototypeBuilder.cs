#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using CoreLoop.Core;
using CoreLoop.Flow;
using Exploration.Camera;
using Exploration.Gathering;
using Exploration.Interaction;
using Exploration.Player;
using Exploration.World;
using Management.Economy;
using Management.Inventory;
using Management.Storage;
using Management.Tools;
using Management.Upgrade;
using Restaurant;
using Shared;
using Shared.Data;
using TMPro;
using UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;
#if ENABLE_INPUT_SYSTEM
#endif

// ProjectEditor 네임스페이스
namespace Editor
{
    public static partial class JongguMinimalPrototypeBuilder
    {
        private static PrototypeGeneratedAssetSettings AssetSettings => PrototypeGeneratedAssetSettings.GetCurrent();
        private static string GeneratedRoot => AssetSettings.ResourcesGeneratedRoot;
        private static string GameDataRoot => AssetSettings.GameDataRoot;
        private static string ResourceDataRoot => AssetSettings.ResourceDataRoot;
        private static string RecipeDataRoot => AssetSettings.RecipeDataRoot;
        private static string InputDataRoot => AssetSettings.InputDataRoot;
        private static string SpriteRoot => AssetSettings.SpriteRoot;
        private static string PlayerSpriteRoot => AssetSettings.PlayerSpriteRoot;
        private static string GatherSpriteRoot => AssetSettings.GatherSpriteRoot;
        private static string UiSpriteRoot => AssetSettings.UiSpriteRoot;
        private static string UiButtonSpriteRoot => AssetSettings.UiButtonSpriteRoot;
        private static string UiMessageBoxSpriteRoot => AssetSettings.UiMessageBoxSpriteRoot;
        private static string UiPanelSpriteRoot => AssetSettings.UiPanelSpriteRoot;
        private static string WorldSpriteRoot => AssetSettings.WorldSpriteRoot;
        private static string RecipeSpriteRoot => AssetSettings.RecipeSpriteRoot;
        private static string SceneRoot => AssetSettings.SceneRoot;
        private static string SharedExplorationHudSourceScene => AssetSettings.SharedExplorationHudSourceScene;
        private static string FontRoot => AssetSettings.FontRoot;
        private static string HubFloorBackgroundSpritePath => AssetSettings.HubFloorBackgroundSpritePath;
        private static string HubFloorTileDesignSourcePath => AssetSettings.HubFloorTileDesignSourcePath;
        private static string HubFloorTileSpritePath => AssetSettings.HubFloorTileSpritePath;
        private static string HubBarDesignSourcePath => AssetSettings.HubBarDesignSourcePath;
        private static string FrontCounterDesignSourcePath => AssetSettings.FrontCounterDesignSourcePath;
        private static string BackCounterDesignSourcePath => AssetSettings.BackCounterDesignSourcePath;
        private static string MosaicTileFloorDesignSourcePath => AssetSettings.MosaicTileFloorDesignSourcePath;
        private static string MosaicTileWallDesignSourcePath => AssetSettings.MosaicTileWallDesignSourcePath;
        private static string TableChair2DesignSourcePath => AssetSettings.TableChair2DesignSourcePath;
        private static string AccountBoardDesignSourcePath => AssetSettings.AccountBoardDesignSourcePath;
        private static string FrontCounterSpritePath => AssetSettings.FrontCounterSpritePath;
        private static string BackCounterSpritePath => AssetSettings.BackCounterSpritePath;
        private static string MosaicTileFloorSpritePath => AssetSettings.MosaicTileFloorSpritePath;
        private static string MosaicTileWallSpritePath => AssetSettings.MosaicTileWallSpritePath;
        private static string TableChair2SpritePath => AssetSettings.TableChair2SpritePath;
        private static string AccountBoardSpritePath => AssetSettings.AccountBoardSpritePath;
        private static string HubBarRightSpritePath => AssetSettings.HubBarRightSpritePath;
        private static Vector4 HubBarMainSpriteBorder => AssetSettings.HubBarMainSpriteBorder;
        private static string HubWallBackgroundDesignSourcePath => AssetSettings.HubWallBackgroundDesignSourcePath;
        private static string HubFrontOutlineDesignSourcePath => AssetSettings.HubFrontOutlineDesignSourcePath;
        private static string HubWallBackgroundSpritePath => AssetSettings.HubWallBackgroundSpritePath;
        private static string HubFrontOutlineSpritePath => AssetSettings.HubFrontOutlineSpritePath;
        private static string HubBarSpritePath => AssetSettings.HubBarSpritePath;
        private static string HubTableUnlockedSpritePath => AssetSettings.HubTableUnlockedSpritePath;
        private static string HubTodayMenuBgSpritePath => AssetSettings.HubTodayMenuBgSpritePath;
        private static string HubTodayMenuItem1SpritePath => AssetSettings.HubTodayMenuItem1SpritePath;
        private static string HubTodayMenuItem2SpritePath => AssetSettings.HubTodayMenuItem2SpritePath;
        private static string HubTodayMenuItem3SpritePath => AssetSettings.HubTodayMenuItem3SpritePath;
        private static string PlayerFrontSpritePath => AssetSettings.PlayerFrontSpritePath;
        private static string PlayerBackSpritePath => AssetSettings.PlayerBackSpritePath;
        private static string PlayerSideSpritePath => AssetSettings.PlayerSideSpritePath;
        private static string CloseButtonDesignSourcePath => AssetSettings.CloseButtonDesignSourcePath;
        private static string HelpButtonDesignSourcePath => AssetSettings.HelpButtonDesignSourcePath;
        private static string SystemTextBoxDesignSourcePath => AssetSettings.SystemTextBoxDesignSourcePath;
        private static string InteractionTextBoxDesignSourcePath => AssetSettings.InteractionTextBoxDesignSourcePath;
        private static string DarkOutlinePanelDesignSourcePath => AssetSettings.DarkOutlinePanelDesignSourcePath;
        private static string DarkOutlinePanelAltDesignSourcePath => AssetSettings.DarkOutlinePanelAltDesignSourcePath;
        private static string DarkSolidPanelDesignSourcePath => AssetSettings.DarkSolidPanelDesignSourcePath;
        private static string DarkThinOutlinePanelDesignSourcePath => AssetSettings.DarkThinOutlinePanelDesignSourcePath;
        private static string LightOutlinePanelDesignSourcePath => AssetSettings.LightOutlinePanelDesignSourcePath;
        private static string LightSolidPanelDesignSourcePath => AssetSettings.LightSolidPanelDesignSourcePath;
        private static float PlayerSpritePixelsPerUnit => AssetSettings.PlayerSpritePixelsPerUnit;
        private static float PlayerVisualScale => AssetSettings.PlayerVisualScale;
        private static Vector3 DefaultPlayerRootScale => AssetSettings.DefaultPlayerRootScale;
        private static float WorldTitleFontSize => AssetSettings.WorldTitleFontSize;
        private static float WorldLabelFontSize => AssetSettings.WorldLabelFontSize;
        private static float WorldLabelSmallFontSize => AssetSettings.WorldLabelSmallFontSize;

        private static TMP_FontAsset _generatedKoreanFont;
        private static TMP_FontAsset _generatedHeadingFont;
        private static readonly Dictionary<string, SceneTransformSnapshot> CachedSceneTransforms = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, bool> CachedSceneObjectActiveStates = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, Dictionary<Type, SceneSerializedComponentSnapshot>> CachedSceneComponentOverrides = new(StringComparer.Ordinal);

        private static readonly Type[] SceneOverrideComponentTypes =
        {
            typeof(SpriteRenderer),
            typeof(TextMeshPro),
            typeof(MeshRenderer),
            typeof(Rigidbody2D),
            typeof(BoxCollider2D),
            typeof(CircleCollider2D),
            typeof(CapsuleCollider2D),
            typeof(Camera),
            typeof(AudioListener),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(InventoryManager),
            typeof(StorageManager),
            typeof(EconomyManager),
            typeof(ToolManager),
            typeof(DayCycleManager),
            typeof(UpgradeManager),
            typeof(GameManager),
            typeof(RestaurantManager),
            typeof(UIManager),
            typeof(InteractionDetector),
            typeof(PlayerController),
            typeof(PlayerDirectionalSprite),
            typeof(CameraFollow),
            typeof(PlayerBoundsLimiter),
            typeof(ScenePortal),
            typeof(SceneSpawnPoint),
            typeof(GuideTriggerZone),
            typeof(MovementModifierZone),
            typeof(DarknessZone),
            typeof(WindGustZone),
            typeof(GatherableResource),
            typeof(RecipeSelectorStation),
            typeof(ServiceCounterStation),
            typeof(StorageStation),
            typeof(UpgradeStation),
            typeof(HubTodayMenuDisplay)
        };

        private static readonly string[] HubPopupSceneImageNames =
        {
            "PopupOverlay",
            "PopupFrame",
            "PopupFrameLeft",
            "PopupFrameRight",
            "PopupLeftBody",
            "PopupRightBody",
            "PopupCloseButton"
        };

        private static string[] SharedExplorationHudTargetScenes => AssetSettings.SharedExplorationHudTargetScenes;

        private static string[] ManagedScenePaths => AssetSettings.ManagedScenePaths;

        private static readonly Dictionary<string, SceneImageSnapshot> CachedHubPopupSceneImages = new(StringComparer.Ordinal);

        private readonly struct SceneImageSnapshot
        {
            public SceneImageSnapshot(Sprite sprite, Image.Type type, Color color, bool preserveAspect)
            {
                Sprite = sprite;
                Type = type;
                Color = color;
                PreserveAspect = preserveAspect;
            }

            public Sprite Sprite { get; }
            public Image.Type Type { get; }
            public Color Color { get; }
            public bool PreserveAspect { get; }
        }

        private readonly struct SceneTransformSnapshot
        {
            public SceneTransformSnapshot(Vector3 position, Quaternion rotation, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
            {
                Position = position;
                Rotation = rotation;
                LocalPosition = localPosition;
                LocalRotation = localRotation;
                LocalScale = localScale;
            }

            public Vector3 Position { get; }
            public Quaternion Rotation { get; }
            public Vector3 LocalPosition { get; }
            public Quaternion LocalRotation { get; }
            public Vector3 LocalScale { get; }
        }

        private enum SceneSerializedValueKind
        {
            Boolean,
            Integer,
            Float,
            String,
            Color,
            ObjectReference,
            Vector2,
            Vector3,
            Vector4,
            Quaternion,
            Rect
        }

        private readonly struct SceneSerializedPropertySnapshot
        {
            public SceneSerializedPropertySnapshot(
                string propertyPath,
                SceneSerializedValueKind valueKind,
                bool boolValue = default,
                int intValue = default,
                float floatValue = default,
                string stringValue = null,
                Color colorValue = default,
                Object objectReferenceValue = null,
                Vector2 vector2Value = default,
                Vector3 vector3Value = default,
                Vector4 vector4Value = default,
                Quaternion quaternionValue = default,
                Rect rectValue = default)
            {
                PropertyPath = propertyPath;
                ValueKind = valueKind;
                BoolValue = boolValue;
                IntValue = intValue;
                FloatValue = floatValue;
                StringValue = stringValue;
                ColorValue = colorValue;
                ObjectReferenceValue = objectReferenceValue;
                Vector2Value = vector2Value;
                Vector3Value = vector3Value;
                Vector4Value = vector4Value;
                QuaternionValue = quaternionValue;
                RectValue = rectValue;
            }

            public string PropertyPath { get; }
            public SceneSerializedValueKind ValueKind { get; }
            public bool BoolValue { get; }
            public int IntValue { get; }
            public float FloatValue { get; }
            public string StringValue { get; }
            public Color ColorValue { get; }
            public Object ObjectReferenceValue { get; }
            public Vector2 Vector2Value { get; }
            public Vector3 Vector3Value { get; }
            public Vector4 Vector4Value { get; }
            public Quaternion QuaternionValue { get; }
            public Rect RectValue { get; }
        }

        private sealed class SceneSerializedComponentSnapshot
        {
            public SceneSerializedComponentSnapshot(IReadOnlyList<SceneSerializedPropertySnapshot> properties)
            {
                Properties = properties ?? Array.Empty<SceneSerializedPropertySnapshot>();
            }

            public IReadOnlyList<SceneSerializedPropertySnapshot> Properties { get; }
        }

        private sealed class ResourceLibrary
        {
            public ResourceData Fish;
            public ResourceData Shell;
            public ResourceData Seaweed;
            public ResourceData Herb;
            public ResourceData Mushroom;
            public ResourceData GlowMoss;
            public ResourceData WindHerb;
        }

        private sealed class RecipeLibrary
        {
            public RecipeData SushiSet;
            public RecipeData SeafoodSoup;
            public RecipeData HerbFishSoup;
            public RecipeData ForestBasket;
            public RecipeData GlowMossStew;
            public RecipeData WindHerbSalad;
        }

        private sealed class SpriteLibrary
        {
            public Sprite PlayerFront;
            public Sprite PlayerBack;
            public Sprite PlayerSide;
            public Sprite HubFloorTile;
            public Sprite HubFloorBackground;
            public Sprite HubWallBackground;
            public Sprite HubFrontOutline;
            public Sprite FrontCounter;
            public Sprite BackCounter;
            public Sprite MosaicTileFloor;
            public Sprite MosaicTileWall;
            public Sprite TableChair2;
            public Sprite AccountBoard;
            public Sprite HubBar;
            public Sprite HubBarRight;
            public Sprite HubTableUnlocked;
            public Sprite HubTodayMenuBg;
            public Sprite HubTodayMenuItem1;
            public Sprite HubTodayMenuItem2;
            public Sprite HubTodayMenuItem3;
            public Sprite Portal;
            public Sprite Selector;
            public Sprite Counter;
            public Sprite Fish;
            public Sprite Shell;
            public Sprite Seaweed;
            public Sprite Herb;
            public Sprite Mushroom;
            public Sprite GlowMoss;
            public Sprite WindHerb;
            public Sprite Floor;
        }

        // 빌드 플로우와 Canvas 동기화 로직은 partial 파일로 분리합니다.

        private static void CacheSceneObjectOverridesRecursive(Transform current)
        {
            if (current == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(current.name))
            {
                CachedSceneObjectActiveStates[current.name] = current.gameObject.activeSelf;
                CachedSceneTransforms[current.name] = new SceneTransformSnapshot(
                    current.position,
                    current.rotation,
                    current.localPosition,
                    current.localRotation,
                    current.localScale);

                CacheSupportedComponentOverrides(current.gameObject);
            }

            for (int index = 0; index < current.childCount; index++)
            {
                CacheSceneObjectOverridesRecursive(current.GetChild(index));
            }
        }

        private static void CacheSupportedComponentOverrides(GameObject gameObject)
        {
            if (gameObject == null || string.IsNullOrWhiteSpace(gameObject.name))
            {
                return;
            }

            Dictionary<Type, SceneSerializedComponentSnapshot> objectOverrides = null;

            foreach (Type componentType in SceneOverrideComponentTypes)
            {
                Component component = gameObject.GetComponent(componentType);
                if (component == null || !TryCreateSceneSerializedComponentSnapshot(component, out SceneSerializedComponentSnapshot snapshot))
                {
                    continue;
                }

                objectOverrides ??= new Dictionary<Type, SceneSerializedComponentSnapshot>();
                objectOverrides[componentType] = snapshot;
            }

            if (objectOverrides != null && objectOverrides.Count > 0)
            {
                CachedSceneComponentOverrides[gameObject.name] = objectOverrides;
            }
        }

        private static bool TryCreateSceneSerializedComponentSnapshot(
            Component component,
            out SceneSerializedComponentSnapshot snapshot)
        {
            snapshot = null;

            if (component == null)
            {
                return false;
            }

            SerializedObject serializedObject = new(component);
            SerializedProperty iterator = serializedObject.GetIterator();
            List<SceneSerializedPropertySnapshot> properties = new();
            while (iterator.NextVisible(true))
            {
                if (ShouldSkipSceneSerializedProperty(iterator)
                    || !TryCreateSceneSerializedPropertySnapshot(iterator, out SceneSerializedPropertySnapshot propertySnapshot))
                {
                    continue;
                }

                properties.Add(propertySnapshot);
            }

            if (properties.Count == 0)
            {
                return false;
            }

            snapshot = new SceneSerializedComponentSnapshot(properties);
            return true;
        }

        private static bool ShouldSkipSceneSerializedProperty(SerializedProperty property)
        {
            if (property == null)
            {
                return true;
            }

            string propertyPath = property.propertyPath;
            return string.IsNullOrWhiteSpace(propertyPath)
                   || string.Equals(propertyPath, "m_Script", StringComparison.Ordinal)
                   || string.Equals(propertyPath, "m_GameObject", StringComparison.Ordinal)
                   || string.Equals(propertyPath, "m_EditorClassIdentifier", StringComparison.Ordinal);
        }

        private static bool TryCreateSceneSerializedPropertySnapshot(
            SerializedProperty property,
            out SceneSerializedPropertySnapshot snapshot)
        {
            snapshot = default;

            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    snapshot = new SceneSerializedPropertySnapshot(property.propertyPath, SceneSerializedValueKind.Boolean, boolValue: property.boolValue);
                    return true;

                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Enum:
                case SerializedPropertyType.ArraySize:
                case SerializedPropertyType.Character:
                case SerializedPropertyType.LayerMask:
                    snapshot = new SceneSerializedPropertySnapshot(property.propertyPath, SceneSerializedValueKind.Integer, intValue: property.intValue);
                    return true;

                case SerializedPropertyType.Float:
                    snapshot = new SceneSerializedPropertySnapshot(property.propertyPath, SceneSerializedValueKind.Float, floatValue: property.floatValue);
                    return true;

                case SerializedPropertyType.String:
                    snapshot = new SceneSerializedPropertySnapshot(property.propertyPath, SceneSerializedValueKind.String, stringValue: property.stringValue);
                    return true;

                case SerializedPropertyType.Color:
                    snapshot = new SceneSerializedPropertySnapshot(property.propertyPath, SceneSerializedValueKind.Color, colorValue: property.colorValue);
                    return true;

                case SerializedPropertyType.ObjectReference:
                    if (property.objectReferenceValue == null || !EditorUtility.IsPersistent(property.objectReferenceValue))
                    {
                        return false;
                    }

                    snapshot = new SceneSerializedPropertySnapshot(property.propertyPath, SceneSerializedValueKind.ObjectReference, objectReferenceValue: property.objectReferenceValue);
                    return true;

                case SerializedPropertyType.Vector2:
                    snapshot = new SceneSerializedPropertySnapshot(property.propertyPath, SceneSerializedValueKind.Vector2, vector2Value: property.vector2Value);
                    return true;

                case SerializedPropertyType.Vector3:
                    snapshot = new SceneSerializedPropertySnapshot(property.propertyPath, SceneSerializedValueKind.Vector3, vector3Value: property.vector3Value);
                    return true;

                case SerializedPropertyType.Vector4:
                    snapshot = new SceneSerializedPropertySnapshot(property.propertyPath, SceneSerializedValueKind.Vector4, vector4Value: property.vector4Value);
                    return true;

                case SerializedPropertyType.Quaternion:
                    snapshot = new SceneSerializedPropertySnapshot(property.propertyPath, SceneSerializedValueKind.Quaternion, quaternionValue: property.quaternionValue);
                    return true;

                case SerializedPropertyType.Rect:
                    snapshot = new SceneSerializedPropertySnapshot(property.propertyPath, SceneSerializedValueKind.Rect, rectValue: property.rectValue);
                    return true;

                default:
                    return false;
            }
        }

        private static void ApplySceneComponentOverride(Component component, string objectName)
        {
            if (component == null
                || string.IsNullOrWhiteSpace(objectName)
                || !CachedSceneComponentOverrides.TryGetValue(objectName, out Dictionary<Type, SceneSerializedComponentSnapshot> objectOverrides)
                || !objectOverrides.TryGetValue(component.GetType(), out SceneSerializedComponentSnapshot snapshot))
            {
                return;
            }

            SerializedObject serializedObject = new(component);
            bool changed = false;

            foreach (SceneSerializedPropertySnapshot propertySnapshot in snapshot.Properties)
            {
                SerializedProperty property = serializedObject.FindProperty(propertySnapshot.PropertyPath);
                if (property == null)
                {
                    continue;
                }

                ApplySceneSerializedPropertySnapshot(property, propertySnapshot);
                changed = true;
            }

            if (changed)
            {
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void ApplySceneSerializedPropertySnapshot(
            SerializedProperty property,
            SceneSerializedPropertySnapshot snapshot)
        {
            switch (snapshot.ValueKind)
            {
                case SceneSerializedValueKind.Boolean:
                    property.boolValue = snapshot.BoolValue;
                    break;

                case SceneSerializedValueKind.Integer:
                    property.intValue = snapshot.IntValue;
                    break;

                case SceneSerializedValueKind.Float:
                    property.floatValue = snapshot.FloatValue;
                    break;

                case SceneSerializedValueKind.String:
                    property.stringValue = snapshot.StringValue;
                    break;

                case SceneSerializedValueKind.Color:
                    property.colorValue = snapshot.ColorValue;
                    break;

                case SceneSerializedValueKind.ObjectReference:
                    property.objectReferenceValue = snapshot.ObjectReferenceValue;
                    break;

                case SceneSerializedValueKind.Vector2:
                    property.vector2Value = snapshot.Vector2Value;
                    break;

                case SceneSerializedValueKind.Vector3:
                    property.vector3Value = snapshot.Vector3Value;
                    break;

                case SceneSerializedValueKind.Vector4:
                    property.vector4Value = snapshot.Vector4Value;
                    break;

                case SceneSerializedValueKind.Quaternion:
                    property.quaternionValue = snapshot.QuaternionValue;
                    break;

                case SceneSerializedValueKind.Rect:
                    property.rectValue = snapshot.RectValue;
                    break;
            }
        }

        private static void ApplySceneActiveOverride(GameObject gameObject, string objectName)
        {
            if (gameObject == null
                || string.IsNullOrWhiteSpace(objectName)
                || !CachedSceneObjectActiveStates.TryGetValue(objectName, out bool isActive))
            {
                return;
            }

            gameObject.SetActive(isActive);
        }

        private static Vector3 ResolveSceneObjectScale(string objectName, Vector3 fallbackScale)
        {
            if (string.IsNullOrWhiteSpace(objectName)
                || !CachedSceneTransforms.TryGetValue(objectName, out SceneTransformSnapshot snapshot)
                || Mathf.Approximately(snapshot.LocalScale.x, 0f)
                || Mathf.Approximately(snapshot.LocalScale.y, 0f))
            {
                return fallbackScale;
            }

            return snapshot.LocalScale;
        }

        private static void ApplySceneTransformOverride(
            Transform target,
            string objectName,
            Vector3 fallbackPosition,
            Quaternion fallbackRotation,
            Vector3 fallbackScale,
            bool useLocalSpace)
        {
            if (target == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(objectName)
                && CachedSceneTransforms.TryGetValue(objectName, out SceneTransformSnapshot snapshot))
            {
                if (useLocalSpace)
                {
                    target.localPosition = snapshot.LocalPosition;
                    target.localRotation = snapshot.LocalRotation;
                }
                else
                {
                    target.position = snapshot.Position;
                    target.rotation = snapshot.Rotation;
                }

                target.localScale = ResolveSceneObjectScale(objectName, fallbackScale);
                return;
            }

            if (useLocalSpace)
            {
                target.localPosition = fallbackPosition;
                target.localRotation = fallbackRotation;
            }
            else
            {
                target.position = fallbackPosition;
                target.rotation = fallbackRotation;
            }

            target.localScale = fallbackScale;
        }

        // Canvas/UI 동기화 로직은 partial 파일로 분리합니다.
    }
}
#endif
