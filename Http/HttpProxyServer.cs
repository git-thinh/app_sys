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
        static Dictionary<string, string> dicBookmark = new Dictionary<string, string>() { };

        public HttpProxyServer()
        {
            if (File.Exists("DIV_CLASS_END.txt"))
                DIV_CLASS_END = File.ReadAllLines("DIV_CLASS_END.txt");
            if (File.Exists("TEXT_END.txt"))
                TEXT_END = File.ReadAllLines("TEXT_END.txt");
            if (File.Exists("BOOKMARK.txt"))
            {
                dicBookmark = File.ReadAllLines("BOOKMARK.txt")
                    .Select(x => x.Trim())
                    .Where(x => x.Trim() != string.Empty)
                    .Select(x => x.Split('=')).Where(x => x.Length > 1)
                    .ToDictionary(x => x[0].Trim().ToLower(), x => x[1]);
            }
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

        static readonly byte[] sseBuffer = new byte[] { 100, 97, 116, 97, 58, 32, 10, 10 };
        protected override void ProcessRequest(System.Net.HttpListenerContext Context)
        {
            HttpListenerRequest Request = Context.Request;
            HttpListenerResponse Response = Context.Response;
            Response.AppendHeader("Access-Control-Allow-Origin", "*");
            Response.AppendHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            Response.AppendHeader("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept");


            // Do any of these result in META tags e.g. <META HTTP-EQUIV="Expire" CONTENT="-1">
            // HTTP Headers or both?            
            // Is this required for FireFox? Would be good to do this without magic strings.
            // Won't it overwrite the previous setting
            //Response.Headers.Add("Cache-Control", "no-cache, no-store");
            Response.AddHeader("Pragma", "no-cache");
            Response.AddHeader("Pragma", "no-store");
            Response.AddHeader("cache-control", "no-cache");

            string result = string.Empty, data = string.Empty,
                content_type = "text/html; charset=utf-8",
                uri = HttpUtility.UrlDecode(Request.RawUrl);
            Stream OutputStream = Response.OutputStream;
            byte[] bOutput;

            switch (Request.Url.LocalPath)
            {
                case "/favicon.ico":
                    break;
                case "/SERVER-SENT-EVENTS":
                    #region
                    content_type = "text /event-stream; charset=utf-8";
                    Response.ContentType = "text/event-stream";
                    while (true)
                    {
                        try
                        {
                            OutputStream.Write(sseBuffer, 0, 8);
                            OutputStream.Flush();
                        }
                        catch { }
                        System.Threading.Thread.Sleep(300);
                    }
                    OutputStream.Close();
                    #endregion
                    break;
                case "/BOOKMARK.txt":
                case "/bookmark.txt":
                    #region
                    content_type = "application/json; charset=utf-8";
                    if (File.Exists("BOOKMARK.txt"))
                    {
                        dicBookmark = File.ReadAllLines("BOOKMARK.txt")
                        .Select(x => x.Trim())
                        .Where(x => x.Trim() != string.Empty)
                        .Select(x => x.Split('=')).Where(x => x.Length > 1)
                        .ToDictionary(x => x[0].Trim().ToLower(), x => x[1]);
                    }
                    result = JsonConvert.SerializeObject(dicBookmark);
                    #endregion
                    break;
                case "/SELECTOR":
                    #region
                    content_type = "text/plain; charset=utf-8";

                    using (StreamReader stream = new StreamReader(Request.InputStream))
                    {
                        data = stream.ReadToEnd();
                        if (!string.IsNullOrEmpty(data)) data = HttpUtility.UrlDecode(data);
                        data = data.Trim().ToLower();

                        if (data.Contains("github.com"))
                        {
                            var ui = new Uri(data);
                            if (data.EndsWith(".md") || data.EndsWith(".txt"))
                            {
                                string ur = "https://raw.githubusercontent.com" + ui.LocalPath;
                            }
                            else
                            {
                                string ur = "https://raw.githubusercontent.com" + ui.LocalPath + "/master/README.md";

                            }
                        }
                        else
                        {
                            if (dicBookmark.ContainsKey(data))
                            {
                                result = dicBookmark[data];
                            }
                        }


                    }


                    #endregion
                    break;
                case "/BOOKMARK":
                    #region

                    string url3 = Request.QueryString["url"];
                    string title3 = Request.QueryString["title"];
                    if (!string.IsNullOrEmpty(url3))
                    {
                        url3 = url3.Replace('|', '/').Replace('~', ':');
                    }

                    using (StreamReader stream = new StreamReader(Request.InputStream))
                    {
                        data = stream.ReadToEnd();
                        if (!string.IsNullOrEmpty(data)) data = HttpUtility.UrlDecode(data);
                    }


                    #endregion
                    break;
                case "/msg.html":
                    #region
                    result = File.ReadAllText("msg.html");
                    #endregion
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
                        id = Request.QueryString["id"],
                        url = getURL(uri),
                        path = string.Empty,
                        folder = string.Empty,
                        folder_new = string.Empty,
                        file_name = string.Empty;
                    if (id == null) id = string.Empty;
                    if (!string.IsNullOrEmpty(type))
                    {
                        /* dir, file */
                        if (string.IsNullOrEmpty(url))
                        {
                            content_type = "application/json; charset=utf-8";
                            //path = Request.QueryString["path"];
                            //folder = Request.QueryString["folder"];
                            //folder_new = Request.QueryString["folder_new"];
                            //file_name = Request.QueryString["file_name"];

                            var dic = new Dictionary<string, string>() { };
                            if (uri.IndexOf('?') != -1)
                                dic = uri.Split('?')[1].Split('&').Select(x => x.Split('=')).ToDictionary(x => x[0].Trim().ToLower(), x => x[1].Trim());

                            if (dic.ContainsKey("path")) path = dic["path"];
                            if (dic.ContainsKey("folder")) folder = dic["folder"];
                            if (dic.ContainsKey("folder_new")) folder_new = dic["folder_new"];
                            if (dic.ContainsKey("file_name")) file_name = dic["file_name"];

                            using (StreamReader stream = new StreamReader(Request.InputStream))
                            {
                                data = stream.ReadToEnd();
                                if (!string.IsNullOrEmpty(data)) data = HttpUtility.UrlDecode(data);
                            }

                            result = processIO(id, type, path, folder, folder_new, file_name, data);
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

            bOutput = Encoding.UTF8.GetBytes(result);
            Response.ContentType = content_type;
            Response.ContentLength64 = bOutput.Length;
            OutputStream.Write(bOutput, 0, bOutput.Length);
            OutputStream.Close();
        }

        private string processIO(string id, string type, string path, string folder, string folder_new, string file_name, string data)
        {
            string result = "{}", path_new, path_file = string.Empty;
            bool isroot = false;
            if (string.IsNullOrEmpty(path)) path = PATH_ROOT;
            path = path.Replace('/', '\\');
            path_new = path;
            if (string.IsNullOrEmpty(folder))
            {
                isroot = true;
            }
            else
            {
                if (!type.Contains("_create"))
                    path = Path.Combine(path, folder);
            }

            if (!Directory.Exists(path)) return JsonConvert.SerializeObject(new { id = id, ok = false, msg = "Cannot find path: " + path });

            switch (type)
            {
                case "dir_get":
                    #region 

                    var dirs = Directory.GetDirectories(path).Select(x => new
                    {
                        dir = Path.GetFileName(x),
                        sum_file =
                        Directory.GetDirectories(x).Length +
                        "*.txt|*.html".Split('|').SelectMany(filter => System.IO.Directory.GetFiles(x, filter)).Count()
                        //Directory.GetFiles(x, "*.txt,*.html").Length + Directory.GetDirectories(x).Length
                    }).ToArray();
                    if (isroot)
                    {
                        result = JsonConvert.SerializeObject(new
                        {
                            id = id,
                            ok = true,
                            path = path.Replace('\\', '/'),
                            dirs = dirs,
                            files = new string[] { }
                        });
                    }
                    else
                    {
                        var files =
                            //Directory.GetFiles(path, "*.txt, *.html")
                            "*.txt|*.html".Split('|').SelectMany(filter => System.IO.Directory.GetFiles(path, filter))
                            .Select(x => new
                            {
                                file = Path.GetFileName(x),
                                //title = Regex.Replace(Regex.Replace(File.ReadAllLines(x)[0], "<.*?>", " "), "[ ]{2,}", " ").Trim()
                                title = File.ReadAllText(x).Split(new char[] { '\r', '\n' })[0]
                            }).ToArray();

                        result = JsonConvert.SerializeObject(new
                        {
                            id = id,
                            ok = true,
                            path = path.Replace('\\', '/'),
                            dirs = dirs,
                            files = files
                        });
                    }
                    #endregion
                    break;
                case "dir_create":
                    #region
                    if (string.IsNullOrEmpty(folder_new)) return JsonConvert.SerializeObject(new { id = id, ok = false, msg = "the field [folder_new] must be not empty" });
                    try
                    {
                        path = Path.Combine(path, folder_new);
                        Directory.CreateDirectory(path);
                        result = JsonConvert.SerializeObject(new { id = id, ok = true, path = path/*.Replace("\\", "/")*/, msg = "Create folder [" + folder_new + "] successfully." });
                    }
                    catch (Exception ex)
                    {
                        return JsonConvert.SerializeObject(new { id = id, ok = false, path = path/*.Replace("\\", "/")*/, msg = "Create folder [" + folder_new + "] fail: " + ex.Message });
                    }
                    #endregion
                    break;
                case "dir_edit":
                    #region
                    if (string.IsNullOrEmpty(folder_new)) return JsonConvert.SerializeObject(new { id = id, ok = false, msg = "the field [folder_new] must be not empty" });
                    try
                    {
                        path_new = Path.Combine(path_new, folder_new);
                        Directory.Move(path, path_new);
                        result = JsonConvert.SerializeObject(new { id = id, ok = true, path = path/*.Replace("\\", "/")*/, msg = "Rename folder [" + folder_new + "] successfully." });
                    }
                    catch (Exception ex)
                    {
                        return JsonConvert.SerializeObject(new { id = id, ok = false, path = path/*.Replace("\\", "/")*/, msg = "Rename folder [" + folder_new + "] fail: " + ex.Message });
                    }
                    #endregion
                    break;
                case "dir_remove":
                    #region
                    if (string.IsNullOrEmpty(folder)) return JsonConvert.SerializeObject(new { id = id, ok = false, msg = "The field [folder] must be not empty" });
                    try
                    {
                        Directory.Delete(path);
                        result = JsonConvert.SerializeObject(new { id = id, ok = true, path = path/*.Replace("\\", "/")*/, msg = "Remove folder [" + folder + "] successfully." });
                    }
                    catch (Exception ex)
                    {
                        return JsonConvert.SerializeObject(new { id = id, ok = false, path = path/*.Replace("\\", "/")*/, msg = "Remove folder [" + folder + "] fail: " + ex.Message });
                    }
                    #endregion
                    break;
                case "file_load":
                    #region

                    if (string.IsNullOrEmpty(file_name)) return JsonConvert.SerializeObject(new { id = id, ok = false, msg = "the field [file_name] must be not empty" });

                    path_file = Path.Combine(path, file_name);

                    if (!File.Exists(path_file)) return JsonConvert.SerializeObject(new { id = id, ok = false, msg = "Cannot find file: " + path_file });
                    string s = File.ReadAllText(path_file), ext = Path.GetExtension(path_file),
                                htm = string.Empty, text = string.Empty, word = string.Empty;
                    string[] line = new string[] { };
                    word[] aword = new word[] { };
                    switch (ext)
                    {
                        case ".html":
                            text = Regex.Replace(s, @"<[^>]*>", " ");
                            text = Regex.Replace(s, "[^0-9a-zA-Z]+", " ").ToLower();
                            text = Regex.Replace(text, "[ ]{2,}", " ").ToLower();
                            aword = text.Split(' ').Where(x => x.Length > 3)
                                .GroupBy(x => x)
                                .OrderByDescending(x => x.Count())
                                .Select(x => new word (){ w = x.Key, k = x.Count() })
                                .ToArray();
                            htm = string.Format("<{0} class=ext_html>{1}</{0}>", EL.TAG_ARTICLE, s);
                            result = JsonConvert.SerializeObject(new { id = id, ok = true, extension = ext, text = text, html = htm, word = aword });
                            break;
                        case ".txt":
                            line = s.Split(new char[] { '\n', '\r' }).Where(x => x != string.Empty).ToArray();
                            text = Regex.Replace(s, "[^0-9a-zA-Z]+", " ").ToLower();
                            text = Regex.Replace(text, "[ ]{2,}", " ").ToLower();
                            aword = text.Split(' ').Where(x => x.Length > 3)
                                .GroupBy(x => x)
                                .OrderByDescending(x => x.Count())
                                .Select(x => new word() { w = x.Key, k = x.Count() })
                                .ToArray();
                            htm = HtmlBuilder.renderFile(line);

                            result = JsonConvert.SerializeObject(new { id = id, ok = true, extension = ext, text = s, html = htm, word = aword });
                            break;
                    }

                    #endregion
                    break;
                case "file_create":
                    #region
                    if (string.IsNullOrEmpty(file_name)) file_name = DateTime.Now.ToString("yyMMddHHmmssfff") + ".txt";
                    try
                    {
                        path_file = Path.Combine(path, file_name);
                        if (File.Exists(path_file))
                        {
                            string r = "_" + DateTime.Now.ToString("yyMMddHHmmssfff") + ".";
                            file_name = string.Join(r, file_name.Split('.'));
                            path_file = Path.Combine(path, file_name);
                        }

                        if (data == null) data = string.Empty;
                        File.WriteAllText(path_file, data);

                        result = JsonConvert.SerializeObject(new { id = id, ok = true, path = path/*.Replace("\\", "/")*/, file_name = file_name, msg = "Create file [" + path_file + "] successfully." });
                    }
                    catch (Exception ex)
                    {
                        return JsonConvert.SerializeObject(new { id = id, ok = false, path = path/*.Replace("\\", "/")*/, file_name = file_name, msg = "Create file [" + path_file + "] fail: " + ex.Message });
                    }
                    #endregion
                    break;
                case "file_edit":
                    #region
                    if (string.IsNullOrEmpty(file_name)) return JsonConvert.SerializeObject(new { id = id, ok = false, msg = "the field [file_name] must be not empty" });
                    try
                    {
                        path_file = Path.Combine(path, file_name);
                        if (!File.Exists(path_file))
                            return JsonConvert.SerializeObject(new { id = id, ok = false, path = path/*.Replace("\\", "/")*/, file_name = file_name, msg = "Cannot find file [" + path_file + "]" });

                        if (data == null) data = string.Empty;
                        File.WriteAllText(path_file, data);

                        result = JsonConvert.SerializeObject(new { id = id, ok = true, path = path/*.Replace("\\", "/")*/, file_name = file_name, msg = "Update file [" + path_file + "] successfully." });
                    }
                    catch (Exception ex)
                    {
                        return JsonConvert.SerializeObject(new { id = id, ok = false, path = path/*.Replace("\\", "/")*/, file_name = file_name, msg = "Update file [" + path_file + "] fail: " + ex.Message });
                    }
                    #endregion
                    break;
                case "file_remove":
                    #region
                    if (string.IsNullOrEmpty(file_name)) return JsonConvert.SerializeObject(new { id = id, ok = false, msg = "the field [file_name] must be not empty" });
                    try
                    {
                        path_file = Path.Combine(path, file_name);
                        if (!File.Exists(path_file))
                            return JsonConvert.SerializeObject(new { id = id, ok = false, path = path/*.Replace("\\", "/")*/, file_name = file_name, msg = "Cannot find file [" + path_file + "]" });

                        File.Delete(path_file);

                        result = JsonConvert.SerializeObject(new { id = id, ok = true, path = path/*.Replace("\\", "/")*/, file_name = file_name, msg = "Remove file [" + path_file + "] successfully." });
                    }
                    catch (Exception ex)
                    {
                        return JsonConvert.SerializeObject(new { id = id, ok = false, path = path/*.Replace("\\", "/")*/, file_name = file_name, msg = "Remove file [" + path_file + "] fail: " + ex.Message });
                    }
                    #endregion
                    break;
                default:
                    result = JsonConvert.SerializeObject(new { id = id, ok = false, msg = "Cannot find action type is " + type });
                    break;
            }
            return result;
        }

        ////private AutoResetEvent _autoEvent = new AutoResetEvent(false);
        private string getHtml(string url)
        {
            string result = string.Empty;
            try
            {
                #region
                //using (WebClient webClient = new WebClient())
                //{
                //    webClient.Encoding = System.Text.Encoding.UTF8;
                //    result = webClient.DownloadString(url);
                //}

                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(url);
                myRequest.Method = "GET";
                WebResponse myResponse = myRequest.GetResponse();
                StreamReader sr = new StreamReader(myResponse.GetResponseStream(), System.Text.Encoding.UTF8);
                result = sr.ReadToEnd();
                sr.Close();
                myResponse.Close();

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
                #endregion

                //////HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(new Uri(url));
                //////wr.BeginGetResponse(rs =>
                //////{
                //////    HttpWebResponse myResponse = (HttpWebResponse)wr.EndGetResponse(rs); //add a break point here

                //////    StreamReader sr = new StreamReader(myResponse.GetResponseStream(), System.Text.Encoding.UTF8);
                //////    result = sr.ReadToEnd();
                //////    sr.Close();
                //////    result = HttpUtility.HtmlDecode(result);
                //////    result = CleanHTMLFromScript(result);
                //////    _autoEvent.Set();
                //////}, wr);
            }
            catch (Exception ex)
            {
                ////_autoEvent.Set();
            }
            //////_autoEvent.WaitOne();
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
