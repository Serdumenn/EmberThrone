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
| Kırağı karosu | `tools/sprites/make_frost.py` | `ob_frost.png`, buz karosundan (`ob_ice.png`) türetilir: beyaz kar kabuğu, kenarlardan içeri eğrelti biçimli buz kristalleri, altındaki taşın rengi görünür kalır. Hedef simgesi `ob_frost_icon.png` aynı kırağının koyu mavi buz üstündeki hâlidir; açık panelde okunsun diye. `python tools/sprites/make_frost.py` |
| Arayüz parçaları | 9-slice PNG. Pencere, butonlar, halka, hap, etiket ve kurdele `ui_kit.png` sayfasından; onay işareti, ışıltılar, alt bar (`nav`, `nav_tab_on`), krem hap (`pill`), satır (`row`), ilerleme çubuğu (`trough`, `fill`) ve rozet (`badge`) `tools/ui_chrome/`'dan (`python tools/ui_chrome/render.py nav pill …`). USS `-unity-slice-*` ile gerer |
| Dünya | Bölge başına bir kart: görsel (`district_N.jpg`; ilk bölge oyun arka planından kırpılır, 2–3. bölgelerin kendi çizimi vardır, 4. bölgeden itibaren onarım manzarasından kırpılır). On bölge, yüz seviye, ilerleme çubuğu, yıldız sayısı, 10 seviye düğümü. Bölgeler `LevelMap.cs`'te ad ve görsel sınıfıyla listelenir |
| Renkler | Yalnız `Resources/UI/Theme/Tokens.uss`. Başka dosyada renk yazılmaz (`ThemeTests` denetler) |
| İçe aktarma | `Editor/GameTextureImport.cs`: mipmap + trilinear, clamp, Android'de ASTC 4x4. Tahta parçaları en fazla 256 px, `bg_*` 2048 px |
| Tasarım dili | [design-system.md](design-system.md): token'lar, bileşenler, hareket, ses ve titreşim paleti, erişilebilirlik |

## Efektler

| Parça | Yer | Görev |
|---|---|---|
| Shader'lar | `Resources/Shaders/Additive.shader`, `Piece.shader` | `EmberThrone/Additive`: toplamalı ışık (parlama, kıvılcım, şimşek). `EmberThrone/Piece`: taşlar; `_Flash` ile beyaza parlama ve dünya konumuna bağlı çapraz parıltı şeridi. İkisi de `UnitySprites.cginc` üstüne kurulu ve `Resources.Load` ile yüklenir (derlemeye girmeleri için) |
| Dokular | `Scripts/Game/Art/FxArt.cs` | Parlama, çekirdek, yıldız, şok halkası, duman, kırıntı atlası (2x2), konfeti, ışık hüzmesi ve şimşek dokuları koddan üretilir; malzemeler burada önbelleğe alınır |
| Parçacıklar | `Scripts/Game/Board/FxParticles.cs` | Yedi `ParticleSystem` (kıvılcım, ışıltı, kırıntı, kıymık, duman, kor, konfeti). Kendiliğinden yaymazlar; her patlama `Emit` ile parçacık atar. `Ambient` arka plandaki korları kurar |
| Sahneler | `Scripts/Game/Board/BoardFx.cs` | Havuzlu sprite ve `LineRenderer` ile parlama, şok halkası, ışık hüzmesi, şimşek, roket ve vuruş uçuşları; hazır patlamalar (`GemBurst`, `Explosion`, `Firework`…) |
| Duraklama | `Scripts/Game/Feel/HitStop.cs` | Büyük vuruşta `Time.timeScale` kısa süre düşer. Yalnız ölçek 1 iken çalışır; yavaş çekim turunu ve hızlandırılmış testleri bozmaz |
| Hedefe uçuş | `Scripts/Game/UI/GoalFlyers.cs` | Hedef taşları arayüz katmanında hedef yuvasına uçar; aynı anda en fazla 14 uçuş, fazlası sayacı doğrudan düşürür. Her inen uçuş sayacı bir düşürür; kırağı yayılınca sayaç bir artar |
| Kırılma | `Scripts/Game/Art/Fracture.cs`, `BoardFx.Shatter` | Bir engel resmi Voronoi hücrelerine bölünür (biri ortada, diğerleri halka üzerinde, sabit tohumla): her parça aynı dokudan çokgen geometrili bir sprite'tır (`Sprite.OverrideGeometry`), parçalar resim ve sayı başına önbellekte tutulur. `Shatter` parçaları resmin yerinde başlatır, dışarı ve yukarı fırlatır, yerçekimi (15 hücre/s²), dönme ve sonda solma ile tahtanın dışına kadar uçurur. Kırılan engelde bütün parçalar, kırılmayan vuruşta yalnız 2 kenar parçası uçar |
| Ateş izi | `FxParticles.Flame` | Normal karışımlı duman dokusu, ömrü boyunca açık sarıdan turuncuya, kırmızıya ve griye döner; açık zeminde de görünür. Roket egzozu ve bombanın ateş topu bunu kullanır |
| Emby portresi | `Scripts/Game/UI/EmbyPortrait.cs` | HUD'un solunda halka içinde Emby. Ruh hâli (`EmbyMood`): kalan hamleye göre temel hâl (5 ve altı endişe, 2 ve altı panik), olaylara 1,6 sn'lik tepki (3+ dalga sevinç, 5+ dalga ve kombo hayran, biten hedef sevinç, kırağı yayılması endişe), kazanınca kalıcı sevinç ve aralıklı zıplama, kaybedince üzüntü (devam alınırsa hamleye göre geri döner). Hareket UI zamanlayıcısıyla: nefes, göz kırpma (bekleme hâlinde), panikte titreme, endişede sallanma, sevinçte zıplayıp ezilme. Pozlar `Resources/UI/Art/mascot*.png` |
| Booster çubuğu | `Scripts/Game/UI/BoosterBar.cs` | Beş booster yeşil düğmede (`Chrome/booster_slot.png`), stok sıfırsa sayı yerine "+" ve dokunmak mağazayı açar. Sondaki halka düğme oyundan çıkıştır (`HomeButton`) |
| Satış | `Scripts/Core/Meta/Store.cs` (`StoreRules`), `Economy.GrantPurchase`, `Scripts/Game/Store/PurchaseService.cs` | Ürün kataloğu, kumbara ve Kor Yolu kuralları Core'dadır. `GrantPurchase` ürünü kimliğine göre verir (altın ve elmas sınırsız), işlem kimliğini hatırlar (son 64), başlangıç paketini kapatır, kumbarayı boşaltır ya da bu ayın Kor Yolu'nu açar. `PurchaseService` Pong'daki IAP 5 akışıdır: bekleyen sipariş önce profile yazılır, sonra onaylanır. Editörde gerçek mağaza yerine anında verilir. Ayrıntı ve ürün kimlikleri: [docs/IAP.md](../IAP.md) |
| Kor Yolu ve kumbara | `Economy.ReportStoreWin`, `ClaimPass`, `PassReached`, `MaxLives`; `Scripts/Game/UI/PassViews.cs` | Her kazanış kumbaraya 50 altın (en çok 3000) ve Kor Yolu'na bir adım ekler. Kor Yolu takvim ayına bağlıdır (`EventRules.MonthOf`), 30 adım; bedava şerit herkese, ücretli şerit `ember_pass` alınan ayda açılır ve o ay en çok can 8 olur. Ay dönünce alınmamış kazanılmış ödüller verilir ve yol sıfırlanır |
| Galibiyet serisi | `Economy.WinStreak`, `ExtendStreak`, `BreakStreak`, `StreakSpecials`; `GameController.PlaceStreakSpecials` | Her kazanış seriyi bir artırır, vazgeçmek (can kaybettiren her çıkış) sıfırlar. Seri 1, 2 ve 3+ iken sonraki seviye açılış animasyonundan sonra tahtaya sırayla roket, bomba ve renk küresi konur (`EconomyRules.StreakSpecials`, `GameSession.PlaceStartingSpecials`). Seri profille kaydedilir (`winStreak`) |
| Final | `GameSession.PlayFinale`, `GameController.PlayFinale` | Kazanınca hamle sayacı geri sayılır, ardından kalan hamle kadar (en çok `GameRules.FinaleMaxSpecials` = 10) serbest taş sırayla yatay ve dikey rokete dönüşür ve hepsi aynı dalgada patlar; zincirleme ve dökülme normal işler, oyun durumu kazanılmış kalır. Yıldızlar finalden önce sayılır. İlk kazanışta roket başına `FinaleCoinsPerSpecial` = 10 altın, kazanma altınına eklenir |
| Devam teklifi | `GameController.ShowLose`, `BuyExtraMoves` | Kaybedince "+5 hamle: N altın" her zaman görünür (reklam hazırsa reklamlı teklifin altında). Altın yetmezse dokunmak mağazayı açar, kaybetme penceresi açık kalır. Seri varsa pencere "Vazgeçersen N galibiyetlik serin biter" der |
| Tahta zemini | `Scripts/Game/Art/BoardSkin.cs` | Seviye açılırken tahtanın biçiminden tek bir doku üretilir: hücre başına 64 piksel, yarım hücre kenar payı. Hücrelere olan uzaklıktan (iki geçişli Öklid uzaklık dönüşümü) yuvarlak köşeli bir dış hat çıkar: çerçeve hücrelerden 0,15 hücre dışarıda, içbükey köşeler 0,25 hücre yarıçapla yuvarlanır. Katmanlar: aşağı düşen yumuşak gölge, ışığı sol üstten alan parlak altın çerçeve, çerçeve dibinde iç gölge, dama düzeninde hücreler ve yalnız komşu hücreler arasında ince dikiş. Üç ton var (`BoardTone`: `Navy`, `Dusk`, `Light`), varsayılan `Light`. Doku `BoardView` yok olurken silinir |

- Taş görünümü iki katmandır: kök (hamle animasyonları) ve gövde (boşta salınım). Aynı taşa yazan animasyonlar `Motion` sayacıyla birbirini iptal eder; yenisi başlayınca eskisi yazmayı bırakır.
- Efektler oyunun durumunu değiştirmez ve beklemez: adım süresi yine `BoardView` içinde hesaplanır.
- Toplamalı ışık açık zeminde zayıf kalır; açık tahtada okunan efektler normal karışımlı olanlardır (ateş izi, kırık parçaları, duman). Yeni efektlerde ikisi birlikte kullanılır.
- Taşlar inişte ezilir ve 0,12 hücre zıplar (`BoardView.Land`).
- Oyun ekranının arka planı `bg_game_calm.jpg`dir: köy resminin bulanık, kararmış ve gece tonuna çekilmiş hâli, tahtayla yarışmasın diye (`python tools/sprites/make_backdrop.py`).

## Tipografi

- Baloo 2 Bold ve ExtraBold. Google Fonts'un değişken fontundan sabit ağırlık olarak, yalnız Latin harfleriyle `tools/fonts/make_baloo2.py` üretir.
- Her font Türkçe harfleri (ş, ğ, ı, İ, ç, ö, ü) içermeli; `ThemeTests` kontrol eder.

## Ses

- Efektler (22 ses; ahşap, buz ve kül kırılması dahil) `tools/audio/generate_sfx.py` ile üretilir: `python3 tools/audio/generate_sfx.py` (ya da tek ses: `... sfx_bomb`). Çıktı `Assets/EmberThrone/Resources/Audio/sfx_<ad>.wav`, 44,1 kHz mono.
- `Feel/Sound.cs`: `Sfx` adı dosya adına çevrilir (`LineBlast` → `sfx_line_blast`); kamerada tek `AudioListener` var. Zincirleme eşleşmede perde her dalgada majör dizide bir basamak yükselir.
- Palet ve olay tablosu: [design-system.md](design-system.md). Müzik henüz yok.

## Reklam

- `Scripts/Game/Ads/AdService.cs`: ödüllü reklam, geçiş reklamı ve onay penceresi (Google UMP). `GameApp` kurar; editörde gerçek reklam açılmaz, ödüllü reklam benzetilir (`AdService.SimulateRewardedInEditor`).
- `Scripts/Game/Ads/AdRules.cs`: reklam birimi kimlikleri, geçiş reklamı aralığı (180 sn) ve ilk geçiş reklamının seviyesi (6).
- Eklenti ve Android bağımlılıkları: `Assets/GoogleMobileAds/`, `Assets/ExternalDependencyManager/`, `Assets/Plugins/Android/` (`mainTemplate.gradle`, `settingsTemplate.gradle`, `gradleTemplate.properties`). Bu klasörler üçüncü taraf dosyalarıdır; yorum satırı kuralı onlara uygulanmaz ve Gradle şablonlarındaki "Android Resolver" satırları silinmez.
- Hesap adımları: [yayın kılavuzu](../release/README.md), adım 4.

## Ekonomi ve lobi

| Parça | Yer | Görev |
|---|---|---|
| Kurallar | `Scripts/Core/Meta/EconomyRules.cs` | Bütün sayılar: başlangıç paketi, kazanma ödülü, can süresi ve fiyatı, booster fiyatları, sandık tablosu, günlük hediyeler, görev şablonları. Kaynak: META-001; farklar karar günlüğünde |
| Profil | `Scripts/Core/Meta/Profile.cs` | Oyuncunun durumu ve JSON'a yazılıp okunması |
| İşlemler | `Scripts/Core/Meta/Economy.cs` | Can yenilenmesi, satın alma, ödül, günlük hediye, sandık, günlük görevler. Saat dışarıdan verilir (`now`, yerel saatle saniye); motor saate bakmaz. Sandık `Pcg32` ile, profil tohumu ve açılan sandık sayısından üretilir |
| Koleksiyon | `Scripts/Core/Meta/Collection.cs`, `Scripts/Game/UI/CollectionViews.cs` | Kurallar ve setler (`CollectionRules`), paket açma, set ve büyük ödül (`CardCollection`); paket `Pcg32` ile profil tohumu ve açılan paket sayısından üretilir. Ekran: set karoları, set penceresi, paket penceresi. Kart çizimi `Resources/UI/Cards/<set>_<kart>.png` (256 piksel); eksik kart aynı çizimin koyu silueti olarak görünür, dosya yoksa kart destesi ikonu gösterilir. `ThemeTests.EveryCard_HasAnIllustration` 36 dosyayı arar |
| Yarışlar, merdiven, takım sandığı | `Scripts/Core/Meta/Contests.cs` (`ContestRules`, `Contests`), `Scripts/Game/UI/ContestViews.cs` | İki süreli yarış (`ContestKind.Rush`: 1 saatte 5 galibiyet, 4 rakip; `ContestKind.Lantern`: 3 günde 15 galibiyet, 9 rakip). Oyuncu `Begin` ile katılır; rakiplerin bitiş süresi profil tohumu ve başlangıç anından `Pcg32` ile çekilir (sürenin %40-160'ı), galibiyetleri süreyle doğru orantılı artar. Sıralama önce bitiş anına, sonra galibiyete göredir; ilk 3'e `ContestRules.Prizes`. `Claim` ödülü verir ve bir sonraki yarışa kadar ara başlatır. Kor Merdiveni günlüktür: art arda 7 galibiyet, `ReportLoss` (vazgeçme ya da kayıp) 7'ye varmadan başa döndürür; 100 oyuncudan zirveye çıkan sayı (8-30) gün ve tohumdan gelir, 10000 altın bunlara bölünür; alınmayan pay ertesi gün verilir. Takım sandığı haftalıktır: oyuncunun takımdayken kazandıkları ve dokuz takım arkadaşının `Village.PointsBy` galibiyetleri toplanır, 30/70/120'de ödül; alınmayanlar hafta dönünce verilir. Açılış: yarışlar 8 ve 12, merdiven 5 kazanılmış seviyeden sonra. Durum `Profile.Contest` dizisinde ve `Ladder*`, `Chest*` alanlarındadır |
| Köy Ligi | `Scripts/Core/Meta/Race.cs`, `Village.cs` | Haftalık yarış. Hafta pazartesi başlar (`RaceRules.WeekOf`); kazanç `AddWin` ile yazılır. Rakip puanı `Village.PointsBy` ile profil tohumu, hafta ve geçen saatten üretilir, saat ilerledikçe azalmaz. Hafta dönünce sonuç saklanır, ödül `ClaimResult` ile alınır |
| Takım | `Scripts/Core/Meta/Team.cs` | Hazır takımlar (`TeamRules.Teams`), üyeleri köy halkından. Can isteği ve gelen canlar (`Request`, `Tick`), üyelerin istekleri (`Asks`: iki saatlik dilim başına bir istek), yardım (`Help`) |
| Seviye üretimi | `tools/levelgen/generate_levels.py`, `plan.json` | 31. seviyeden sonrası. Bölge planından tahta, hedef ve engeller üretilir, `tools/LevelSim --tune` hamle sayısını bota göre yazar ([README](../../tools/levelgen/README.md)). Yeni engel aileleri `NEW_KINDS` ile bölgelere bağlıdır (3 kandil, 4 saksı, 5 köylü); tanıtım bölgesinin ilk üç seviyesi öğreticidir; `DENSE_FROM` (5) bölgesinden itibaren `densify` tahtanın %36 + bölge başına %2 (zorda +%5, en çok %50) kadarını üst üç satır dışında tek canlı kutu ve kandille doldurur. Ayardan sonra hamle sayısı `MAX_MOVES` (40) üstündeyse seviye bir sonraki tohumla yeniden çizilir (en çok 6 kez). Karlı Geçit ve Donmuş Göl'de (`FROST_DISTRICTS`) örtü hedefi buz yerine kırağıdır: kenar sütunlarından uzak, 1–2 aynalı küme, küme başına 3–4 hücre (zor seviyede 1 fazla) |
| Bölge onarımı | `Scripts/Core/Meta/Restoration.cs`, `Scripts/Game/UI/RestoreViews.cs`, `Resources/UI/Restore/`, `tools/restore/make_restore.py` | Kurallar (`RestoreRules`: bölge başına görevler ve bölge ödülü), ilerleme (`ReportWin`: ilk kazanış ve rekoru aşan yıldızlar, `ReportBooster`), ödül (`Claim`; son görev bölge ödülünü de verir), ana ekranda gösterilecek bölge (`Focus`). Eski oyuncunun ilerlemesi ilk açılışta seviye yıldızlarından bir kez sayılır (`Seed`). Ekran: `HomeDiorama` manzarayı ekranı kaplayacak biçimde yerleştirir, donmuş zeminin üstüne çözülen alanları koyar; `RestorePopup` görev listesidir. Görseller ve yerleşim [ArtSource/restore/README.md](../../ArtSource/restore/README.md) |
| Videolar | `Scripts/Game/UI/VideoViews.cs`, `Resources/Video/`, `tools/video/make_scenes.py` | `VideoView` bir klibi `VideoPlayer` ile bir render texture'a oynatır ve arayüzde gösterir; hazırlanana kadar ilk kare (`Resources/UI/Art/scene_*.jpg`) görünür, grafik aygıtı yoksa (ör. `-nographics`) yalnız ilk kare kalır. `StoryScene` bölge sahnesidir: başlık, metin, "Geç" butonu; video bitince ya da oynatılamazsa kendisi kapanır. Yükleme ekranı `scene_loading` klibini döngüyle, sessiz oynatır. Klipler VP8 WebM'dir, çünkü Unity'nin Linux sürümü MP4 açamaz ([ArtSource/video/README.md](../../ArtSource/video/README.md)) |
| Etkinlikler | `Scripts/Core/Meta/LiveEvent.cs`, `Scripts/Game/UI/EventViews.cs` | Aylık takvim (`EventRules.Calendar`, ay sırasıyla 12 tanım). İki tip: merdiven (`EventKind.Ladder`: kazanç 10/15/25 kor, zor seviye +20, engel başına 2, booster başına 5; beş kademe) ve sprint (`EventKind.Sprint`: gün sayısı, tek hedef, ödül). İlerleme `ReportWin` ve `Report` ile yazılır, ödül `Claim` ile alınır; ay dönünce kazanılmış ödüller verilir ve puan sıfırlanır. Ekran: ana ekrandaki `EventButton` (geri sayım, rozet) ve `EventPopup` |
| Sıralama ve takım ekranları | `Scripts/Game/UI/SocialViews.cs` | `RaceView`, `TeamView`, `Avatar` (kart çizimini yuvarlak çerçevede gösterir; oyuncu için Emby). Gerçek oyunculara geçiş: [ONLINE.md](../ONLINE.md) |
| Servis | `Scripts/Game/Meta/Meta.cs` | Profili yükler, kaydeder, `Changed` olayını yayar; `Meta.Clock` testlerde sahte saatle değişir |
| Lobi | `Scripts/Game/UI/LobbyViews.cs` | Üst bar (can ve sayaç, altın, elmas, ayarlar), alt sıra (Dünya, büyük "Seviye N" butonu, Görevler), solda hediye ve etkinlik butonları, sağda onarım butonu (ilerleme ve rozet), beş sekmeli alt bar (`TabBar`), yükleme ekranı (`LoadingScreen`) |
| Ana ekran ve dünya | `Scripts/Game/UI/LevelSelectScreen.cs` | Tek ekran, iki görünüm: ana ekran (köy manzarası, `et-backdrop--home`) ve dünya (bölge kartları). `ShowMap(true/false)` geçirir; Dünya butonuyla açılır, Ana Ekran sekmesiyle kapanır |
| Ödül uçuşu | `Scripts/Game/UI/RewardFlyers.cs` | Ödül penceresi kapanınca her altın, elmas, can ve paket ödülü birkaç simge olarak önce dağılır, sonra kavisle üst bardaki yerine (paket: Koleksiyon sekmesine) uçar. Üst bar ödülü pencere açılırken tutar (`TopBar.Hold`), simgeler indikçe bırakır (`Release`); sayaç böylece gelen ödülle birlikte artar. `UiBurst.Confetti` arayüz konfetisidir |
| Pencereler | `Scripts/Game/UI/MetaPopups.cs`, `Popup.cs` | Mağaza, canlar, günlük hediye, görevler, ödül, onay. Aynı anda bir pencere (`GameApp.Modal`) ve üstünde bir ödül penceresi (`GameApp.Rewards`) açılır |

Akış kuralları:
- Can, oyuncu vazgeçince düşer: kaybettikten sonra "Tekrar dene" ya da "Seviyeler", ya da hamle yaptıktan sonra seviyeden çıkmak (önce onay sorulur). Kazanmak can harcamaz. Can yokken seviye başlamaz, "Canlar" penceresi açılır.
- Kazanınca altın verilir (1 yıldız 100, 2 yıldız +40, 3 yıldız +80). Daha önce geçilmiş seviyeyi yeniden kazanmak 25 altın verir.
- Kazanınca ayrıca kupa verilir (Köy Ligi): yıldız sayısı kadar, ilk kazanışta iki katı.
- Kazanma penceresinden "Seviyeler" ile dönülünce kazanılan altın ve paket ana ekranda üst bara uçarak gelir.
- Altın, sınırın %90'ına (`EconomyRules.CoinWarnPercent`) gelince altın sayacının altında "Harca!" etiketi çıkar.
- Sıralama sekmesi açılınca bekleyen hafta sonucu varsa ödül penceresi gösterilir.
- Etkinlik yokken ana ekrandaki etkinlik butonu görünmez. Aynı anda tek etkinlik vardır.
- Bir bölgenin ilk seviyesine (1, 11, 21) basılınca bölge sahnesi önce oynar, sonra "Seviye başı" penceresi açılır. Her sahne bir kez kendiliğinden oynar (`Profile.ScenesSeen`); dünya ekranında açık bölgenin resmine dokununca yeniden oynar.
- Kaybedince "+5 hamle": bir kez ödüllü reklamla, ayrıca altınla (900, her alışta 1,5 katı). Altın seçeneği yalnız para yetiyorsa görünür.
- Boş booster'a dokunmak mağazayı açar.
- Günlük görevler: her gün aynı üç görev (gün numarasından üretilir, üçü farklı türde); hepsi alınınca elmas ödülü.
- Yükleme ekranı açılışta 1,4 sn görünür, sonra solar; arayüz zamanlayıcısıyla (gerçek zaman) çalışır. Testler `GameApp.ShowLoading = false` ile kapatır.
- Sekme rozetleri: Koleksiyon'da açılmamış paket ve alınmamış ödül sayısı, Sıralama'da bekleyen hafta ödülü, Takım'da yardım bekleyen istek sayısı.
- Ana ekranın beş sayfası vardır (`LobbyPage`): ana ekran, dünya, koleksiyon, sıralama, takım. Etkin sekme alt barda büyür ve adını gösterir.
- Gün değişince canlar dolar ve günlük reklam hakları sıfırlanır. Saat şimdilik cihazın saati; geri alınan saat can ya da hediye kazandırmaz. Sunucu saati Faz 4'te.

## Cihazda saklananlar (PlayerPrefs)

| Anahtar | İçerik |
|---|---|
| `et.stars.<id>` | Seviyenin en iyi yıldızı |
| `et.profile` | Oyuncu profili, tek JSON: altın, elmas, can ve can saati, booster hakları, günlük hediye serisi, günlük reklam sayaçları, günlük görev ilerlemesi, açılan sandık sayısı, kart sayıları, bekleyen ve açılmış paketler, alınmış set ödülleri, yarış haftası ve kupaları, bekleyen hafta sonucu, takım, son can isteğinin saati, yardım edilen istekler, etkinlik ayı, puanı ve alınan kademeler, izlenmiş bölge sahneleri, bölge onarımı ilerlemesi ve alınmış görevler |
| `et.booster.<Hammer\|RowBlast\|ColumnBlast\|Shuffle>` | Eski booster anahtarları. Profil ilk kez oluşurken bir kez okunur, sonra kullanılmaz |
| `et.sound`, `et.haptics`, `et.shake` | Ayarlar (ses, titreşim, ekran sarsıntısı): 1 açık, 0 kapalı |
| `et.ads.lastInterstitial` | Son geçiş reklamının kapandığı an (Unix saniyesi) |

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
| `Assets/EmberThrone/Scripts/Core/` | Saf C# oyun motoru, asmdef `EmberThrone.Core` (`noEngineReferences`): tahta, eşleşme, özel taşlar ve kombolar, engeller, yerçekimi, hedefler, booster'lar, seviye JSON'u, bot, ekonomi (`Meta/`) |
| `Assets/EmberThrone/Scripts/Game/` | Unity tarafı, asmdef `EmberThrone.Game`: `GameApp` (giriş noktası), `GameController` (tek seviye), `Board/` (görünüm, girdi, efektler, parçacıklar), `UI/` (UI Toolkit ekranları), `Art/` (render'lar ve üretilen şekiller), `Feel/` (sarsıntı, titreşim, olay tablosu), `Anim/` (tween'ler, `Timing`) |
| `Assets/EmberThrone/Editor/` | Yalnız editör: `ProjectSetup`, `GameTextureImport` |
| `Assets/EmberThrone/Tests/EditMode/` | NUnit testleri; Unity Test Runner ve `dotnet test` ikisinde de koşar |
| `Assets/EmberThrone/Tests/PlayMode/` | Oyunu sanal girdiyle oynatan testler ve ekran turu (`ScreenshotTour`) |
| `Assets/EmberThrone/Resources/Levels/` | Seviye dosyaları |
| `Assets/EmberThrone/Resources/Board/` | Tahta render'ları |
| `Assets/EmberThrone/Resources/UI/` | `Theme/` (USS, TSS), `Chrome/` (9-slice), `Art/` (3B ikonlar), `Fonts/` |
| `Assets/EmberThrone/Resources/Audio/` | Ses efektleri (`sfx_*.wav`) |
| `Assets/EmberThrone/Resources/Shaders/` | Efekt ve taş shader'ları |
| `Assets/_Game/` | Eski Faz 0 altyapısı (servis bulucu, kayıt, havuz, Firebase taslakları). Oyun kullanmaz; Faz 3 için saklanır |
| `ArtSource/sheets/` | Çizim sayfaları ve kesim tarifi; oyundaki görsellerin kaynağı |
| `ArtSource/blender/` | v3 dönemindeki Blender scriptleri; oyunda artık kullanılmıyor |
| `tools/CoreLib`, `tools/CoreTests` | Unity kaynaklarını derleyen dotnet projeleri (netstandard2.1, C# 9) |
| `tools/LevelSim` | Bot simülatörü: kazanma oranı, değişmez denetimi, hamle ayarı, oyun dökümü. Tablodaki `near` sütunu, kaybedilen oyunların kaçının 5 hamle daha verilince kazanıldığını gösterir (yakın kayıp payı) |
| `tools/gen_meta.py` | Eksik `.meta` dosyalarını üretir |
| `tools/sprites/` | Çizim sayfalarından sprite, arka plan, harita ve arayüz dokusu üretir (Pillow + numpy) |
| `tools/ui_chrome/`, `tools/fonts/`, `tools/audio/`, `tools/icon/` | Arayüz parçası, font, ses ve uygulama ikonu üreticileri. Pillow gerektirenler (`tools/icon/`) Pillow kurulu Python ile çalışır |

## Kod kuralları

1. **Core, UnityEngine'e dokunmaz.** En fazla C# 9 ve .NET Standard 2.1 (csproj zorlar).
2. **Determinizm.** Core'da rastgelelik yalnız `Pcg32`'den gelir; `System.Random`, saat ya da hash sırasına bağlı mantık yok. Aynı seviye + tohum + hamleler ⇒ aynı oyun (tekrar, sunucu doğrulaması ve bot buna dayanır).
3. **Sunum yalnız izler.** Unity kodu `GameSession`'ı çağırır ve dönen `MoveOutcome.Steps` adımlarını canlandırır; `Board`'u hiç değiştirmez.
4. **Denge veride.** `GameRules` ve seviye JSON'u; kodda sihirli sayı yok.
5. **`.meta` dosyaları.** Editör açık değilken `Assets/` altına kod, JSON ya da klasör eklenince `python3 tools/gen_meta.py` (`.androidlib` klasörlerinin içine dokunmaz). `--check`, `.gitignore`'un gizlediği bir dosyanın `.meta`'sı repoda kalmışsa bunu da hata sayar; depoya girmeyen dosyanın `.meta`'sı da girmemelidir. PNG, WAV, font gibi içe aktarılan dosyaların `.meta`'sını Unity yazar (editörü ya da batch testlerini bir kez açmak yeter); araç onları atlar. Var olan GUID değişmez.
6. **Testler.** Yalnız NUnit 3 klasik assert'leri (`Assert.AreEqual/IsTrue/Throws`, `CollectionAssert`); Unity'nin NUnit 3.5 sürümü yüzünden `Assert.Multiple` ve NUnit 4 API'leri yok. Tahta yardımcıları `TestUtil.cs`'de (satırlar üstten, y=0 altta).
7. **Özgün kimlik.** İsimler, karakterler, görseller, müzik, seviyeler ve arayüz EmberThrone'a aittir; başka oyunlarla yalnız türün genel mekanikleri ortaktır.
8. **Yorum satırı yok.** Kod, USS, Python ve workflow dosyaları yorumsuzdur; açıklama gereken bilgi bu belgeye ya da `docs/` altına yazılır.
9. **Arayüz.** C# yalnız `et-` sınıfları ekler ve çıkarır. İstisnalar: güvenli alan boşlukları, `HudView.Height`, animasyon stilleri (Tween ve zamanlayıcı), çalışma anında atanan sprite'lar, harita düğümlerinin yüzde konumu, ilerleme çubuğunun genişliği ve hedefe uçan taşın boyutu.

## Seviye biçimi

```json
{
  "id": 12, "name": "Ash Gate", "moves": 24, "colors": 5, "difficulty": "hard",
  "goals": [ {"type": "box"}, {"type": "color", "color": "R", "count": 20} ],
  "board":    [ "..1111..", "..." ],
  "floor":    [ "00111100", "..." ],
  "cover":    [ "00000000", "..." ],
  "frost":    [ "..1.....", "..." ],
  "spawners": [ "SSSSSSSS", "..." ],
  "collectibles": {"total": 3, "maxOnBoard": 2, "spawnChance": 0.12}
}
```

- Satırlar üstten alta yazılır; satır içindeki boşluklar yok sayılır.
- `board` zorunlu. `floor` ve `cover` hücre başına HP, `spawners` doğurucu hücreler (yoksa her sütunun en üstü), `collectibles` isteğe bağlı.
- `difficulty`: `normal`, `hard` ya da `superhard` (`LevelDefinition.IsHard`, `IsSuperHard`). Seviye başı penceresi zorları "Zor seviye", süper zorları kırmızı "Süper zor" etiketiyle gösterir.
- `frost` kırağı hücreleridir (`1` kırağı, `.` yok). Kırağı tek katlı bir örtüdür: aynı hücrede `cover` olamaz, `cover` hedefine sayılır.
- Tahta işaretleri: `.` rastgele taş, `#` delik, `_` boş, `R G B Y P O` taş, `1-9` kutu HP, `-` satır roketi, `|` sütun roketi, `*` bomba, `^` kor kuşu, `@` renk bombası, `$` toplanan obje, `L` kor kandili, `F` kül saksısı, `S` buzdaki köylü (dört `S` bir 2x2 kare olmalı; ayrıştırıcı kareleri sol üst köşeden gruplar, yarım kare hatadır).
- Hedef türleri: `color`, `box`, `floor`, `cover`, `collectible`, `lamp`, `flower`, `statue` (1 ile 4 hedef). `flower` sayısı saksıların toplam canıdır (saksı başına 3). `count` -1 ya da yoksa tahtadakilerin hepsi.
- Boyut: en az 3, en fazla 12 genişlik ve 14 yükseklik.

## Motor özeti

- Özel taşlar renksizdir. Sırada 4 → roket (eşleşmeye dik, `GameRules.RocketPerpendicularToMatch`), 2x2 → kor kuşu, L/T → bomba (5x5), sırada 5 → renk bombası. Dokunmak ya da kaydırmak ateşler; iki özel taş birlikte kombo yapar (`ComboKind`).
- Hücre katmanları: zemin (HP) → taş → örtü (HP). Örtü taşı kilitler ve vuruşu emer. Kutular komşu eşleşme ve patlamadan hasar alır. Toplanan objeler patlamadan etkilenmez, sütunun en alt hücresine inince toplanır.
- Engeller (`PieceKind.IsBlocker`): kutu, kandil (2 can), saksı (3 can), köylü bloğu (4 can). Hepsi yerinde durur, takas edilmez, yanındaki eşleşmeden ve patlamadan hasar alır. Kandil sonunda `lamp` hedefine sayılır; saksı her vuruşta bir `flower` sayar; köylü bloğu dört hücrede aynı `Piece` nesnesidir (`Board.Clone` paylaşımı korur, `CountPieces` bir sayar), bir temizleme dalgasında en çok bir hasar alır ve kırılınca sol alt hücresinden `statue` hedefine sayılır.
- Ayaz kırağısı yayılan örtüdür (`Cell.Frost`, örtü HP 1). Oyuncunun hamlesi hiç kırağı kırmadıysa, hamle bittikten sonra bir kırağı hücresi komşusundaki (yukarı, sağ, aşağı, sol) örtüsüz ve tetiklenmemiş bir taşa yayılır; aday `Pcg32` ile seçilir. Son hamleden sonra ve hedefler bitmişse yayılmaz. Booster kullanmak hamle sayılmaz, yayılma da olmaz. Sayısı verilmemiş `cover` hedefi yayılınca bir artar (`Goal.CountsAll`); motor `FrostSpreadEvent` yayar, görünüm kırağıyı kaynaktan hedefe sürünerek taşır ve sayaç bir artar.
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
- CI testleri Linux editöründe koşar. Orada bazı platform ayarları saklanmaz (`Screen.sleepTimeout` hep `NeverSleep` döner). Testler platformdan geri okumaz, oyunun kendi durumuna bakar (`GameApp.ScreenStaysOn`).
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
