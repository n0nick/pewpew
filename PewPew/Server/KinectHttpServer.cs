using Bend.Util;
using PewPew.Server;
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

        public Player player;

        public KinectHttpServer(int port)
            : base(port)
        {
        }

        public override void handleGETRequest(HttpProcessor p)
        {
            Console.WriteLine("request: {0}", p.http_url);
            p.writeSuccess();

            string callback = String.Empty;
            this.setData(ParseParameters(p.http_url, out callback));

            try
            {
                p.outputStream.WriteLine(callback + "(\"OK\")");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: {0}", ex.Message);
            }
        }

        private void setData(DataFromClient data)
        {
            if (data != null)
            {
                this.player.UpdateWeapon(data.weapons);
                this.player._contollerDirection.xy = data.xy;
                this.player._contollerDirection.xz = data.xz;
                this.player._contollerDirection.yz = data.yz;
            }
            else
            {
                this.player.UpdateWeapon(String.Empty);
            }
        }

        public override void handlePOSTRequest(HttpProcessor p, StreamReader inputData)
        {
            p.outputStream.WriteLine("POST method not supported!");
        }

        private DataFromClient ParseParameters(string url, out string callback)
        {
            callback = "invalid";
            var data = new DataFromClient();

            try
            {
                var querystring = url.Substring(url.IndexOf('?'));
                var parameters = HttpUtility.ParseQueryString(querystring);

                if (parameters != null)
                {
                    callback = parameters["callback"];
                    data.weapons = parameters["w"];

                    if(parameters["xy"] != null)
                        data.xy = double.Parse(parameters["xy"]);
                    if (parameters["xz"] != null)
                        data.xy = double.Parse(parameters["xz"]);
                    if (parameters["yz"] != null)
                        data.xy = double.Parse(parameters["yz"]);
                }
            }
            catch
            {
                data = null;
            }

            return data;
        }
    }
}
