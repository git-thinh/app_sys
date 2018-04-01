using System;
using System.Security.Permissions;
using System.Threading;
using System.Reflection;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Speech.Synthesis;
using System.Text.RegularExpressions;

namespace app_sys
{
    [PermissionSet(SecurityAction.LinkDemand, Name = "Everything"), PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
    public class app
    {
        static int HTTP_PORT = 3456;
        static HttpServer _serverHTTP = null;

        static app()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (se, ev) =>
            {
                Assembly asm = null;
                string comName = ev.Name.Split(',')[0];
                string resourceName = @"DLL\" + comName + ".dll";
                var assembly = Assembly.GetExecutingAssembly();
                resourceName = typeof(app).Namespace + "." + resourceName.Replace(" ", "_").Replace("\\", ".").Replace("/", ".");
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        byte[] buffer = new byte[stream.Length];
                        using (MemoryStream ms = new MemoryStream())
                        {
                            int read;
                            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                                ms.Write(buffer, 0, read);
                            buffer = ms.ToArray();
                        }
                        asm = Assembly.Load(buffer);
                    }
                }
                return asm;
            };
        }

        public static void RUN()
        {

            //TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            //l.Start();
            //config.HTTP_PORT = ((IPEndPoint)l.LocalEndpoint).Port;
            //l.Stop();
            Console.Title = HTTP_PORT.ToString();

            //http://127.0.0.1:8888/http_-_genk.vn/ai-nay-da-danh-bai-20-luat-su-hang-dau-nuoc-my-trong-linh-vuc-ma-ho-gioi-nhat-20180227012111793.chn?_format=text 
            _serverHTTP = new HttpProxyServer();
            _serverHTTP.Start(string.Format("http://127.0.0.1:{0}/", HTTP_PORT));
            //_serverHTTP.Stop();

            Console.ReadLine();
        }
    }

    class Program
    {
        //static string _path_root = AppDomain.CurrentDomain.BaseDirectory;
        static void Main(string[] args)
        {
            app.RUN();
        }
    }
}

