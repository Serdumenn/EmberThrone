# EmberThrone — Tasarım Süreci

Bu klasör oyunun tasarım ve teknik kararlarının kaydıdır. Plan ve durum [ROADMAP.md](../../ROADMAP.md)'dedir. Her faz bir **onay kapısı** ile biter; kapıdan geçmeyen iş bir sonraki faza taşınmaz (PongMobilePrototype'taki düzen).

## Kaynaklar

| Kaynak | İçerik |
|---|---|
| `GDD/` (yalnız yerelde, git'e girmiyor) | 16 Word belgesi: MASTER-GDD-001 özeti, PTH-001 vizyon, SYS-CORE-001 çekirdek döngü, UX-001 ekran ve animasyon, TECH-001 teknik şartname, LVL-DESIGN-001, META-001, MONETIZATION-001, LIVEOPS-001, DATA-001 ve yamaları |
| [brief.md](brief.md) | Creative Brief: kime, neden, hangi hisle; görsel yön seçenekleri ve GDD ile farkların kararları (onaylandı). GDD ile çeliştiği yerde geçerli olan budur |
| [style-frames/index.html](style-frames/index.html) | Style frame v3, Blender'da 3B render. 6 ekran var: ana sayfa (ada), seviye başı, oyun, kazandın, mağaza, sıralama. Ayrıca oyun ekranı 3 oranda, 3B parça seti ve 3 panel rengi seçeneği. Tarayıcıda açılır |
| [../../ArtSource/blender/](../../ArtSource/blender/README.md) | 3B görselleri üreten Blender scriptleri (taşlar, ikonlar, maskot, ada) |
| [design-system.md](design-system.md) | Design system "Kor": renk ve yazı token'ları, bileşenler, hareket, ses ve titreşim, erişilebilirlik, UI Toolkit uygulaması |
| [../../tools/ui_chrome/](../../tools/ui_chrome/render.py) | Arayüz parçalarını 9-slice PNG olarak çizen araç (UI Toolkit'te renk geçişi ve gölge olmadığı için) |
| `Assets/EmberThrone/Resources/UI/` | Oyundaki arayüzün görünümü: tema (USS), parçalar, 3B ikonlar, font |
| `GDD/Referances` (yalnız yerelde) | Türün lideri bir oyundan 9 referans ekranı; kalite çıtası ve ekran düzeni için, kopyalanmaz |
| [ROADMAP.md](../../ROADMAP.md) | Fazlar, yapılanlar, sahibinin vereceği kararlar (GDD ile motor arasındaki farklar dahil) |
| [../qa/](../qa/) | Kalite denetimleri |
| [../release/README.md](../release/README.md) | Yayın kılavuzu, sürüm ve commit düzeni |

## Roller

| Rol | Kim | Sorumluluk |
|---|---|---|
| Creative Director, ürün sahibi, kapı onayları, commit, cihaz testi | Fatih | Vizyon, kararlar, GitHub Desktop'tan commit ve push, telefonda deneme |
| Geliştirme, test, belgeler | Yapay zekâ ajanları | Kod, araçlar, otomatik testler, ekran görüntüsü turu, denetimler |

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
| 2026-11-05 | Tek boş sahne; sahne ve arayüz koddan kurulur | Prefab ve sahne referansı kırılmaz, ajanlar metin üzerinden çalışır |
| 2026-11-05 | Geçici görseller kodla üretilir; renk + şekil birlikte (UX-001 ACC-004) | Faz 2'deki gerçek sanata kadar görsel dosya yok |
| 2026-11-05 | Arayüz: uGUI, CanvasScaler Expand (1080×1920) | 1080×1920 alan her ekrana sığar; Pong UI Toolkit'e geçti, Faz 2'de yeniden değerlendirilir |
| 2026-11-05 | Seviyede en fazla 3 hedef | SYS-CORE-001 DL-002; hedef paneli bu sayıya göre |
| 2026-11-05 | APK yalnızca GitHub Actions'ta | Ürün sahibinin kararı; Pong ile aynı |
| 2026-11-05 | Unity editörünü ajanlar MCP for Unity köprüsüyle yönetir | Ücretsiz, MIT, yalnız bu bilgisayardan erişilir |
| 2026-11-05 | Unity AI paketleri projede kalır | Ürün sahibinin kararı |
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
