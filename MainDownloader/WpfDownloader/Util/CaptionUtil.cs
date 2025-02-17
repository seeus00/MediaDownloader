using ImageMagick;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WpfDownloader.Data.Captions;

namespace WpfDownloader.Util
{
    public class CaptionUtil
    {
        private static readonly int MAX_THREADS = 5;

        public static async Task WriteCaptionsDanbooru(List<DanbooruCaptionContainer> captionsContainer)
        {
            var semaphoreSlim = new SemaphoreSlim(MAX_THREADS);

            var tasks = captionsContainer.Select(async container =>
            {
                await semaphoreSlim.WaitAsync();
                try
                {
                    using (var image = new MagickImage(container.OrigImagePath))
                    {
                        foreach (var caption in container.Captions)
                        {

                            var readSettings = new MagickReadSettings
                            {
                                Font = "Calibri",
                                TextGravity = Gravity.Center,
                                BackgroundColor = caption.BgColor,
                                Height = caption.Width, // height of text box
                                Width = caption.Height // width of text box
                            };

                            var writeCaption = new MagickImage($"caption:{caption.CaptionText}",
                                readSettings);
                            image.Composite(writeCaption, caption.PosX, caption.PosY, CompositeOperator.Over);
                        }

                        await image.WriteAsync(container.OutputImagePath);
                    }
                }finally
                {
                    semaphoreSlim.Release();
                }
            });

            await Task.WhenAll(tasks);
        }
    }
}
