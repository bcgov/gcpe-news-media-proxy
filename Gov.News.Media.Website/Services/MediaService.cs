using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Gov.News.Media.Services
{
    /// <summary>
    /// Provides html wrapped content of youtube, soundcloud and facebook resources
    /// </summary>
    public class MediaService
    {
        private IHostingEnvironment _appEnv;
        private Dictionary<string, string> _templates = new Dictionary<string, string>();

        public MediaService(IHostingEnvironment appEnv)
        {                        
            _appEnv = appEnv;
            foreach (var name in new string[] { "youtube.html", "soundcloud.html", "facebook.html" })
                _templates.Add(name, GetTemplate(name));           
        }

        /// <summary>
        /// Generates html for external resrouces to be embeded on a target website
        /// </summary>
        /// <param name="templateName">Name of corresponding resource html template</param>
        /// <param name="parameters">A dictionary with keys corresponding to templates place holders to be replaced by values</param>
        /// <returns></returns>
        public string ApplyTemplate (string templateName, Dictionary<string,string> parameters)
        {
            if (!_templates.ContainsKey(templateName))
                return null;
            
            var template = _templates[templateName];
            foreach (var param in parameters)
                template = template.Replace(param.Key, param.Value);

            return template;                                                          
        }

        /// <summary>
        /// Load a template from Templates folder
        /// </summary>        
        private string GetTemplate(string templateName)
        {
            return File.ReadAllText(Path.Combine(_appEnv.ContentRootPath, "Templates", templateName), new UTF8Encoding());
        }
    }
}
