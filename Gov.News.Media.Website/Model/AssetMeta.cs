using System;

namespace Gov.News.Media.Model
{
    public class AssetMeta
    {
        public string SourceUrl { get; set; }
        public string ContentType { get; set; }
        public DateTimeOffset? Expires { get; set; }
        public DateTimeOffset RequestTime { get; set; }
        public int ResponseCode { get; set; }        
    }
}
