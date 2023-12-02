using ImageMagick;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WpfDownloader.Util
{
    public static class ImageUtil
    {
        public static async Task<string> WebpToJpg(string webpPath)
        {
            using var image = new MagickImage(webpPath); // fileData could be file path or byte array etc.
            
            image.Format = MagickFormat.Jpeg;

            var jpgBytes = image.ToByteArray();

            var splitUrl = webpPath.Split('/');
            string pathWithoutFile = string.Join('/', splitUrl.SkipLast(1));
            string fileName = splitUrl[splitUrl.Length - 1].Split('.')[0];
                    
            string jpgPath = $"{pathWithoutFile}/{fileName}.jpg";
            if (File.Exists(jpgPath)) return jpgPath;

            await image.WriteAsync(jpgPath);
            

            File.Delete(webpPath);

            return jpgPath;
        }

        public static async Task<string> PngToJpg(string pngPath)
        {
            using var image = new MagickImage(pngPath); // fileData could be file path or byte array etc.
            
            image.Format = MagickFormat.Jpeg;

            var jpgBytes = image.ToByteArray();

            var splitUrl = pngPath.Split('/');
            string pathWithoutFile = string.Join('/', splitUrl.SkipLast(1));
            string fileName = splitUrl[splitUrl.Length - 1].Split('.')[0];

            string jpgPath = $"{pathWithoutFile}/{fileName}.jpg";
            await image.WriteAsync(jpgPath);
            
            File.Delete(pngPath);

            return jpgPath;
        }

        public static async Task CompressJpg(string jpgPath)
        {
            using (var image = new MagickImage(jpgPath))
            {
                image.Strip();
                image.Interlace = Interlace.Plane;
                image.GaussianBlur(0.05);
                image.Quality = 85;

                await image.WriteAsync(jpgPath);
            }
        }
    }
}
