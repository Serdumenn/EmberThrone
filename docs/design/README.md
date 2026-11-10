# EmberThrone — Tasarım Süreci

Bu klasör oyunun tasarım ve teknik kararlarının kaydıdır. Plan ve durum [ROADMAP.md](../../ROADMAP.md)'dedir. Her faz bir **onay kapısı** ile biter; kapıdan geçmeyen iş bir sonraki faza taşınmaz (PongMobilePrototype'taki düzen).

## Kaynaklar

| Kaynak | İçerik |
|---|---|
| `GDD/` (yalnız yerelde, git'e girmiyor) | 16 Word belgesi: MASTER-GDD-001 özeti, PTH-001 vizyon, SYS-CORE-001 çekirdek döngü, UX-001 ekran ve animasyon, TECH-001 teknik şartname, LVL-DESIGN-001, META-001, MONETIZATION-001, LIVEOPS-001, DATA-001 ve yamaları |
| [brief.md](brief.md) | Creative Brief: kime, neden, hangi hisle; görsel yön seçenekleri ve GDD ile farkların kararları (onaylandı). GDD ile çeliştiği yerde geçerli olan budur |
| [story.md](story.md) | Hikâye "Ayaz'a Karşı": öncül, ton, karakter kadrosu, bölge sahne kalıbı, oyunla bağı |
| [style-frames/index.html](style-frames/index.html) | Style frame v3, Blender'da 3B render. 6 ekran var: ana sayfa (ada), seviye başı, oyun, kazandın, mağaza, sıralama. Ayrıca oyun ekranı 3 oranda, 3B parça seti ve 3 panel rengi seçeneği. Tarayıcıda açılır |
| [../../ArtSource/blender/](../../ArtSource/blender/README.md) | 3B görselleri üreten Blender scriptleri (taşlar, ikonlar, maskot, ada) |
| [design-system.md](design-system.md) | Design system "Kor": renk ve yazı token'ları, bileşenler, hareket, ses ve titreşim, erişilebilirlik, UI Toolkit uygulaması |
| [tech-spec.md](tech-spec.md) | Teknik şartname: ekran ve ölçek, görsel ve font standartları, adlandırma, klasör yapısı, kod kuralları, seviye biçimi, test ve araç komutları |
| [../../tools/ui_chrome/](../../tools/ui_chrome/render.py) | Arayüz parçalarını 9-slice PNG olarak çizen araç (UI Toolkit'te renk geçişi ve gölge olmadığı için) |
| `Assets/EmberThrone/Resources/UI/` | Oyundaki arayüzün görünümü: tema (USS), parçalar, 3B ikonlar, font |
| `GDD/Referances` (yalnız yerelde) | Türün lideri bir oyundan 9 referans ekranı; kalite çıtası ve ekran düzeni için, kopyalanmaz |
| [ROADMAP.md](../../ROADMAP.md) | Fazlar, yapılanlar, sahibinin vereceği kararlar (GDD ile motor arasındaki farklar dahil) |
| [../qa/](../qa/) | Kalite denetimleri |
| [../release/README.md](../release/README.md) | Yayın kılavuzu, sürüm ve commit düzeni |

## Roller

| Rol | Kim | Sorumluluk |
|---|---|---|
| Creative Director, Art Direction, UX, Development, QA | Fatih | Vizyon, kapı onayları, üretim, uygulama, doğrulama, cihaz testi |

## Fazlar

| Faz | Çıktı | Kapı | Durum |
|---|---|---|---|
| 0 · Temel | Deterministik C# motoru, seviye formatı, bot simülatörü, CI | — | Tamam |
| 1 · Oynanabilir prototip | Tek sahne, koddan arayüz, giriş, animasyonlar, 10 seviye, test + APK + imzalı AAB akışları | Kapı 1: telefonda deneme | Kodda tamam; bulut derlemesi Unity secret'larını, cihaz testi APK'yı bekliyor |
| 2 · Görünüm ve his | Creative Brief, 3 style frame, design system, sprite hattı, efektler, ses, titreşim | Kapı 2: brief onayı ve görsel yön seçimi | Brief onaylandı. v1 ve v2 reddedildi; v3 (3B render) ara seviye olarak kabul edildi. Panel rengi ve maskot henüz kesinleşmedi (şimdilik Kor ve kor ruhu) |
| 3 · Meta ve ekonomi | Harita, canlar, coin, booster envanteri, 100+ seviye | Kapı 3 | — |
| 4 · Backend ve sosyal | Hesap, bulut kayıt, sunucu doğrulaması | Kapı 4 | GDD ile çelişki açık (ROADMAP karar 4) |
| 5 · LiveOps | Etkinlikler, remote config, analitik | Kapı 5 | — |
| 6 · Yayın | Mağaza materyalleri, Play Console, kapalı test | Kapı 6: yayın | Akış hazır, hesap adımları sahibinde |

## Karar kaydı

| Tarih | Karar | Gerekçe |
|---|---|---|
| 2026-11-05 | Oyun kuralları Unity'den bağımsız, deterministik C# motorunda | Unity'siz test, bot ile seviye ayarı, ileride tekrar ve sunucu doğrulaması |
| 2026-11-05 | Tek boş sahne; sahne ve arayüz koddan kurulur | Prefab ve sahne referansı kırılmaz, her değişiklik kodda görünür |
| 2026-11-05 | Geçici görseller kodla üretilir; renk + şekil birlikte (UX-001 ACC-004) | Faz 2'deki gerçek sanata kadar görsel dosya yok |
| 2026-11-05 | Arayüz: uGUI, CanvasScaler Expand (1080×1920) | 1080×1920 alan her ekrana sığar; Pong UI Toolkit'e geçti, Faz 2'de yeniden değerlendirilir |
| 2026-11-05 | Seviyede en fazla 3 hedef | SYS-CORE-001 DL-002; hedef paneli bu sayıya göre |
| 2026-11-05 | APK yalnızca GitHub Actions'ta | Ürün sahibinin kararı; Pong ile aynı |
| 2026-11-05 | Unity Assistant ve Inference paketleri projede kalır | Ürün sahibinin kararı |
| 2026-11-05 | Eski Faz 0 kodu (`Assets/_Game`) main'e birleştirildi, kendiliğinden başlamaz | Ürün sahibinin kararı; Faz 3'te kullanılabilir |
| 2026-11-05 | Tek branch: `main`; commit'leri ürün sahibi atar | Ürün sahibinin kararı |
| 2026-11-05 | Unity 6000.3.25f1 | Pong ile aynı sürüm; testler bu sürümde geçti |
| 2026-11-06 | Pong'daki git ve yayın düzeni: imzalı AAB akışı, Git LFS, tescilli lisans, `docs/` yapısı, commit kuralları | Ürün sahibinin kararı; profesyonel ve tutarlı süreç |
| 2026-11-06 | Git LFS yalnızca bundan sonraki binary'ler için | Geçmiş yeniden yazılmaz, force push gerekmez |
| 2026-11-06 | Android: ARMv7 + ARM64, şirket adı SERDUMEN | Pong ile aynı; eski 32 bit telefonlar da desteklenir |
| 2026-11-06 | Paket adı `com.oceanforge.emberthrone` | Ürün sahibinin kararı; Pong ile aynı stüdyo düzeni. İlk mağaza yüklemesinden sonra değişmez |
| 2026-11-06 | Creative Brief v1 onaylandı; GDD ile çeliştiği yerde brief ve ROADMAP geçerli | Ürün sahibinin kararı ([brief.md](brief.md)) |
| 2026-11-06 | Özel taşlar, booster'lar ve 4 engel motordaki gibi kalır; ilk yeni engel ateş kutusu | Motor hazır, testli, seviyeler bununla ayarlı; ateş temaya uyuyor |
| 2026-11-06 | Tahta varsayılanı 8×9, özel seviyelerde 9×9 | Dikey ekranda daha büyük taşlar |
| 2026-11-06 | Animasyon: UX-001 kademeleri, kilit anlarda güçlü kutlama | Netlik korunur, büyük anlar büyür |
| 2026-11-06 | v1'de takım yok; kişisel ilerleme ve haftalık sıralama, takımlar lansmandan sonra | Tek kişilik ekip; sunucu ve destek yükü |
| 2026-11-06 | Sunucu: Unity Gaming Services | Pong'da denendi: giriş, bulut kayıt, sıralama |
| 2026-11-06 | Lansman dilleri İngilizce ve Türkçe, yapı çok dile hazır | Pong'daki yerelleştirme düzeni sonradan genişletilebilir |
| 2026-11-06 | Arayüz UI Toolkit'e geçecek (Design System adımında) | Pong ile aynı token ve tema düzeni |
| 2026-11-06 | Maskot style frame'lerle birlikte seçilecek; gelir modeli GDD'deki gibi | Görsel yönle birlikte karar verilmesi daha tutarlı |
| 2026-11-06 | Style frame v1'deki üç yön reddedildi. Hedef, türün liderinin parlak, tatlı, hacimli görünümü; karakter, taşlar, ikonlar ve renkler bize ait | Ürün sahibinin kararı: v1 düz ve ucuz görünüyordu |
| 2026-11-06 | Oyun yazı tipi Baloo 2 ExtraBold | Tombul ve okunaklı; Türkçe harflerin hepsi var (Lilita One'da "ş" yok) |
| 2026-11-06 | Style frame v2 (kodla çizim) de reddedildi; oyun görselleri Blender'da 3B modellenip render edilir (`ArtSource/blender/`) | Ürün sahibinin kalite çıtası: referanstaki 3B render görünüm. Blender ücretsiz ve bilgisayarda kurulu; scriptler aynı görseli her zaman yeniden üretir |
| 2026-11-06 | Ana sayfa: ada üstünde kül mahallelerini yıldızla "yeniden yakmak" | Brief'teki "soğuktan sıcağa dönüş" fikrinin meta oyundaki karşılığı (öneri, Kapı 2'de onaylanacak) |
| 2026-11-06 | Design system "Kor" v1. Arayüz yüzeyleri 9-slice PNG'dir, renk ve ölçüler USS token'ıdır; yazı tipi Baloo 2 | UI Toolkit'te renk geçişi ve gölge yok; v3'ün parlak görünümü ancak görsellerle korunur |
| 2026-11-06 | Style frame v3 ara seviye olarak kabul edildi; işe bu seviyede devam. Hedef kalite daha yüksek, görseller sonra ayrı bir turda yükseltilecek | Ürün sahibinin kararı ("istediğim seviyenin altında ama şimdilik bu seviyede ilerleyelim") |
| 2026-11-06 | Arayüz UI Toolkit'e geçti. `PanelSettings` koddan kurulur, görünüm `Resources/UI/Theme`'deki USS'lerdedir; C# yalnız sınıf ekler | Pong ile aynı token düzeni; tek sahne kuralı korunur; renkler tek dosyada, testle denetlenir |
| 2026-11-06 | Baloo 2, Google Fonts'un değişken fontundan sabit Bold/ExtraBold olarak, yalnız Latin harfleriyle üretilir | Unity değişken fontun yalnız varsayılan ağırlığını okur; Devanagari kısmı kullanılmıyor (683 KB yerine 2×69 KB) |
| 2026-11-06 | Animasyon sürelerinin tek kaynağı `Timing`; USS'de yalnız basma geçişi var | Testler ve ekran turu aynı süreleri bekler, iki kaynak birbirinden kopmaz |
| 2026-11-06 | Oyun içi hedef ikonları tahtadaki taşın aynısı | Oyuncu hedefi tahtada bir bakışta bulur; tahta 3B taşlara geçince ikonlar da birlikte geçer |
| 2026-11-06 | Tahta 3B render'larla çizilir; koddan çizilen geçici şekiller kaldırıldı | Style frame v3'ün görünümü; kod yalnız kare, çerçeve ve efekt şekillerini üretir |
| 2026-11-06 | Seviye sonunda kalan hamleler havai fişeğe dönüşür; yalnız gösteri, oyunun durumunu değiştirmez | Kutlama hissi; skor sistemi yok, coin ödülü Faz 3'teki ekonomiyle gelecek |
| 2026-11-06 | Sarsıntı ve titreşim gücü tek tablodan (`Feel.cs`), olayın büyüklüğüne göre | Küçük olaylar yalnız hafifçe titrer; büyük anlar büyür (brief kararı 5) |
| 2026-11-06 | Güdümlü özel taş "kor kuşu", toplanan obje "kor feneri", zemin katmanı "için için yanan kül" | Kendi kimliğimiz ve kül krallık teması; türde yaygın uçak ve taç imgelerinden uzak durur |
| 2026-11-06 | Teknik şartname [tech-spec.md](tech-spec.md)'de, Pong'daki gibi | Klasör yapısı, kurallar, seviye biçimi, testler ve araç komutları tek yerde |
| 2026-11-06 | Unity testleri ve ekran turu batch modunda çalışır; editör köprüsü kaldırıldı | Köprü 8080 portunda Docker Desktop ile çakışıyordu; batch modu editörü açmadan aynı işi yapar |
| 2026-11-06 | Kodda yorum satırı yok, belgeler nötr dilde, kişisel çalışma dosyaları git'e girmez; commit geçmişi tek yazarlı olarak yeniden yazıldı | Pong ile aynı kural; public repo profesyonel görünür |
| 2026-11-07 | Açılış ekranı Pong ile aynı stüdyo açılışı; ayarlar ve ikon bağlantısı koddan (`EmberThrone > Apply Branding`, Setup Project da çağırır) | Stüdyonun bütün oyunlarında aynı açılış; ayar elle kaybolmaz |
| 2026-11-07 | Model kalitesi çıtası: alt bölümlemeli (subdivision) modelleme, abartılı siluet, büyük/orta/küçük detay; her model önce gri kil render ve tel kafesle kontrol edilir | Ürün sahibinin gösterdiği sandık örneği; ilk modeller ilkel şekillerin üst üste konmasıydı |
| 2026-11-10 | Oyuna hikâye ve karakter kadrosu eklendi: kor ruhu, 3–5 karakter ve bir kötü karakter; her bölge açılışında oyunun içinde 20–40 saniyelik sahne (brief kararı 12) | Türün lideri karakter animasyonu ve bölüm sahneleriyle ilerliyor; GDD'deki "hikâyesiz restorasyon" kararı bununla değişti |
| 2026-11-10 | Bölge açılış sahneleri video dosyası değil, oyun içinde aynı karakter modelleriyle kurgulanır | İndirme boyutu küçük kalır, görüntü keskin olur, yazılar dile göre değişir |
| 2026-11-10 | Hikâye yönü "Ayaz'a Karşı": Emby, Lina, Odo, Mira; kötü karakter buz büyücüsü Ayaz ([story.md](story.md)) | Üç yön arasından seçildi; brief'teki "soğuktan sıcağa dönüş" ve tahtadaki buz ile kül engelleriyle birebir uyuyor |
| 2026-11-11 | Oyun içi booster çubuğu: çekiç, satır, sütun, karıştır. Ekonomi gelene kadar her birinden 3 hak (`GameRules.StartingBoosters`), cihazda saklanır | Booster'lar motorda hazırdı; coin ve mağaza Faz 3'te. Haklar o zaman envantere bağlanır |
| 2026-11-11 | Kaybedince seviye başına bir kez ücretsiz "+5 hamle" (`GameRules.ContinueMoves`) | Türün standart kurtarma anı; Faz 3'te coin ya da reklam karşılığı olur |
| 2026-11-11 | 10 sn hamle yapılmazsa ipucu: geçerli bir hamlenin taşları nabız gibi büyüyüp küçülür. İlk 3 seviyede 3 sn | Takılan oyuncu bırakmasın, yeni oyuncu ne yapacağını hemen görsün; süreler `Timing`'de |
| 2026-11-11 | Seviyeler sırayla açılır (öncekinden en az 1 yıldız); karta dokununca önce hedefleri gösteren "Seviye başı" penceresi | Türün standart akışı; oyuncu neyi toplayacağını oynamadan görür. Oyun öncesi booster seçimi Faz 3'te bu pencereye eklenir |
| 2026-11-11 | Bütün oyun görselleri karakter sayfalarının tarzına geçti: yumuşak gölgeli, parlak, koyu renkli dış çizgili çizim. Taşlar, özel taşlar, engeller, ikonlar, Emby, arka planlar ve arayüz parçaları yeniden üretildi | Ürün sahibi v3 render'larını ve arayüzü beğenmedi; karakter sayfalarındaki görünümü istedi |
| 2026-11-11 | Görseller çizim sayfası olarak üretilir, sprite'ları `tools/sprites/cut_sheets.py` keser. Kaynak sayfalar ve kesim tarifi `ArtSource/sheets/` altında | Her sprite tek komutla yeniden üretilir; boy, doluluk ve renk ayarı tarifte durur |
| 2026-11-11 | Seviye listesi yerine kıvrılan yollu harita: her bölgenin kendi harita görseli, 10 seviye düğümü yolun üstünde, bölge adı bantta; sıradaki düğümün üstünde Emby | Türün standart ilerleme ekranı; kart ızgarası beğenilmedi |
| 2026-11-11 | Yeni görünüm onaylandı: çizim tarzı, harita ve yeni arayüz parçaları oyunun görünümü olarak kalır | Ürün sahibi önce/sonra karşılaştırmasını görüp onayladı. Telefonda görülünce ince ayar yapılabilir |
| 2026-11-11 | Geçici uygulama ikonu: mevcut maskot render'ı kor ışınlı arka planda | Test APK'ları Unity'nin varsayılan ikonuyla gelmesin; kalıcı ikon maskot yenilenince |
| 2026-11-11 | Ses efektleri koddan sentezlenir (`tools/audio/generate_sfx.py`, Pong'daki yöntem). Müzik sonra | Lisans derdi yok, her ses yeniden üretilebilir; müzik için ayrı karar gerekir |
| 2026-11-11 | Seviye 11–30: Fırın Sokağı (11–20) ve Demirci Çarşısı (21–30). Her 5. seviye zor, ardından kolay bir nefes seviyesi; hamle sayıları botla ayarlı | Dalgalı zorluk eğrisi türün standardı; her bölüm yeni bir engel karışımı öğretir |

## Asset takibi

Durumlar: `Brief → WIP → Review → Approved → Integrated`

| Asset | Tür | Durum | Not |
|---|---|---|---|
| Style frame v3 | Mockup (Blender render) | Approved | Ara seviye olarak kabul edildi; v1 ve v2 reddedildi. `style-frames/` |
| `piece_R/G/B/Y/P/O` | Çizim (sayfa: `pieces.png`) | Integrated | Altı renkli taş: yakut, zümrüt, elmas, yıldız, ametist, kehribar. Mavi taş buzdan ayrılsın diye koyulaştırıldı |
| `sp_rocket`, `sp_rocket_v`, `sp_bomb`, `sp_orb`, `sp_homing` | Çizim (`specials.png`) | Integrated | Çift uçlu roket (yatay ve dikey aynı çizimden), kor bombası, renk küresi, kor kuşu |
| `ob_box1/2/3`, `ob_ice` | Çizim (`obstacles.png`) | Integrated | Sandık, demir bantlı sandık, zincirli kilitli sandık, buz (ortası yarı saydam, içindeki taş görünür) |
| `ob_floor1/2`, `ob_lantern` | Çizim (`tiles_rewards.png`, `specials.png`) | Integrated | Kül zemin (açık kül, korlu kömür), kor feneri |
| `bg_game`, `bg_map` | Çizim (`bg_game.png`) | Integrated | Oyun arkası: çayırlar, iki kulübe, tepede Kor Tahtı ocağı. `bg_map` aynı görselin bulanığı (harita kaydırma boşluğu) |
| `map_1`, `map_2`, `map_3` | Çizim (`map_1a/1b`, `map_2a/2b`, `map_3`) | Integrated | Bölge haritaları: Kıvılcım Köyü, Fırın Sokağı, Demirci Çarşısı (tepesinde Kor Tahtı). İlk ikisi ikişer yatay parçadan birleştirilir |
| `mascot`, `mascot_joy`, `mascot_sad` | Çizim (`emby.png`) | Integrated | Emby: el sallayan, sevinen, üzgün. Haritada, seviye başında, kazandın ve kaybettin pencerelerinde |
| `ui_star`, `ui_star_empty`, `ic_home`, `ic_gear` | Çizim (`specials.png`, `icons.png`) | Integrated | Yıldız (boşu aynı çizimin soluğu), ev (seviyelere dönüş), dişli (Ayarlar) |
| `bo_hammer`, `bo_row`, `bo_col`, `bo_shuffle` | Çizim (`icons.png`) | Integrated | Oyun içi booster çubuğu |
| `panel`, `btn_green`, `btn_orange`, `ring`, `chip`, `tag`, `ribbon` | 9-slice PNG (`ui_kit.png`) | Integrated | Ahşap çerçeveli krem pencere, yeşil ve turuncu buton, altın halka, lacivert hap, turuncu etiket, kurdele. Esneyen orta bölümler çizimin orta kesitinden üretilir |
| `check`, `fx_glow`, `fx_rays` | PNG | Integrated | `tools/ui_chrome/` üretir (onay işareti, ışıltı, ışınlar) |
| Altın, kalp, sandık, kupa | Çizim (`tiles_rewards.png`) | Brief | Sayfada hazır, oyuna Faz 3'te ekonomiyle girecek |
| Baloo 2 Bold, ExtraBold | Font | Integrated | `tools/fonts/make_baloo2.py` üretir; Türkçe harfleri `ThemeTests` denetler |
| Efekt şekilleri (kıymık, ışıltı, halka, kıvılcım…) | Koddan üretilen sprite | Integrated | `Scripts/Game/Art/Art.cs`; beyaz çizilir, olayın rengine boyanır |
| Uygulama ikonu | Adaptive + eski boy + mağaza | Review | Geçici: mevcut maskot render'ı kor arka planda, koyu dış çizgiyle (`tools/icon/make_app_icon.py`; `Art/AppIcon/`, mağaza boyu `docs/release/store_icon_512.png`). Maskot yeni model kalitesine çıkınca seçeneklerle yenilenecek. Unity'ye `Editor/Branding.cs` bağlar |
| Açılış ekranı | Ocean Forge logosu | Integrated | Pong'daki stüdyo açılışının aynısı: lacivert `#1E2140` zemin, Unity logosu kapalı, "Dolly", 2 sn. Logo `Art/Brand/ocean_forge_splash.png` (Pong'dan); ayarları `Editor/Branding.cs` uygular |
| Ses efektleri (18) | Sentezlenmiş WAV | Integrated | `tools/audio/generate_sfx.py` üretir, `Resources/Audio/`; palet `design-system.md`'de. Telefonda dinlenip ayarlanacak |
| Müzik | Döngü | Brief | Menü ve oyun içi; kaynak (besteci ya da lisans) henüz seçilmedi |
| Karakter kadrosu | 3B model + rig + animasyon | WIP | Emby, Lina, Odo, Mira, kötü karakter Ayaz ve kar bulutu Pofuduk ([story.md](story.md)); kral ya da kraliçe yok. Beş karakterin 4 yönlü konsept sayfası ve Ayaz'ın ifade sayfası hazır (görsel üretim aracıyla, yalnız modelleme referansı; repoda değil) |
| Bölge açılış sahneleri | Oyun içi sahne | Brief | Bölge başına 20–40 sn, karakter modelleriyle oyunun içinde |
| Mağaza tanıtım videosu ve reklam videoları | Video | Brief | Faz 6; oyun kaydı ve karakter render'larıyla |

## Lisans kaydı

| Varlık | Lisans | Kaynak | Not |
|---|---|---|---|
| Baloo 2 (değiştirilmiş) | SIL OFL 1.1 | fonts.google.com | Değişken fonttan sabit 700/800 ağırlık, yalnız Latin harfleri. Lisans metni `Assets/EmberThrone/Resources/UI/Fonts/OFL.txt` |
| Blender 5.1 | GPL (araç) | blender.org | Render edilen görseller bizimdir; modeller ve scriptler `ArtSource/blender/` (v3 dönemi, oyunda artık kullanılmıyor) |
| Çizim sayfaları (`ArtSource/sheets/`) | Stüdyonun kendi | Görsel üretim aracı, stüdyonun hesabı | Stüdyonun yazdığı tariflerle üretildi, seçildi, kesildi ve düzenlendi. Ticari kullanım aracın kullanım koşullarına göre; yayından önce koşullar yeniden okunacak |
| Ocean Forge logosu | Stüdyonun kendi | PongMobilePrototype | Açılış ekranı; yazısı Fredoka (SIL OFL 1.1), PNG'ye çizili olarak gelir |
| `GDD/Referances` ekran görüntüleri | Başkasına ait | — | Yalnız yerelde, git'e girmez; yalnız kalite kıyası için |
