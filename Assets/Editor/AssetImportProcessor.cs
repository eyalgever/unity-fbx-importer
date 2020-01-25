using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace kmty.importer {
    public class AssetImportTester : AssetPostprocessor {
        private const string PREFAB_DESTINATION_DIRECTORY = "Assets/Prefabs/";
        private const string ANIMATOR_DESTINATION_DIRECTORY = "Assets/Animator/";

        void OnPreprocessModel() {
            var modelImporter = assetImporter as ModelImporter;
            modelImporter.materialLocation = ModelImporterMaterialLocation.External;
        }

        void OnPreprocessAnimation() {
            var modelImporter = assetImporter as ModelImporter;
            if (modelImporter.animationType == ModelImporterAnimationType.Legacy)
                modelImporter.animationType = ModelImporterAnimationType.Generic;

            modelImporter.animationCompression = ModelImporterAnimationCompression.Off;
            var clipAnimations = modelImporter.clipAnimations;
            if (clipAnimations.Length == 0) clipAnimations = modelImporter.defaultClipAnimations;
            foreach (var clip in clipAnimations) { if (clip.name.Contains("_loop")) clip.loopTime = true; }
            modelImporter.clipAnimations = clipAnimations;
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
            if (!Directory.Exists(PREFAB_DESTINATION_DIRECTORY)) Directory.CreateDirectory(PREFAB_DESTINATION_DIRECTORY);
            if (!Directory.Exists(ANIMATOR_DESTINATION_DIRECTORY)) Directory.CreateDirectory(ANIMATOR_DESTINATION_DIRECTORY);

            foreach (var path_imp in importedAssets) {
                var lower = path_imp.ToLower();
                if (!lower.Contains("fbx/avatar") && !lower.Contains("fbx/animations/props")) continue;
                if (!lower.Contains(".fbx")) continue;


                var model = AssetDatabase.LoadAssetAtPath(path_imp, typeof(GameObject)) as GameObject;
                var tmpgo = PrefabUtility.InstantiatePrefab(model) as GameObject;

                AnimatorController controller;
                Animator animator = tmpgo.GetComponent<Animator>();
                if (tmpgo.GetComponent<AnimationController>() == null) tmpgo.AddComponent<AnimationController>();
                if (animator == null) animator = tmpgo.AddComponent<Animator>();
                if (animator.runtimeAnimatorController == null) {
                    controller = CreateAnimationController(tmpgo);
                    animator.runtimeAnimatorController = controller;
                } else {
                    controller = (AnimatorController)animator.runtimeAnimatorController;
                }

                var state_importer = ScriptableObject.CreateInstance<AnimatorStateImporter>();
                state_importer.isClearOnStartUp = true;
                state_importer.controller = controller;
                state_importer.fbxFiles = new GameObject[1] { model };
                state_importer.Import();
                state_importer.Alignment();

                var player = tmpgo.GetComponent<AnimationPlayer>();
                var playList = new List<AnimationPlaylistItem>();
                var objects = AssetDatabase.LoadAllAssetsAtPath(path_imp);

                foreach (var obj in objects) {
                    var clip = obj as AnimationClip;
                    if (clip != null && !clip.name.Contains("_preview")) playList.Add(new AnimationPlaylistItem(clip.name, 1));
                }

                if (player == null) AddAnimationPlayerComponent(tmpgo, playList);
                else player.SetPlaylist(playList);
                
                var prefab = PrefabUtility.SaveAsPrefabAsset(tmpgo, $"{PREFAB_DESTINATION_DIRECTORY}{model.name}.prefab");
                PrefabUtility.UnpackPrefabInstance(tmpgo, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
                Object.DestroyImmediate(tmpgo);
                AssetDatabase.SaveAssets();
            }
        }

        static AnimatorController CreateAnimationController(GameObject modelAsset) {
            string path = ANIMATOR_DESTINATION_DIRECTORY + modelAsset.name + ".controller";
            return AnimatorController.CreateAnimatorControllerAtPath(path);
        }

        static void AddAnimationPlayerComponent(GameObject prefab, List<AnimationPlaylistItem> playlist) {
            var component = prefab.AddComponent<AnimationPlayer>();
            component.SetPlaylist(playlist);
        }
    }
}
