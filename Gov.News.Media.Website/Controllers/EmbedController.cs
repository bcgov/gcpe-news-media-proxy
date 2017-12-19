using Gov.News.Media.Model;
using Gov.News.Media.Services;
using Gov.News.Media.Website.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;

namespace Gov.News.Media.Website.Controllers
{
    [Route("[controller]")]
    public class EmbedController : Controller
    {
        
        private readonly MediaService _mediaService;
        private readonly ILogger _logger;

        public EmbedController(MediaService mediaService, IOptions<Settings> options, ILogger<EmbedController> logger)
        {            
            _mediaService = mediaService;
            _logger = logger;
        }

        /// <summary>
        /// Youtube resource entry point
        /// </summary>
        /// <param name="id">Track id</param>
        /// <param name="autoPlay"></param>
        /// <returns></returns>
        [HttpGet, Route("youtube"), ServiceFilter(typeof(ValidateRefererAttribute))]        
        public IActionResult GetYoutube(string id, bool autoPlay)
        {
            try
            {
                var mediaUrl = string.Format("//www.youtube-nocookie.com/embed/{0}?rel=0&amp;modestbranding=1&amp;wmode=transparent&amp;autoplay={1}", id, autoPlay.ToString().ToLower());
                var data = _mediaService.ApplyTemplate("youtube.html", new Dictionary<string, string>() { { "###media-placeholder###", mediaUrl } });

                return Content(data, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(string.Format("Failed to get Youtube html content. Please, check templates are available. Input parameters are Id:\"{0}\" , Autoplay:\"{1}\".", id, autoPlay), ex);
                throw; // 500 Internal Server Error
            }
        }

        /// <summary>
        /// Soundcloud resource
        /// </summary>
        /// <param name="id">Track id</param>
        /// <param name="autoPlay"></param>
        /// <returns></returns>
        [HttpGet, Route("soundcloud"), ServiceFilter(typeof(ValidateRefererAttribute))]        
        public IActionResult GetSoundCloud(string id, bool autoPlay)
        {
            try
            {
                string mediaUrl = string.Format("https://w.soundcloud.com/player/?url=https%3A//api.soundcloud.com/tracks/{0}&amp;color=ff5500&amp;hide_related=false&amp;show_comments=true&amp;show_user=true&amp;show_reposts=false&amp;auto_play={1}", id, autoPlay.ToString().ToLower());
                var data = _mediaService.ApplyTemplate("soundcloud.html", new Dictionary<string, string>() { { "###media-placeholder###", mediaUrl } });

                return Content(data, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(string.Format("Failed to get SoundCloud html content. Please, check templates are available. Input parameters are Id:\"{0}\" , Autoplay:\"{1}\".", id, autoPlay), ex);
                throw; // 500 Internal Server Error
            }
        }

        /// <summary>
        /// Facebook resource
        /// </summary>
        /// <param name="id">Id in the following format {user}/videos/{number}</param>
        /// <param name="autoPlay"></param>
        /// <returns></returns>
        [HttpGet, Route("facebook"), ServiceFilter(typeof(ValidateRefererAttribute))]        
        public IActionResult GetFacebook(string id, bool autoPlay)
        {
            try
            {
                var mediaUrl = string.Format("https://www.facebook.com/{0}/", id);
                var data = _mediaService.ApplyTemplate("facebook.html", new Dictionary<string, string>() { { "###media-placeholder###", mediaUrl }, { "###autoplay###", autoPlay.ToString().ToLower() } });

                return Content(data, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(string.Format("Failed to get Facebook html content. Please, check templates are available. Input parameters are Id:\"{0}\" , Autoplay:\"{1}\".", id, autoPlay), ex);
                throw; // 500 Internal Server Error
            }
        }

    }
}
