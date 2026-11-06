# Creative Brief — EmberThrone

*v1 · 2026-11-06 · ürün sahibi onayladı. Kaynaklar: GDD (PTH-001, MASTER-GDD-001, SYS-CORE-001, UX-001, META-001), `GDD/Referances` ekranları, PongMobilePrototype'ın brief'i.*

## Oyun tek cümlede
Taşları eşleştirip sönmüş korları yeniden yak: her seviye, kül olmuş krallığında bir köşeyi ısıtıp canlandırır.

GDD'deki oyuncu fantezisi: "Parçaları yerleştiriyorum, dünyam büyüyor — her hamle bir adım ileri." (PTH-001)

## Platform ve format
- Android önce, dikey (portrait), tek el / tek başparmak, 60 FPS
- Oturum 7–12 dakika, seviye 2–4 dakika, oturumda 2–4 seviye (SYS-CORE-001 §9)
- Hamle sınırlı match-3: 8×9 tahta, 3–5 renk, hedefler, özel taşlar ve combolar
- Meta: 15 seviyelik bölümler; her bölüm krallıkta bir alanı onarır, hikâyesiz görsel ilerleme (META-001 §6.2)
- Gelir (GDD): tek seferlik uygulama içi satın alma (gem paketleri), ödüllü reklam (can, booster)

## Hedef kitle
- 25–45 yaş, ağırlıklı kadın, casual, günde 2–3 oturum (PTH-001)
- Rahatlama, görsel tatmin ve hafif zihin egzersizi için oynar; booster ve ek hamle için küçük harcama yapar
- Çocuklara yönelik değil: Pong'daki gerekçeyle, Google Play Families ve reklam kısıtlarından kaçınmak için

## Deneyim hedefi
Üç kelime: **sıcak · tatmin edici · "bir seviye daha"**

Her eşleşme küçük bir ödüldür (görsel, ses ve titreşim birlikte). Özel taşlar ve combolar "vay" anlarıdır. Her oturum sonunda krallıkta bir ışık daha yanmış olur. Oyuncu hiçbir an ne yapacağını düşünüp takılmaz; kaybettiğinde suçu kendinde görür ve hemen yeniden dener.

## Tasarım ilkeleri
GDD'deki sıra korunur: çatışmada üstteki kazanır (PTH-001).

1. **Netlik önce (PIL-01).** Hedef, kalan hamle ve geçerli hamle her an belli. 10 saniye hareketsizlikte ipucu gösterilir. Durum hiçbir zaman yalnız renkle anlatılmaz (renk + şekil).
2. **Tahta kahramandır.** Oyun sırasında arayüz ekranın en fazla %18'i (UX-001 HUD-RULE-01); süs tahtayı örtmez.
3. **Her eşleşme hissedilir, büyük anlar büyür.** Temel geri bildirim her zaman; zengin kutlama yalnız kilit anlarda: combo, kazanma, alan onarımı (UX-001 §6).
4. **İlerleme görünür (PIL-02).** Her oturumda krallıkta bir şey değişir.
5. **Para hız kazandırır, avantaj değil (PIL-03).** Ücretsiz oyuncu her seviyeyi geçebilir.
6. **Tek kişi ve yapay zekâ ile sürdürülebilir (PIL-04).** Seviyeler ve etkinlikler kural ve araçla üretilir (bot simülatörü), elle değil.
7. **Okunabilirlik.** Metin kontrastı en az 4.5:1, dokunma hedefi en az 48dp.

## Görsel yön (style frame'ler için çerçeve)
**Referanslar** `GDD/Referances` klasöründe duruyor: türün lideri bir krallık match-3'ünden 9 ekran. Ekran görüntüleri başka bir oyuna ait olduğu için repoya konmaz. Onlardan alınan şunlar:
- **Kalite çıtası:** parlak, hacimli taşlar ve kalın çerçeveli paneller
- **Ekran düzeni:** büyük yeşil "Oyna" butonu, altta sekme çubuğu, üstte hedef ve hamle kutusu, haritada bölge onarımı, oyun öncesi booster seçimi

**Kopyalanmayacaklar** (CLAUDE.md kural 7): kırmızı-altın kraliyet paleti ve kral maskotu, isimler, ikonlar, booster çizimleri. Tür dili tanıdık olacak, kimlik bize ait olacak.

**EmberThrone'un kimliği:** Korları sönmüş, kül grisine dönmüş bir krallık. Oyuncu ilerledikçe sıcak ışıklar yanar, renk geri gelir. Ana duygu "soğuktan sıcağa dönüş".

Kapı 2'de seçilmek üzere üç yön hazırlanacak: tahta, HUD ve kazandın penceresi, her biri üç ekran oranında.

| Yön | Fikir | Renk ve doku |
|---|---|---|
| **A · Kor Işığı** | Gece, fener ve sıcak ışık; sakin, büyülü | Derin lacivert ve mor zemin, amber ve kor turuncusu ışıltı, cam gibi parlayan taşlar |
| **B · Masal Ocağı** | Yumuşak, yuvarlak, oyuncak gibi; en geniş kitleye hitap eden | Krem ve sıcak pastel, kalın yumuşak çerçeveler, şekerleme taşlar |
| **C · Ejder Tahtı** | Hazine ve mücevher, küçük sevimli bir ejder maskot | Doygun mücevher renkleri, bakır ve altın metal çerçeveler, keskin parlaklık |

**Güncelleme (2026-11-06, ürün sahibi):** Bu üç yön (style frame v1) reddedildi. Hedef görünüm türün liderinin parlak, tatlı ve hacimli dili:
- doygun renkler
- cam gibi parlayan taşlar
- kalın kenarlı, parlak paneller
- dış çizgili tombul yazılar
- yeşil ana buton
- canlı bir sahne

Karakter, taşlar, ikonlar ve panel renkleri yine EmberThrone'a ait.

v2 (kodla çizim) de basit bulundu. [Style frame v3](style-frames/index.html)'te bütün görseller Blender'da 3B modellenip render edildi (`ArtSource/blender/`). Panel rengi ve maskot v3 üzerinden seçilecek.

Ana sayfa fikri: ada üstünde kül olmuş mahalleler var; oyuncu yıldız harcayarak onları "yeniden yakar" ve renk geri gelir. Bu, brief'teki "soğuktan sıcağa dönüş" fikrinin oyundaki karşılığı.

## Kısıtlar
- Unity 6000.3.25f1, URP 2D, Android API 36 (en düşük 25), IL2CPP, ARMv7 + ARM64
- Orta seviye telefonda sabit 60 FPS; ilk AAB 100 MB'ın altında (TECH-001)
- Animasyon süreleri UX-001 §6.2'ye bağlı (ANI-001…020); olay başına en fazla 50 parçacık
- Bütün görsel, ses ve fontlar ticari kullanıma ve yeniden dağıtıma uygun lisanslı (repo public). Yapay zekâ ile üretilen görsellerin kaynağı `ArtSource/` altında tutulur
- Oyun kuralları deterministik C# motorunda: bot ile seviye ayarı ve ileride sunucu doğrulaması için
- Repo, CI ve yayın düzeni PongMobilePrototype ile aynı

## Kapsam dışı (v1)
Gerçek zamanlı PvP, süren hikâye, haftalık elle hazırlanan etkinlik, lansmanda 1000'den fazla seviye, PC ve konsol (PTH-001 §6). Lansmanda takımlar ve İngilizce ile Türkçe dışındaki diller de yok (aşağıdaki kararlar 6 ve 8).

## Kararlar (2026-11-06, ürün sahibi onayladı)
GDD ile bugünkü motor ve yol haritası birkaç yerde ayrışıyordu. Aşağıdaki kararlar onaylandı; GDD ile çeliştiği yerde bu brief ve ROADMAP geçerlidir.

| # | Konu | Seçenekler | Karar |
|---|---|---|---|
| 1 | Özel taşlar | GDD: roket, 3×3 bomba, TNT, 6'lı gökkuşağı · Motor: roket, 2×2'den güdümlü, 5×5 bomba, 5'li renk bombası | **Motor.** Hazır, testli, 10 seviye bununla ayarlı ve türün bugünkü standardı |
| 2 | Oyun içi booster'lar | GDD: çekiç, bomba, renk fırçası · Motor: çekiç, satır, sütun, karıştır | **Motor**; renk fırçası Faz 3'te eklenebilir |
| 3 | Engeller | GDD: 7 tip (ateş kutusu, zincir, tel kafes, kristal…) · Motor: kutu, zemin, buz, toplanan obje | **Motorun 4'ü ile başla.** Temaya uyduğu için ateş kutusu (yayılan engel) ilk eklenecek yeni engel olsun |
| 4 | Tahta boyutu | GDD 9×9 · Seviyeler 8×9 | **8×9 varsayılan** (dikey ekranda daha büyük taşlar), özel seviyelerde 9×9 |
| 5 | Animasyon yoğunluğu | UX-001: minimal · Yol haritası ve Pong: zengin "oyun hissi" | **UX-001'in kademeleri kalsın**, kilit anlara (combo, kazanma, onarım) güçlü kutlama |
| 6 | Takımlar ve sıralama | GDD: klan kapsam dışı · Yol haritası: takımlar · Referanslar: haftalık yarışma, takımlar | **v1'de takım yok**; kişisel ilerleme ve haftalık sıralama. Takımlar lansmandan sonra |
| 7 | Sunucu | GDD: Firebase · Yol haritası: Nakama · Pong: Unity Gaming Services | **Unity Gaming Services**: Pong'da denendi (giriş, bulut kayıt, sıralama) |
| 8 | Diller | PTH-001: lansmanda tek dil · Pong: 8 dil | **Lansmanda EN + TR**, yapı çok dile hazır |
| 9 | Arayüz teknolojisi | Bugün: uGUI · Pong: UI Toolkit | Design System adımında karar; **UI Toolkit'e geçiş** (Pong ile aynı token ve tema düzeni) |
| 10 | Maskot | Yok · küçük bir kor ruhu · yavru ejder | Style frame'lerle birlikte seçilecek (UX-001 OQ-302) |
| 11 | Gelir modeli | GDD: gem paketleri + ödüllü reklam · Pong: Pass + reklamsız + tek tek satış + geçiş reklamı | GDD'deki model; ayrıntısı Faz 3 ekonomi tasarımında |
