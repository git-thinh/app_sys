using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using WebSocket4Net;
using Newtonsoft.Json;

namespace app_sys
{
    public class Chrome
    {
        public static string getDataTabOpening(string url)
        {
            string result = string.Empty;

            var chrome = new ChromeAPI("http://localhost:9222");

            var sessions = chrome.GetAvailableSessions();
            if (sessions.Count == 0) return result;
            //////int index = -1;
            //////for (int i = 0; i < sessions.Count; i++)
            //////{
            //////    if (sessions[i].url.StartsWith("http"))
            //////    {
            //////        index = i;
            //////        break;
            //////    }
            //////}
            //////if (index == -1) return result;

            var ses = sessions.Where(x => x.url == url).Take(1).SingleOrDefault();
            if (ses == null) return result;

            var uri = new Uri(ses.url);

            // Will drive first tab session 
            chrome.SetActiveSession(ses.webSocketDebuggerUrl);

            //string result = chrome.Eval("var s = ''; Array.from(document.querySelectorAll('" + selector + "')).forEach(function (it) { s += it.innerHTML; s += '<hr>' }); s;");
            string js = string.Empty;
            js = File.ReadAllText("bookmark/-/" + uri.Host + ".js");
            string s = chrome.Eval(js);
            chrome_data dt = JsonConvert.DeserializeObject<chrome_data>(s);
            result = dt.result.result.value;

            //Console.WriteLine("Available debugging sessions");
            //foreach (var s in sessions)
            //    Console.WriteLine(s.url);
            //if (sessions.Count == 0)
            //    throw new Exception("All debugging sessions are taken.");
            //////// Will drive first tab session
            //////var sessionWSEndpoint = 
            //////    sessions[0].webSocketDebuggerUrl;
            //////chrome.SetActiveSession(sessionWSEndpoint);
            //////chrome.NavigateTo("http://www.google.com");
            //////var result = chrome.Eval("document.getElementById('lst-ib').value='Hello World'");
            //////result = chrome.Eval("document.forms[0].submit()");
            //Console.ReadLine();

            if (result == null) result = string.Empty;
            return result;
        }
    }

    public class ChromeAPI
    {
        const string JsonPostfix = "/json";

        string remoteDebuggingUri;
        string sessionWSEndpoint;

        public ChromeAPI(string remoteDebuggingUri)
        {
            this.remoteDebuggingUri = remoteDebuggingUri;
        }

        public TRes SendRequest<TRes>()
        {
            string s = string.Empty;
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(remoteDebuggingUri + JsonPostfix);
            using (var resp = req.GetResponse())
            {
                var respStream = resp.GetResponseStream();

                StreamReader sr = new StreamReader(respStream);
                s = sr.ReadToEnd();
                //resp.Dispose();
            }
            return Deserialise<TRes>(s);
        }

        public List<RemoteSessionsResponse> GetAvailableSessions()
        {
            var res = this.SendRequest<List<RemoteSessionsResponse>>();
            return (from r in res
                    where r.devtoolsFrontendUrl != null
                    select r).ToList();
        }

        public string NavigateTo(string uri)
        {
            // Page.navigate is working from M18
            //var json = @"{""method"":""Page.navigate"",""params"":{""url"":""http://www.seznam.cz""},""id"":1}";

            // Instead of Page.navigate, we can use document.location
            var json = @"{""method"":""Runtime.evaluate"",""params"":{""expression"":""document.location='" + uri + @"'"",""objectGroup"":""console"",""includeCommandLineAPI"":true,""doNotPauseOnExceptions"":false,""returnByValue"":false},""id"":1}";
            return this.SendCommand(json);
        }

        public string GetElementsByTagName(string tagName)
        {
            // Page.navigate is working from M18
            //var json = @"{""method"":""Page.navigate"",""params"":{""url"":""http://www.seznam.cz""},""id"":1}";

            // Instead of Page.navigate, we can use document.location
            var json = @"{""method"":""Runtime.evaluate"",""params"":{""expression"":""document.getElementsByTagName('" + tagName + @"')"",""objectGroup"":""console"",""includeCommandLineAPI"":true,""doNotPauseOnExceptions"":false,""returnByValue"":false},""id"":1}";
            return this.SendCommand(json);
        }


        public string Eval(string cmd)
        {
            //var json = @"{""method"":""Runtime.evaluate"",""params"":{""expression"":""" + cmd + @""",""objectGroup"":""console"",""includeCommandLineAPI"":true,""doNotPauseOnExceptions"":false,""returnByValue"":false},""id"":1}";
            string json = JsonConvert.SerializeObject(new chrome_cmd() { _params = new chrome_params() { expression = cmd } });
            return this.SendCommand(json);
        }

        public string SendCommand(string cmd)
        {
            WebSocket j = new WebSocket(this.sessionWSEndpoint);
            ManualResetEvent waitEvent = new ManualResetEvent(false);
            ManualResetEvent closedEvent = new ManualResetEvent(false);
            string message = "";
            byte[] data;

            Exception exc = null;
            j.Opened += delegate (System.Object o, EventArgs e)
            {
                j.Send(cmd);
            };

            j.MessageReceived += delegate (System.Object o, MessageReceivedEventArgs e)
            {
                message = e.Message;
                waitEvent.Set();
            };

            j.Error += delegate (System.Object o, SuperSocket.ClientEngine.ErrorEventArgs e)
            {
                exc = e.Exception;
                waitEvent.Set();
            };

            j.Closed += delegate (System.Object o, EventArgs e)
            {
                closedEvent.Set();
            };

            j.DataReceived += delegate (System.Object o, DataReceivedEventArgs e)
            {
                data = e.Data;
                waitEvent.Set();
            };

            j.Open();

            waitEvent.WaitOne();
            if (j.State == WebSocket4Net.WebSocketState.Open)
            {
                j.Close();
                closedEvent.WaitOne();
            }
            if (exc != null)
                throw exc;

            return message;
        }

        private T Deserialise<T>(string json)

        {

            T obj = Activator.CreateInstance<T>();
            using (MemoryStream ms = new MemoryStream(Encoding.Unicode.GetBytes(json)))
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(obj.GetType());
                obj = (T)serializer.ReadObject(ms);
                return obj;
            }
        }

        private T Deserialise<T>(Stream json)
        {
            T obj = Activator.CreateInstance<T>();
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(obj.GetType());
            obj = (T)serializer.ReadObject(json);
            return obj;
        }

        public void SetActiveSession(string sessionWSEndpoint)
        {
            // Sometimes binding to localhost might resolve wrong AddressFamily, force IPv4
            this.sessionWSEndpoint = sessionWSEndpoint.Replace("ws://localhost", "ws://127.0.0.1");

        }
    }

    //{""method"":""Runtime.evaluate"",""params"":{""expression"":""" + cmd + @""",""objectGroup"":""console"",""includeCommandLineAPI"":true,""doNotPauseOnExceptions"":false,""returnByValue"":false},""id"":1}
    public class chrome_cmd
    {
        public string method = "Runtime.evaluate";
        [JsonProperty(PropertyName = "params")]
        public chrome_params _params { set; get; }
        public int id = 1;
    }

    public class chrome_params
    {
        public string expression { set; get; }
        public string objectGroup = "console";
        public bool includeCommandLineAPI = true;
        public bool doNotPauseOnExceptions = false;
        public bool returnByValue = false;
    }
}
