# EmberThrone — Ekran Kopya Şartnamesi

Tarih: 2026-11-22

Royal Kingdom'ın (RK) ekranları, yerleşimi, pencere içerikleri ve akışları birebir kopyalanır. Taşlar, engeller, karakterler, lobi haritası ve binalar EmberThrone'un (ET) kendi çizimleri olur; renkler RK'ya yakın kalır, yalnızca biraz kayar. Ölçüler 720×1564 ekran kaydından (A ve B videoları) kare kare alındı ve ET panel pikseline çevrildi (×1,5; ekran 1080×2346).

## Renk paleti

**Kural:** RK ekranlarından ölçülen her rengin yalnız tonu kaydırıldı; açıklık ve doygunluk aynı kaldı. Mor, lacivert, mavi ve bordo **+8°** (mor erik tonuna, bordo kırmızıya, mavi çivite kayar), altın **−4°** (turuncuya), yeşil **+6°**, kırmızı **+4°**. Krem ve karartma aynen kalır.

**ET'de nereye yazılır:** düğme ve panel degradeleri `tools/ui_chrome/chrome.html` içindeki `V2` ve `PANEL` dizilerinde; değişince `render.py` ile `Resources/UI/Chrome` PNG'leri yeniden üretilir. Düz renkler `Resources/UI/Theme/Tokens.uss` içinde.

| Alan | Rol | RK ölçüm | ET hedef | ET'de yeri |
|---|---|---|---|---|
| Oyun | Arka plan üst (gece göğü) | `#181244` | `#1F1244` | oyun arka plan görseli |
| Oyun | Arka plan orta | `#2D3472` | `#2F2D72` | oyun arka plan görseli |
| Oyun | Arka plan alt / zemin | `#07223F` | `#071B3F` | oyun arka plan görseli |
| Oyun | Zemin karoları | `#143556` | `#142C56` | oyun arka plan görseli |
| Oyun | HUD mor plaka | `#4E1B7C` | `#5B1B7C` | yeni: et-hud plaka |
| Oyun | HUD plaka üst parlama | `#762CC9` | `#8B2CC9` | yeni: et-hud plaka |
| Oyun | HUD sekme moru (Hedef/Hamle) | `#411164` | `#4C1164` | et-tag yerine mor sekme |
| Oyun | HUD panel kremi | `#FEF3E4` | `#FEF3E4` | et-hud__goals / __moves |
| Oyun | Portre çerçevesi altını | `#FCBF11` | `#FCAF11` | EmbyPortrait çerçeve |
| Oyun | Tahta hücresi | `#A2B0DA` | `#A2A9DA` | --color-tile (ET zaten #A6AED5) |
| Oyun | Tahta çerçevesi altını | `#F4BD1F` | `#F4AF1F` | tahta kenar sprite |
| Oyun | Güçlendirici yeşili (orta) | `#49BA00` | `#36BA00` | BoosterBar düğmesi |
| Oyun | Güçlendirici yeşili (alt) | `#035600` | `#005606` | BoosterBar düğmesi |
| Oyun | Sayaç rozeti kırmızısı | `#E53436` | `#E53E34` | et-booster sayaç |
| Oyun | Ayar düğmesi moru | `#6421A9` | `#7621A9` | BoosterBar sağ düğme |
| Pencere | Karartma | siyah %85 | siyah %85 | --color-scrim |
| Pencere | Gövde moru | `#4B1776` | `#581776` | PANEL.plum orta ton |
| Pencere | Gövde moru (alt) | `#3C1365` | `#471365` | PANEL.plum alt ton |
| Pencere | Başlık bandı moru | `#401468` | `#4B1468` | band_plum |
| Pencere | Başlık bandı parlama | `#6923AD` | `#7B23AD` | band_plum üst |
| Pencere | Çerçeve altını (açık) | `#FFB21F` | `#FFA31F` | .rim / .gold |
| Pencere | Çerçeve altını (koyu) | `#AF5309` | `#AF4809` | .rim alt |
| Pencere | İçerik kartı kremi | `#FEF4DE` | `#FEF4DE` | PANEL.cream |
| Pencere | İç panel (kazan) | `#F3E4CE` | `#F3E4CE` | yeni: iç gri-krem panel |
| Pencere | Güçlendirici yuvası mavisi (üst) | `#5BA8EE` | `#5B94EE` | V2.blue / PANEL.frost |
| Pencere | Güçlendirici yuvası mavisi (alt) | `#1B5ACC` | `#1B42CC` | V2.blue alt |
| Pencere | Oyna yeşili (üst) | `#A3EB0F` | `#8DEB0F` | V2.green 1. durak |
| Pencere | Oyna yeşili (orta) | `#6BB400` | `#59B400` | V2.green orta |
| Pencere | Oyna yeşili (alt) | `#297C00` | `#1D7C00` | V2.green alt |
| Pencere | Oyna dudağı | `#0C5303` | `#045303` | V2.green kenar |
| Pencere | Kapat kırmızısı | `#C60F2D` | `#C60F21` | btn_close |
| Pencere | Ödüller etiketi kırmızısı | `#BA0623` | `#BA0617` | yeni: ödül etiketi |
| Pencere | İlerleme çubuğu moru | `#6700B0` | `#7E00B0` | yeni: kazan çubuğu |
| Pencere | Alt etkinlik paneli laciverti | `#0A2A60` | `#0A1F60` | yeni: alt etkinlik şeridi |
| Lobi | Üst bar bordosu | `#8D1231` | `#8D1221` | hdr3 / et-topbar |
| Lobi | Değer hapı kremi | `#F1EEE0` | `#F1EEE0` | et-pill |
| Lobi | Değer yazısı | `#730C1D` | `#730C0F` | et-pill__text |
| Lobi | Avatar mavisi | `#3A71E1` | `#3A5BE1` | yeni: avatar |
| Lobi | İlerleme çubuğu mavisi | `#2543C8` | `#252DC8` | PANEL.royal |
| Lobi | Süre hapı laciverti | `#2F3DA8` | `#312FA8` | et-side__chip |
| Lobi | Etkinlik etiketi bordosu | `#8A233A` | `#8A232C` | et-side__chip (kırmızı tür) |
| Lobi | Bölüm düğmesi moru (üst) | `#6F239E` | `#7F239E` | V2.purple |
| Lobi | Bölüm düğmesi moru (alt) | `#410F57` | `#4B0F57` | V2.purple alt |
| Lobi | Kurdele bordosu (Dünya/Görevler) | `#96172D` | `#96171C` | et-ring__caption zemini |
| Lobi | Sekme çubuğu bordosu | `#8E1231` | `#8E1220` | nav3 |
| Lobi | Seçili sekme | `#BD1A46` | `#BD1A30` | nav3_on |
| Lobi | Sekme ayırıcı | `#490810` | `#490908` | nav3 çizgi |
| Meta | Başlık bandı üst | `#7F112B` | `#7F111C` | hdr3 üst |
| Meta | Başlık bandı alt | `#A51539` | `#A51526` | hdr3 alt |
| Meta | Sayfa zemini laciverti | `#1B2F4B` | `#1B294B` | bg3 (--color-page) |
| Yazı | Başlık dolgusu (krem) | `#FEF5D4` | `#FEF5D4` | et-title, et-outlined |
| Yazı | Mor zemin kontur | `#350D5D` | `#400D5D` | kontur: mor zeminde |
| Yazı | Yeşil düğme kontur | `#294309` | `#234309` | kontur: yeşil düğmede |
| Yazı | Bordo zemin kontur | `#742126` | `#742721` | kontur: bordo kurdelede |
| Yazı | Krem üstü mor yazı | `#47107E` | `#56107E` | et-ink (krem kartta) |
| Yazı | Altın başlık dolgusu | `#FCD721` | `#FCC821` | Sihirli Kazan tipi |
| Yazı | Altın başlık kontur | `#6E2C21` | `#6E2721` | Sihirli Kazan tipi |

### Yazı stili ve boyları

Yazı tipi Paytone One kalır. Başlık ve düğme yazısı: krem dolgu, ince koyu kontur (yazı boyunun yaklaşık %3'ü) ve aynı renkte aşağı kayık sert gölge (yaklaşık %7). Kontur rengi zemine göre değişir (tablodaki üç kontur). Krem kart üstündeki düz yazı konturlu değil, mor.

Boylar RK'deki büyük harf yüksekliğinden hesaplandı (Paytone One büyük harfi = 0,688 em), ET panel pikseli:

| Yazı | Boy | ET şimdi |
|---|---|---|
| Pencere başlığı (Bölüm 389) | 129 | `--font-size-title` 92 |
| Ana düğme (Oyna) | 131 | `--font-size-button` 68; uzun yazıda sığana kadar küçülür (Tekrar Dene ≈ 98) |
| Hamle sayısı | 120 | — |
| Devam Et? ve Hamlen Kalmadı! | 116 | — |
| Devam Et düğmesi | 83 | — |
| Lobi Bölüm düğmesi | 70 + 44 | iki satır: "Bölüm N" ve zorluk |
| Açıklama satırı (krem bant) | 63 | mor, sayı kırmızı |
| Güçlendirici Seç: | 57 | mor, konturlu değil |
| Hedef sayısı, üst bar değerleri | 50 | `--font-size-label` 50 |
| Kurdele (Dünya, Görevler) | 48 | — |
| Bayrak yazısı, rozet rakamı | 46 | — |
| HUD sekmesi, lobi zorluk satırı | 44 | `--font-size-body` 44 |
| Sekme etiketi (Ana Ekran) | 41 | yalnız seçili sekmede görünür |

## Oyun ekranı

**Birim:** ölçüm telefonu 720×1564 (RK kaydı ve ET'nin `MotionCapture` karesi aynı boy). Tüm sayılar ET panel pikseli: 1080×2346 (RK pikseli × 1,5). Konum = sol üst köşe (x, y).

**Ne değişecek:** HUD tek mor plakaya oturur; Emby portresi altıgen çerçeveye girer ve altındaki "Seviye N" etiketi kalkar. "Hedefler" → "Hedef", turuncu sekmeler mor olur. Tahta neredeyse tam genişliğe çıkar. Alt çubukta ev düğmesinin yerine mor ayar düğmesi gelir, sayaç rozetleri kırmızı olur. Taşlar, engeller ve karakter ET'nin kendi çizimi kalır.

| Öğe | Konum | Boyut | Ayrıntı | ET şimdi |
|---|---|---|---|---|
| Portre çerçevesi | 42, 138 | 288×327 | sivri üstlü altıgen, kalın altın kenar; karakterin başı çerçeveden taşabilir | yuvarlak çerçeve + "Seviye N" etiketi |
| Mor plaka | 22, 188 | 1040×244 | portrenin arkasından sağ kenara uzanan yuvarlak köşeli plaka, üst kenarı parlak | yok, paneller boşlukta durur |
| Hedef paneli | 354, 222 | 447×182 | krem; en çok 4 hedef, ikon ≈105, sayı ikonun sağ altında (50) | "Hedefler" paneli |
| Hedef sekmesi | 476, 174 | 201×68 | mor, altın kenar, "Hedef" (44); panelin üst kenarına biner | turuncu sekme |
| Hamle paneli | 826, 222 | 207×182 | krem; sayı 120, krem dolgu + koyu mor kontur + gölge | aynı yapı, kahverengi rakam |
| Hamle sekmesi | 834, 174 | 192×68 | mor, "Hamle" | turuncu sekme |
| Tahta | 12, 644 | 1056×1292 | örnek 9×11: hücre 117,4; kenar boşluğu 0,10 hücre; HUD altı ile güçlendirici üstü arasında dikey ortalı | `BoardMargin` 0,24 |
| Tahta çerçevesi | tahtanın dış hattı | kalınlık ≈10 | altın, köşe yarıçapı ≈24 | altın çerçeve var |
| Hücre | — | — | açık lavanta, ince açık ara çizgiler | zaten yakın |
| Güçlendirici 1–5 | x = 16 + 183·i, y 2124 | 166×166 | yeşil yuvarlak kare, altın kenar, ikon taşabilir | 5 yeşil düğme |
| Sayaç rozeti | düğmenin sağ altı | ⌀ 68 | kırmızı daire, krem rakam (46); sayı 0 ise yeşil "+" daire | lacivert daire |
| Ayar düğmesi | merkez 998, 2211 | ⌀ 130 | mor daire, krem dişli | altın ev düğmesi |
| Arka plan | tam ekran | — | üst: gece göğü ve seyrek yıldız; orta: çivit; alt dörtte bir: lacivert taş zemin karoları | koyu dumanlı gök |

## Seviye penceresi

**Ne değişecek:** Pencere mor gövde + kalın altın çerçeve olur. RK hedefleri pencerede göstermiyor (yalnız oyun içinde), bu yüzden hedef listesi, seviye adı, hamle sayısı, ipucu ve "Geri" düğmesi pencereden kalkar; kapatma sağ üstteki kırmızı X ile olur. Yeni öğeler: zorluk bayrağı, ödül etiketi, kazan ilerlemesi, 3 güçlendirici seçimi, altta etkinlik şeridi. İşlevleri karar bekleyen öğeler son bölümde.

| Öğe | Konum | Boyut | Ayrıntı |
|---|---|---|---|
| Karartma | tam ekran | — | siyah %85, lobi arkada seçilir |
| Zorluk bayrağı | orta bayrak 352, 218 | 326×210 | mor bayrak, altın direkler, tepede altın topuz (⌀ 60, y 150); iki yanda küçük bayrak (≈185×128, y 308); kurukafa + "Süper Zor!" (46); bayrak ortası pencere ortasının 25 px solunda |
| Çerçeve | 42, 405 | 996×1350 | altın kenar, desenli mor gövde |
| Başlık bandı | 90, 405 | 900×262 | mor, altın kenar, iki ucunda altın kulak; "Bölüm 389" (129) |
| Kapat | merkez 972, 472 | ⌀ 158 | kırmızı daire, altın kenar, krem X |
| Ödüller etiketi | 60, 532 | 218×322 | kırmızı asılı etiket, üstte halka; "Ödüller" + "x5", mavi "i" |
| Krem kart | 128, 728 | 825×585 | — |
| Kazan paneli | 172, 758 | 735×218 | krem-gri iç panel: kazan ikonu, "Sihirli Kazan" altın yazı + "i", mor çubuk 370×64 (424, 873), "x2" altın etiket 90×100 (742, 848) |
| Kazan ödülü | merkez 939, 813 | ⌀ 192 | altın kenarlı mavi daire, altında "0/10" etiketi; kartın sağ üst kenarına taşar |
| "Güçlendirici Seç:" | ortalı, y ≈1020 | — | mor, 57 |
| Güçlendirici yuvaları | x 168 / 435 / 693, y 1062 | ≈212×201 | mavi yuvarlak kare, açık mavi kenar; sağ altta kırmızı sayaç ⌀ 82 |
| Oyna | 237, 1362 | 606×270 | yeşil, altın kenar; "Oyna" (131) |
| Sayfa noktaları | 963, 1868 ve 1017, 1868 | ⌀ 33 | alt şeridin sayfa göstergesi |
| Alt etkinlik şeridi | 0, 1908 | 1080×438 | lacivert, üstte altın çizgi; başlık sekmesi 600×96 (240, 1887); iki bilgi kutusu 303×188 (402 ve 732, 2043); solda etkinlik görseli; yana kaydırılır |

**ET şimdi:** krem panel + bordo başlık bandı; içinde "Seviye N", zorluk etiketi, seviye adı, hedef listesi, hamle sayısı, seri satırı, ipucu; yanda maskot; Oyna + Geri. Kapat düğmesi yok.

**Açılış (kayıt B, 95,83 s, kare kare):** pencere kaydırmadan anında gelir (ET'de böyle, kalıyor). Aynı anda zorluk bayrağı başlık bandının arkasından yükselir: orta bayrak 0,16 s'de son yerinin 56 px üstüne çıkar, 0,30 s'de yerine oturur. Yan bayraklar 0,12 s sonra aynı hareketi yapar (0,25 s, 56 px taşma). Alt etkinlik şeridi 0,05 s'de alttan kayarak gelir. RK oyunu 60 fps çiziyor.

## Kayıp akışı

**Ne değişecek:** ET'deki tek "Kaybettin" penceresi (4 düğme) RK'deki gibi adımlara bölünür: önce bant, sonra tek teklifli "Devam Et?", X'e ilk dokunuşta kaybedilecekleri gösteren uyarı, ikinci dokunuşta "Bölüm Başarısız!" penceresi. Devam Et ya da Tekrar Dene oyuncuyu oyuna döndürür; çıkmak için iki ayrı X gerekir.

| # | Ekran | Ne olur | Süre |
|---|---|---|---|
| 1 | Son hamle | taşlar durur, hamle 0 | — |
| 2 | "Hamlen Kalmadı!" bandı | ekranın ortasında tam genişlik mor bant | 1,87 s |
| 3 | "Devam Et?" | tek teklif: hamle + 2 güçlendirici, 900 altın | oyuncu karar verene kadar |
| 4 | X'e 1. dokunuş | aynı pencerede teklif alanı uyarıya döner: büyücü + dolu ilerleme çubuğu + "Bunları kaybedeceksin!"; Devam Et düğmesi durur | — |
| 5 | X'e 2. dokunuş | etkinlik kaybı ekranı (RK: Zindandan Kaçış, "Mücadelede başarısız oldun") | dokununca geçer |
| 6 | "Bölüm Başarısız!" | seviye penceresinin varyantı, "Tekrar Dene" | — |
| 7 | X | yükleme ekranı | ≈1 s |
| 8 | Lobi | bekleyen pencereler sırayla açılır (RK: Krallık Düelloları, yeni etkinlik oku, Haftalık Hedefler) | — |

### Hareket ölçümleri (kayıt B, 120 fps)

| Hareket | Ölçüm (ET panel pikseli) |
|---|---|
| Bant girişi | ekranın üstünden düşer; 0,25 s'de son yerinin 69 px altına taşar, 0,41 s'de 12 px yukarı döner, 0,48 s'de durur |
| Bant bekleme | 1,05 s sabit |
| Bant çıkışı | önce 0,10 s'de 51 px yukarı kalkar, sonra hızlanarak aşağı düşer ve 0,23 s'de ekrandan çıkar |
| Pencereye geçiş | bant çıktıktan 0,1 s sonra karartma 0,1 s'de gelir; pencere yukarıdan düşer, 0,17 s'de 72 px taşar, 0,38 s'de durur. ET'deki `ResultPopup` yayı (`Timing.ModalDrop`, `ModalBounce`) bu ölçümle zaten eşleşiyor, aynısı kullanılır |
| Altın hapı | pencere otururken sol üstte belirir |
| Tanıtım kartı | pencere oturunca alttan kayarak gelir, 0,15 s |
| Teklif artışı | pencere oturduktan hemen sonra üstte "+25" parıltıyla belirir; 0,95 s sonra altıgene uçar, altıgen beyaz parlar, 0,07 s'de %14 büyür, 0,17 s'de %11 küçülür, 0,33 s'de oturur (yaylı); "+5" → "+30" |

### "Devam Et?" penceresi

Seviye penceresinden farklı: çerçevesiz, ekran genişliğinde bantlardan oluşur. Oyun karartılmış halde arkada görünür.

| Öğe | Konum | Boyut | Ayrıntı |
|---|---|---|---|
| Altın hapı | 27, 442 | 333×135 | altın simgesi + yeşil "+", krem hap içinde mor sayı |
| Üst altın çizgi | 0, 600 | 1080×15 | — |
| Başlık bandı | 0, 615 | 1080×180 | mor, desenli; "Devam Et?" (116) |
| Kapat | merkez 978, 592 | ⌀ 150 | kırmızı daire, üst altın çizgiye biner |
| Krem bant | 0, 802 | 1080×420 | açık ışın desenli krem |
| Hamle altıgeni | 142, 840 | 260×270 | kırmızı altıgen, altın kenar, "+5" altın yazı |
| Artı | 428, 922 | 98×112 | krem "+" |
| Güçlendirici kutusu | 550, 867 | 376×213 | mavi, altın kenar, içinde 2 güçlendirici |
| Açıklama | ortalı, y ≈1167 | — | "Devam etmek için **30** hamle daha ekle!" (63, mor; sayı kırmızı) |
| Mor bant | 0, 1222 | 1080×360 | — |
| Devam Et düğmesi | 188, 1272 | 708×270 | yeşil, altın kenar; "Devam Et" + altın simgesi + "900" (83) |
| Alt altın çizgi | 0, 1582 | 1080×15 | — |
| Tanıtım kartı | 22, 1620 | 1035×458 | kayan teklif kartı (RK: Altın Akını); altında 4 sayfa noktası, y 2118 |
| "Hamlen Kalmadı!" bandı (2. adım) | 0, 1054 | 1080×340 | tam genişlik mor, üst ve altta 15 px altın çizgi, hafif desen; yazı 116 |

### "Bölüm Başarısız!" penceresi

Seviye penceresinin aynısı, üç farkla: başlık bandının altına kırık kalp + "Bölüm Başarısız!" satırı girer (≈90 px, pencere bu kadar uzar); "Oyna" yerine "Tekrar Dene" (98); alt etkinlik şeridi yok. Pencere 105 px aşağıda durur (başlık yazısının ortası y ≈636).

**ET şimdi:** `GameController.ShowLose` tek pencere açıyor: "Kaybettin" + kalan hedef mesajı + seri/merdiven uyarısı; düğmeler "İzle: +5 hamle", "+5 hamle: N altın", "Tekrar dene", "Seviyeler". Seri ve merdiven uyarısı RK'deki 4. adımın (uyarı) karşılığı olur.

## Lobi

**Ne değişecek:** Üst barın soluna avatar dairesi gelir; sıra avatar · altın · can · üçüncü para · ayar olur. Barın altına tam genişlik ilerleme çubuğu ve süre hapı eklenir. Yan sütunlar 3'er düğmeden 5'er düğmeye çıkar, her düğmenin altında süre hapı durur. Yeşil "Seviye N" düğmesi mor "Bölüm N" düğmesine döner (alt satırda zorluk). Dünya ve Görevler altına bordo kurdele etiket gelir. Harita ve binalar ET'nin kendi tasarımı kalır.

| Öğe | Konum | Boyut | Ayrıntı | ET şimdi |
|---|---|---|---|---|
| Üst bar | 8, 176 | 1065×132 | bordo, iki ucu yuvarlak | bordo bar |
| Avatar | merkez 116, 244 | ⌀ 225 | bordo dış halka, gümüş halka, mavi iç; barın üstüne ve altına taşar | yok |
| Altın hapı | 244, 200 | 228×90 | altın simgesi solda taşar, yeşil "+" ⌀ 48, krem hap, bordo sayı (50) | var, 2. sırada |
| Can hapı | 488, 200 | 236×90 | kalp içinde can sayısı, yanında dolum süresi; sağ üstte kırmızı rozet ⌀ 54 | var, 1. sırada |
| Üçüncü para hapı | 735, 200 | 213×90 | RK'de mor iksir | elmas hapı |
| Ayar | merkez 1008, 252 | ⌀ 88 | turuncu daire, krem dişli | var |
| İlerleme çubuğu | 18, 380 | 1044×142 | kraliyet mavisi çerçeve, krem-altın kenarlı mavi iz; solda şimşek, ortada "0/10", sağda ödül | yok |
| Süre hapı | 447, 495 | 190×57 | lacivert, saat simgesi + süre; çubuğun alt kenarına biner | yok |
| Sol sütun | ilk daire 33, 592 | ⌀ 170 | 5 düğme, adım ≈237; altın kenarlı daire, altında lacivert ya da bordo süre hapı | 3 düğme |
| Davet Et eki | 158, 600 | 240×162 | ilk düğmenin sağına yapışık lacivert panel, yeşil "+" ve "Davet Et" | yok |
| Sağ sütun | ilk daire 878, 570 | ⌀ 170 | 5 düğme, aynı düzen; bazılarında sol üstte yeşil sayı rozeti | 3 düğme |
| Dünya | küre 60, 1785 | 147×150 | altında bordo kurdele 200×82 (38, 1950), "Dünya" (48) | yuvarlak düğme |
| Bölüm düğmesi | 300, 1808 | 483×220 | mor, altın kenar; "Bölüm N" (70) + "Süper Zor!" (44) | yeşil "Seviye N" |
| Görevler | kurdele 843, 1950 | 200×82 | parşömen simgesi + bordo kurdele | yuvarlak düğme |
| Sekme çubuğu | 0, 2108 | 1080×238 | bordo, üstte altın çizgi, koyu ayırıcılar, 5 sekme | var |
| Seçili sekme | 405, 2055 | 266×291 | daha açık bordo, altın kenar, 53 px yukarı taşar; büyük ikon + etiket (41); diğer sekmelerde etiket yok | var |

## Diğer ekranlar

**Ortak çerçeve:** üstte bordo başlık bandı y 0–308 (üstte koyu, aşağı doğru açılan degrade; altında 15 px altın çizgi), başlık ortada altın yazı (Mağaza'da solda altın hapı durur), sayfa zemini ince ızgara desenli lacivert, altta sekme çubuğu. Ayarlar ve Profil sekme çubuksuz, sağ üstte kırmızı X ile açılır. Bu ekranlarda konum ölçüsü yerine yapı ve sıra verildi.

| Ekran | RK yapısı (yukarıdan aşağı) | ET şimdi |
|---|---|---|
| Mağaza | "Özel Teklifler" bordo bölüm başlığı; tam genişlik büyük teklif kartları (her biri kendi renginde: bordo, mor-pembe, mavi; süre hapı, "%90 Ucuz" yıldız rozeti, yeşil fiyat düğmesi); paket kartları (üst yarı krem: içerik ikon ızgarası; alt yarı mor: paket adı + yeşil fiyat); "Altınlar" bölümü: tek satır altın paketleri | bölümlü mağaza: öne çıkanlar, elmas ve altın kartları, güçlendiriciler |
| Sıralama | 4'lü sekme (Haftalık · Arkadaşlar · Oyuncular · Takımlar) mavi bölmeli şerit; süre hapı; "Haftalık Yarış" başlık hapı; 3'lü kürsü (2-1-3 sırası, ortadaki yüksek; altın, gümüş, bronz kartlar; sandık + puan); satırlar ≈180 yüksek krem, kendi satırın yeşil; altta "Arkadaşlarınla Oyna" | sıralama + takımlar ekranı |
| Takım | takım kartı (rozet, ad, yıldızlar, turuncu "Takım Bilgisi"); sohbet akışı: can istekleri (kalp + ilerleme çubuğu + yeşil "Yardım"), ödül mesajı (lacivert kart, yeşil "AL"); altta Can İste · Kart İste · emoji · sohbet düğmeleri | takım ekranı + takım sandığı |
| Koleksiyon | etkinlik başlığı + büyük ödül çubuğu + süre; 3 sütunlu set kartları (ad kurdelesi, "6/9" ilerleme, ödül; tamamlanan sette yeşil tik); mor zemin | koleksiyon, kart seti, kart paketi |
| Ayarlar | tam ekran; Müzik · Ses · Titreşim · İpucu yuvarlak yeşil anahtarlar (kapalıysa kırmızı çizgi); Bildirimler anahtarı; Takım Ara · Dil yarım genişlik; tam genişlik düğmeler: İlerlemeni Kaydet (mavi), Destek, Ebeveyn Kontrolü, Şartlar ve Gizlilik (yeşil) | pencere: "Ses: Açık" türü yazılı düğmeler, Gizlilik, Tamam |
| Profil | avatara dokununca: lacivert kart (avatar, ad, takım) + sağda bordo bölüm/kupa bayrağı; "Arena İstatistikleri" (3 kutu) ve "Genel İstatistikler" (6 kutu) kurdele başlıklı krem kartlar | yok (yalnız kayıt verisi var) |
| Dünya haritası | lacivert yıldızlı zemin, ortada dönen küre, üstte krallık adı + ilerleme yüzdesi kartı; sağ kenarda kesit (film) düğmesi; altta krallık kaydırıcısı + numaralı sekmeler ("1 Ana Krallık", "2 Buz Krallığı"…) | dünya ekranı (bölgeler) |
| Kesitler | dikey kart listesi: kesit adı, kilitli ya da "Yeni", oynat düğmesi | sahneler (video görünümü) |
| Yükleme | tam ekran karakter görseli, altta "Yükleniyor" | logo + ilerleme çubuğu |
| Etkinlik pencereleri | büyük başlık görseli + süre hapı + ödül ızgarası + yeşil eylem düğmesi; sağ üstte X | etkinlik, yarış, merdiven pencereleri |

## Uygulama iş listesi

**Yerleşim kuralı:** tablolardaki y değerleri tam ekran ölçüsü. RK telefonunda güvenli alan üstte ≈60 px, altta ≈37 px (RK pikseli) duruyor: güçlendirici çubuğu alt güvenli kenara oturuyor, sekme çubuğu zemini ekranın dibine kadar iniyor. ET'de üst gruptaki öğeler (HUD, üst bar, ilerleme çubuğu) güvenli alan üstüne göre: y − 90 panel; alt gruptakiler güvenli alan altına göre yerleşir.

**Çalışma şekli:** her iş ayrı commit, testler yeşil. Her işten sonra ilgili `MotionCapture` / `ScreenshotTour` kareleri `TestResults` altına bırakılır; RK kareleriyle ölçüm karşılaştırması bu karelerle yapılır.

### Faz 1 — Yapı ve yerleşim

| # | İş | Dosyalar | Kabul |
|---|---|---|---|
| 1.0 | 720×1564 referans düzeninde güvenli alanı üst 60 / alt 37 px yap | MotionCapture.cs, ScreenshotTour.cs | kareler bu düzende üretilir |
| 1.1 | Oyun HUD'u: mor plaka, altıgen portre ("Seviye N" etiketi kalkar), Hedef ve Hamle panel + sekmeleri tablodaki ölçülerde, "Hedefler" → "Hedef" | HudView.cs, EmbyPortrait.cs, Screens.uss, Components.uss | kutular tabloya ±6 px |
| 1.2 | Tahta: kenar boşluğu 0,10 hücre, HUD altı ile güçlendirici üstü arasında dikey ortalı | GameController.cs (FitCamera, BoardMargin) | 9 sütunda hücre 117 ±2 |
| 1.3 | Alt çubuk: 5 × 166 kare, adım 183; kırmızı sayaç ⌀ 68, sayı 0 ise yeşil "+"; ev düğmesi yerine mor ayar ⌀ 130 | BoosterBar.cs, Components.uss | ±6 px |
| 1.4 | Seviye penceresi: mor gövde + altın çerçeve, başlık bandı, sağ üst X (Geri kalkar), zorluk bayrağı (şimdilik yalnız süper zor seviyede; kayıtta yalnız o görüldü) ve yükselme hareketi, Oyna. Krem kartın içi Faz 3 kararına kadar ET'deki gibi kalır (hedefler, hamle, seri). | LevelStartPopup.cs, Screens.uss | ölçüler ±6 px, bayrak zamanları ±1 kare |
| 1.5 | Kayıp akışı: "Hamlen Kalmadı!" bandı → "Devam Et?" (tam genişlik) → X: uyarı hali (ET'nin seri/merdiven uyarısı) → X: "Bölüm Başarısız!" penceresi (Tekrar Dene) → X: lobi | GameController.cs (ShowLose), ResultPopup.cs ya da yeni pencere, LevelStartPopup.cs | MotionCapture'a kayıp akışı klibi; zamanlar tabloya ±1 kare |
| 1.6 | Lobi: üst bar sırası (altın · can · üçüncü para · ayar), mor iki satırlı Bölüm düğmesi, Dünya ve Görevler kurdeleleri, seçili sekmenin taşması. Avatar ve ilerleme çubuğu karar bekliyor. | LobbyViews.cs, Screens.uss | ±6 px |
| 1.7 | Yan sütunlar RK ölçüsünde (daire ⌀ 170, adım 237, altında süre hapı); mevcut 6 düğme 3+3 yerleşir, kalan 4 yuva karar bekliyor. | LobbyViews.cs | — |
| 1.8 | Ortak çerçeve: meta başlık bandı + altın çizgi, ızgara desenli lacivert zemin; Ayarlar tam ekran ve yuvarlak anahtarlar | Screens.uss, SettingsPopup.cs | ScreenshotTour kareleri |

### Faz 2 — Renk ve yazı

| # | İş | Dosyalar |
|---|---|---|
| 2.1 | Palet tablosundaki ET değerleri: `V2`, `PANEL` dizileri ve Tokens.uss; PNG'ler render.py ile yeniden | tools/ui_chrome, Resources/UI/Chrome, Tokens.uss |
| 2.2 | Yazı boyları tokenlara; kontur rengi zemine göre üç sınıf (mor, yeşil, bordo) + sert gölge | Tokens.uss, Components.uss |
| 2.3 | Oyun arka planı: gece göğü + seyrek yıldız + alt çeyrekte taş zemin, renkler tablodan, çizim ET'nin | oyun arka plan görseli |

### Faz 3 — Karar sonrası (şimdilik yapılmaz)

| # | İş | Bekleyen karar |
|---|---|---|
| 3.1 | Seviye öncesi 3 güçlendirici seçimi (seçilen tahtaya roket, bomba ya da renk küresi olarak konur) | eklensin mi |
| 3.2 | Ödüller etiketi ve Kazan paneli | ET'de neye karşılık gelecek |
| 3.3 | Devam Et teklif içeriği, fiyatı, "+25" artışı ve reklamla devam | içerik ve fiyat |
| 3.4 | Avatar ve profil ekranı | eklensin mi |
| 3.5 | Alt etkinlik şeridi ve tanıtım kartı içerikleri | hangi ET etkinliği ve teklifi |

## Kayıtta olmayanlar

### Doğrulanamayanlar

Mevcut iki kayıtta (A: 42 s lobi ve harita, B: 193 s lobi, mağaza, sosyal ekranlar ve kaybedilen bir seviye) şunlar yok, bu yüzden şartnamede ölçüleri yok:

- Kazanma akışı: son hamleden lobiye dönüşe kadar (ödül, yıldız, kutlama). Kayıtta hiç kazanılan seviye yok.
- Zor olmayan bir seviyenin penceresi: bayrak çıkıyor mu, renk değişiyor mu.
- Oyun içinde ayar (dişli) düğmesinin açtığı pencere.
- Canlar bitince açılan pencere.
- Sayacı 0 olan güçlendiriciye (yeşil "+") dokununca açılan satın alma penceresi.
- Seçilen ön güçlendiricilerin seviye başında tahtaya nasıl konduğu.
- "Devam Et?"teki "+25" bonusun kaynağı ve ikinci devamda fiyatın artması.
- 7 ya da 8 sütunlu tahtada hücre boyu.
- Hedef sayısı 1–3 iken Hedef panelinin genişliği.
- Ödüller x5, Sihirli Kazan ve şimşek çubuğunun ne işe yaradığı.

### Çekilmesi gereken kayıtlar

Telefondan ekran kaydı, şu sırayla (her biri ayrı video olabilir):

1. Bir seviyeyi kazan: son hamleden lobiye dönene kadar hiçbir yere dokunmadan bekle, sonra devam et.
2. Zor olmayan bir seviyenin penceresini aç, 2 saniye bekle.
3. Oyun içinde dişliye dokun, açılan pencereyi kapat.
4. Sayacı 0 olan bir güçlendiriciye dokun.
5. Seviye penceresinde bir ön güçlendirici seçip Oyna'ya bas, ilk 5 saniyeyi kaydet.
6. Kaybettiğinde bu sefer "Devam Et"e bas (altın yetiyorsa).
7. Ödüller etiketindeki ve Sihirli Kazan'daki "i" düğmelerine dokun.

### Karar gerekenler

| Konu | RK'de görünen | Seçenekler |
|---|---|---|
| Ödüller etiketi, Sihirli Kazan | seviye penceresinde "x5" etiketi ve dolan kazan | ET'nin galibiyet serisi buraya taşınsın · yeni sistem · şimdilik konmasın |
| Seviye öncesi güçlendirici | 3 yuva, seçilen tahtaya konuyor | eklensin · eklenmesin |
| Reklamla devam | RK'de yok | ET'de kalsın (Devam Et penceresinde ikinci düğme) · kaldırılsın |
| Avatar ve profil | lobide avatar, dokununca istatistik ekranı | eklensin · eklenmesin |
| Üçüncü para | mor iksir | elmas kalsın · başka |
| Devam Et teklifi | 900 altın: +5 hamle + 2 güçlendirici, sonra +25 bonus | aynı · ET'nin mevcut fiyatı |
| Alt şerit ve tanıtım kartı | etkinlik (Zindandan Kaçış) ve teklif (Altın Akını) | hangi ET etkinliği ve teklifi |

Bunlara ek olarak karar bekleyen iki lobi öğesi: tam genişlik ilerleme çubuğu (RK'de şimşek, 0/10) ne gösterecek ve yan sütunlardaki 4 boş yuvaya hangi ET düğmeleri gelecek.
