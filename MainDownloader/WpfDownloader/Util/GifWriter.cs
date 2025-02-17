using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using WpfDownloader.Data.Imaging;

namespace Downloader.Util
{
    public class GifWriter
    {
        public static async Task ZipToGifBatch(List<ZipToGifData> entries)
        {
            foreach (var zipToGifData in entries.ToList())
            {
                string zipPath = zipToGifData.TempPathName + ".zip";
                string gifPath = zipToGifData.TempPathName + ".gif";

                using (var file = ZipFile.Open(zipPath, ZipArchiveMode.Read))
                {
                    await Task.Run(() => file.ExtractToDirectory(zipToGifData.TempPathName));
                }

                using (MagickImageCollection collection = new MagickImageCollection())
                {
                    int i = 0;
                    foreach (GifFrameData frameData in zipToGifData.Frames)
                    {
                        collection.Add($"{zipToGifData.TempPathName}/{frameData.FrameName}");
                        collection[i++].AnimationDelay = frameData.FrameDelay / 10;
                    }

                    // Optionally reduce colors
                    QuantizeSettings settings = new QuantizeSettings();
                    settings.Colors = 256;
                    collection.Quantize(settings);

                    // Optionally optimize the images (images should have the same size).
                    collection.Optimize();

                    // Save gif
                    await collection.WriteAsync(gifPath);

                    collection.Dispose();
                }

                //Delete zip + extracted folder
                await Task.Run(() =>
                {
                    File.Delete(zipPath);
                    Directory.Delete(zipToGifData.TempPathName, true);
                });
            }
        }
    }
}
