using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Drawing;
using SixLabors.ImageSharp.Processing.Text;
using SixLabors.Primitives;
using SixLabors.Shapes;

namespace HomeAutomation.Controllers
{
    [Route("debugimages")]
    public class DebugImageController : Controller
    {
        [Route("generate/{text}.png")]
        public IActionResult GenerateImage(string text)
        {
            using (Image<Rgba32> image = new Image<Rgba32>(1280, 720))
            {
                PathBuilder pathBuilder = new PathBuilder();
                pathBuilder.SetOrigin(new PointF(0, 0));
                pathBuilder.AddLine(new PointF(350, 350), new PointF(1000, 300));

                IPath path = pathBuilder.Build();

                Font font = SystemFonts.CreateFont("Arial", 60, FontStyle.Regular);
                
                string text2 = $"{text}{Environment.NewLine}{DateTime.Now}";
                var textGraphicsOptions = new TextGraphicsOptions(true)
                {
                    WrapTextWidth = path.Length
                };

                image.Mutate(ctx => ctx
                    .Fill(Rgba32.LightGray)
                    .DrawText(textGraphicsOptions, text2, font, Rgba32.DarkGreen, path)
                );

                using(MemoryStream ms = new MemoryStream())
                {
                    image.SaveAsPng(ms);
                    return File(ms.ToArray(), "image/png");
                }
            }
        }
    }
}