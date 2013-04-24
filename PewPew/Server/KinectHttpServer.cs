using Bend.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PewPew.Server
{
    public class KinectHttpServer : HttpServer
    {
        public KinectHttpServer(int port)
            : base(port)
        {
        }

        public override void handleGETRequest(HttpProcessor p)
        {
            Console.WriteLine("request: {0}", p.http_url);
            p.writeSuccess();

            var data = ParseParameters(p.http_url);

            try
            {
                p.outputStream.WriteLine("\"OK\"");
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

        private GameData ParseParameters(string url)
        {
            GameData data = new GameData();

            try
            {
                var querystring = url.Substring(url.IndexOf('?'));
                var parameters = HttpUtility.ParseQueryString(querystring);

                if (parameters != null)
                {
                    data.Weapon = parameters["w"];

                    if(parameters["x"] != null)
                        data.x = float.Parse(parameters["x"]);

                    if (parameters["y"] != null)
                        data.y = float.Parse(parameters["y"]);

                    if (parameters["z"] != null)
                        data.z = float.Parse(parameters["z"]);

                    if (parameters["a"] != null)
                        data.a = float.Parse(parameters["a"]);

                    if (parameters["b"] != null)
                        data.b = float.Parse(parameters["b"]);

                    if (parameters["c"] != null)
                        data.g = float.Parse(parameters["g"]);
                }
            }
            catch (Exception ex)
            {
            }

            return data;
        }
    }
}
