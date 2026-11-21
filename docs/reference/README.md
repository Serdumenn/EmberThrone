# Referans ölçümleri

Tahta ve pencere animasyonlarının hedefleri, türün liderinden (Royal Kingdom) alınmış 60 fps ekran kayıtlarının kare kare ölçümüdür. Videolar repoya girmez; burada yalnız sayılar ve yöntem durur.

## Kaynak

| Kayıt | Görüntü | İçerik |
|---|---|---|
| 2026-11-15, kayıt A | 720×1564, 60 fps, 42 sn | Ana ekran, dünya haritası, bölüm videosu |
| 2026-11-15, kayıt B | 720×1564, ortalama 60 fps, 193 sn | Lobi, mağaza, sıralama, takım, 389. bölüm, kayıp akışı |

Hata payı ±1 kare (17 ms). Hareket eden taş, rengine göre maskelenip her karede ağırlık merkezinden izlenir (`tools/reference/track.py`); hızlı efektlerde her hücrenin ilk değiştiği kare alınır. 2026-11-15'teki ilk tabloda takas, düşüş ve pencere girişi göz kararı okunmuştu; 2026-11-21'de izlemeyle yeniden ölçüldü ve aşağıdaki değerlerle değiştirildi.

## Tahta

| Olay | Referans | EmberThrone | Değer |
|---|---|---|---|
| Takas | 125–135 ms, yavaşlayarak biter (yolun yarısı ~45 ms'de, %90'ı ~85 ms'de); taşlar büyümez. İki takas ölçüldü | `Timing.Swap`, `Ease.OutQuad` | 130 ms |
| Eşleşme parlaması | 70–80 ms; taş boyu değişmeden beyazlar | `Timing.MatchFlash` | 70 ms |
| Eşleşme küçülmesi | ~65 ms; taş kaybolunca halka ve kırıntılar | `Timing.MatchShrink` | 65 ms |
| Kaybolmadan düşüşe | 100–115 ms; kırıntılar uçarken düşüş başlar | `Timing.PopToFall` | 110 ms |
| Düşüş | Yerçekimi gibi: ~5 px/kare ile başlar, kare başına ~0,5 px hızlanır (hücre 79 px). Tek hücre ~150 ms, ~7,5 hücre ~0,55 sn | `Timing.FallSpeed`, `Timing.FallGravity` | 4 hücre/sn, 30 hücre/sn² (tek hücre 157 ms, yedi hücre 563 ms) |
| Roket, hücre başına | ~46 ms; iki uç ters yönlere, ateş izi 0,3 sn kalır | `Timing.RocketPerCell` | 45 ms |
| Renk küresi, ilk işaret | 150–200 ms | `Timing.ColorMarkLead` | 150 ms |
| Renk küresi, hedef başına | ortalama ~43 ms (16–83 ms arası), sıra karışık; şimşek ışınları patlamaya kadar yanık kalır | `Timing.ColorMark`, `BoardView.MarkOrder` | 40 ms |
| Renk küresi, son işaretten patlamaya | ~450 ms; sonra bütün hedefler aynı anda patlar | `Timing.ColorHold` | 440 ms |
| Bomba + bomba | 4 hücre yarıçap (resmi oyun rehberi) | `GameRules.BombBombRadius` | 4 |

## Ekran ve akış

| Olay | Referans | EmberThrone | Değer |
|---|---|---|---|
| Seviye başı ve takım isteği penceresi | Tek karede tam boyutta açılır, arka plan aynı karede kararır; üst şerit ve alt panel 35–150 ms'de yerine iner | `LevelStartPopup`, `Popup` | tek kare |
| "Devam Et?" penceresi | Üstten iner, ~165 ms'de yerini ~%13 aşar, ~430 ms'de oturur (sönümlü yay) | `ResultPopup`: `Timing.ModalIn`, `Timing.ModalDrop`, `Timing.ModalBounce` | 165 ms, 430 ms, 0,54 |
| Seviye girişi | Oyun ekranı açılır; HUD ~250 ms'de üstten, booster çubuğu 270–460 ms'de alttan gelir; tahta ~485 ms'de sağdan tek parça kayar (~100 ms ve hafif aşma) | `BoardView` `IntroDelay`, `IntroSlide` | 0,5 sn, 160 ms; HUD ve booster çubuğu henüz anında |
| Sekme geçişi | Kayma yok, tek karede değişir | — | aynı |
| Hamle bitince | "Hamlen Kalmadı!" afişi 0,1 sn'de iner, 1,6 sn durur; devam penceresi son hamleden ~2,1 sn sonra | `Timing.LoseDelay` | 1,5 sn, afiş yok |

## Karşılaştırma yöntemi

EmberThrone kareleri telefona gerek kalmadan alınır: **Actions → Motion capture → Run workflow** `MotionCapture.Record` testini çalıştırır, tahta animasyonlarını 60 fps sabit adımla kare kare kaydeder ve kareleri taslak bir release'e zip olarak ekler. Kare `n`, olaydan `n × 16,7 ms` sonrasıdır; gerçek oyun zamanı her klasördeki `times.csv`'dedir.

Telefonda ölçmek için:

1. Oyunu telefonda 60 fps ekran kaydıyla oyna.
2. Kareleri şerit olarak çıkar:

```
python tools/reference/strip.py kayit.mp4 serit.jpg --row "takas;130.29;130.47;1;200,580,560,820"
```

3. Olayın başladığı ve bittiği kareyi bul. Süre = kare sayısı × 16,7 ms.
4. Taşın yolunu sayıyla görmek için rengine göre izle. Aynı komut bulut kaydının klasörüyle de çalışır (`--frames TestResults/motion/02_takas_eslesme_dusme`):

```
python tools/reference/track.py --video kayit.mp4 --start 130.30 --end 130.50 --box 325,620,395,880 --color green
```

5. Fark ±1 kareden büyükse ilgili `Timing` sabitini ayarla ve bu tabloyu güncelle.

`strip.py` ve `track.py` için `pip install opencv-python pillow numpy` yeterlidir. `--row` birden çok kez verilebilir; biçimi `etiket;başlangıç sn;bitiş sn;kaç karede bir;x0,y0,x1,y1`. `track.py` her kare için geçen süreyi, maskelenen piksel sayısını ve ağırlık merkezini yazar; piksel sayısının düşmesi taşın küçüldüğünü ya da beyazladığını gösterir.
