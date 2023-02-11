using MelonLoader;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Game;
using System.Collections;

[assembly: MelonInfo(typeof(AISomniumFiles2Mod.AISomniumFiles2Fix), "AI: Somnium Files 2", "1.0.6-custom", "FatalErrorX")]
[assembly: MelonGame("SpikeChunsoft", "AI_TheSomniumFiles2")]
namespace AISomniumFiles2Mod
{
    public class AISomniumFiles2Fix : MelonMod
    {
        public static MelonPreferences_Category Fixes;
        public static MelonPreferences_Entry<int> DesiredResolutionX;
        public static MelonPreferences_Entry<int> DesiredResolutionY;
        public static MelonPreferences_Entry<bool> Fullscreen;
        public static MelonPreferences_Entry<bool> UIFix;
        public static MelonPreferences_Entry<bool> IncreaseQuality;
        public static MelonPreferences_Entry<bool> bDisableMouseCursor;


        public static bool shouldApplyMainMenuFix = true;



        public override void OnApplicationStart()
        {
            LoggerInstance.Msg("Application started.");

            Fixes = MelonPreferences.CreateCategory("AISomnium2Fix");
            Fixes.SetFilePath("Mods/AISomnium2Fix.cfg");
            DesiredResolutionX = Fixes.CreateEntry("Resolution_Width", Display.main.systemWidth, "", "Custom resolution width"); // Set default to something safe
            DesiredResolutionY = Fixes.CreateEntry("Resolution_Height", Display.main.systemHeight, "", "Custom resolution height"); // Set default to something safe
            Fullscreen = Fixes.CreateEntry("Fullscreen", true, "", "Set to true for fullscreen or false for windowed");
            UIFix = Fixes.CreateEntry("UI_Fixes", true, "", "Fixes UI issues at ultrawide/wider");
            IncreaseQuality = Fixes.CreateEntry("IncreaseQuality", true, "", "Increase graphical quality."); // 
            bDisableMouseCursor = Fixes.CreateEntry("DIsableMouseCursor", true, "", "Set to true to force the mouse cursor to be invisible.");
            SceneManager.add_sceneUnloaded( new System.Action<Scene>(sceneUnloadHandler));
            SceneManager.add_sceneLoaded(new System.Action<Scene, LoadSceneMode>(sceneLoadHandler));
        }

        public void sceneLoadHandler(Scene scene, LoadSceneMode mode)
        {
            if(scene.name == "OptionMenuMain")
            {
                if (shouldApplyMainMenuFix)
                {
                    MelonCoroutines.Start(waitUntil());
                }
            }
        }

        public void sceneUnloadHandler(Scene scene)
        {
            MelonLogger.Msg("Scene is unloaded " + scene.name);
            if (scene.name == "OptionMenuMain") {
                MelonLogger.Msg("Main Menu Unloaded, Fix should be reapplied next time!");
                shouldApplyMainMenuFix = true;
            }
        }

        // ongaboonga code
        IEnumerator waitUntil() {

            while (UIFixes.iSUIFixNeeded) {
                GameObject mainOptionMenuScene = GameObject.Find("$Root/Canvas/ScreenScaler/");
                if (mainOptionMenuScene != null)
                {
                    mainOptionMenuScene.transform.localScale = new Vector3(1f, 1f / UIFixes.AspectMultiplier, 1f);
                    shouldApplyMainMenuFix = false;
                    break;
                }
                yield return new WaitForSeconds(0.1F);
            }
            yield break;

        }
        
        public override void OnSceneWasInitialized(int buildIndex, string sceneName) {
            ScreenScaler[] scalers = GameObject.FindObjectsOfType<Game.ScreenScaler>();
            foreach (Game.ScreenScaler scaler in scalers)
            {
                MelonLogger.Msg("Scaler detected?! SCALING?!" + scaler.name);
                scaler.transform.localScale = new Vector3(1f, 1f / UIFixes.AspectMultiplier, 1f);
            }
        }
        

        [HarmonyPatch]
        public class CustomResolution
        {
            [HarmonyPatch(typeof(Game.LauncherArgs), nameof(Game.LauncherArgs.OnRuntimeMethodLoad))]
            [HarmonyPostfix]
            public static void SetResolution()
            {
                if (!Fullscreen.Value)
                    
                {
                    Screen.SetResolution(DesiredResolutionX.Value, DesiredResolutionY.Value, FullScreenMode.Windowed);
                }
                else
                {
                    Screen.SetResolution(DesiredResolutionX.Value, DesiredResolutionY.Value, FullScreenMode.FullScreenWindow);
                }

                MelonLogger.Msg($"Screen resolution set to {DesiredResolutionX.Value}x{DesiredResolutionY.Value}, Fullscreen = {Fullscreen.Value}");                
            }
        }

        [HarmonyPatch]
        public class UIFixes
        {
            public static float NativeAspectRatio = (float)16 / 9;
            public static float NewAspectRatio = (float)DesiredResolutionX.Value / DesiredResolutionY.Value;
            public static float AspectMultiplier = NewAspectRatio / NativeAspectRatio;

            public static bool isGreaterThanOriginalAspect = NewAspectRatio > NativeAspectRatio;
            public static bool isLesserThanOriginalAspect = NewAspectRatio < NativeAspectRatio;
            public static bool iSUIFixNeeded = UIFix.Value && (isGreaterThanOriginalAspect || isLesserThanOriginalAspect);

            // Set screen match mode when object has CanvasScaler enabled
            [HarmonyPatch(typeof(CanvasScaler), "OnEnable")]
            [HarmonyPostfix]
            public static void SetScreenMatchMode(CanvasScaler __instance)
            {
                if (iSUIFixNeeded)
                {
                    __instance.m_ScreenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                }  
            }


            // Fix letterboxing to span screen
            [HarmonyPatch(typeof(Game.CinemaScope), "Show")]
            [HarmonyPostfix]
            public static void LetterboxFix()
            {
                if (iSUIFixNeeded)
                {

                    var GameObjects = GameObject.FindObjectsOfType<Game.CinemaScope>();
                    foreach (var GameObject in GameObjects)
                    {
                        if (isGreaterThanOriginalAspect) { 
                            GameObject.transform.localScale = new Vector3(1 * AspectMultiplier, 1f, 1f);
                            MelonLogger.Msg("Letterboxing spanned.");
                        } else if(isLesserThanOriginalAspect) { 
                            GameObject.transform.localScale = new Vector3(1, 1 / AspectMultiplier, 1f);
                            MelonLogger.Msg("Letterboxing shrunk.");
                        }
                    }
                   
                }
            }
            // Fix filters to span screen
            // This is jank but I can't think of a better solution right now.
            [HarmonyPatch(typeof(Game.FilterController), "Black")]
            [HarmonyPatch(typeof(Game.FilterController), "FadeIn")]
            [HarmonyPatch(typeof(Game.FilterController), "FadeInWait")]
            [HarmonyPatch(typeof(Game.FilterController), "FadeOut")]
            [HarmonyPatch(typeof(Game.FilterController), "FadeOutWait")]
            [HarmonyPatch(typeof(Game.FilterController), "Flash")]
            [HarmonyPatch(typeof(Game.FilterController), "Set")]
            [HarmonyPatch(typeof(Game.FilterController), "SetValue")]
            [HarmonyPostfix]
            public static void FilterFix()
            {
                if (iSUIFixNeeded)
                {
                    var GameObjects = GameObject.FindObjectsOfType<Game.FilterController>();
                    foreach (var GameObject in GameObjects)
                    {

                        if (isGreaterThanOriginalAspect)
                        {
                            GameObject.transform.localScale = new Vector3(1 * AspectMultiplier, 1f, 1f);
                            MelonLogger.Msg("Eye box spanned.");
                        }
                        else if (isLesserThanOriginalAspect)
                        {
                            GameObject.transform.localScale = new Vector3(1, 1 / AspectMultiplier, 1f);
                            MelonLogger.Msg("Eye box shrunk.");
                        }
                    }
                    // Log spam
                    //MelonLogger.Msg("Filter spanned.");
                    
                }
            }



            // Fix eye fade filter
            [HarmonyPatch(typeof(Game.EyeFadeFilter), "FadeIn")]
            [HarmonyPatch(typeof(Game.EyeFadeFilter), "FadeInWait")]
            [HarmonyPatch(typeof(Game.EyeFadeFilter), "FadeOut")]
            [HarmonyPatch(typeof(Game.EyeFadeFilter), "FadeOutWait")]
            [HarmonyPostfix]
            public static void EyeFadeFilterFix()
            {
                if (iSUIFixNeeded)
                {
                    var GameObjects = GameObject.FindObjectsOfType<Game.EyeFadeFilter>();
                    foreach (var GameObject in GameObjects)
                    {

                        if (isGreaterThanOriginalAspect)
                        {
                            GameObject.transform.localScale = new Vector3(1 * AspectMultiplier, 1f, 1f);
                            MelonLogger.Msg("eye spanned.");
                        }
                        else if (isLesserThanOriginalAspect)
                        {
                            GameObject.transform.localScale = new Vector3(1, 1 / AspectMultiplier, 1f);
                            MelonLogger.Msg("Eye box shrunk.");
                        }
                    }
                    // Log spam
                    //MelonLogger.Msg("EyeFade filter spanned.");
                }
            }

            // Scale cutscene viewport when video plays
            [HarmonyPatch(typeof(Game.VideoController), nameof(Game.VideoController.Prepare))]
            [HarmonyPostfix]
            public static void FixCutsceneViewport(Game.VideoController __instance)
            {
                if (iSUIFixNeeded)
                {
                    var cutsceneImage = __instance.world.Image;
                    
                    if (isGreaterThanOriginalAspect)
                    {
                        cutsceneImage.transform.localScale = new Vector3(1 / AspectMultiplier, 1f, 1f);
                    }
                    else if (isLesserThanOriginalAspect)
                    {
                        cutsceneImage.transform.localScale = new Vector3(1, 1 * AspectMultiplier, 1f);
                    }

                    MelonLogger.Msg("Cutscene viewport scaled horizontally.");
                }
            }

            // Reset cutscene viewport after video ends
            [HarmonyPatch(typeof(Game.VideoController), nameof(Game.VideoController.Stop))]
            [HarmonyPostfix]
            public static void ResetCutsceneViewport(Game.VideoController __instance)
            {
                if (iSUIFixNeeded)
                {
                    var cutsceneImage = __instance.world.Image;
                    cutsceneImage.transform.localScale = new Vector3(1f, 1f, 1f);
                    MelonLogger.Msg("Cutscene viewport scale reset to default.");
                }
            }
        }

        [HarmonyPatch]
        public class QualityPatches
        {
            // Enable high-quality SMAA for all cameras
            [HarmonyPatch(typeof(Game.CameraController), "OnEnable")]
            [HarmonyPostfix]
            public static void CameraQualityFix(Game.CameraController __instance)
            {
                if (IncreaseQuality.Value)
                {
                    var UACD = __instance._camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                    UACD.antialiasing = UnityEngine.Rendering.Universal.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                    UACD.antialiasingQuality = UnityEngine.Rendering.Universal.AntialiasingQuality.High;
                    MelonLogger.Msg("Camera set to SMAA High.");
                }
            }
        }

        [HarmonyPatch]
        public class CursorPatches
        {
            // Mouse cursor visibility
            [HarmonyPatch(typeof(UnityEngine.Cursor), nameof(UnityEngine.Cursor.visible), MethodType.Setter)]
            [HarmonyPrefix]
            public static void CameraQualityFix(UnityEngine.Cursor __instance, ref bool __0)
            {
                if (bDisableMouseCursor.Value)
                {
                    __0 = false;
                    // Log Spam
                    // Seems to set cursor visibility a lot, :|
                    //MelonLogger.Msg("Forced mouse cursor to be invisible.");
                }
            }
        }
    }
}