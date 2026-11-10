# Teknik Şartname

*v1 · 2026-11-06 — EmberThrone'un bütün kod, asset ve araçları için geçerli*

## Ekran ve ölçek

| Konu | Standart |
|---|---|
| Motor | Unity 6000.3.25f1 (6.3 LTS), URP 2D, yalnız Android, dikey |
| Referans çözünürlük | 1080×1920. UI Toolkit `PanelSettings` koddan kurulur: Scale With Screen Size, **Expand** |
| Güvenli alan | HUD ve pencereler güvenli alan içinde (`UiRoot.ApplyLayout`); arka plan tam ekran |
| Test oranları | 20:9, 21:9, 16:9 ve tablet 3:4. Ekran turu dördünü de çeker |
| Sahne | Tek, boş `Bootstrap.unity`. Sahne, arayüz ve tahta koddan kurulur; prefab yoktur. `EmberThrone > Setup Project` sahneyi ve build ayarlarını yeniden kurar |

## Görseller

| Konu | Standart |
|---|---|
| Oyun görselleri | Çizim sayfalarından kesilir: kaynak `ArtSource/sheets/`, araç `tools/sprites/cut_sheets.py`, tarif `sheets.json` ([README](../../ArtSource/sheets/README.md)). Her sprite tek komutla yeniden üretilir |
| Arayüz parçaları | 9-slice PNG. Pencere, butonlar, halka, hap, etiket ve kurdele `ui_kit.png` sayfasından; onay işareti ve ışıltılar `tools/ui_chrome/`'dan. USS `-unity-slice-*` ile gerer |
| Harita | Bölge başına bir görsel (`map_N.jpg`), USS'te en-boy oranıyla (`padding-top` yüzdesi). Düğüm yerleri `LevelMap.cs`'teki yol noktalarından, eşit aralıkla hesaplanır |
| Renkler | Yalnız `Resources/UI/Theme/Tokens.uss`. Başka dosyada renk yazılmaz (`ThemeTests` denetler) |
| İçe aktarma | `Editor/GameTextureImport.cs`: mipmap + trilinear, clamp, Android'de ASTC 4x4. Tahta parçaları en fazla 256 px, `bg_*` 2048 px |
| Tasarım dili | [design-system.md](design-system.md): token'lar, bileşenler, hareket, ses ve titreşim paleti, erişilebilirlik |

## Tipografi

- Baloo 2 Bold ve ExtraBold. Google Fonts'un değişken fontundan sabit ağırlık olarak, yalnız Latin harfleriyle `tools/fonts/make_baloo2.py` üretir.
- Her font Türkçe harfleri (ş, ğ, ı, İ, ç, ö, ü) içermeli; `ThemeTests` kontrol eder.

## Ses

- Efektler `tools/audio/generate_sfx.py` ile üretilir: `python3 tools/audio/generate_sfx.py` (ya da tek ses: `... sfx_bomb`). Çıktı `Assets/EmberThrone/Resources/Audio/sfx_<ad>.wav`, 44,1 kHz mono.
- `Feel/Sound.cs`: `Sfx` adı dosya adına çevrilir (`LineBlast` → `sfx_line_blast`); kamerada tek `AudioListener` var. Zincirleme eşleşmede perde her dalgada majör dizide bir basamak yükselir.
- Palet ve olay tablosu: [design-system.md](design-system.md). Müzik henüz yok.

## Cihazda saklananlar (PlayerPrefs)

| Anahtar | İçerik |
|---|---|
| `et.stars.<id>` | Seviyenin en iyi yıldızı |
| `et.booster.<Hammer\|RowBlast\|ColumnBlast\|Shuffle>` | Booster hakkı; yoksa `GameRules.StartingBoosters` |
| `et.sound`, `et.haptics`, `et.shake` | Ayarlar (ses, titreşim, ekran sarsıntısı): 1 açık, 0 kapalı |

PlayMode testleri bu anahtarları başta kaydeder, sonda geri yazar (`StarsBackup`); sahibinin kendi oyunu bozulmaz.

## Adlandırma

| Tür | Biçim | Örnek |
|---|---|---|
| Taş | `piece_<renk>` | `piece_R` |
| Özel taş | `sp_<ad>` | `sp_rocket_v` |
| Engel | `ob_<ad><hp>` | `ob_box2` |
| Arka plan | `bg_<yer>` | `bg_game.jpg` |
| Arayüz ikonu | `ui_<ad>`, `ic_<ad>`, `bo_<ad>` | `ui_star`, `ic_home`, `bo_hammer` |
| Bölge haritası | `map_<sıra>` | `map_2.jpg` |
| Maskot | `mascot`, `mascot_<duygu>` | `mascot_sad` |
| Arayüz parçası | bileşen adı | `btn_green` |
| USS sınıfı | `et-` önekli, BEM | `et-btn--cta` |
| Seviye | `level_NNNN.json` | `level_0007.json` |

## Klasör yapısı

| Yol | İçerik |
|---|---|
| `Assets/EmberThrone/Scripts/Core/` | Saf C# oyun motoru, asmdef `EmberThrone.Core` (`noEngineReferences`): tahta, eşleşme, özel taşlar ve kombolar, engeller, yerçekimi, hedefler, booster'lar, seviye JSON'u, bot |
| `Assets/EmberThrone/Scripts/Game/` | Unity tarafı, asmdef `EmberThrone.Game`: `GameApp` (giriş noktası), `GameController` (tek seviye), `Board/` (görünüm, girdi, efektler), `UI/` (UI Toolkit ekranları), `Art/` (render'lar ve üretilen şekiller), `Feel/` (sarsıntı, titreşim, olay tablosu), `Anim/` (tween'ler, `Timing`) |
| `Assets/EmberThrone/Editor/` | Yalnız editör: `ProjectSetup`, `GameTextureImport` |
| `Assets/EmberThrone/Tests/EditMode/` | NUnit testleri; Unity Test Runner ve `dotnet test` ikisinde de koşar |
| `Assets/EmberThrone/Tests/PlayMode/` | Oyunu sanal girdiyle oynatan testler ve ekran turu (`ScreenshotTour`) |
| `Assets/EmberThrone/Resources/Levels/` | Seviye dosyaları |
| `Assets/EmberThrone/Resources/Board/` | Tahta render'ları |
| `Assets/EmberThrone/Resources/UI/` | `Theme/` (USS, TSS), `Chrome/` (9-slice), `Art/` (3B ikonlar), `Fonts/` |
| `Assets/EmberThrone/Resources/Audio/` | Ses efektleri (`sfx_*.wav`) |
| `Assets/_Game/` | Eski Faz 0 altyapısı (servis bulucu, kayıt, havuz, Firebase taslakları). Oyun kullanmaz; Faz 3 için saklanır |
| `ArtSource/sheets/` | Çizim sayfaları ve kesim tarifi; oyundaki görsellerin kaynağı |
| `ArtSource/blender/` | v3 dönemindeki Blender scriptleri; oyunda artık kullanılmıyor |
| `tools/CoreLib`, `tools/CoreTests` | Unity kaynaklarını derleyen dotnet projeleri (netstandard2.1, C# 9) |
| `tools/LevelSim` | Bot simülatörü: kazanma oranı, değişmez denetimi, hamle ayarı, oyun dökümü |
| `tools/gen_meta.py` | Eksik `.meta` dosyalarını üretir |
| `tools/sprites/` | Çizim sayfalarından sprite, arka plan, harita ve arayüz dokusu üretir (Pillow + numpy) |
| `tools/ui_chrome/`, `tools/fonts/`, `tools/audio/`, `tools/icon/` | Arayüz parçası, font, ses ve uygulama ikonu üreticileri. Pillow gerektirenler (`tools/icon/`) Pillow kurulu Python ile çalışır |

## Kod kuralları

1. **Core, UnityEngine'e dokunmaz.** En fazla C# 9 ve .NET Standard 2.1 (csproj zorlar).
2. **Determinizm.** Core'da rastgelelik yalnız `Pcg32`'den gelir; `System.Random`, saat ya da hash sırasına bağlı mantık yok. Aynı seviye + tohum + hamleler ⇒ aynı oyun (tekrar, sunucu doğrulaması ve bot buna dayanır).
3. **Sunum yalnız izler.** Unity kodu `GameSession`'ı çağırır ve dönen `MoveOutcome.Steps` adımlarını canlandırır; `Board`'u hiç değiştirmez.
4. **Denge veride.** `GameRules` ve seviye JSON'u; kodda sihirli sayı yok.
5. **`.meta` dosyaları.** Editör açık değilken `Assets/` altına kod, JSON ya da klasör eklenince `python3 tools/gen_meta.py`. PNG, WAV, font gibi içe aktarılan dosyaların `.meta`'sını Unity yazar (editörü ya da batch testlerini bir kez açmak yeter); araç onları atlar. Var olan GUID değişmez.
6. **Testler.** Yalnız NUnit 3 klasik assert'leri (`Assert.AreEqual/IsTrue/Throws`, `CollectionAssert`); Unity'nin NUnit 3.5 sürümü yüzünden `Assert.Multiple` ve NUnit 4 API'leri yok. Tahta yardımcıları `TestUtil.cs`'de (satırlar üstten, y=0 altta).
7. **Özgün kimlik.** İsimler, karakterler, görseller, müzik, seviyeler ve arayüz EmberThrone'a aittir; başka oyunlarla yalnız türün genel mekanikleri ortaktır.
8. **Yorum satırı yok.** Kod, USS, Python ve workflow dosyaları yorumsuzdur; açıklama gereken bilgi bu belgeye ya da `docs/` altına yazılır.
9. **Arayüz.** C# yalnız `et-` sınıfları ekler ve çıkarır. İstisnalar: güvenli alan boşlukları, `HudView.Height`, animasyon stilleri (Tween ve zamanlayıcı), çalışma anında atanan sprite'lar ve harita düğümlerinin yüzde konumu.

## Seviye biçimi

```json
{
  "id": 12, "name": "Ash Gate", "moves": 24, "colors": 5, "difficulty": "hard",
  "goals": [ {"type": "box"}, {"type": "color", "color": "R", "count": 20} ],
  "board":    [ "..1111..", "..." ],
  "floor":    [ "00111100", "..." ],
  "cover":    [ "00000000", "..." ],
  "spawners": [ "SSSSSSSS", "..." ],
  "collectibles": {"total": 3, "maxOnBoard": 2, "spawnChance": 0.12}
}
```

- Satırlar üstten alta yazılır; satır içindeki boşluklar yok sayılır.
- `board` zorunlu. `floor` ve `cover` hücre başına HP, `spawners` doğurucu hücreler (yoksa her sütunun en üstü), `collectibles` isteğe bağlı.
- Tahta işaretleri: `.` rastgele taş, `#` delik, `_` boş, `R G B Y P O` taş, `1-9` kutu HP, `-` satır roketi, `|` sütun roketi, `*` bomba, `^` kor kuşu, `@` renk bombası, `$` toplanan obje.
- Hedef türleri: `color`, `box`, `floor`, `cover`, `collectible` (1 ile 3 hedef). `count` -1 ya da yoksa tahtadakilerin hepsi.
- Boyut: en az 3, en fazla 12 genişlik ve 14 yükseklik.

## Motor özeti

- Özel taşlar renksizdir. Sırada 4 → roket (eşleşmeye dik, `GameRules.RocketPerpendicularToMatch`), 2x2 → kor kuşu, L/T → bomba (5x5), sırada 5 → renk bombası. Dokunmak ya da kaydırmak ateşler; iki özel taş birlikte kombo yapar (`ComboKind`).
- Hücre katmanları: zemin (HP) → taş → örtü (HP). Örtü taşı kilitler ve vuruşu emer. Kutular komşu eşleşme ve patlamadan hasar alır. Toplanan objeler patlamadan etkilenmez, sütunun en alt hücresine inince toplanır.
- Çözüm sırası: kaydırma/dokunma → temizleme dalgaları (zincirlenen özel taşlar sonraki dalgada) → tik tabanlı yerçekimi (önce düz, engelin çevresinden çapraz; doğurucular doldurur) → zincirleme → kazanma/kaybetme → hamle kalmadıysa otomatik karıştırma.

## Testler ve komutlar

Her push'tan önce:

```bash
python3 tools/gen_meta.py --check
dotnet test tools/CoreTests/EmberThrone.Core.Tests.csproj -c Release
dotnet run --project tools/LevelSim -c Release -- --runs 100 --validate --fail-on-error Assets/EmberThrone/Resources/Levels
```

Unity testleri (editör kapalıyken; batch modu açık bir projeyi açamaz):

```bash
"C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults TestResults/editmode.xml
"C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe" -batchmode -nographics -projectPath . -runTests -testPlatform PlayMode -testResults TestResults/playmode.xml
```

Ekran turu (grafik gerekir, `-nographics` yok). Bütün ekranları ve animasyonları ağır çekimde oynatır, her telefon biçimi için `TestResults/screens` altına PNG yazar:

```bash
"C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe" -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter EmberThrone.Game.Tests.ScreenshotTour.TakeAll -testResults TestResults/tour.xml
```

- Batch modunda `WaitForEndOfFrame` gelmez; tur kamerayı kendisi çizer.
- Arayüz kendi render texture'ına çizilir. Bu doku doğrusal uzayda karışmış, önceden çarpılmış renk tutar; birleştirme doğrusal uzayda yapılır, yoksa yarı saydam arayüz fazla parlak görünür.

Seviye araçları:

```bash
dotnet run --project tools/LevelSim -c Release -- --runs 300 --mistakes 0.35 Assets/EmberThrone/Resources/Levels
dotnet run --project tools/LevelSim -c Release -- --runs 300 --mistakes 0.35 --tune 0.8 --write Assets/EmberThrone/Resources/Levels/level_0007.json
dotnet run --project tools/LevelSim -c Release -- --trace 3 Assets/EmberThrone/Resources/Levels/level_0001.json
```

Birincisi zorluk tablosu (açgözlü bot, %35 rastgele hamle ≈ sıradan oyuncu), ikincisi hedef kazanma oranına göre hamle sayısını bulup dosyaya yazar, üçüncüsü tek oyunu hamle hamle döker.

## Derleme ve CI

- `core-tests.yml`: Unity lisansı gerekmez; `.meta` denetimi, motor testleri, seviye simülasyonu.
- `build-android.yml`: `main`'e her push'ta EditMode + PlayMode testleri, sonra geliştirme APK'sı. Unity secret'ları yokken atlanır.
- `release-android.yml`: `v*` etiketi ya da elle çalıştırma → imzalı AAB.
- APK yalnız GitHub Actions'ta üretilir; ayrıntılar [../CI-ANDROID.md](../CI-ANDROID.md) ve [../release/README.md](../release/README.md).

## Versiyon kontrol

- Tek branch: `main`. Commit'ler GitHub Desktop'tan atılır.
- Binary dosyalar Git LFS'te (`.gitattributes`). Anahtar deposu, şifre ve yeniden dağıtılamayan lisanslı paketler repoya girmez (repo public).
- Commit mesajı İngilizce, emir kipinde, ilk satır en fazla ~60 karakter; bir commit tek konu.

## "Tamamlandı" (Definition of Done)

- Testler geçer: dotnet, Unity EditMode ve PlayMode.
- Ekran turu dört ekran biçiminde gözle kontrol edilmiştir; görsel iş `GDD/Referances` ile yan yana karşılaştırılmıştır.
- `gen_meta.py --check` ve seviye simülasyonu temizdir.
- ROADMAP, karar kaydı ve gerekiyorsa bu belge güncellenmiştir.
- Doğrulanamayan kısım (ör. telefonda his) açıkça yazılmıştır.
