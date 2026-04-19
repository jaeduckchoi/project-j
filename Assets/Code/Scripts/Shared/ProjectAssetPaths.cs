namespace Code.Scripts.Shared
{
    /// <summary>
    /// 프로젝트 1st-party Unity 자산 경로의 정본을 제공한다.
    /// 구조 재편 시 경로 상수는 이 타입만 갱신하고, 다른 코드와 문서는 이를 참조해 드리프트를 줄인다.
    /// </summary>
    public static class ProjectAssetPaths
    {
        public const string AssetsRoot = "Assets";

        public const string ArtRoot = AssetsRoot + "/Art";

        public const string CodeRoot = AssetsRoot + "/Code";
        public const string ScriptsRoot = CodeRoot + "/Scripts";
        public const string EditorRoot = CodeRoot + "/Editor";
        public const string TestsRoot = CodeRoot + "/Tests";
        public const string EditModeTestsRoot = TestsRoot + "/EditMode";
        public const string PlayModeTestsRoot = TestsRoot + "/PlayMode";

        public const string DataRoot = AssetsRoot + "/Data";
        public const string GameDataSourceRoot = DataRoot + "/GameDataSource";

        public const string LevelRoot = AssetsRoot + "/Level";
        public const string ScenesRoot = LevelRoot + "/Scenes";
        public const string HubScenePath = ScenesRoot + "/Hub.unity";
        public const string BeachScenePath = ScenesRoot + "/Beach.unity";

        public const string ResourcesRoot = AssetsRoot + "/Resources";
        public const string GeneratedResourcesRoot = ResourcesRoot + "/Generated";
        public const string PrototypeGeneratedAssetSettingsAssetPath = GeneratedResourcesRoot + "/prototype-generated-asset-settings.asset";
        public const string PopupInteractionBindingSettingsAssetPath = GeneratedResourcesRoot + "/popup-interaction-bindings.asset";
        public const string UiLayoutBindingSettingsAssetPath = GeneratedResourcesRoot + "/ui-layout-bindings.asset";
        public const string SceneHierarchyContractSettingsAssetPath = GeneratedResourcesRoot + "/scene-hierarchy-contracts.asset";

        public const string SettingsRoot = AssetsRoot + "/Settings";
        public const string DefaultVolumeProfileAssetPath = SettingsRoot + "/DefaultVolumeProfile.asset";
        public const string InputSystemActionsAssetPath = SettingsRoot + "/InputSystemActions.inputactions";
        public const string UniversalRenderPipelineGlobalSettingsAssetPath = SettingsRoot + "/UniversalRenderPipelineGlobalSettings.asset";
    }
}
