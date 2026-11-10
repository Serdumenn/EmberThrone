using System.Collections;
using System.IO;
using System.Linq;
using EmberThrone.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EmberThrone.Game.Tests
{
    [Explicit("Takes screenshots; run on demand.")]
    public class ScreenshotTour
    {
        const string Folder = "TestResults/screens";
        const string Even = "YPOGY";
        const string Odd = "OGYPO";
        const float SlowMotion = 0.1f;

        GameApp _app;
        RenderTexture _target;
        RenderTexture _uiTarget;
        System.Collections.Generic.Dictionary<int, int> _savedStars;
        GameController Game => _app.Game;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Directory.CreateDirectory(Folder);
            _savedStars = StarsBackup.Snapshot();
            StarsBackup.Restore(new System.Collections.Generic.Dictionary<int, int>());
            Time.timeScale = 1f;
            UiRoot.SafeAreaOverride = null;
            UiRoot.ScreenSizeOverride = new Vector2Int(1080, 1920);
            _app = GameApp.Create();
            UseScreen(1080, 1920, null);
            for (int i = 0; i < 3; i++) yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            UiRoot.SafeAreaOverride = null;
            UiRoot.ScreenSizeOverride = null;
            Object.Destroy(_app.gameObject);
            Object.Destroy(_target);
            Object.Destroy(_uiTarget);
            StarsBackup.Restore(_savedStars);
            yield return null;
        }

        void UseScreen(int width, int height, Rect? safeArea)
        {
            UiRoot.ScreenSizeOverride = new Vector2Int(width, height);
            UiRoot.SafeAreaOverride = safeArea;
            var oldBoard = _target;
            var oldUi = _uiTarget;
            _target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _app.Camera.targetTexture = _target;

            _uiTarget = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var settings = _app.Ui.Settings;
            settings.targetTexture = _uiTarget;
            settings.clearColor = true;
            settings.colorClearValue = Color.clear;

            if (oldBoard != null) Object.Destroy(oldBoard);
            if (oldUi != null) Object.Destroy(oldUi);
        }

        static readonly float[] Linear = BuildLinear();

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

        IEnumerator Shot(string name)
        {
            yield return null;
            _app.Camera.Render();
            var board = Read(_target);
            var ui = Read(_uiTarget);
            for (int i = 0; i < board.Length; i++)
            {
                var u = ui[i];
                var b = board[i];
                float k = 1f - u.a / 255f;
                board[i] = new Color32(Over(u.r, b.r, k), Over(u.g, b.g, k), Over(u.b, b.b, k), 255);
            }
            var tex = new Texture2D(_target.width, _target.height, TextureFormat.RGBA32, false);
            tex.SetPixels32(board);
            File.WriteAllBytes(Path.Combine(Folder, name + ".png"), tex.EncodeToPNG());
            Object.Destroy(tex);
        }

        static LevelDefinition Level(int moves, GoalDefinition goal, params string[] board)
        {
            var level = LevelParser.FromLayers(board, null, null, null);
            level.Name = "Deneme";
            level.Id = 99;
            level.Moves = moves;
            level.ColorCount = 5;
            level.Goals.Add(goal ?? new GoalDefinition(GoalType.Color, 40, GemColor.Red));
            return level;
        }

        IEnumerator Begin(LevelDefinition level, int seed = 7)
        {
            _app.StartLevel(level, 0, seed);
            _app.Game.HintDelay = float.MaxValue;
            for (int i = 0; i < 3; i++) yield return null;
        }

        IEnumerator WaitIdle()
        {
            yield return null;
            while (Game.IsBusy) yield return null;
        }

        static IEnumerator GameTime(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end) yield return null;
        }

        [UnityTest]
        public IEnumerator TakeAll()
        {
            yield return Shot("01_seviye_secimi");
            _app.ShowLevelStart(8);
            yield return GameTime(Timing.ModalIn + 0.1f);
            yield return Shot("01b_seviye_basi");
            _app.LevelStart.Close();

            yield return Begin(_app.Levels[0]);
            yield return Shot("02_tahta_ve_hud");
            var firstMove = Game.Session.FindValidMoves().First(m => m.Type == MoveType.Swap);
            Game.OnTap(firstMove.A);
            yield return null;
            yield return Shot("03_secili_tas");
            Game.OnTap(firstMove.A);

            yield return Begin(Level(20, null, Even, Odd, Even, Odd, Even));
            Time.timeScale = SlowMotion;
            Game.RequestSwap(new GridPos(1, 1), new GridPos(2, 1));
            yield return GameTime(Timing.Swap * 0.6f);
            yield return Shot("04_takas_gidis");
            yield return GameTime(Timing.Swap * 0.8f);
            yield return Shot("05_takas_geri_donus");
            Time.timeScale = 1f;
            yield return WaitIdle();

            yield return Begin(Level(20, null, "BPOGY", "OGYPO", "RRBRY", "OGRPO", "YPOGY", "OGYPO", "YPOGY"));
            Time.timeScale = SlowMotion;
            Game.RequestSwap(new GridPos(2, 3), new GridPos(2, 4));
            yield return GameTime(Timing.Swap + Timing.Pop * 0.5f);
            yield return Shot("06_patlama_ve_ozel_tas_olusumu");
            yield return GameTime(Timing.SpecialForm + 0.1f);
            yield return Shot("07_dusme_ve_yeni_tas");
            Time.timeScale = 1f;
            yield return WaitIdle();

            yield return Fire("-", "08_roket_ates");
            yield return Fire("*", "09_bomba_ates");
            yield return Fire("@", "10_renk_bombasi_ates", new GridPos(3, 3));
            yield return Fire("^", "11_gudumlu_ates");

            yield return Begin(Level(20, null, "YPOGYO", "OGYPOG", "YP-*YO", "OGYPOG", "YPOGYO", "OGYPOG", "YPOGYO"));
            yield return Shot("12_ozel_taslar_tahtada");
            Time.timeScale = SlowMotion;
            Game.RequestSwap(new GridPos(2, 4), new GridPos(3, 4));
            yield return GameTime(Timing.Swap + 0.1f);
            yield return Shot("13_combo_roket_bomba");
            yield return GameTime(Timing.PraisePop);
            yield return Shot("13b_ovgu_muhtesem");
            Time.timeScale = 1f;
            yield return WaitIdle();

            yield return Begin(_app.Levels[2]);
            yield return Shot("14_kutulu_seviye");
            yield return Begin(_app.Levels[3]);
            yield return Shot("15_zeminli_seviye");
            yield return Begin(_app.Levels[4]);
            yield return Shot("16_delikli_seviye");
            yield return Begin(_app.Levels[8]);
            yield return Shot("17_uc_hedefli_buzlu_seviye");
            yield return Begin(_app.Levels[6]);
            yield return Shot("18_toplanan_obje_seviyesi");

            yield return Win("19_kazandin", "19a_kutlama_havai_fisek");
            yield return Lose("20_kaybettin");

            yield return Shape("21_uzun_telefon_20x9", 1080, 2400, new Rect(0f, 90f, 1080f, 2220f));
            yield return Shape("22_cok_uzun_telefon_21x9", 1080, 2520, new Rect(0f, 90f, 1080f, 2330f));
            yield return Shape("23_kisa_telefon_16x9", 720, 1280, null);
            yield return Shape("24_tablet_3x4", 1536, 2048, null);
        }

        IEnumerator Fire(string token, string shot, GridPos? cell = null)
        {
            var p = cell ?? new GridPos(2, 3);
            string[] rows = { "YPOGYO", "OGYPOG", "YPOGYO", "OGYPOG", "YPOGYO", "OGYPOG", "YPOGYO" };
            int row = rows.Length - 1 - p.Y;
            rows[row] = rows[row].Substring(0, p.X) + token + rows[row].Substring(p.X + 1);
            yield return Begin(Level(20, null, rows));
            Time.timeScale = SlowMotion;
            Game.OnTap(p);
            yield return GameTime(0.13f);
            yield return Shot(shot);
            Time.timeScale = 1f;
            yield return WaitIdle();
            Assert.IsNull(Game.LastMismatch);
        }

        IEnumerator Win(string shot, string celebrationShot = null)
        {
            yield return Begin(Level(9, new GoalDefinition(GoalType.Color, 3, GemColor.Red), "YPOGY", "OGYPO", "RRBRY", "OGRPO", "YPOGY"));
            Game.RequestSwap(new GridPos(2, 1), new GridPos(2, 2));
            yield return WaitIdle();
            if (celebrationShot != null)
            {
                yield return GameTime(Timing.WinDelay + Timing.WinSparkFlight + Timing.WinSparkInterval * 3f);
                yield return Shot(celebrationShot);
            }
            while (Game.Popup == null) yield return null;
            yield return GameTime(Timing.ModalIn + Timing.StarFill * 3f + 0.3f);
            yield return Shot(shot);
        }

        IEnumerator Lose(string shot)
        {
            yield return Begin(Level(1, null, "YPOGY", "OGYPO", "RRBRY", "OGRPO", "YPOGY"));
            Game.RequestSwap(new GridPos(2, 1), new GridPos(2, 2));
            yield return WaitIdle();
            yield return GameTime(Timing.LoseDelay + Timing.ModalIn + 0.2f);
            yield return Shot(shot);
        }

        IEnumerator Shape(string name, int width, int height, Rect? safeArea)
        {
            UseScreen(width, height, safeArea);
            yield return null;
            _app.ShowLevelSelect();
            for (int i = 0; i < 3; i++) yield return null;
            yield return Shot(name + "_seviye_secimi");
            yield return Begin(_app.Levels[8]);
            yield return Shot(name + "_oyun");
            yield return Win(name + "_pencere");
        }
    }
}
