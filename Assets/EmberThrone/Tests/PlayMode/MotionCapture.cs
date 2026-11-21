using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using EmberThrone.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EmberThrone.Game.Tests
{
    [Explicit("Records 60 fps frame sequences of board animations; run on demand.")]
    public class MotionCapture
    {
        const int Fps = 60;
        const int Width = 720;
        const int Height = 1564;
        const float Inset = 60f;
        const int Quality = 90;
        const string Even = "YPOGYPOGY";
        const string Odd = "OGYPOGYPO";

        GameApp _app;
        FrameGrabber _grabber;
        Dictionary<int, int> _savedStars;
        string _savedProfile;
        GameController Game => _app.Game;

        static string Folder => Path.Combine(Path.GetDirectoryName(Application.dataPath), "TestResults", "motion");

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Directory.CreateDirectory(Folder);
            _savedStars = StarsBackup.Snapshot();
            StarsBackup.Restore(new Dictionary<int, int>());
            _savedProfile = StarsBackup.SnapshotProfile();
            StarsBackup.FreshProfile();
            Meta.Save();
            Time.timeScale = 1f;
            Time.captureFramerate = Fps;
            GameApp.ShowLoading = false;
            GameApp.ShowStories = false;
            GameApp.StreakBoosts = false;
            GameApp.OfferStarter = false;
            var safe = new Rect(0f, Inset, Width, Height - Inset * 2f);
            UiRoot.SafeAreaOverride = safe;
            UiRoot.ScreenSizeOverride = new Vector2Int(Width, Height);
            _app = GameApp.Create();
            _grabber = new FrameGrabber(_app);
            _grabber.Use(Width, Height, safe);
            for (int i = 0; i < 3; i++) yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.captureFramerate = 0;
            Time.timeScale = 1f;
            GameApp.ShowLoading = true;
            GameApp.ShowStories = true;
            GameApp.StreakBoosts = true;
            GameApp.OfferStarter = true;
            UiRoot.SafeAreaOverride = null;
            UiRoot.ScreenSizeOverride = null;
            Object.Destroy(_app.gameObject);
            _grabber.Release();
            StarsBackup.Restore(_savedStars);
            StarsBackup.RestoreProfile(_savedProfile);
            yield return null;
        }

        [UnityTest]
        [Timeout(1800000)]
        public IEnumerator Record()
        {
            yield return Clip("01_seviye_girisi", 1.0f, () =>
            {
                _app.StartLevel(_app.Levels[8], 8, 7);
                Game.HintDelay = float.MaxValue;
            });
            yield return Settle();

            yield return Begin(Level("YPOGYO", "OGYPOG", "YPOGYO", "OGYPGG", "YPGGYO", "OGYPOG", "YPOGYO"));
            yield return Clip("02_takas_eslesme_dusme", 1.3f, () => Game.RequestSwap(new GridPos(4, 3), new GridPos(4, 2)));
            yield return Settle();

            yield return Begin(Level("YPOGY", "OGYPO", "YPOGY", "OGYPO", "YPOGY"));
            yield return Clip("03_gecersiz_takas", 0.5f, () => Game.RequestSwap(new GridPos(1, 1), new GridPos(2, 1)));
            yield return Settle();

            yield return Begin(Level("BPOGY", "OGYPO", "RRBRY", "OGRPO", "YPOGY", "OGYPO", "YPOGY"));
            yield return Clip("04_ozel_tas_olusumu", 1.3f, () => Game.RequestSwap(new GridPos(2, 3), new GridPos(2, 4)));
            yield return Settle();

            yield return Fire("05_sutun_roketi", '|', new GridPos(4, 1), 1.2f);
            yield return Fire("06_satir_roketi", '-', new GridPos(1, 4), 1.2f);
            yield return Fire("07_bomba", '*', new GridPos(4, 4), 1.2f);
            yield return Fire("08_renk_bombasi", '@', new GridPos(4, 4), 2.2f);

            var rows = Square();
            rows[4] = "YPOG**OGY";
            yield return Begin(Level(rows));
            yield return Clip("09_bomba_bomba", 1.5f, () => Game.RequestSwap(new GridPos(5, 4), new GridPos(4, 4)));
            yield return Settle();

            _app.ShowLevelSelect();
            for (int i = 0; i < 4; i++) yield return null;
            yield return Clip("10_pencere_acilisi", 0.4f, () => _app.ShowLevelStart(8));
            _app.LevelStart.Close();

            var last = Level("YPOGY", "OGYPO", "RRBRY", "OGRPO", "YPOGY");
            last.Moves = 1;
            yield return Begin(last);
            Game.RequestSwap(new GridPos(2, 1), new GridPos(2, 2));
            while (Game.Popup == null) yield return null;
            yield return Clip("11_sonuc_penceresi", 0.6f, () => { });
        }

        IEnumerator Fire(string name, char token, GridPos cell, float seconds)
        {
            var rows = Square();
            int row = rows.Length - 1 - cell.Y;
            rows[row] = rows[row].Substring(0, cell.X) + token + rows[row].Substring(cell.X + 1);
            yield return Begin(Level(rows));
            yield return Clip(name, seconds, () => Game.OnTap(cell));
            yield return Settle();
        }

        IEnumerator Clip(string name, float seconds, System.Action start)
        {
            var dir = Path.Combine(Folder, name);
            Directory.CreateDirectory(dir);
            var times = new StringBuilder("frame,time,unity_frame\n");
            int frames = Mathf.CeilToInt(seconds * Fps);
            float origin = Time.time;
            start();
            for (int i = 0; ; i++)
            {
                times.Append(i).Append(',')
                    .Append((Time.time - origin).ToString("0.0000", CultureInfo.InvariantCulture)).Append(',')
                    .Append(Time.frameCount).Append('\n');
                var tex = _grabber.Grab();
                File.WriteAllBytes(Path.Combine(dir, i.ToString("000") + ".jpg"), tex.EncodeToJPG(Quality));
                Object.Destroy(tex);
                if (i == frames) break;
                yield return null;
            }
            File.WriteAllText(Path.Combine(dir, "times.csv"), times.ToString());
            Debug.Log("[motion] " + name + " " + (frames + 1) + " frames");
        }

        IEnumerator Begin(LevelDefinition level)
        {
            _app.StartLevel(level, 0, 7);
            Game.HintDelay = float.MaxValue;
            for (int i = 0; i < 3; i++) yield return null;
            float end = Time.time + Game.View.IntroLength + 0.1f;
            while (Time.time < end) yield return null;
        }

        IEnumerator Settle()
        {
            yield return null;
            while (Game.IsBusy) yield return null;
            Assert.IsNull(Game.LastMismatch);
        }

        static string[] Square()
        {
            var rows = new string[9];
            for (int i = 0; i < rows.Length; i++) rows[i] = i % 2 == 0 ? Even : Odd;
            return rows;
        }

        static LevelDefinition Level(params string[] board)
        {
            var level = LevelParser.FromLayers(board, null, null, null);
            level.Name = "Deneme";
            level.Id = 99;
            level.Moves = 20;
            level.ColorCount = 5;
            level.Goals.Add(new GoalDefinition(GoalType.Color, 40, GemColor.Red));
            return level;
        }
    }
}
