using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RSBM.WebUtil
{
    class CookieAwareWebClient : WebClient
    {
        private readonly CookieContainer cc = new CookieContainer();
        private string lastPage;

        protected override WebRequest GetWebRequest(System.Uri address)
        {
            WebRequest R = base.GetWebRequest(address);
            if (R is HttpWebRequest WR)
            {
                WR.CookieContainer = cc;
                //WR.UnsafeAuthenticatedConnectionSharing = true;
                WR.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:2.0) Gecko/20100101 Firefox/4.0";
                if (lastPage != null)
                {
                    WR.Referer = lastPage;
                }
            }
            lastPage = address.ToString();
            return R;
        }

    }
}
