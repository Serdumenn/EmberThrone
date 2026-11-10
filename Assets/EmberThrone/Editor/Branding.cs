using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace EmberThrone.EditorTools
{
    public static class Branding
    {
        public const string SplashLogo = "Assets/EmberThrone/Art/Brand/ocean_forge_splash.png";
        public const string IconFolder = "Assets/EmberThrone/Art/AppIcon/";
        const float SplashLogoSeconds = 2f;
        static readonly Color32 SplashBackground = new Color32(0x1E, 0x21, 0x40, 0xFF);

        [MenuItem("EmberThrone/Apply Branding")]
        public static void Apply()
        {
            ApplySplash();
            ApplyIcons();
            AssetDatabase.SaveAssets();
        }

        static void ApplySplash()
        {
            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.backgroundColor = SplashBackground;
            PlayerSettings.SplashScreen.overlayOpacity = 1f;
            PlayerSettings.SplashScreen.animationMode = PlayerSettings.SplashScreen.AnimationMode.Dolly;
            PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.AllSequential;
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.LightOnDark;
            var logo = AssetDatabase.LoadAssetAtPath<Sprite>(SplashLogo);
            PlayerSettings.SplashScreen.logos = logo == null
                ? new PlayerSettings.SplashScreenLogo[0]
                : new[] { PlayerSettings.SplashScreenLogo.Create(SplashLogoSeconds, logo) };
        }

        static Texture2D Icon(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(IconFolder + name + ".png");

        static void ApplyIcons()
        {
            var background = Icon("app_icon_bg");
            var foreground = Icon("app_icon_fg");
            var legacy = Icon("app_icon_legacy");
            if (background == null || foreground == null || legacy == null) return;

            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { legacy }, IconKind.Any);
#if UNITY_ANDROID
            var android = NamedBuildTarget.Android;
            var kind = UnityEditor.Android.AndroidPlatformIconKind.Adaptive;
            var icons = PlayerSettings.GetPlatformIcons(android, kind);
            foreach (var icon in icons) icon.SetTextures(background, foreground);
            PlayerSettings.SetPlatformIcons(android, kind, icons);
#endif
        }
    }
}
