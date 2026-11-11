# EmberThrone — Yol Haritası

Hedef: türün en iyileri seviyesinde akıcı, sosyal, canlı etkinlikli bir match-3.
Mekanikler türün ortak mekanikleri; isim, karakter, görsel ve seviyeler EmberThrone'a özgü.

## Faz 0 — Temel ✅
- [x] Saf C# oyun motoru (Unity'siz test edilebilir, deterministik)
  - eşleşme: 3'lü, 4'lü → roket, 2x2 → güdümlü, L/T → bomba, 5'li → renk bombası
  - 10 combo (roket+roket, bomba+bomba, renk+renk vb.), zincirleme patlama
  - engeller: kutu (can puanlı), zemin katmanı, kilit/buz katmanı, toplanan obje
  - yerçekimi: düz + çapraz akış, şekilli tahtalar (delikler), spawner'lar
  - hedefler, hamle sayacı, kazanma/kaybetme, "+5 hamle ile devam"
  - booster'lar: çekiç, satır, sütun, karıştır + oyun öncesi başlangıç özel taşları
  - hamle kalmayınca otomatik karıştırma, ipucu
- [x] JSON seviye formatı + 10 örnek seviye
- [x] Bot simülatörü: zorluk ölçümü, otomatik hamle ayarı, kırılma testi
- [x] 69 birim testi + CI (her push'ta)
- [x] Proje ayarları: dikey ekran, geçerli Android paket adı, IL2CPP + ARM64

## Faz 1 — Oynanabilir prototip (sıradaki)
- [x] Yerel geliştirmeye geç (Unity bulutta çalışmıyor); Unity testleri ve ekran turu batch modunda çalışır ([tech-spec](docs/design/tech-spec.md))
- [x] Motor kodu Unity'de ilk kez derlendi, EditMode testleri geçti
- [x] Unity sahne kurulumu kodla (tek boş bootstrap sahnesi, prefab yok)
- [x] Tahta görünümü, dokunma/kaydırma girişi (yeni Input System), hamle olaylarını animasyonla oynatma
- [x] Geçici görseller (koddan üretilen şekiller), hedef paneli, hamle sayacı, seviye seçimi, kazandın/kaybettin pencereleri
- [x] Dikey ekran, farklı ekran oranları ve çentik uyumu (20:9, 21:9, 16:9, tablet 3:4 denendi)
- [x] Play mode testleri: sanal fareyle giriş, bütün özel taş ikilileri, botun 10 seviyeyi bitirmesi; görünüm her hamlede motorla karşılaştırılıyor
- [x] GitHub Actions: test + APK akışı (PongMobilePrototype örnek alındı)
- [x] Pong'daki repo düzeni: etiketle imzalı AAB akışı, Git LFS, tescilli lisans, README, `docs/` (CI, yayın, tasarım kararları, QA), tek branch `main`
- [x] Kod denetimi ve 10 düzeltme ([docs/qa/audit-2026-11-05.md](docs/qa/audit-2026-11-05.md))
- [ ] Unity secret'larını bu repoya ekle (sahibi yapacak), ilk bulut APK'sını al
- [ ] Gerçek telefonda dene: his, dokunma, performans (henüz hiç cihazda çalışmadı)
- [ ] Bulutta test edilecek: `Sprites/Default`, UI Toolkit paneli ve Baloo 2 yazı tipi Android build'inde sorunsuz mu

## Faz 2 — Profesyonel görünüm ve his
- [x] Creative Brief taslağı ([docs/design/brief.md](docs/design/brief.md)): his, ilkeler, 3 görsel yön, GDD ile farklar için öneriler
- [x] Brief onayı ve 11 açık sorunun kararı (2026-11-06, kararlar brief'te)
- [x] Style frame v1 (3 yön): sahibi reddetti, hedef türün liderinin parlak ve hacimli görünümü
- [x] Style frame v2 (kodla çizim): sahibi basit buldu
- [x] Style frame v3: bütün görseller Blender'da 3B render. 6 ekran (ana sayfa, seviye başı, oyun, kazandın, mağaza, sıralama), 3 ekran oranı, 28 parçalık set ([docs/design/style-frames/index.html](docs/design/style-frames/index.html))
- [x] 3B görsel hattı: Blender scriptleri, şeffaf PNG sprite ve arka plan ([ArtSource/blender/](ArtSource/blender/README.md))
- [x] Kapı 2: v3 ara seviye olarak kabul edildi (2026-11-06). Panel rengi şimdilik Kor, maskot kor ruhu; ikisi de kesinleşmedi
- [x] 3B sprite'ları oyuna bağla (2026-11-06): taşlar, özel taşlar, kutular, buz, zemin ve toplanan obje Blender render'ı; arkada kale ve köy; v3'teki açık dama kareler ve koyu çerçeve. Yeni modeller: kor kuşu (güdümlü), kilitli kutu, kül zemin, kor feneri. Oyun içi hedef ikonları da 3B oldu
- [x] Görsel yenileme (2026-11-11): bütün oyun görselleri karakter sayfalarının tarzında yeniden üretildi (taşlar, özel taşlar, engeller, ikonlar, Emby'nin üç pozu, arka plan, arayüz parçaları); kaynak `ArtSource/sheets/`, kesen araç `tools/sprites/`
- [x] Seviye haritası (2026-11-11): kart ızgarası yerine kıvrılan yollu üç bölge haritası (Kıvılcım Köyü, Fırın Sokağı, Demirci Çarşısı), yol üstünde düğümler, sıradaki düğümde Emby
- [ ] Yeni görünümü telefonda gör (sahibi bilgisayarda onayladı, 2026-11-11); ödül çizimleri (altın, kalp, sandık, kupa) sayfada hazır, ekonomiyle bağlanacak
- [x] Design system belgesi: renk ve yazı token'ları, bileşenler, hareket, ses ve titreşim paleti, erişilebilirlik ([docs/design/design-system.md](docs/design/design-system.md))
- [x] Arayüz parçalarını (panel, buton, hap, rozet, sekme…) 9-slice PNG olarak üreten araç ([tools/ui_chrome/](tools/ui_chrome/render.py))
- [x] Arayüzün UI Toolkit'e geçişi (2026-11-06): token, bileşen ve ekran USS'leri; Baloo 2 (Latin, 69 KB); HUD, kazandın/kaybettin penceresi ve seviye seçimi yeni görünümde; `ThemeTests` (token dışı renk, kontrast, 9-slice, Türkçe harf); testler butonlara sanal fareyle gerçekten dokunuyor
  - öğrenilen: Unity değişken fontun yalnız varsayılan ağırlığını okur, bu yüzden sabit 700/800 dosyaları üretildi (`tools/fonts/`)
  - öğrenilen: ekran turu editör kapalıyken batch modunda çalışır
- [ ] Sanat yönü: EmberThrone teması (ateş/kor krallığı), renk paleti, karakter
- [x] Hikâye ve karakter kadrosu (2026-11-10, brief kararı 12): "Ayaz'a Karşı"; Emby, Lina, Odo, Mira, kötü karakter Ayaz ve Pofuduk; bölge sahnesi kalıbı ([docs/design/story.md](docs/design/story.md))
- [ ] Karakter modelleri, rig ve animasyonları; pilot maskotla (Unity'de gerçek zamanlı 3B ile kare dizisi karşılaştırılacak)
- [ ] Bölge açılış sahneleri: oyunun içinde, karakter modelleriyle, bölge başına 20–40 sn
- [ ] Sprite hattını genişlet: patlama ve parçacık kareleri, harita mahalleleri, yeni engeller
- [x] Efektler ve his (2026-11-06): eşleşmede savrulan renkli kırıntılar ve ışıltılar; kutuda kıymık, buzda kristal, külde duman ve kor; roket, bomba ve kombolarda ekran sarsıntısı; "Harika!", "Muhteşem!", "İnanılmaz!" yazıları; Android titreşimi (3 seviye); kazanma penceresinde konfeti
- [x] Seviye sonu kutlaması: kalan her hamle tahtada havai fişeğe dönüşür, sayaç sıfıra iner (yalnız gösteri; coin ödülü Faz 3'te ekonomiyle gelecek)
- [x] Ses efektleri (2026-11-11): 18 sentezlenmiş ses (`tools/audio/`), zincirleme eşleşmede yükselen perde; seviye listesinde Ayarlar penceresi (ses, titreşim, ekran sarsıntısı)
  - öğrenilen: koddan kurulan kamerada `AudioListener` yoktu; testte yakalandı, ses telefonda hiç duyulmayacaktı
- [ ] Müzik (menü ve oyun içi döngü), Ayarlar'a müzik seçeneği; "hareketi azalt" için sarsıntıdan sonra efekt yoğunluğu
- [ ] Sesleri telefonda dinleyip düzeylerini ayarla
- [ ] Telefonda his ayarı: titreşim gücü, sarsıntı miktarı ve efekt yoğunluğu gerçek cihazda denenip ayarlanacak
- [x] Oyun içi booster çubuğu, ipucu, "+5 hamle" teklifi (2026-11-11): tahtanın altında 4 booster (her birinden 3 hak, ekonomi gelene kadar); seçili booster parlar, tahtaya dokununca çalışır, hamle harcamaz; 10 sn hamle yoksa (ilk 3 seviyede 3 sn) ipucu nabız gibi atar; kaybedince bir kez ücretsiz "+5 hamle"
- [x] Seviye akışı (2026-11-11): bir seviye, öncekinden en az 1 yıldız alınınca açılır; kilitli kart soluk, sıradaki kart altın ve liste ona kayar. Karta dokununca "Seviye başı" penceresi: hedefler, hamle sayısı, zor seviye etiketi, "Oyna"
- [x] Ocean Forge logolu açılış ekranı, Unity logosu kapalı (Pong'daki gibi; 2026-11-07, `Editor/Branding.cs`)
- [ ] Uygulama ikonu (3B render; adaptive, eski boy ve mağaza boyları). Geçici ikon hazır (2026-11-11, `tools/icon/`); kalıcısı maskot yenilenince

## Faz 3 — Meta ve ekonomi
- [ ] Harita / ilerleme, bölge inşa-dekor meta oyunu (yıldızlarla görevler)
- [ ] Canlar (5 can, 30 dk yenilenme, sunucu saati), coin, booster envanteri
- [ ] Günlük ödül, seri bonusları, kumbara
- [x] 30 seviye (2026-11-11): 11–20 Fırın Sokağı, 21–30 Demirci Çarşısı; her 5. seviye zor, ardından nefes seviyesi; hamleler `--tune` ile ayarlı
- [ ] Seviye üretimi: prosedürel üretici + bot ile ayar → 100, sonra 500+ seviye
- [ ] Yeni engeller (önce ateş kutusu: yayılan engel), renk fırçası booster'ı

## Faz 4 — Backend ve sosyal
- [ ] Hesap + bulut kayıt
- [ ] Haftalık sıralama (v1)
- [ ] Takımlar/klanlar: kur, katıl, ara; takım sohbeti; can isteme/gönderme (lansmandan sonra)
- [ ] Liderlik tabloları, takım etkinlikleri (ortak hedefe birlikte oynama), turnuvalar
- [ ] Sunucu doğrulama: deterministik motorla hamle tekrarı (hile koruması)
- Sunucu: Unity Gaming Services, Pong'daki gibi (2026-11-06 kararı)

## Faz 5 — LiveOps
- [ ] Etkinlik altyapısı (başlangıç/bitiş sunucudan), sezon kartı, sınırlı süreli etkinlikler
- [ ] Teklifler, remote config, A/B test, analitik hunileri, push bildirim

## Faz 6 — Yayın
- [x] İmzalı AAB akışı (`release-android.yml`, etiketle çalışır)
- [ ] Yükleme anahtarı + imza secret'ları (sahibi yapacak, [docs/release/README.md](docs/release/README.md))
- [x] Reklam altyapısı (2026-11-11): AdMob eklentisi ve Gradle şablonları Pong'dan; `AdService` (ödüllü, geçiş, UMP onayı), "+5 hamle" ödüllü reklamla, kazanılan seviyeden sonra aralıklı geçiş reklamı, Ayarlar'da gizlilik seçenekleri
- [ ] Gerçek AdMob uygulama ve reklam birimi kimlikleri (sahibi açacak; şu an Google'ın deneme kimlikleri), GDPR mesajı, test cihazı
- [ ] Reklamı gerçek telefonda dene (APK çıkınca): onay penceresi, ödüllü ve geçiş reklamı
- [x] Gizlilik politikası ve oyun sayfası stüdyo sitesinde hazırlandı (2026-11-11), Ayarlar'da bağlantısı var; `app-ads.txt` zaten yayında
- [ ] Siteyi yayınla (sahibi), Play Console veri güvenliği formu, mağaza sayfası (ikon ve öne çıkan görsel `docs/release/` altında hazır)
- [ ] Google Play Billing (IAP), Play Console
- [ ] Mağaza tanıtım videosu, reklam videoları ve oynanabilir reklam
- [ ] Diller: İngilizce + Türkçe (yapı çok dile hazır)
- [ ] Soft launch (tek ülke), KPI: D1 ≥ %40, D7 ≥ %15, seviye bazlı kayıp analizi
- [ ] Gerçek oyuncu verisiyle bot kalibrasyonu ve seviye ayarı

## Sahibinin vereceği kararlar
1. GitHub secret'ları (PongMobilePrototype'takilerin aynısı) → APK derlemesi için ([docs/CI-ANDROID.md](docs/CI-ANDROID.md))
2. Tema ve sanat yönü onayı
3. ~~Paket adı / şirket adı~~ Karar verildi (2026-11-06): `com.oceanforge.emberthrone`, şirket adı SERDUMEN (Pong ile aynı düzen)
4. ~~GDD ile yol haritasının ayrıştığı noktalar~~ Karar verildi (2026-11-06): [docs/design/brief.md](docs/design/brief.md) "Kararlar" tablosu
