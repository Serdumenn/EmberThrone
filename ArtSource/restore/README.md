# Bölge onarımı görselleri

Ana ekran o anki bölgenin manzarasını gösterir. Bölge Ayaz'ın buzuyla donmuş başlar; her onarım görevi bir alanı sıcak rengine döndürür, bölgenin son görevi bütün manzarayı ısıtır (META-001 §6.2).

```bash
python tools/restore/make_restore.py
python tools/restore/make_restore.py home_2
```

İlki bütün bölgeleri, ikincisi yalnız adı verilen bölgeyi yeniden üretir. Pillow ve numpy gerekir.

## Tarif (`restore.json`)

| Alan | Anlamı |
|---|---|
| `width` | Çıktı genişliği (piksel) |
| `feather` | Alan kenarındaki yumuşak geçişin payı (0–1) |
| `districts[].name` | Çıktı adı (`home_1`, `home_2`…); sıra `LevelMap.Chapters` sırasıdır |
| `districts[].source` | `ArtSource/` altındaki sıcak (onarılmış) manzara |
| `districts[].card`, `cardPath` | İsteğe bağlı: dünya ekranı kartı için manzaradan kırpılacak yatay şerit (`[x0, y0, x1, y1]`) ve çıktı yolu |
| `districts[].areas` | Görev sırasıyla alanlar: `[merkez x, merkez y, yarıçap x, yarıçap y]`, resmin oranı olarak |

## Çıktılar (`Resources/UI/Restore/`)

| Dosya | İçerik |
|---|---|
| `home_N.jpg` | Sıcak manzara |
| `home_N_frozen.jpg` | Donmuş manzara: aynı resim, soğuk mavi ton, kar parlaması (araç üretir) |
| `home_N_K.png` | K. görevin alanı: sıcak manzaradan kesilmiş, kenarı yumuşak elips |
| `restore.json` | Oyunun okuduğu yerleşim: her alanın resmi ve resim içindeki kutusu |

Görev adları, hedefleri ve ödülleri motordadır (`Scripts/Core/Meta/Restoration.cs`); alan sırası görev sırasıyla aynıdır.

## Manzaralar

| Kaynak | Bölge |
|---|---|
| `sheets/soft_home.png` | Kıvılcım Köyü (önceki ana ekran resmi) |
| `restore/home_2.png` | Fırın Sokağı |
| `restore/home_3.png` | Demirci Çarşısı |
| `restore/home_4.png` | Fener Limanı |
| `restore/home_5.png` | Bilge Kütüphanesi |
| `restore/home_6.png` | Çiçek Bahçeleri |
| `restore/home_7.png` | Kristal Maden |
| `restore/home_8.png` | Karlı Geçit |
| `restore/home_9.png` | Donmuş Göl |
| `restore/home_10.png` | Kor Tahtı |

Yeni manzara: dikey 9:16, yukarıdan izometrik diorama, yumuşak 3B; görevlere karşılık gelen yapılar resmin farklı yerlerine dağılmış olmalı. Alan kutuları resme bakılarak elle yazılır.
