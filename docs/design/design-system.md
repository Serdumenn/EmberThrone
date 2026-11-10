# Design System — Kor

*v1 · 2026-11-06 · Kaynak: Kapı 2'de ara seviye olarak kabul edilen [style frame v3](style-frames/index.html)*

Tüm ölçüler **referans pikseldir** (1080×1920 tuval, UI Toolkit `PanelSettings`: Scale With Screen Size, Expand). Uygulamada tek kaynak `Assets/EmberThrone/Resources/UI/Theme/Tokens.uss` dosyasıdır; bu doküman onun açıklamasıdır.

**UI Toolkit'in sınırı:** USS'de renk geçişi (gradient) ve gölge (box-shadow) yok. v3'ün altın kenarları, parlak butonları ve gölgeleri bu yüzden **dilimlenebilir görsellere** (9-slice PNG) dönüştürülür. Kaynak bu dokümandaki HTML/CSS'tir: `tools/ui_chrome/` her bileşeni şeffaf PNG olarak çizer, USS `-unity-slice-*` ile kenarları korunarak esnetir. Renk, yazı ve boşluk ise USS token'larıdır.

## İlkeler
1. **Tahta kahramandır.** Oyun sırasında arayüz kenarlarda kalır; tahtanın üstüne yalnız kutlama yazıları çıkar.
2. **Her şey hacimli.** Düz renk yüzey yok: her panel altın kenarlı, her buton parlak bantlı ve alt dudaklı.
3. **Yazı her zemin üstünde okunur.** Renkli zemindeki beyaz yazı her zaman koyu dış çizgiyle kullanılır.
4. **Renk tek başına anlam taşımaz.** Taşlarda renk + şekil, durumlarda ikon + yazı.

## Renkler

### Tema: Kor (varsayılan)

| Token | Değer | Kullanım |
|---|---|---|
| `--color-primary-light` | `#FF9440` | Başlık bandı, sekme, nav üst ışığı |
| `--color-primary` | `#E2541A` | Başlık bandı ve aktif sekme gövdesi |
| `--color-primary-deep` | `#A3330C` | Bandın alt tonu, desenli zemin |
| `--color-outline` | `#4A1604` | Panel ve butonların dış çizgisi, beyaz yazının dış çizgisi |
| `--color-nav-top`, `--color-nav-bottom` | `#B4400F`, `#6A1F06` | Alt menü zemini |
| `--color-face`, `--color-face-deep` | `#FFF8E9`, `#FFE5BB` | Panel içi (krem yüz) |
| `--color-ink` | `#6A2A0A` | Panel içi metin ve sayılar |
| `--color-ink-muted` | `#86532A` | İkincil metin ("Skor", açıklamalar) |
| `--color-gold-light`, `--color-gold`, `--color-gold-deep` | `#FFF7C4`, `#FFD447`, `#B96A05` | Altın kenar, başlık yazısı, şeritler |
| `--color-cta`, `--color-cta-edge` | `#44AD1C`, `#18480A` | Yeşil ana buton ("Oyna", "Devam", fiyat) |
| `--color-accent`, `--color-accent-edge` | `#F2780E`, `#6A2A04` | Turuncu ikincil buton ("Yak") |
| `--color-alert`, `--color-alert-edge` | `#E8102A`, `#5A0610` | Kapat (X), sayaç rozeti, "Popüler" |
| `--color-chip` | `#1E1030` (%90) | Koyu hap: süre, "Can iste", seviye etiketi |
| `--color-white` | `#FFFFFF` | Renkli zemin ve koyu hap üstündeki yazı |
| `--color-highlight` | `rgba(255,255,255,.9)` | Krem panelde sayının altındaki ışık (hamle sayısı) |
| `--color-scrim` | `rgba(10,5,25,.78)` | Pencere arkası perde |
| `--color-board-back` | `rgba(26,16,58,.72)` | Tahta zemini |
| `--color-tile`, `--color-tile-alt` | `rgba(236,244,255,.93)`, `rgba(208,224,255,.93)` | Tahta kareleri (dama) |
| `--color-select` | `#7AD83A` | Sıralamada oyuncunun satırı |
| `--color-pressed` | `#EBEBEB` | Basılı bileşenin görsel tonu (%8 koyulaşma) |
| `--color-none` | şeffaf | Unity'nin varsayılan buton zeminini silmek için |

### Diğer temalar
Gece ve Zümrüt yalnız birincil, dış çizgi, nav ve yüz tonlarını değiştirir; altın, yeşil buton ve kırmızı uyarı her temada aynı kalır. Değerler style frame sayfasındaki `PAL` tablosundadır. Seçim kesinleşene kadar oyun Kor temasıyla çalışır.

### Kontrast (WCAG)

| Çift | Oran | Eşik |
|---|---|---|
| `ink` / `face` | 10,2 | 7 |
| `ink` / `face-deep` | 8,8 | 7 |
| `ink-muted` / `face` | 6,0 | 4,5 |
| Beyaz / `chip` | 17,9 | 4,5 |
| Beyaz / `alert` (rozet sayısı) | 4,6 | 3 (büyük yazı) |
| Altın başlık / `outline` | 10,8 | 3 |
| Beyaz / `outline` (dış çizgili yazı) | 14,9 | 3 |

Renkli dolgu üzerindeki beyaz yazı tek başına yetmez: yeşilde 2,9, turuncuda 2,8. Bu yüzden bu yazılar her zaman koyu dış çizgiyle kullanılır ve kontrast dış çizgiye göre ölçülür (yeşilde 10,7). `ThemeTests` bu çiftleri her temada denetler ve USS'de token dışı renk kullanılmasını engeller.

## Tipografi

Tek aile: **Baloo 2** (SIL OFL), Türkçe harflerin hepsi var. Ağırlıklar: 700 Bold, 800 ExtraBold.

| Token | Boyut | Ağırlık | Kullanım |
|---|---|---|---|
| `--font-size-display` | 120 | 800 | Ekran başlığı ("Mağaza"), büyük buton ("Oyna") |
| `--font-size-number` | 124 | 800 | Hamle sayısı |
| `--font-size-title` | 104 | 800 | Pencere başlığı ("Seviye 8"), "Muhteşem!" |
| `--font-size-button` | 76 | 800 | Buton etiketleri, fiyat |
| `--font-size-label` | 56 | 800 | Satır adı, hedef sayısı, bant yazısı |
| `--font-size-body` | 48 | 800 | Bölüm başlığı, açıklama |
| `--font-size-caption` | 36 | 700 | Süre hapı, "Skor", rozet yazısı |

**Yazı stilleri**

| Stil | Dolgu | Dış çizgi | Gölge | Nerede |
|---|---|---|---|---|
| Altın başlık | `gold` | `outline`, boyutun %7'si | 0, %5 aşağı, `outline` | Ekran ve pencere başlıkları, "Muhteşem!" |
| Beyaz dış çizgili | beyaz | zeminin kenar rengi (`cta-edge`, `outline`…) | 0, %4 aşağı | Butonlar, bantlar, sayılar |
| Mürekkep | `ink` | yok | yok | Krem panel içi |

USS karşılığı: `-unity-text-outline-width`, `-unity-text-outline-color`, `text-shadow`.

## Boşluk ve şekil
- Grid: 8 px. Ölçek: `8 · 16 · 24 · 32 · 48 · 64 · 96`
- Ekran kenar boşluğu: 24 (oyun), 40 (menüler), hep safe area'nın içinde
- Köşe yarıçapı: panel 46 · buton yüksekliğin %36'sı · hap ve rozet tam yuvarlak · booster yuvası 34 · tahta karesi kenarın %20'si
- Dış çizgi 3 · altın kenar 7,5 · buton alt dudağı 10

## Bileşenler

Pencere, butonlar, halka, hap, etiket ve kurdele `ArtSource/sheets/ui_kit.png` çiziminden (`tools/sprites/cut_sheets.py`), onay işareti ve ışıltılar `tools/ui_chrome/`'dan üretilir; hepsi `Resources/UI/Chrome/` altındadır. "Dilim" sütunu, köşelerin esnetilmeyen kısmıdır (`-unity-slice-*`).

| Bileşen | Görsel | Dilim | İçerik |
|---|---|---|---|
| `Panel` | ahşap çerçeve, köşelerde altın çivi, krem yüz | 60 | Mürekkep metin, ikonlar |
| `Panel` bantlı | üstte `primary` bant | 60 / bant 190 | Altın başlık |
| `Button` yeşil / turuncu | parlak hap, üstte ışık, altta koyu dudak | yan 100, üst-alt 70 | Beyaz dış çizgili etiket |
| `CloseButton` | kırmızı daire, beyaz X | — (sabit 150) | — |
| `Pill` (para) | krem hap, solda taşan 3B ikon, sağda yeşil + | 45 | Sayı, mürekkep |
| `ChipPill` | lacivert hap, ince altın çizgi | yan 34, üst-alt 32 | Seviye adı, bölge adı, sayaç |
| `Badge` | kırmızı daire, beyaz halka | — (80) | Sayı |
| `Tag` | turuncu hap, koyu çizgi | yan 38, üst-alt 35 | "Hedefler", "Hamle", "Zor seviye" |
| `Check` | yeşil daire, beyaz onay işareti | — (80) | Tamamlanan hedef |
| `Ring` | altın halka, gök mavisi iç; kilitli seviyede gri (`ring_locked`) | — | Ev, dişli, booster, harita düğümü |
| `Slot` | altın kare, açık mavi iç | 40 | Booster ikonu + rozet |
| `NavBar` / `NavTab` | `nav` zemin, altın üst çizgi; aktif sekme yükselir | 40 | 3B ikon; etiket yalnız aktifte |
| `Progress` | koyu oluk, altın halka, yeşil parlak dolgu | 32 | Beyaz dış çizgili oran ("5/8") |
| `Ribbon` | kırmızı-turuncu kurdele, altın kenar, kıvrık kırlangıç uçlar | yan 250 | Altın başlık |
| `Scrim` | `scrim` | — | Pencere arkası |
| `Board` | `board-back` zemin, beyaz ışıltı, dama kareler (koddan, `Art.cs`); arkada `bg_game` | — | Taşlar ve kutular karenin %110'u, zemin %108, buz %112 ve %82 opak |

USS sınıfları `et-` ile başlar: `.et-panel`, `.et-tag`, `.et-chip`, `.et-ring`, `.et-ribbon`, `.et-btn--cta`, `.et-btn--secondary`, `.et-check`, `.et-star`, `.et-title`, `.et-outlined`, `.et-ink`. Ekranlara özel yerleşimler `Screens.uss`'tedir (`.et-hud__…`, `.et-result__…`, `.et-level-card…`).

**Durumlar:**
- **normal**
- **basılı:** %94 ölçek, %8 koyulaşma, 90 ms
- **pasif:** %45 opaklık, renk doygunluğu düşük, dokunma yok
- **seçili:** yeşil ışıltılı çerçeve, köşede ✓
- **kilitli seviye kartı:** `locked` griye boyanır, sayı ve yıldızlar soluk; dokununca kart sallanır
- **sıradaki seviye kartı:** altına boyanır, %106 ölçek; liste açılınca ona kayar
- **seçili booster:** altına boyanır, %114 ölçek; hakkı bitmiş booster %55 opak, dokununca sallanır

Dokunma hedefi en az 144 px (yaklaşık 48dp).

## Oyun görselleri
Çizim sayfalarından kesilir (`ArtSource/sheets/`, kurallar ve çalıştırma: README). Tarz: yumuşak gölgeli, parlak, koyu renkli dış çizgili çizim; karakter sayfalarıyla aynı. Sprite'lar 512×512, şeffaf.

| Grup | Dosyalar |
|---|---|
| Taşlar | `piece_R/O/Y/G/B/P` |
| Özel taşlar | `sp_rocket`, `sp_rocket_v`, `sp_bomb`, `sp_orb`, `sp_homing` (kor kuşu, 2x2) |
| Engeller | `ob_box1`, `ob_box2`, `ob_box3` (kilitli), `ob_ice`, `ob_floor1`, `ob_floor2` (kül zemin), `ob_lantern` (toplanan kor feneri) |
| Booster | `bo_hammer`, `bo_row`, `bo_col`, `bo_shuffle` |
| Para ve ödül | `cur_coin`, `cur_heart`, `ui_star`, `ui_star_empty`, `ic_chest`, `ic_trophy` |
| Menü | `ic_home`, `ic_gear` |
| Maskot | `mascot` (el sallayan), `mascot_joy`, `mascot_sad` |
| Arka plan | `bg_game` (çayırlar, tepede Kor Tahtı), `bg_map` (bulanığı) |
| Harita | `map_1` Kıvılcım Köyü, `map_2` Fırın Sokağı, `map_3` Demirci Çarşısı ve Kor Tahtı |

Kural: 3B ikonlar kutularından %10–20 taşabilir (para hapı, sekme, hedef). Bu, referanstaki "dolu, hacimli" görünümü verir.

## Hareket
Süreler UX-001 §6.2'ye bağlıdır. Kodda tek kaynakları `Anim/Tweens.cs` → `Timing` sınıfıdır (testler ve ekran turu da onu bekler); USS'de yalnız basma süresi yazılıdır.

| Ad | Süre | Eğri | Kullanım |
|---|---|---|---|
| `--dur-press` | 90 ms | ease-out | Buton basma |
| `--dur-modal` | 200 ms | ease-out-back | Pencere girişi (ANI-020) |
| `--dur-screen` | 220 ms | ease-out-cubic | Ekran geçişi (solma + 48 px kayma) |
| `--dur-pop` | 320 ms | ease-out-back | Rozet, ödül, yıldız belirişi |
| `--dur-count` | 100 ms | ease-out | Hamle ve hedef sayacı zıplaması (ANI-019) |
| `--dur-star` | 200 ms | ease-out-back | Yıldız dolumu (ANI-009) |

**Kutlama katmanları** (brief kararı 5: UX-001 kademeleri, kilit anlarda güçlü kutlama):
1. **Her eşleşme:** taş pop'u, küçük parıltı, hafif titreşim
2. **Özel taş ve kombo:** ışık hüzmesi, parıltılar, "Harika!" / "Muhteşem!" altın yazı, orta titreşim
3. **Kazanma ve mahalle yakma:** ışık hüzmeleri, konfeti, sevinen maskot, yıldızlar tek tek, güçlü titreşim

Uygulama (2026-11-06):
- Oyun ve pencere animasyonları aynı ölçekli saatte çalışır (`Tweens`); yavaş çekim turu ve hızlı testler ikisini birlikte yavaşlatır ya da hızlandırır. Konfeti ve ışık hüzmesi dönüşü gerçek zamanlıdır.
- Her olayın sarsıntı ve titreşim gücü tek tabloda: `Scripts/Game/Feel/Feel.cs`. Sarsıntı kameradadır (`CameraShake`), tahtaya dokunmaz. Titreşim Android'in Vibrator servisiyle üç seviyedir (`Haptics`): hafif 12 ms, orta 24 ms, güçlü 45 ms.
- Övgü: bir hamlede 3 dalga "Harika!", 4 dalga ya da kombo "Muhteşem!", 6 dalga ya da iki renk bombası "İnanılmaz!". Hamle başına en büyüğü bir kez gösterilir.
- Seviye sonu: kalan her hamle (en fazla 10) sayaçtan çıkan bir kıvılcım olur, tahtada havai fişek gibi patlar; sonra konfetili pencere açılır. Yalnız gösteridir, oyunun durumu değişmez.

## Ses ve titreşim paleti

| Olay | Ses karakteri | Dosya | Titreşim |
|---|---|---|---|
| Kaydırma | kısa, yükselen "vıt" | `sfx_swap` | — |
| Eşleşmeyen kaydırma | inen iki nota | `sfx_swap_back` | — |
| Eşleşme | marimba + cam tınısı; zincirde perde majör dizide yükselir | `sfx_match` | hafif |
| Özel taş oluşur | parlak üçlü arpej ve çan | `sfx_special_made` | hafif |
| Roket | "vuuş" süpürme | `sfx_rocket` | orta |
| Kor bombası | boğuk patlama + çıtırtı | `sfx_bomb` | orta |
| Kor küresi | yükselen çan dizisi | `sfx_color_bomb` | orta |
| Kombo | patlama ve çan dizisi birlikte | `sfx_combo` | güçlü |
| Kor kuşu | yükselen ıslık + küçük patlama | `sfx_firework` | hafif |
| Hedef toplandı | iki çan | `sfx_collect` | — |
| Çekiç | tok vuruş | `sfx_hammer` | — |
| Satır ve sütun booster'ı | çift süpürme | `sfx_line_blast` | — |
| Karıştırma | marimba tıkırtı dizisi | `sfx_shuffle` | — |
| Buton | yumuşak tık | `sfx_ui_tap` | — |
| Kazandın | kısa fanfar | `sfx_win` | güçlü |
| Yıldız | çan; her yıldızda bir ton yukarı | `sfx_star` | — |
| Kaybettin | inen üç nota | `sfx_lose` | orta |
| "+5 hamle" | yükselen güç sesi | `sfx_moves_added` | — |
| Altın kazanma | jeton şıngırtısı | Faz 3 | hafif |

Sesler `tools/audio/generate_sfx.py` ile koddan üretilir (44,1 kHz, mono WAV, −3 dB tepe). Hangi olayın hangi sesi ve titreşimi çaldığını `Feel` tablosu tutar; ses düzeyleri `Sound.VolumeOf`'ta. Ses, titreşim ve ekran sarsıntısı Ayarlar'dan ayrı ayrı kapatılır. Müzik henüz yok.

## Erişilebilirlik
- Metin kontrastı ≥ 4,5:1. Büyük yazı (≥ 72 px, 800) ve dış çizgili yazı ≥ 3:1
- Dokunma hedefi ≥ 144 px
- Taşlarda renk + şekil (ACC-004), hedeflerde ikon + sayı
- "Hareketi azalt" seçeneği: konfeti, ekran sarsıntısı ve ışık hüzmeleri kapanır, süreler yarıya iner

## Uygulama (UI Toolkit)

| Dosya | İçerik |
|---|---|
| `Resources/UI/Theme/Tokens.uss` | Renk, yazı boyutu ve boşluk değişkenleri. Token dışı renk yasak |
| `Resources/UI/Theme/Components.uss` | Bileşen sınıfları (`.et-panel`, `.et-btn--cta` …) |
| `Resources/UI/Theme/Screens.uss` | Ekran yerleşimleri |
| `Resources/UI/Theme/EmberThrone.tss` | Tema: yukarıdakileri içe aktarır |
| `Resources/UI/Chrome/*.png` | 9-slice bileşen görselleri (`python tools/ui_chrome/render.py <adlar>`); yalnız kullanılanlar |
| `Resources/UI/Art/*.png` | Kullanılan 3B ikonlar ve arka plan (`docs/design/style-frames/img`'den kopya) |
| `Resources/UI/Fonts/` | Baloo 2 Bold ve ExtraBold, yalnız Latin harfleri (`tools/fonts/make_baloo2.py`), lisans `OFL.txt` |
| `Editor/UiTextureImport.cs` | Bu klasördeki görsellerin içe aktarma ayarı: piksel boyutu korunur, 3B ikonlarda mipmap, Android'de ASTC 4x4 |
| `Scripts/Game/UI/` | `UiRoot` (panel, safe area), `Ui` (eleman yardımcıları), `HudView`, `ResultPopup`, `LevelSelectScreen` |

`PanelSettings` sahne dosyası olmadan koddan kurulur (tek sahne kuralı). Görünümü yalnız USS ve görseller belirler; C# kodu sınıf ekler ve kaldırır, renk ya da ölçü yazmaz. İki istisna var: safe area boşlukları ve HUD yüksekliği (`HudView.Height`, tahtanın yerleşimiyle tek kaynak). Animasyon süreleri `Anim/Tweens.cs` → `Timing`'den gelir; USS'de yalnız basma geçişi (90 ms) vardır.

Unity'nin varsayılan teması (`unity-theme://default`) Pong'daki gibi içe aktarılır, çünkü kaydırma alanı (ScrollView) ona dayanır. Butonlardan Unity'nin `unity-button` sınıfı kaldırılır; görünümü yalnız bizim sınıflarımız verir.

Fontta ✓, ✕ ve ★ yok: bunlar görsel olarak çizilir (onay işareti, kapat butonu, yıldız).
