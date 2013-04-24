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

        private GameData ParseParameters(string url, out string callback)
        {
            GameData data = new GameData();
            callback = "invalid";

            try
            {
                var querystring = url.Substring(url.IndexOf('?'));
                var parameters = HttpUtility.ParseQueryString(querystring);

                if (parameters != null)
                {
                    callback = parameters["callback"];
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

                    if (parameters["g"] != null)
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
