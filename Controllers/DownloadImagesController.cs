﻿using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Mvc;

namespace DownloadImages.Controllers
{
    [ApiController]
    [Route("downoload-images")]
    public class DownloadImagesController : Controller
    {
        private readonly HttpClient _httpClient;

        public DownloadImagesController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpPost("download")]
        public async Task<IActionResult> DownloadImages([FromBody] string siteUrl)
        {
            var html = await GetHtmlAsync(siteUrl);
            var images = await GetImagesSrc(html);
            if (images.Count() == 0)
                return BadRequest("Images not found");

            await SaveImagesLocal(images);
            return Ok("Task completed");
        }

        private async Task<string> GetHtmlAsync(string url)
        {
            return await _httpClient.GetStringAsync(url);
        }

        private async Task<IEnumerable<string>> GetImagesSrc(string source)
        {
            var imagesSrc = new List<string>();
            var parser = new HtmlParser();
            var document = await parser.ParseDocumentAsync(source);
            var images = document.QuerySelectorAll("img");

            if (images.Count() == 0)
                return imagesSrc;

            foreach (var element in images)
            {
                if (element is not null)
                {
                    var attribute = element.GetAttribute("src");
                    if (attribute != null)
                        imagesSrc.Add(attribute);
                }
            }

            return imagesSrc;
        }

        /// <summary>
        /// make an HTTP Get request, then read the response content into a memory stream which can be copied to a physical file.
        /// </summary>
        private async Task<bool> SaveImagesLocal(IEnumerable<string> imagesSrc)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "ImagesStorage");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            foreach (var src in imagesSrc)
            {
                var fileInfo = new FileInfo(src);
                if (!string.IsNullOrEmpty(fileInfo.Extension))
                {
                    var filePath = Path.Combine(path, $"{Guid.NewGuid()}{fileInfo.Extension}");
                    var response = await _httpClient.GetAsync(src);
                    await using var ms = await response.Content.ReadAsStreamAsync();
                    await using var fs = System.IO.File.Create(filePath);
                    ms.Seek(0, SeekOrigin.Begin);
                    ms.CopyTo(fs);
                }
            }

            return true;
        }
    }
}