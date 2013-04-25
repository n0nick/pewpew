using Bend.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PewPew.Game
{
    public class KinectHttpServer : HttpServer
    {
        public const int SERVER_PORT = 8080;

        public KinectHttpServer(int port)
            : base(port)
        {
        }

        public override void handleGETRequest(HttpProcessor p)
        {
            Console.WriteLine("request: {0}", p.http_url);
            p.writeSuccess();

            string callback = String.Empty;
            var data = ParseParameters(p.http_url, out callback);

            try
            {
                p.outputStream.WriteLine(callback + "(\"OK\")");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: {0}", ex.Message);
            }
        }

        public override void handlePOSTRequest(HttpProcessor p, StreamReader inputData)
        {
            p.outputStream.WriteLine("POST method not supported!");
        }

        private string ParseParameters(string url, out string callback)
        {
            string retVal = string.Empty;
            callback = "invalid";

            try
            {
                var querystring = url.Substring(url.IndexOf('?'));
                var parameters = HttpUtility.ParseQueryString(querystring);

                if (parameters != null)
                {
                    callback = parameters["callback"];
                    retVal = parameters["w"];   
                }
            }
            catch (Exception ex)
            {
            }

            return retVal;
        }
    }
}
