using System;
using System.IO;
using System.Threading.Tasks;
using Gov.News.Media.Model;
using Newtonsoft.Json;
using System.Net.Http;
using System.Linq;
using System.Collections.Generic;
using Gov.News.Media.Website.Model;
using Gov.News.Media.Website.Utility;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Gov.News.Media.Services
{

    public class ServiceUnavailableException : Exception
    {
        public ServiceUnavailableException(string message) : base(message) {}
    }

    public class UnsupportedContentTypeException : Exception
    {
        public UnsupportedContentTypeException(string message) : base(message) {}
    }

    public class AssetNotFoundException : Exception
    {
        public AssetNotFoundException(string message) : base(message) { }
    }

    public class InvalidUriException : Exception
    {
        public InvalidUriException(string message) : base(message) { }
    }

    /// <summary>
    /// Provides proxy and caching functionality for images.
    /// </summary>
    public class CacheService
    {
        private readonly ILogger _logger;
        private readonly IOptions<Settings> _options;
       
        private ConcurrentDictionary<Uri, Task<ProxyContent>> _tasks = new ConcurrentDictionary<Uri, Task<ProxyContent>>();
        private ConcurrentDictionary<Uri, object> _locks = new ConcurrentDictionary<Uri, object>();

        public CacheService(IOptions<Settings> options, ILogger<CacheService> logger)
        {
            _logger = logger;
            _options = options;
        }

        /// <summary>
        /// If set the service will only serve images from cache and will not request external resource
        /// </summary>
        private bool IsExternalRequestDisabled { get; set; }

        /// <summary>
        /// Returns an existing running task for a given resource or creates a new one and registers it in _tasks pool
        /// </summary>
        /// <param name="key">Resource unique Uri</param>        
        public Task<ProxyContent> GetContent(Uri key)
        {
            lock (_locks.GetOrAdd(key, new object()))
            {
                Task<ProxyContent> task;
                if (_tasks.TryGetValue(key, out task))
                    return task;
                else
                {
                    task = Task.Run(async () =>
                    {
                        return await FetchAndCacheData(key);
                    });

                    _tasks.TryAdd(key, task);

                    task.ContinueWith((t) =>
                    {
                        _tasks.TryRemove(key, out task);
                    });
                    return task;
                }
            }
        }        

        /// <summary>
        /// Returns image binary to pass to the output stream.
        /// </summary>
        /// <param name="cacheFilePath">Full path to the cached file or where it should be.</param>
        /// <param name="sourceLocation">External source url to look up the image if not found in cache.</param>
        /// <returns></returns>
        private async Task<ProxyContent> FetchAndCacheData(Uri sourceLocation)
        {                        
            using (var memoryStream = new MemoryStream())
            {
                var cacheFilePath = GetFilePathFromUri(sourceLocation);
                var meta = await ReadMeta(cacheFilePath + ".meta");

                if (meta == null || 
                    meta.Expires < DateTimeOffset.Now ||
                    (!IsValidContentForProxy(meta.ResponseCode, meta.ContentType) && meta.RequestTime.AddHours(_options.Value.InvalidRequestRetryHours) < DateTimeOffset.UtcNow))
                {
                    if (IsExternalRequestDisabled)
                        throw new ServiceUnavailableException("Caching service disabled due to IO error.");
                    
                    HttpResponseMessage response;               
                    using (var client = new HttpClient())
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", _options.Value.UserAgent);
                        response = await client.GetAsync(sourceLocation);
                    }

                    if (IsValidContentForProxy((int)response.StatusCode, response.Content.Headers.ContentType?.MediaType))
                        using (var responseStream = await response.Content.ReadAsStreamAsync())
                            await responseStream.CopyToAsync(memoryStream);

                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(cacheFilePath)));
                        meta = await SaveMeta(cacheFilePath + ".meta", sourceLocation, response);

                        ThrowExceptionsIfInvalid((int)response.StatusCode, response.Content.Headers.ContentType?.MediaType, cacheFilePath + ".meta");           

                        using (var fileStream = new FileStream(cacheFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                        {
                            memoryStream.Position = 0;
                            await memoryStream.CopyToAsync(fileStream);
                        }                        
                    }
                    catch (Exception ex)
                    {
                        if (ex is AssetNotFoundException || ex is UnsupportedContentTypeException)
                            throw;

                        _logger.LogError(string.Format("Possibly IO write error for {0}. The service is going to be downgraded to allow only cached data read access. Please, restart the service after the issue has been resolved.", cacheFilePath), ex);
                        IsExternalRequestDisabled = true; //Puts the service in readonly mode

                        if (memoryStream.Length == 0) //Only throw if no content is in the stream (not really possible condition atm because of other exceptions) otherwise lets return it to the client.
                            throw;                        
                    }
                }
                else
                {
                    ThrowExceptionsIfInvalid(meta.ResponseCode, meta.ContentType, cacheFilePath + ".meta");

                    using (var fileStream = new FileStream(cacheFilePath, FileMode.Open , FileAccess.Read, FileShare.Read, 4096, true))
                        await fileStream.CopyToAsync(memoryStream);                                     
                }
                return  new ProxyContent() { Data = memoryStream.ToArray(), ContentType = meta?.ContentType };
            }
        }

        /// <summary>
        /// Builds local file system file path from the Uri data
        /// </summary>        
        private string GetFilePathFromUri(Uri uri)
        {
            var filePath = Path.Combine(_options.Value.AssetsCacheFolder, uri.Host, SecurityUtility.GetHash(uri.AbsoluteUri));

            if (!SecurityUtility.IsInsidePath(_options.Value.AssetsCacheFolder, filePath)) //Impossible really, but ...
                throw new InvalidUriException(string.Format("Invalid request and attempt to access outside Cache folder with the url {0}.", uri.AbsoluteUri));

            return filePath;    
        }

        /// <summary>
        /// Check if valid content and status code.
        /// </summary>        
        private bool IsValidContentForProxy (int httpStatusCode, string contentType)
        {
            return SecurityUtility.IsSuccessStatusCode(httpStatusCode) && SecurityUtility.IsAllowedContentType(contentType, _options.Value.AllowedContentType);
        }
        
        /// <summary>
        /// Throw appropriate exceptions if not successful status code or wrong content type.
        /// </summary>        
        private void ThrowExceptionsIfInvalid(int httpStatusCode, string contentType, string metaFilePath)
        {
            if (!SecurityUtility.IsSuccessStatusCode(httpStatusCode))
                throw new AssetNotFoundException(string.Format("External resource request unsuccessful. Asset not found with the error code {0}. Metadata file: {1}", httpStatusCode, metaFilePath));
            else if (!SecurityUtility.IsAllowedContentType(contentType, _options.Value.AllowedContentType))
                throw new UnsupportedContentTypeException(string.Format("External resource content-type header {0} is not an allowed type by the proxy. Check appsettings.json configuration. Metadata file: {1}", contentType, metaFilePath));
        }

        /// <summary>
        /// Saves meta info for cached files such as Source, ContentType and Expiry date
        /// </summary>
        /// <param name="cacheFilePath">Full path to the cached (image) file.</param>
        /// <param name="sourceLocation">External source url.</param>
        /// <param name="response">Response object from the external server contining information we want to store in meta data.</param>
        private async Task <AssetMeta> SaveMeta(string metaFilePath, Uri sourceLocation, HttpResponseMessage response)
        {
            if (response != null)
            {    
                var meta = new AssetMeta()
                {
                    SourceUrl = sourceLocation.AbsoluteUri,
                    ContentType = response.Content.Headers.ContentType?.MediaType,
                    ResponseCode = (int)response.StatusCode,
                    RequestTime = DateTimeOffset.UtcNow
                    //Expires = response.Content.Headers?.Expires //unfortunately expires does not always comply with standards (see below)
                };

                IEnumerable<string> expires;                
                if (response.Content.Headers.TryGetValues("Expires", out expires))
                {
                    DateTimeOffset expiryDate;
                    var dateStr = expires.FirstOrDefault()?.Replace("UTC", "GMT"); //Flickr date is not compliant to ISO 8601
                    if (DateTimeOffset.TryParse(dateStr, out expiryDate))
                        meta.Expires = expiryDate;
                }

                using (var fileStream = new FileStream(metaFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                    using (var writer = new StreamWriter(fileStream))
                        await writer.WriteAsync(JsonConvert.SerializeObject(meta));

                return meta;
            }
            return null;
        }

        /// <summary>
        /// Reads meta data file and deserializes it into AssetMeta object
        /// </summary>        
        private async Task<AssetMeta> ReadMeta (string metaFilePath)
        {            
            try
            {
                if (File.Exists(metaFilePath))
                {
                    using (var fileStream = new FileStream(metaFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                        using (var reader = new StreamReader(fileStream))
                            return JsonConvert.DeserializeObject<AssetMeta>(await reader.ReadToEndAsync());
                }    
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(string.Format("Failed to read metadata file {0}. Recoverable error. If the metadata file is corrupt it will be restored. You can ignore the warning unless it persists.", metaFilePath), ex);                
            }
            return null;
        }
    }
}
