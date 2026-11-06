using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace EmberThrone.Core.Tests
{
    /// <summary>
    /// Guards the design system (docs/design/design-system.md) by reading the theme files, so it runs in Unity and
    /// with dotnet: colors come only from tokens, text stays readable on its background, every image and slice
    /// fits, and every font has the Turkish letters.
    /// </summary>
    public class ThemeTests
    {
        const string TurkishLetters = "şŞğĞıİçÇöÖüÜ";

        static readonly Regex Comment = new Regex(@"/\*.*?\*/", RegexOptions.Singleline);
        static readonly Regex ColorLiteral = new Regex(@"#[0-9A-Fa-f]{3,8}\b|\brgba?\s*\(");
        static readonly Regex Rule = new Regex(@"([^{}]+)\{([^{}]*)\}");
        static readonly Regex Url = new Regex(@"url\(\s*""([^""]+)""\s*\)");

        static string _ui;

        static string UiFolder => _ui ?? (_ui = FindUiFolder());
        static string ThemeFolder => Path.Combine(UiFolder, "Theme");
        static IEnumerable<string> StyleFiles => Directory.GetFiles(ThemeFolder, "*.uss");
        static string Read(string path) => Comment.Replace(File.ReadAllText(path), "");

        /// <summary>Unity runs tests from the project folder, dotnet from its bin folder: walk up from both.</summary>
        static string FindUiFolder()
        {
            foreach (var start in new[] { Directory.GetCurrentDirectory(), TestContext.CurrentContext.TestDirectory })
                for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
                {
                    var path = Path.Combine(dir.FullName, "Assets", "EmberThrone", "Resources", "UI");
                    if (Directory.Exists(path)) return path;
                }
            Assert.Fail("Assets/EmberThrone/Resources/UI not found");
            return null;
        }

        static Dictionary<string, string> Tokens()
        {
            var tokens = new Dictionary<string, string>();
            foreach (Match m in Regex.Matches(Read(Path.Combine(ThemeFolder, "Tokens.uss")), @"(--[\w-]+)\s*:\s*([^;]+);"))
                tokens[m.Groups[1].Value] = m.Groups[2].Value.Trim();
            return tokens;
        }

        [Test]
        public void Colors_AreOnlyInTheTokenFile()
        {
            foreach (var file in StyleFiles)
            {
                if (Path.GetFileName(file) == "Tokens.uss") continue;
                var m = ColorLiteral.Match(Read(file));
                Assert.IsFalse(m.Success, Path.GetFileName(file) + " has a color outside the tokens: " + m.Value);
            }
        }

        [Test]
        public void EveryVariable_IsAToken()
        {
            var tokens = Tokens();
            foreach (var file in StyleFiles)
                foreach (Match m in Regex.Matches(Read(file), @"var\(\s*(--[\w-]+)\s*\)"))
                    Assert.IsTrue(tokens.ContainsKey(m.Groups[1].Value), Path.GetFileName(file) + ": " + m.Groups[1].Value + " is not a token");
        }

        // design-system.md, "Kontrast". Large text (72 px and up) and outlined text need 3, body text 4.5.
        [TestCase("--color-ink", "--color-face", 7.0)]
        [TestCase("--color-ink", "--color-face-deep", 7.0)]
        [TestCase("--color-ink-muted", "--color-face", 4.5)]
        [TestCase("--color-white", "--color-chip", 4.5)]
        [TestCase("--color-white", "--color-alert", 3.0)]
        [TestCase("--color-gold", "--color-outline", 3.0)]
        [TestCase("--color-white", "--color-outline", 3.0)]
        [TestCase("--color-white", "--color-cta-edge", 3.0)]
        [TestCase("--color-white", "--color-accent-edge", 3.0)]
        [TestCase("--color-primary", "--color-face", 3.0)]
        [TestCase("--color-alert", "--color-face", 3.0)]
        public void Text_IsReadableOnItsBackground(string text, string background, double minimum)
        {
            var tokens = Tokens();
            var fg = ParseColor(tokens[text]);
            var bg = ParseColor(tokens[background]);
            // A see-through background is checked over both white and black, whichever is worse.
            double ratio = Math.Min(Contrast(fg, Over(bg, 1.0)), Contrast(fg, Over(bg, 0.0)));
            Assert.GreaterOrEqual(ratio, minimum, text + " on " + background);
        }

        [Test]
        public void EveryImage_Exists_AndItsSlicesFit()
        {
            int images = 0;
            foreach (var file in StyleFiles)
                foreach (Match rule in Rule.Matches(Read(file)))
                {
                    string where = Path.GetFileName(file) + " " + rule.Groups[1].Value.Trim();
                    string body = rule.Groups[2].Value;
                    var url = Url.Match(body);
                    if (!url.Success) continue;
                    string path = Path.GetFullPath(Path.Combine(ThemeFolder, url.Groups[1].Value));
                    Assert.IsTrue(File.Exists(path), where + ": missing " + url.Groups[1].Value);
                    images++;
                    if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;

                    var bytes = ReadBinary(path);
                    int width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
                    int height = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
                    int Slice(string side)
                    {
                        var m = Regex.Match(body, @"-unity-slice-" + side + @"\s*:\s*(\d+)");
                        return m.Success ? int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
                    }
                    Assert.Less(Slice("left") + Slice("right"), width, where + ": slices wider than the image");
                    Assert.Less(Slice("top") + Slice("bottom"), height, where + ": slices taller than the image");
                }
            Assert.Greater(images, 0);
        }

        [Test]
        public void ThemeImports_Exist()
        {
            var tss = File.ReadAllText(Path.Combine(ThemeFolder, "EmberThrone.tss"));
            int count = 0;
            foreach (Match m in Url.Matches(tss))
            {
                if (m.Groups[1].Value.StartsWith("unity-theme://", StringComparison.Ordinal)) continue;
                Assert.IsTrue(File.Exists(Path.Combine(ThemeFolder, m.Groups[1].Value)), "missing " + m.Groups[1].Value);
                count++;
            }
            Assert.AreEqual(3, count, "Tokens, Components, Screens");
        }

        [Test]
        public void EveryFont_HasTheTurkishLetters()
        {
            var fonts = Directory.GetFiles(Path.Combine(UiFolder, "Fonts"), "*.ttf");
            Assert.IsNotEmpty(fonts);
            foreach (var font in fonts)
            {
                var codes = CharacterMap(ReadBinary(font));
                foreach (char c in TurkishLetters)
                    Assert.IsTrue(codes.Contains(c), Path.GetFileName(font) + " has no '" + c + "'");
            }
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>Images and fonts live in Git LFS; a checkout without LFS leaves small text pointers instead.</summary>
        static byte[] ReadBinary(string path)
        {
            var bytes = File.ReadAllBytes(path);
            string head = System.Text.Encoding.ASCII.GetString(bytes, 0, Math.Min(bytes.Length, 24));
            Assert.IsFalse(head.StartsWith("version https://git-lfs", StringComparison.Ordinal),
                Path.GetFileName(path) + " is a Git LFS pointer: run 'git lfs pull' (CI: checkout with lfs: true)");
            return bytes;
        }

        struct Rgba
        {
            public double R, G, B, A;
        }

        static Rgba ParseColor(string value)
        {
            if (value.StartsWith("#", StringComparison.Ordinal))
            {
                int v = int.Parse(value.Substring(1, 6), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                return new Rgba { R = (v >> 16) & 255, G = (v >> 8) & 255, B = v & 255, A = 1.0 };
            }
            var m = Regex.Match(value, @"rgba\(\s*([\d.]+)\s*,\s*([\d.]+)\s*,\s*([\d.]+)\s*,\s*([\d.]+)\s*\)");
            Assert.IsTrue(m.Success, "unknown color " + value);
            double P(int i) => double.Parse(m.Groups[i].Value, CultureInfo.InvariantCulture);
            return new Rgba { R = P(1), G = P(2), B = P(3), A = P(4) };
        }

        /// <summary>The color over a gray backdrop (1 = white, 0 = black).</summary>
        static Rgba Over(Rgba c, double backdrop)
        {
            double under = backdrop * 255.0 * (1.0 - c.A);
            return new Rgba { R = c.R * c.A + under, G = c.G * c.A + under, B = c.B * c.A + under, A = 1.0 };
        }

        static double Luminance(Rgba c)
        {
            double Channel(double v)
            {
                v /= 255.0;
                return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
            }
            return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
        }

        static double Contrast(Rgba a, Rgba b)
        {
            double la = Luminance(a), lb = Luminance(b);
            return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
        }

        /// <summary>The characters a TrueType font maps to a glyph (cmap formats 4 and 12).</summary>
        static HashSet<int> CharacterMap(byte[] f)
        {
            int U16(int o) => (f[o] << 8) | f[o + 1];
            long U32(int o) => ((long)f[o] << 24) | ((long)f[o + 1] << 16) | ((long)f[o + 2] << 8) | f[o + 3];

            int cmap = -1;
            for (int i = 0, tables = U16(4); i < tables; i++)
            {
                int record = 12 + i * 16;
                if (f[record] == 'c' && f[record + 1] == 'm' && f[record + 2] == 'a' && f[record + 3] == 'p') cmap = (int)U32(record + 8);
            }
            Assert.GreaterOrEqual(cmap, 0, "no cmap table");

            var codes = new HashSet<int>();
            for (int i = 0, subtables = U16(cmap + 2); i < subtables; i++)
            {
                int sub = cmap + (int)U32(cmap + 8 + i * 8);
                int format = U16(sub);
                if (format == 4)
                {
                    int segments2 = U16(sub + 6);
                    int ends = sub + 14, starts = ends + segments2 + 2, deltas = starts + segments2, offsets = deltas + segments2;
                    for (int s = 0; s < segments2; s += 2)
                    {
                        int start = U16(starts + s), end = U16(ends + s), delta = U16(deltas + s), rangeOffset = U16(offsets + s);
                        for (int c = start; c <= end && c != 0xFFFF; c++)
                        {
                            int glyph = rangeOffset == 0 ? (c + delta) & 0xFFFF : U16(offsets + s + rangeOffset + 2 * (c - start));
                            if (rangeOffset != 0 && glyph != 0) glyph = (glyph + delta) & 0xFFFF;
                            if (glyph != 0) codes.Add(c);
                        }
                    }
                }
                else if (format == 12)
                {
                    long groups = U32(sub + 12);
                    for (long g = 0; g < groups; g++)
                    {
                        int at = sub + 16 + (int)g * 12;
                        long first = U32(at), last = U32(at + 4), glyph = U32(at + 8);
                        for (long c = first; c <= last; c++)
                            if (glyph + (c - first) != 0) codes.Add((int)c);
                    }
                }
            }
            return codes;
        }
    }
}
