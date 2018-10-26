using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Gov.News.Media.Website.Utility
{
    public class SecurityUtility
    {
        /// <summary>
        /// This a very basic test if file path is inside path.
        /// </summary>
        public static bool IsInsidePath(string path, string filePath)
        {
            var fullPath = Path.GetFullPath(path).Replace('/', '\\').TrimEnd('\\') + '\\';
            var fullFilePath = Path.GetFullPath(filePath).Replace('/', '\\');

            return fullFilePath.StartsWith(fullPath, System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verify that the host domain name is in a sub-domain of the white listed hosts
        /// </summary>
        /// <returns></returns>
        public static bool IsAllowedHost(string hostName, string[] allowedMediaHosts)
        {
            foreach (var allowed in allowedMediaHosts)
                if (hostName.EndsWith('.' + allowed))
                    return true;

            return false;
        }

        /// <summary>
        /// Verify that the content type is in the list allowed by this proxy
        /// </summary>
        public static bool IsAllowedContentType(string contentType, string[] allowedContentTypes)
        {
            foreach (var allowed in allowedContentTypes)
                if (allowed.ToLower() == contentType.ToLower())
                    return true;

            return false;
        }

        /// <summary>
        /// Verify that the host url is in the list of allowed referrer hosts list
        /// </summary>
        public static bool IsAllowedReferer(string url, string[] allowedReferers)
        {
            Uri uri;
            // allow http traffic - as pod to pod traffic in OpenShift is http.
            if (Uri.TryCreate(url, UriKind.Absolute, out uri))
            {

                foreach (var allowed in allowedReferers)
                    if (allowed.ToLower() == uri.Host.ToLower())
                        return true;
            }

                

            return false;
        }

        /// <summary>
        /// Check for successful status code. It complies with HttpResponseMessage.IsSuccessStatusCode implementation.
        /// </summary>
        public static bool IsSuccessStatusCode(int httpStatusCode)
        {
            return httpStatusCode >= 200 && httpStatusCode <= 299;
        }

        /// <summary>
        /// Obliterates any string into gibberish.
        /// </summary>
        public static string GetHash(string inputString)
        {
            using (MD5 md5 = MD5.Create())
            {
                return BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(inputString))).Replace("-", "");
            }
        }


        public static string GetHMAC_SHA256(string data, string key)
        {
            if (string.IsNullOrEmpty(data) || string.IsNullOrEmpty(key))
                return null;

            var hmac = new HMACSHA256(Convert.FromBase64String(key));
            return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));
        }

    }
}
