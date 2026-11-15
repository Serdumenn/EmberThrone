# Referans ölçümleri

Tahta animasyonlarının hedef süreleri, türün liderinden (Royal Kingdom) alınmış 60 fps ekran kayıtlarının kare kare ölçümüdür. Videolar repoya girmez; burada yalnız sayılar ve yöntem durur.

## Kaynak

| Kayıt | Görüntü | İçerik |
|---|---|---|
| 2026-11-15, kayıt A | 720×1564, 60 fps, 42 sn | Ana ekran, dünya haritası, bölüm videosu |
| 2026-11-15, kayıt B | 720×1564, ortalama 60 fps, 193 sn | Lobi, mağaza, sıralama, takım, 389. bölüm, kayıp akışı |

Hata payı: 60 fps şeritlerde ±1 kare (17 ms), 30 fps şeritlerde ±33 ms.

## Tahta

| Olay | Referans | EmberThrone | Değer |
|---|---|---|---|
| Takas | 82 ms (5 kare); seçilen taş hafifçe büyüyerek kalkar | `Timing.Swap` | 85 ms |
| Eşleşme parlaması | yaklaşık 70 ms; taş beyaza döner | `Timing.MatchFlash` | 70 ms |
| Eşleşme küçülmesi | yaklaşık 65 ms; taş kaybolunca halka ve kırıntılar | `Timing.MatchShrink` | 65 ms |
| Kaybolmadan düşüşe | yaklaşık 150 ms; kırıntılar uçarken düşüş başlar | `BoardFx.GemBurst` süresi − 0,12 sn | yaklaşık 140 ms |
| Düşüş, hücre başına | yaklaşık 50 ms; inişte hafif zıplama | `Timing.GravityTick` | 55 ms |
| Roket, hücre başına | yaklaşık 48 ms; iki uç ters yönlere, ateş izi 0,3 sn kalır | `Timing.RocketPerCell` | 45 ms |
| Renk küresi, ilk işaret | yaklaşık 150 ms | `Timing.ColorMarkLead` | 150 ms |
| Renk küresi, hedef başına | yaklaşık 39 ms; hedefler sırayla şimşekle işaretlenir | `Timing.ColorMark` | 40 ms |
| Renk küresi, son işaretten patlamaya | yaklaşık 440 ms; sonra bütün hedefler aynı anda patlar | `Timing.ColorHold` | 440 ms |
| Bomba + bomba | 4 hücre yarıçap (resmi oyun rehberi) | `GameRules.BombBombRadius` | 4 |

## Ekran ve akış

| Olay | Referans | EmberThrone | Değer |
|---|---|---|---|
| Pencere açılışı | yaklaşık 165 ms; arka plan tek karede kararır | `Timing.ModalIn` | 165 ms |
| Seviye girişi | arka plan, 0,2 sn sonra HUD üstten, booster çubuğu alttan, 0,45 sn sonra tahta sağdan tek parça kayar (yaklaşık 150 ms, hafif aşma) | `BoardView` `IntroDelay`, `IntroSlide` | 0,4 sn, 160 ms |
| Sekme geçişi | kayma yok, tek karede değişir | — | aynı |
| Hamle bitince | "Hamlen Kalmadı!" afişi 0,1 sn'de iner, 1,6 sn durur; devam penceresi son hamleden yaklaşık 2,1 sn sonra | `Timing.LoseDelay` | 1,5 sn, afiş yok |

## Karşılaştırma yöntemi

1. Oyunu telefonda 60 fps ekran kaydıyla oyna.
2. Kareleri şerit olarak çıkar:

```
python tools/reference/strip.py kayit.mp4 serit.jpg --row "takas;130.29;130.47;1;200,580,560,820"
```

3. Olayın başladığı ve bittiği kareyi bul. Süre = kare sayısı × 16,7 ms.
4. Fark ±1 kareden büyükse ilgili `Timing` sabitini ayarla ve bu tabloyu güncelle.

`strip.py` için `pip install opencv-python pillow` yeterlidir. `--row` birden çok kez verilebilir; biçimi `etiket;başlangıç sn;bitiş sn;kaç karede bir;x0,y0,x1,y1`.
