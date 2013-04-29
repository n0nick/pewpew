using Bend.Util;
using PewPew.Server;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
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

                if (this.player._contollerDirection == null)
                {
                    this.player._contollerDirection = new ControllerDirection();
                }

                this.player._contollerDirection.xy = data.xy;
                this.player._contollerDirection.xz = data.xz;
                this.player._contollerDirection.yz = data.yz;
            }
            else
            {
                this.player.UpdateWeapon(String.Empty);
            }
        }

        public override void listen()
        {
            base.listener = new TcpListener(IPAddress.Any, port); // moualem changed
            listener.Start();
            while (is_active)
            {
                TcpClient s = null;
                try
                {
                    s = listener.AcceptTcpClient();
                }
                catch
                {
                    return;
                }
                HttpProcessor processor = new HttpProcessor(s, this);
                Thread thread = new Thread(new ThreadStart(processor.process));
                thread.Start();
                Thread.Sleep(1);
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

                    if(parameters["xy"] != null && parameters["xy"] != String.Empty)
                        data.xy = double.Parse(parameters["xy"]);
                    if (parameters["xz"] != null && parameters["xz"] != String.Empty)
                        data.xy = double.Parse(parameters["xz"]);
                    if (parameters["yz"] != null && parameters["yz"] != String.Empty)
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
