using Gov.News.Media.Model;
using Gov.News.Media.Website.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Gov.News.Media.Website.Attributes
{
    public class ValidateRefererAttribute : ActionFilterAttribute
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        public ValidateRefererAttribute(IConfiguration configuration, ILogger<ValidateRefererAttribute> logger)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var request = filterContext.HttpContext.Request;
            if (! string.IsNullOrEmpty (_configuration["EnableRefererHeaderFilter"]))
            {
                var hasRefererHeader = request.Headers.ContainsKey("Referer");
                var refererHeader = hasRefererHeader ? request.Headers["Referer"].ToString() : string.Empty;

                string[] refererAllowedDomains = _configuration["RefererAllowedDomains"].Split(',');

                if (!hasRefererHeader || !SecurityUtility.IsAllowedReferer(refererHeader, refererAllowedDomains))
                {
                    _logger.LogWarning(string.Format("Access attempted with forbidden or not set referer header \"{0}\".", refererHeader));                   
                   filterContext.Result = new StatusCodeResult(403);
                }
            }
                       
            base.OnActionExecuting(filterContext);
        }
    }
}
