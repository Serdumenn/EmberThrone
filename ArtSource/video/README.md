# Videolar

Yükleme ekranının ve bölge açılış sahnelerinin kaynağı bu klasördür. Videolar görsel üretim aracıyla dikey (9:16, 720x1280, 24 kare) üretilir; oyunun yumuşak 3B görünümünde, Emby (alev ruhu) ile, yazısız ve konuşmasız.

```bash
python tools/video/make_scenes.py
python tools/video/make_scenes.py forge_story.mp4
```

İlki bütün videoları, ikincisi yalnız adı verilen kaynağı yeniden üretir. Dönüştürücü `imageio-ffmpeg` paketiyle gelir (`python -m pip install imageio-ffmpeg`), yoksa sistemdeki `ffmpeg` kullanılır.

## Neden WebM

Oyun Unity'nin `VideoPlayer`'ıyla oynatır. APK GitHub Actions'ta Linux üzerinde derlenir ve Unity'nin Linux sürümü MP4 (H.264) dosyasını içe aktaramaz; VP8 kodlu WebM her yerde açılır. Kaynak MP4'ler burada durur, oyuna giden dosyalar `make_scenes.py` ile üretilir.

## Tarif (`scenes.json`)

| Alan | Anlamı |
|---|---|
| `file` | Bu klasördeki kaynak video |
| `start`, `length` | Kaynaktan alınacak bölüm (saniye) |
| `audio` | `false` ise ses atılır (yükleme ekranı) |
| `bitrate` | Görüntü bit hızı, varsayılan `1600k` |
| `clip` | Oyuna giden WebM (`Resources/Video/`) |
| `poster` | İlk kare, JPG (`Resources/UI/Art/`). Video hazırlanırken ve oynatılamadığında görünür |

## Videolar

| Kaynak | Sahne | Oyunda |
|---|---|---|
| `village_loop.mp4` | Köy meydanı, ocağın yanında el sallayan Emby | Yükleme ekranı (`scene_loading`, ilk 4,5 sn, sessiz) |
| `village_story.mp4` | İlk 10 sn yükleme videosunun aynısı; ikinci 10 sn: bacası buzla tıkalı ocağa Emby girer, ocak yanar | Kıvılcım Köyü açılışı (`scene_1`, 10,1. saniyeden) |
| `bakery_story.mp4` | Soğuk fırın, bacada buz; Emby fırını yakar, sokak ısınır | Fırın Sokağı açılışı (`scene_2`) |
| `forge_story.mp4` | Buz tutmuş demirci ocağı; Emby ocağı yakar, kıvılcımlar uçar | Demirci Çarşısı açılışı (`scene_3`) |

## Yeni sahne eklerken

- Aynı sohbette ikinci video istenirse araç önceki videoyu uzatabilir; her sahne yeni sohbette istenir.
- İstem şablonu: dikey 9:16, yumuşak 3B CGI, çizgisiz; başta Ayaz'ın hilesi (buz, soğuk mavi ışık), Emby ocağı ya da lambayı yakar, renk ve sıcak ışık geri gelir; kamera sabit; insan, yazı ve logo yok; yalnız ortam sesi.
- Sahne `LevelMap.cs`'te bölgeye bağlanır (`SceneOf`), metni `District.Story`'dedir.
