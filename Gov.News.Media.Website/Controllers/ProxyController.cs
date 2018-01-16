using Gov.News.Media.Model;
using Gov.News.Media.Services;
using Gov.News.Media.Website.Attributes;
using Gov.News.Media.Website.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace Gov.News.Media.Controllers
{
    [Route("[controller]")]
    public class ProxyController : Controller
    {
        private readonly CacheService _cacheService;        
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        public ProxyController(CacheService cacheService, ILogger<ProxyController> logger, IConfiguration configuration)
        {
            _cacheService = cacheService;
            _configuration = configuration;
            _logger = logger;
        }

        private bool VerifyUrlIfEncryptionMode(string url, string token)
        {
            if (!string.IsNullOrEmpty(_configuration["EnableCryptoMode"]))
            {
                string[] passwordKeys = _configuration["PasswordKeys"].Split(',');
                foreach (var key in passwordKeys)
                {                    
                    if (SecurityUtility.GetHMAC_SHA256(url, key) == token)
                        return true;                                        
                }                
                return false;
            }
            return true;
        }

        /// <summary>
        /// Serves an image binary in the response fetched from the local server cache storage or the external server.
        /// </summary>
        /// <param name="url">Encoded url for the image source.</param>
        /// <returns></returns>
        [HttpGet, ServiceFilter(typeof(ValidateRefererAttribute))]        
        public async Task<IActionResult> Get(string url, string token)
        {
            try
            {                
                Uri uri;

                string[] allowedHosts = _configuration["AllowedHosts"].Split(',');

                if (!VerifyUrlIfEncryptionMode(url, token) ||
                    !Uri.IsWellFormedUriString(url, UriKind.Absolute) ||
                    !Uri.TryCreate(url, UriKind.Absolute, out uri) ||
                    !SecurityUtility.IsAllowedHost(uri.Host, allowedHosts)                
                   )
                {
                    _logger.LogWarning(string.Format("Invalid request attempted with the following url \"{0}\".", url));
                    return new BadRequestResult();
                }               

                var data = await _cacheService.GetContent(uri);
                return new FileContentResult(data.Data, data.ContentType ?? "application/octet-stream");
            }
            catch (AssetNotFoundException ex)
            {
                _logger.LogError(ex.Message);
                return new StatusCodeResult(502); //Bad Gateway                
            }
            catch (ServiceUnavailableException ex)
            {
                _logger.LogError(ex.Message);

                return new StatusCodeResult(503); //Service Unavailable
            }
            catch (UnsupportedContentTypeException ex)
            {
                _logger.LogError(ex.Message);
                return new StatusCodeResult(415); //Unsupported Media Type
            }
            catch (InvalidUriException ex)
            {
                _logger.LogError(ex.Message);
                return new StatusCodeResult(400); //Bad request
            }
            catch (Exception ex)
            {
                _logger.LogCritical("Configuration error - please check application configuration", ex);
                throw; // 500 Internal Server Error
            }
        }
    }
}
