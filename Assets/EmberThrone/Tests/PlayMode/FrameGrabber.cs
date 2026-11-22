using UnityEngine;

namespace EmberThrone.Game.Tests
{
    public sealed class FrameGrabber
    {
        public const int ReferenceWidth = 720;
        public const int ReferenceHeight = 1564;
        public const float ReferenceSafeTop = 60f;
        public const float ReferenceSafeBottom = 37f;

        public static Rect ReferenceSafeArea =>
            new Rect(0f, ReferenceSafeBottom, ReferenceWidth, ReferenceHeight - ReferenceSafeTop - ReferenceSafeBottom);

        static readonly float[] Linear = BuildLinear();

        readonly GameApp _app;
        RenderTexture _board;
        RenderTexture _ui;

        public FrameGrabber(GameApp app) => _app = app;

        public void UseReference() => Use(ReferenceWidth, ReferenceHeight, ReferenceSafeArea);

        public void Use(int width, int height, Rect? safeArea)
        {
            UiRoot.ScreenSizeOverride = new Vector2Int(width, height);
            UiRoot.SafeAreaOverride = safeArea;
            var oldBoard = _board;
            var oldUi = _ui;
            _board = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _app.Camera.targetTexture = _board;

            _ui = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var settings = _app.Ui.Settings;
            settings.targetTexture = _ui;
            settings.clearColor = true;
            settings.colorClearValue = Color.clear;

            if (oldBoard != null) Object.Destroy(oldBoard);
            if (oldUi != null) Object.Destroy(oldUi);
        }

        public Texture2D Grab()
        {
            _app.Camera.Render();
            var board = Read(_board);
            var ui = Read(_ui);
            for (int i = 0; i < board.Length; i++)
            {
                var u = ui[i];
                var b = board[i];
                float k = 1f - u.a / 255f;
                board[i] = new Color32(Over(u.r, b.r, k), Over(u.g, b.g, k), Over(u.b, b.b, k), 255);
            }
            var tex = new Texture2D(_board.width, _board.height, TextureFormat.RGBA32, false);
            tex.SetPixels32(board);
            return tex;
        }

        public void Release()
        {
            if (_board != null) Object.Destroy(_board);
            if (_ui != null) Object.Destroy(_ui);
            _board = null;
            _ui = null;
        }

        static float[] BuildLinear()
        {
            var table = new float[256];
            for (int i = 0; i < 256; i++) table[i] = Mathf.GammaToLinearSpace(i / 255f);
            return table;
        }

        static byte Over(byte ui, byte board, float k)
        {
            float linear = Mathf.Min(1f, Linear[ui] + Linear[board] * k);
            return (byte)Mathf.RoundToInt(Mathf.LinearToGammaSpace(linear) * 255f);
        }

        static Color32[] Read(RenderTexture rt)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = previous;
            var pixels = tex.GetPixels32();
            Object.Destroy(tex);
            return pixels;
        }
    }
}
