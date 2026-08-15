#if UNITY_EDITOR
using _001_Scripts.Controller;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace _001_Scripts.Editor
{
    [InitializeOnLoad]
    internal static class SurvivalFirstPersonAnimationInstaller
    {
        private const string RigName = "FirstPersonItemRig";

        static SurvivalFirstPersonAnimationInstaller() => EditorApplication.delayCall += InstallIfNeeded;

        [MenuItem("Tools/Survival/Install Scene-Authored First Person Item Rig")]
        private static void InstallMenu() => Install(forceReposition: true);

        private static void InstallIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.name != "SampleScene") return;
            Install(forceReposition: false);
        }

        private static void Install(bool forceReposition)
        {
            InventoryController inventory = UnityEngine.Object.FindAnyObjectByType<InventoryController>(
                FindObjectsInactive.Include);
            Camera viewCamera = FindMainCamera();
            if (!inventory || !viewCamera)
            {
                Debug.LogWarning("[Survival] InventoryController or Main Camera was not found; item rig was not installed.");
                return;
            }

            Transform rigTransform = viewCamera.transform.Find(RigName);
            bool created = !rigTransform;
            if (created)
            {
                GameObject rigObject = new GameObject(RigName);
                Undo.RegisterCreatedObjectUndo(rigObject, "Install First Person Item Rig");
                rigTransform = rigObject.transform;
                rigTransform.SetParent(viewCamera.transform, false);
            }

            if (created || forceReposition)
            {
                rigTransform.localPosition = new Vector3(.28f, -.25f, .52f);
                rigTransform.localRotation = Quaternion.identity;
                rigTransform.localScale = Vector3.one;
            }

            FirstPersonItemMotion motion = rigTransform.GetComponent<FirstPersonItemMotion>();
            if (!motion) motion = Undo.AddComponent<FirstPersonItemMotion>(rigTransform.gameObject);
            motion.Configure(rigTransform);

            FirstPersonItemHolder holder = inventory.GetComponent<FirstPersonItemHolder>();
            if (!holder) holder = Undo.AddComponent<FirstPersonItemHolder>(inventory.gameObject);
            holder.Configure(rigTransform, motion);

            EditorUtility.SetDirty(rigTransform.gameObject);
            EditorUtility.SetDirty(motion);
            EditorUtility.SetDirty(holder);
            PrefabUtility.RecordPrefabInstancePropertyModifications(holder);
            EditorSceneManager.MarkSceneDirty(inventory.gameObject.scene);
            EditorSceneManager.SaveScene(inventory.gameObject.scene);
            if (created)
                Debug.Log("[Survival] Installed editable FirstPersonItemRig under Main Camera.");
        }

        private static Camera FindMainCamera()
        {
            foreach (Camera camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
                if (camera.CompareTag("MainCamera")) return camera;
            return null;
        }
    }
}
#endif
