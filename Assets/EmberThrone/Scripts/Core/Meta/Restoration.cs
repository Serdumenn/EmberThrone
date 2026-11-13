using System;
using System.Collections.Generic;

namespace EmberThrone.Core
{
    public enum RestoreGoal
    {
        Levels,
        Stars,
        Boosters,
    }

    public sealed class RestoreTask
    {
        public readonly string Name;
        public readonly RestoreGoal Goal;
        public readonly int Target;
        public readonly Reward[] Rewards;

        public RestoreTask(string name, RestoreGoal goal, int target, params Reward[] rewards)
        {
            Name = name;
            Goal = goal;
            Target = target;
            Rewards = rewards;
        }
    }

    public sealed class RestoreArea
    {
        public readonly RestoreTask[] Tasks;
        public readonly Reward[] Bonus;

        public RestoreArea(Reward[] bonus, params RestoreTask[] tasks)
        {
            Bonus = bonus;
            Tasks = tasks;
        }
    }

    public readonly struct RestoreState
    {
        public readonly RestoreTask Task;
        public readonly int Progress;
        public readonly bool Claimed;

        public RestoreState(RestoreTask task, int progress, bool claimed)
        {
            Task = task;
            Progress = Math.Min(progress, task.Target);
            Claimed = claimed;
        }

        public bool Done => Progress >= Task.Target;
    }

    public sealed class RestoreRules
    {
        public RestoreArea[] Areas =
        {
            new RestoreArea(new[] { Reward.Coins(300), Reward.Pack() },
                new RestoreTask("Yel Değirmeni", RestoreGoal.Levels, 2, Reward.Coins(100)),
                new RestoreTask("Fırın", RestoreGoal.Stars, 6, Reward.Coins(150)),
                new RestoreTask("Pazar Yeri", RestoreGoal.Boosters, 2, Reward.Of(BoosterType.Hammer)),
                new RestoreTask("Köy Evleri", RestoreGoal.Levels, 6, Reward.Coins(200)),
                new RestoreTask("Köy Ocağı", RestoreGoal.Stars, 20, Reward.Coins(300))),
            new RestoreArea(new[] { Reward.Coins(400), Reward.Pack() },
                new RestoreTask("Büyük Fırın", RestoreGoal.Levels, 3, Reward.Coins(150)),
                new RestoreTask("Un Değirmeni", RestoreGoal.Stars, 8, Reward.Coins(200)),
                new RestoreTask("Ekmek Pazarı", RestoreGoal.Boosters, 3, Reward.Of(BoosterType.RowBlast)),
                new RestoreTask("Çeşme Meydanı", RestoreGoal.Levels, 7, Reward.Coins(250)),
                new RestoreTask("Dere Köprüsü", RestoreGoal.Stars, 22, Reward.Coins(350))),
            new RestoreArea(new[] { Reward.Coins(500), Reward.Gems(10), Reward.Pack() },
                new RestoreTask("Demirci Ocağı", RestoreGoal.Levels, 3, Reward.Coins(150)),
                new RestoreTask("Bakırcılar", RestoreGoal.Stars, 10, Reward.Coins(200)),
                new RestoreTask("Kalkan Tezgâhı", RestoreGoal.Boosters, 3, Reward.Of(BoosterType.ColumnBlast)),
                new RestoreTask("Çarşı Kapısı", RestoreGoal.Levels, 8, Reward.Coins(250)),
                new RestoreTask("Büyük Örs", RestoreGoal.Stars, 24, Reward.Coins(400))),
            new RestoreArea(new[] { Reward.Coins(500), Reward.Pack() },
                new RestoreTask("Deniz Feneri", RestoreGoal.Levels, 3, Reward.Coins(150)),
                new RestoreTask("Balık Pazarı", RestoreGoal.Stars, 10, Reward.Coins(200)),
                new RestoreTask("Balıkçı İskelesi", RestoreGoal.Boosters, 3, Reward.Of(BoosterType.Shuffle)),
                new RestoreTask("Çapa Meydanı", RestoreGoal.Levels, 8, Reward.Coins(250)),
                new RestoreTask("Kayıkhane", RestoreGoal.Stars, 24, Reward.Coins(400))),
            new RestoreArea(new[] { Reward.Coins(600), Reward.Gems(10), Reward.Pack() },
                new RestoreTask("Kütüphane Kulesi", RestoreGoal.Levels, 3, Reward.Coins(150)),
                new RestoreTask("Gözlemevi", RestoreGoal.Stars, 10, Reward.Coins(200)),
                new RestoreTask("Okuma Bahçesi", RestoreGoal.Boosters, 4, Reward.Of(BoosterType.Hammer, 2)),
                new RestoreTask("Küre Meydanı", RestoreGoal.Levels, 8, Reward.Coins(250)),
                new RestoreTask("Ciltçi Dükkânı", RestoreGoal.Stars, 25, Reward.Coins(450))),
            new RestoreArea(new[] { Reward.Coins(600), Reward.Pack() },
                new RestoreTask("Sera", RestoreGoal.Levels, 3, Reward.Coins(150)),
                new RestoreTask("Arı Evi", RestoreGoal.Stars, 10, Reward.Coins(200)),
                new RestoreTask("Çiçek Labirenti", RestoreGoal.Boosters, 4, Reward.Of(BoosterType.Shuffle)),
                new RestoreTask("Nilüfer Göleti", RestoreGoal.Levels, 8, Reward.Coins(250)),
                new RestoreTask("Gül Çardağı", RestoreGoal.Stars, 25, Reward.Coins(450))),
            new RestoreArea(new[] { Reward.Coins(650), Reward.Pack() },
                new RestoreTask("Maden Girişi", RestoreGoal.Levels, 3, Reward.Coins(150)),
                new RestoreTask("Cevher Kulesi", RestoreGoal.Stars, 10, Reward.Coins(200)),
                new RestoreTask("Maden Rayları", RestoreGoal.Boosters, 4, Reward.Of(BoosterType.RowBlast)),
                new RestoreTask("Kristal Anıt", RestoreGoal.Levels, 8, Reward.Coins(250)),
                new RestoreTask("Mücevher Atölyesi", RestoreGoal.Stars, 25, Reward.Coins(450))),
            new RestoreArea(new[] { Reward.Coins(700), Reward.Pack() },
                new RestoreTask("Dağ Evi", RestoreGoal.Levels, 3, Reward.Coins(150)),
                new RestoreTask("Teleferik", RestoreGoal.Stars, 10, Reward.Coins(200)),
                new RestoreTask("Kızak Tepesi", RestoreGoal.Boosters, 4, Reward.Of(BoosterType.ColumnBlast)),
                new RestoreTask("Gözcü Kulesi", RestoreGoal.Levels, 8, Reward.Coins(250)),
                new RestoreTask("Taş Köprü", RestoreGoal.Stars, 25, Reward.Coins(450))),
            new RestoreArea(new[] { Reward.Coins(750), Reward.Pack(2) },
                new RestoreTask("Kayıkhane", RestoreGoal.Levels, 3, Reward.Coins(150)),
                new RestoreTask("Balıkçı Kulübesi", RestoreGoal.Stars, 10, Reward.Coins(200)),
                new RestoreTask("Ada Köşkü", RestoreGoal.Boosters, 4, Reward.Of(BoosterType.Hammer, 2)),
                new RestoreTask("Çan Kulesi", RestoreGoal.Levels, 8, Reward.Coins(250)),
                new RestoreTask("Göl İskelesi", RestoreGoal.Stars, 25, Reward.Coins(450))),
            new RestoreArea(new[] { Reward.Coins(800), Reward.Gems(20), Reward.Pack(2) },
                new RestoreTask("Kor Kulesi", RestoreGoal.Levels, 3, Reward.Coins(200)),
                new RestoreTask("Büyük Kapı", RestoreGoal.Stars, 10, Reward.Coins(250)),
                new RestoreTask("Fener Bahçesi", RestoreGoal.Boosters, 4, Reward.Of(BoosterType.Shuffle, 2)),
                new RestoreTask("Büyük Ocak", RestoreGoal.Levels, 8, Reward.Coins(300)),
                new RestoreTask("Ocak Salonu", RestoreGoal.Stars, 25, Reward.Coins(500))),
        };
    }

    public sealed class Restoration
    {
        public readonly RestoreRules Rules;
        readonly Economy _economy;

        Profile Profile => _economy.Profile;

        public Restoration(RestoreRules rules, Economy economy)
        {
            Rules = rules;
            _economy = economy;
            Fit();
        }

        void Fit()
        {
            int n = Rules.Areas.Length;
            Profile.RestoreLevels = Grow(Profile.RestoreLevels, n);
            Profile.RestoreStars = Grow(Profile.RestoreStars, n);
            Profile.RestoreBoosters = Grow(Profile.RestoreBoosters, n);
            Profile.RestoreClaimed = Grow(Profile.RestoreClaimed, n);
        }

        static int[] Grow(int[] values, int count)
        {
            if (values.Length == count) return values;
            var grown = new int[count];
            Array.Copy(values, grown, Math.Min(count, values.Length));
            return grown;
        }

        public int Count => Rules.Areas.Length;

        bool Valid(int area) => area >= 0 && area < Rules.Areas.Length;

        int ProgressOf(int area, RestoreGoal goal)
        {
            switch (goal)
            {
                case RestoreGoal.Levels: return Profile.RestoreLevels[area];
                case RestoreGoal.Stars: return Profile.RestoreStars[area];
                default: return Profile.RestoreBoosters[area];
            }
        }

        public bool IsClaimed(int area, int task) => Valid(area) && (Profile.RestoreClaimed[area] & (1 << task)) != 0;

        public RestoreState[] Tasks(int area)
        {
            if (!Valid(area)) return new RestoreState[0];
            var tasks = Rules.Areas[area].Tasks;
            var states = new RestoreState[tasks.Length];
            for (int i = 0; i < tasks.Length; i++) states[i] = new RestoreState(tasks[i], ProgressOf(area, tasks[i].Goal), IsClaimed(area, i));
            return states;
        }

        public int ClaimedCount(int area)
        {
            int n = 0;
            foreach (var state in Tasks(area))
                if (state.Claimed) n++;
            return n;
        }

        public int Claimable(int area)
        {
            int n = 0;
            foreach (var state in Tasks(area))
                if (state.Done && !state.Claimed) n++;
            return n;
        }

        public bool Restored(int area) => Valid(area) && ClaimedCount(area) >= Rules.Areas[area].Tasks.Length;

        public int Focus(int reached)
        {
            int last = Math.Min(reached, Rules.Areas.Length - 1);
            for (int area = 0; area <= last; area++)
                if (!Restored(area)) return area;
            return Math.Max(0, last);
        }

        public void ReportWin(int area, int newStars, bool firstTime)
        {
            if (!Valid(area)) return;
            if (firstTime) Profile.RestoreLevels[area]++;
            if (newStars > 0) Profile.RestoreStars[area] += newStars;
        }

        public void ReportBooster(int area)
        {
            if (Valid(area)) Profile.RestoreBoosters[area]++;
        }

        public bool Seed(IList<int> levels, IList<int> stars)
        {
            if (Profile.RestoreSeeded) return false;
            Profile.RestoreSeeded = true;
            for (int area = 0; area < Rules.Areas.Length; area++)
            {
                if (area < levels.Count) Profile.RestoreLevels[area] = Math.Max(Profile.RestoreLevels[area], levels[area]);
                if (area < stars.Count) Profile.RestoreStars[area] = Math.Max(Profile.RestoreStars[area], stars[area]);
            }
            return true;
        }

        public Reward[] Claim(int area, int task)
        {
            var tasks = Tasks(area);
            if (task < 0 || task >= tasks.Length || tasks[task].Claimed || !tasks[task].Done) return null;
            Profile.RestoreClaimed[area] |= 1 << task;
            var rewards = new List<Reward>(tasks[task].Task.Rewards);
            if (Restored(area)) rewards.AddRange(Rules.Areas[area].Bonus);
            foreach (var reward in rewards) _economy.Grant(reward);
            return rewards.ToArray();
        }
    }
}
