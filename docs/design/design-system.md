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
| `--color-board-back` | `#131A4A` | Tahta zemini |
| `--color-tile`, `--color-tile-alt` | `#34459A`, `#2B3A88` | Tahta kareleri (dama). Koyu lacivert: taşlar öne çıkar, ışık efektleri görünür. Tahtanın dış kenarı altın (`Palette.BoardGlow`) |
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

Tek aile: **Paytone One** (SIL OFL, 2026-11-14'ten beri), tek ağırlık, Türkçe harflerin hepsi var. Oyun yazıyı açılışta 90 px örnekle ve 20 px boşlukla bir SDF atlasına çizer (`UiRoot.LoadFont`); kalın dış çizgi ve gölge bu boşluk sayesinde kesilmez. Eski Baloo 2 dosyaları yalnız karşılaştırma için durur.

| Token | Boyut | Ağırlık | Kullanım |
|---|---|---|---|
| `--font-size-display` | 112 | — | Ekran başlığı, büyük buton ("Oyna") |
| `--font-size-number` | 116 | — | Hamle sayısı |
| `--font-size-title` | 92 | — | Pencere başlığı ("Seviye 8"), "Muhteşem!" |
| `--font-size-button` | 68 | — | Buton etiketleri, fiyat |
| `--font-size-label` | 50 | — | Satır adı, hedef sayısı, bant yazısı |
| `--font-size-body` | 44 | — | Bölüm başlığı, açıklama |
| `--font-size-caption` | 34 | — | Süre hapı, rozet yazısı |

**Yazı stilleri**

| Stil | Dolgu | Dış çizgi | Gölge | Nerede |
|---|---|---|---|---|
| Başlık | `title` (açık altın) | `outline`, 9-10 px | 0, 8 px aşağı, `outline` | Ekran ve pencere başlıkları, "Muhteşem!" |
| Beyaz dış çizgili | beyaz | zeminin kenar rengi (`cta-edge`, `accent-edge`, `off-edge`, `outline`), 6-7 px | 0, 5 px aşağı | Butonlar, bantlar, sayılar |
| Mürekkep | `ink` | yok | yok | Krem panel içi |

USS karşılığı: `-unity-text-outline-width`, `-unity-text-outline-color`, `text-shadow`.

## Boşluk ve şekil
- Grid: 8 px. Ölçek: `8 · 16 · 24 · 32 · 48 · 64 · 96`
- Ekran kenar boşluğu: 24 (oyun), 40 (menüler), hep safe area'nın içinde
- Köşe yarıçapı: panel 46 · buton yüksekliğin %36'sı · hap ve rozet tam yuvarlak · booster yuvası 34 · tahta karesi kenarın %20'si
- Dış çizgi 3 · altın kenar 7,5 · buton alt dudağı 10

## Bileşenler

**v2, 2026-11-14 ("kraliyet" görünümü).** Sahibi arayüzün türün liderine yakın olmasını istedi: bordo başlık bandı ve alt menü, lacivert desenli sayfa zemini, kalın altın kenarlı panolar, krem satırlar, köşeleri yuvarlak dikdörtgen parlak butonlar (koyu dış çizgi, altta kalın dudak, üstte parlak bant), pencerelerde köşede kırmızı X. Parçaların hepsi `tools/ui_chrome/chrome.html`'den çizilir (`python tools/ui_chrome/render.py <ad>`):

| Parça | Dosya | Dilim | Nerede |
|---|---|---|---|
| Buton yeşil / turuncu / gri | `b2_green`, `b2_orange`, `b2_grey` (300×150) | yan 46, üst 42, alt 52 | `.et-btn--cta`, `.et-btn--secondary`, pasif buton |
| Çerçeveli buton | `b2_green_framed`, `b2_orange_framed`, `b2_purple_framed` (320×176) | yan 56, üst 54, alt 64 | Pencere butonları (`.et-result__button`), lobi "Bölüm N" (mor) |
| Pano | `p2_cream` (ayrıca `p2_crimson`, `p2_royal`, `p2_plum`, `p2_gold`) | 72, alt 76 | `.et-panel` |
| Başlık bandı | `band_crimson` (ayrıca `band_royal`, `band_plum`) | yan 60, üst 50, alt 56 | Pencere başlığı (`.et-ribbon`) |
| Kapat | `btn_close` | — | Pencerenin sağ üst köşesi (`.et-close`) |
| Satır | `row2`, kendi satırı `row2_me` (yeşil), istek `row3_rose` | 40, alt 48 | `.et-row`, `.et-rank`, pass ve set satırları |
| Sayfa başlığı | `hdr3` (1080×308, üstte koyu, aşağı açılır) + `gold_line` | 4 | Ana ekran dışındaki sekmelerde ve Ayarlar'da üst bant, ortada sayfa adı (`.et-page-header`, `Ui.PageHeader`) |
| Sayfa zemini | `bg3` (512, tekrar eder, ince ızgara) | — | `.et-backdrop--map`, `.et-settings` |
| Alt menü | `nav3`, seçili sekme `nav3_on` | üst 20 / üst 56 | `.et-tabbar`, `.et-tab`, `.et-tab--active` |
| Etiket, fiyat plakası, desen | `corner_tab`, `price2`, `pat_star` | 44 / 28 / — | Teklif panoları için hazır |

**Ekran kopyası Faz 1, 2026-11-23.** Ölçüler [ekran-kopya-sartnamesi.md](../reference/ekran-kopya-sartnamesi.md) tablolarından, panel pikseli (1080×2346). Üst gruptaki öğeler güvenli alanın üstüne göre (y − 90), alt gruptakiler altına göre yerleşir; pencereler ekranın ortasındaki 1080×2346 sahneye (`.et-stage`) konur, böylece şartname koordinatları olduğu gibi yazılır.

| Parça | Dosya | Dilim | Nerede |
|---|---|---|---|
| HUD | `hud_plate` (mor plaka), `hud_card` (krem kart), `hud_tab` (mor sekme), `hex_frame` (altıgen portre) | 60 / 32 / 30 / — | `.et-hud__plate`, `.et-hud__goals`, `.et-hud__tab`, `.et-emby` |
| Booster çubuğu | `booster_green` (166), `count_red`, `count_green` (68), `btn_purple_round` (130), `ic_gear_cream` | — | `.et-booster`, `.et-booster__count`, `.et-boosters__home` |
| Seviye penceresi | `p2_plum` gövde, `lvl_band`, `btn_close_gold`, `flag_center`, `flag_side` | 72 / 130, 90 / — | `LevelStartPopup` |
| Kayıp akışı | `wide_plum`, `wide_cream`, `gold_line`, `hex_red`, `heart_broken` | — | `OutOfMovesBanner`, `ContinuePopup`, "Bölüm Başarısız!" |
| Lobi | `bar_crimson`, `b2_purple_framed`, `ribbon_crimson`, `btn_orange_round` | — | `.et-topbar__bar`, `.et-lobby__play`, `.et-lobby__ribbon`, `.et-topbar__gear` |
| Yan sütunlar | `side_crimson`, `side_royal`, `side_purple` (170), `pill_navy`, `pill_crimson` (158×55) | — / 27 | `.et-side__button`, `.et-side__chip` |
| Ayarlar | `toggle_on`, `toggle_slash` (200), `ic_sound`, `ic_vibrate`, `ic_shake` (128) | — | `.et-toggle`, `.et-toggle--off` |

Renkli bir 3B simge `-unity-background-image-tint-color` ile kreme boyanmaz (renkle çarpılır, soluk kalır); krem simge gerekiyorsa `tools/ui_chrome`'da çizgi simge olarak çizilir (`ic_gear_cream`).

Aşağıdaki v1 tablosu eski parçaları anlatır; `chip`, `pill`, `ring`, `tag`, `trough`, `fill`, `badge`, `check` hâlâ kullanılır.

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
- **pasif:** buton gri olur (`b2_grey`, yazı çizgisi `off-edge`), dokunma yok
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
Süreler UX-001 §6.2'ye bağlıdır; tahta ve pencere hareketleri 2026-11-21'de kare kare referans ölçümüne çekildi ([docs/reference/](../reference/README.md)). Kodda tek kaynakları `Anim/Tweens.cs` → `Timing` sınıfıdır (testler ve ekran turu da onu bekler); USS'de yalnız basma süresi yazılıdır.

| Ad | Süre | Eğri | Kullanım |
|---|---|---|---|
| `--dur-press` | 90 ms | ease-out | Buton basma |
| `--dur-modal` | 165 ms | yay (sönümleme 0,54) | Sonuç penceresi üstten iner, 165 ms'de yerini aşar, 430 ms'de oturur. Seviye başı, ayarlar ve lobi pencereleri tek karede açılır (ANI-020) |
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

Lobi bileşenleri (2026-11-12):
- **Hap** (`et-pill`): koyu hap üstünde solda taşan büyük ikon, ortada sayı, sağda yeşil artı. Can hapında sayı kalbin üstünde, yanında "Dolu" ya da geri sayım.
- **Halka buton**: altın halka içinde ikon (mağaza, ayarlar, hediye, görevler). Bildirim varsa sağ üstte kırmızı rozet (`et-badge`).
- **Satır** (`et-row`): ikon, başlık, alt yazı ve küçük buton (`et-btn--small`, içinde fiyat ve para ikonu). Yetmeyen fiyat butonu soluk ve basılmaz.
- **İlerleme çubuğu** (`et-progress`): kahverengi zemin, yeşil dolgu, ortada "10/20".
- **Gün kutusu** (`et-day`): günlük hediye takvimi; bugünkü yeşil çerçeveli, alınmış olan soluk ve onay işaretli, 7. gün geniş.
- Ana buton her ekranda tektir ve yeşildir: lobide "Seviye N".

Efekt yenilemesi (2026-11-12, telefondaki ilk denemeden sonra):
- Tahta koyu laciverte döndü. Açık karelerde toplamalı (additive) ışık görünmüyordu.
- Eşleşme: taş beyazlamadan 70 ms'de küçülür ve kendi renginde kırıntılar, kıvılcımlar, ışıltı ve küçük bir şok halkasıyla patlar. Hedef taşları tahtadan hedef paneline uçar; sayaç taş varınca düşer.
- Roket: iki yöne alev izi ve duman bırakarak uçar, yolunda ışık şeridi kalır. Bomba: parlama, iki şok halkası, kıvılcım, kor ve duman; çevredeki taşlar dışarı itilip geri gelir, görüntü 50 ms durur. Renk bombası: hedefler karışık sırayla şimşekle işaretlenir; ışınlar patlamaya kadar yanık kalır, kısa bir bekleyişten sonra hepsi birlikte patlar. Güdümlü: kavisli uçuş ve kıvılcım izi.
- Özel taş oluşurken arkasında dönen ışık hüzmesi çıkar. Tahtadaki özel taşlar nefes alır gibi hareket eder ve arkalarında renkli hale yanıp söner. Taşların üstünden ara sıra çapraz bir parıltı geçer, rastgele taşlarda ışıltı belirir.
- Takas 130 ms sürer, yavaşlayarak biter, taşlar büyümez. Düşen taşlar yerçekimi gibi yavaş başlayıp hızlanır (tek hücre yaklaşık 160 ms, yedi hücre yaklaşık 0,56 sn), uzar, yere değince ezilip zıplar. Seviye başında tahta 0,5 sn sonra sağdan tek parça kayarak gelir.
- Kazanınca tahtanın iki alt köşesinden konfeti fışkırır. Arka planda yavaşça yükselen korlar vardır.
- Olay güçleri yine tek tabloda (`Feel.cs`): sarsıntı, titreşim, ses ve duraklama süresi. "Ekran sarsıntısı" ayarı kapalıyken duraklama da olmaz.

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
