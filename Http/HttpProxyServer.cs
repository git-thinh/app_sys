using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

namespace app_sys
{
    public class HttpProxyServer : HttpServer
    {
        static string PATH_ROOT = AppDomain.CurrentDomain.BaseDirectory;
        static string[] DIV_CLASS_END = new string[] { };
        static string[] TEXT_END = new string[] { };

        public HttpProxyServer()
        {
            if (File.Exists("DIV_CLASS_END.txt"))
                DIV_CLASS_END = File.ReadAllLines("DIV_CLASS_END.txt");
            if (File.Exists("TEXT_END.txt"))
                TEXT_END = File.ReadAllLines("TEXT_END.txt");
        }

        private bool hasH1 = false, hasContentEnd = false;

        private string getURL(string url)
        {
            string s = string.Empty;
            int p = url.IndexOf("url=");
            if (p > 0)
            {
                p += 4;
                s = url.Substring(p, url.Length - p).Trim();
                p = s.IndexOf("type=");
                if (p > 0)
                    s = s.Substring(p);
            }
            return s;
        }

        protected override void ProcessRequest(System.Net.HttpListenerContext Context)
        {
            HttpListenerRequest Request = Context.Request;
            HttpListenerResponse Response = Context.Response;
            string result = string.Empty,
                content_type = "text/html; charset=utf-8",
                uri = HttpUtility.UrlDecode(Request.RawUrl);
            Stream OutputStream = Response.OutputStream;

            switch (uri)
            {
                case "/favicon.ico":
                    break;
                case "/DIV_CLASS_END":
                    #region
                    content_type = "text/plain; charset=utf-8";
                    if (File.Exists("DIV_CLASS_END.txt"))
                    {
                        DIV_CLASS_END = File.ReadAllLines("DIV_CLASS_END.txt");
                        result = string.Join(Environment.NewLine, DIV_CLASS_END);
                    }
                    else
                        result = "Cannot find file DIV_CLASS_END.txt";
                    #endregion
                    break;
                case "/TEXT_END":
                    #region
                    content_type = "text/plain; charset=utf-8";
                    if (File.Exists("TEXT_END.txt"))
                    {
                        TEXT_END = File.ReadAllLines("TEXT_END.txt");
                        result = string.Join(Environment.NewLine, TEXT_END);
                    }
                    else
                        result = "Cannot find file TEXT_END.txt";
                    #endregion
                    break;
                default:
                    #region
                    HtmlDocument doc = null;
                    string type = Request.QueryString["type"],
                        url = getURL(uri),
                        path = string.Empty,
                        folder = string.Empty,
                        file_name = string.Empty;
                    if (!string.IsNullOrEmpty(type))
                    {
                        /* dir, file */
                        if (string.IsNullOrEmpty(url))
                        {
                            content_type = "application/json; charset=utf-8";
                            path = Request.QueryString["path"];
                            folder = Request.QueryString["folder"];
                            file_name = Request.QueryString["file_name"];
                            result = processIO(type, path, folder, file_name);
                        }
                        else
                        {
                            /* crawler */
                            result = getHtml(url);
                            if (!string.IsNullOrEmpty(result))
                            {
                                content_type = "text/plain; charset=utf-8";
                                switch (type)
                                {
                                    case "html":
                                        content_type = "text/html; charset=utf-8";
                                        break;
                                    case "text":
                                        #region 
                                        doc = new HtmlDocument();
                                        doc.LoadHtml(result);

                                        StringWriter sw = new StringWriter();
                                        hasH1 = false;
                                        hasContentEnd = false;
                                        ConvertToText(doc.DocumentNode, sw);
                                        sw.Flush();
                                        result = sw.ToString();
                                        int p = result.IndexOf("{H1}");
                                        if (p > 0) { p += 4; result = result.Substring(p, result.Length - p).Trim(); }
                                        if (!hasContentEnd)
                                        {
                                            int pos_end = -1;
                                            for (int k = 0; k < TEXT_END.Length; k++)
                                            {
                                                pos_end = result.IndexOf(TEXT_END[k]);
                                                if (pos_end != -1)
                                                {
                                                    result = result.Substring(0, pos_end);
                                                    hasContentEnd = true;
                                                    break;
                                                }
                                            }
                                        }
                                        result = result.Replace(@"""", "”");

                                        result = string.Join(Environment.NewLine, result.Split(new char[] { '\r', '\n' })
                                            //.Select(x => x.Trim())
                                            .Where(x => x != string.Empty && !x.Contains("©"))
                                            .ToArray());

                                        #endregion
                                        break;
                                    case "image":
                                        break;
                                    case "link":
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                    }
                    else result = "Cannot find [type] in QueryString.";

                    #endregion
                    break;
            }
            byte[] bOutput = Encoding.UTF8.GetBytes(result);
            Response.ContentType = content_type;
            Response.ContentLength64 = bOutput.Length;
            OutputStream.Write(bOutput, 0, bOutput.Length);
            OutputStream.Close();
        }

        private string processIO(string type, string path, string folder, string file_name)
        {
            string result = "{}";
            switch (type)
            {

                case "dir_get":
                    #region 
                    bool isroot = false;
                    if (string.IsNullOrEmpty(path)) path = PATH_ROOT;
                    path = path.Replace('/', '\\');
                    if (string.IsNullOrEmpty(folder)) { isroot = true; } else { path = Path.Combine(path, folder); }
                    if (Directory.Exists(path))
                    {
                        var dirs = Directory.GetDirectories(path).Select(x => new
                        {
                            dir = Path.GetFileName(x),
                            sum_file = Directory.GetFiles(x, "*.txt").Length + Directory.GetDirectories(x).Length
                        }).ToArray();
                        if (isroot)
                        {
                            result = JsonConvert.SerializeObject(new
                            {
                                path = path.Replace('\\', '/'),
                                dirs = dirs
                            });
                        }
                        else
                        {
                            var files = Directory.GetFiles(path, "*.txt").Select(x => new
                            {
                                file = Path.GetFileName(x),
                                title = Regex.Replace(Regex.Replace(File.ReadAllLines(x)[0], "<.*?>", " "), "[ ]{2,}", " ").Trim()
                            }).ToArray();

                            result = JsonConvert.SerializeObject(new
                            {
                                path = path.Replace('\\', '/'),
                                dirs = dirs,
                                files = files
                            });
                        }
                    }

                    #endregion
                    break;
                case "dir_create":
                    #region

                    #endregion
                    break;
                case "dir_edit":
                    #region
                    #endregion
                    break;
                case "dir_remove":
                    #region

                    #endregion
                    break;
                case "file_load":
                    #region

                    #endregion
                    break;
                case "file_create":
                    #region

                    #endregion
                    break;
                case "file_edit":
                    #region

                    #endregion
                    break;
                case "file_remove":
                    #region

                    #endregion
                    break;
            }
            return result;
        }

        AutoResetEvent _autoEvent = new AutoResetEvent(false);
        private string getHtml(string url)
        {
            string result = string.Empty;
            try
            {
                //using (WebClient webClient = new WebClient())
                //{
                //    webClient.Encoding = System.Text.Encoding.UTF8;
                //    result = webClient.DownloadString(url);
                //}

                //HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url);
                //myRequest.Method = "GET";
                //WebResponse myResponse = myRequest.GetResponse();
                //StreamReader sr = new StreamReader(myResponse.GetResponseStream(), System.Text.Encoding.UTF8);
                //result = sr.ReadToEnd();
                //sr.Close();
                //myResponse.Close();

                //                var uri = new Uri(url);
                //                string req = @"GET " + uri.PathAndQuery + @" HTTP/1.1
                //User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/64.0.3282.186 Safari/537.36
                //Host: " + uri.Host + @"
                //Accept: */*
                //Accept-Encoding: gzip, deflate
                //Connection: Keep-Alive

                //";
                //                var requestBytes = Encoding.UTF8.GetBytes(req);
                //                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //                socket.Connect(uri.Host, 80);
                //                if (socket.Connected)
                //                {
                //                    socket.Send(requestBytes);
                //                    var responseBytes = new byte[socket.ReceiveBufferSize];
                //                    socket.Receive(responseBytes);
                //                    result = Encoding.UTF8.GetString(responseBytes);
                //                }
                //result = HttpUtility.HtmlDecode(result);
                //result = CleanHTMLFromScript(result);

                HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(new Uri(url));
                wr.BeginGetResponse(rs =>
                {
                    HttpWebResponse myResponse = (HttpWebResponse)wr.EndGetResponse(rs); //add a break point here

                    StreamReader sr = new StreamReader(myResponse.GetResponseStream(), System.Text.Encoding.UTF8);
                    result = sr.ReadToEnd();
                    sr.Close();
                    result = HttpUtility.HtmlDecode(result);
                    result = CleanHTMLFromScript(result);
                    _autoEvent.Set();
                }, wr);
            }
            catch (Exception ex)
            {
                _autoEvent.Set();
            }
            _autoEvent.WaitOne();
            return result;
        }

        private void ConvertToText(HtmlNode node, TextWriter outText)
        {
            if (hasContentEnd) return;

            string html;
            switch (node.NodeType)
            {
                case HtmlNodeType.Comment:
                    // don't output comments
                    break;

                case HtmlNodeType.Document:
                    ConvertContentTo(node, outText);
                    break;

                case HtmlNodeType.Text:
                    // script and style must not be output
                    string parentName = node.ParentNode.Name;
                    if ((parentName == "script") || (parentName == "style"))
                        break;

                    // get text
                    html = ((HtmlTextNode)node).Text;

                    // is it in fact a special closing node output as text?
                    if (HtmlNode.IsOverlappedClosingElement(html))
                        break;

                    // check the text is meaningful and not a bunch of whitespaces
                    if (html.Trim().Length > 0)
                    {
                        outText.Write(HtmlEntity.DeEntitize(html));
                    }
                    break;

                case HtmlNodeType.Element:
                    bool isHeading = false, isList = false, isCode = false;
                    switch (node.Name)
                    {
                        case "pre":
                            isCode = true;
                            outText.Write("\r\n^\r\n");
                            break;
                        case "ol":
                        case "ul":
                            isList = true;
                            outText.Write("\r\n⌐\r\n");
                            break;
                        case "li":
                            outText.Write("\r\n● ");
                            break;
                        case "div":
                            outText.Write("\r\n");
                            if (hasH1 && !hasContentEnd)
                            {
                                var css = node.getAttribute("class");
                                if (css != null && css.Length > 0)
                                {
                                    bool is_end_content = DIV_CLASS_END.Where(x => css.IndexOf(x) != -1).Count() > 0;
                                    if (is_end_content)
                                        hasContentEnd = true;
                                }
                            }
                            break;
                        case "p":
                            outText.Write("\r\n");
                            break;
                        case "h2":
                        case "h3":
                        case "h4":
                        case "h5":
                        case "h6":
                            isHeading = true;
                            outText.Write("\r\n■ ");
                            break;
                        case "h1":
                            hasH1 = true;
                            outText.Write("\r\n{H1}\r\n");
                            break;
                        case "img":
                            var src = node.getAttribute("src");
                            if (!string.IsNullOrEmpty(src))
                                outText.Write("\r\n{IMG-" + src + "-IMG}\r\n");

                            break;
                    }

                    if (node.HasChildNodes)
                    {
                        ConvertContentTo(node, outText);
                    }

                    if (isHeading) outText.Write("\r\n");
                    if (isList) outText.Write("\r\n┘\r\n");
                    if (isCode) outText.Write("\r\nⱽ\r\n");

                    break;
            }
        }


        private string CleanHTMLFromScript(string str)
        {
            Regex re = new Regex("<script.*?</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            str = re.Replace(str, string.Empty);
            re = new Regex("<script[^>]*>", RegexOptions.IgnoreCase);
            str = re.Replace(str, string.Empty);
            re = new Regex("<[a-z][^>]*on[a-z]+=\"?[^\"]*\"?[^>]*>", RegexOptions.IgnoreCase);
            str = re.Replace(str, string.Empty);
            re = new Regex("<a\\s+href\\s*=\\s*\"?\\s*javascript:[^\"]*\"[^>]*>", RegexOptions.IgnoreCase);
            str = re.Replace(str, string.Empty);
            return (str);
        }

        private void ConvertContentTo(HtmlNode node, TextWriter outText)
        {
            foreach (HtmlNode subnode in node.ChildNodes)
            {
                if (hasContentEnd) break;
                ConvertToText(subnode, outText);
            }
        }

    }
}
