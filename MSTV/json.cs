using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSTV
{
    class VHX
    {
        public class json
        {
            public string config_url { get; set; }
        }
    }


    class Vimeo
    {
        public class Progressive
        {
            public string url { get; set; }
            public int height { get; set; }
        }

        public class Files
        {
            public List<Progressive> progressive { get; set; }
        }
        
        public class TextTrack
        {
            public string lang { get; set; }
            public string url { get; set; }
        }

        public class Request
        {
            public Files files { get; set; }
            public IList<TextTrack> text_tracks { get; set; }
        }
      
        public class Json
        {
            public Request request { get; set; }
        }
    }

    
    public class Collection
    {
        public string partial { get; set; }
        public bool load_more { get; set; }
    }


    public class Credentials
    {
        public string email { get; set; }
        public string password { get; set; }
    }
}
