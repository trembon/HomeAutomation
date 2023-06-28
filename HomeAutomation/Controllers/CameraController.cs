using HomeAutomation.Models.Camera;
using HomeAutomation.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Drawing;
using SixLabors.ImageSharp.Processing.Text;
using SixLabors.Primitives;
using SixLabors.Shapes;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace HomeAutomation.Controllers
{
    public class CameraController : Controller
    {
        private readonly IJsonDatabaseService jsonDatabaseService;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IConfiguration configuration;

        public CameraController(IJsonDatabaseService jsonDatabaseService, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            this.jsonDatabaseService = jsonDatabaseService;
            this.httpClientFactory = httpClientFactory;
            this.configuration = configuration;
        }

        public IActionResult Index()
        {
            ListCameraModel model = new()
            {
                Cameras = jsonDatabaseService.Cameras.OrderBy(x => x.Name).ToList()
            };

            return View(model);
        }

        [Route("/camera/capture/{deviceId}/{size}")]
        public async Task<IActionResult> CaptureCamera(int deviceId, string size)
        {
            var camera = jsonDatabaseService.Cameras.FirstOrDefault(x => x.ID == deviceId);
            if (camera == null)
                return NotFound();

            if (configuration.GetValue("Cameras:CaptureImages", false))
            {
                string captureUrl = size == "thumbnail" ? camera.ThumbnailURL : camera.URL;
                try
                {
                    var webClient = httpClientFactory.CreateClient("cameras");
                    using var stream = await webClient.GetStreamAsync(captureUrl);
                    return File(stream, "image/png");
                }
                catch
                {
                    return NotFound();
                }
            }
            else
            {
                using Image<Rgba32> image = new(1280, 720);

                PathBuilder pathBuilder = new();
                pathBuilder.SetOrigin(new PointF(0, 0));
                pathBuilder.AddLine(new PointF(350, 350), new PointF(1000, 300));

                IPath path = pathBuilder.Build();

                Font font = SystemFonts.CreateFont("Arial", 60, FontStyle.Regular);

                string text2 = $"{camera.Name}{Environment.NewLine}{DateTime.Now}";
                var textGraphicsOptions = new TextGraphicsOptions(true)
                {
                    WrapTextWidth = path.Length
                };

                image.Mutate(ctx => ctx
                    .Fill(Rgba32.LightGray)
                    .DrawText(textGraphicsOptions, text2, font, Rgba32.DarkGreen, path)
                );

                using MemoryStream ms = new();
                image.SaveAsPng(ms);
                return File(ms.ToArray(), "image/png");
            }
        }
    }
}