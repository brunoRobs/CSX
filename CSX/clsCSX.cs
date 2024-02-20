using Log;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using ClearTimer = System.Timers.Timer;

namespace CSX
{
    public class clsCSX : ICSX
    {
        HttpListener Server = new HttpListener();

        clsLog log = new clsLog();

        HttpListenerContext Request;

        Dictionary<string, JsonObject> Container = new Dictionary<string, JsonObject>();

        Thread Clear;

        ClearTimer Timer;

        public int ClearTime, LogSize, Time = 0;

        public string Path;

        string IP, Port;

        bool Ready;

        uint Seq = 0;

        public void Start(string IPAdress, string Port)
        {
            if (Server.Prefixes.Count == 0)
            {
                Server.Prefixes.Add($"http://{IP = IPAdress}:{this.Port = Port}/");
            }

            log.Init(Path, LogSize);

            Server.Start();

            log.Log($"{DateTime.Now} - Servidor {IP}:{this.Port} iniciado\n\n");

            Clear = new Thread(StartClear)
            {
                Name = "ClearThread",
                IsBackground = true
            };

            Clear.Start(); 

            while (true)
            {
                try
                {
                    Ready = true;

                    Request = Server.GetContext();

                    log.Log($"{DateTime.Now} - Requisição recebida: {Request.Request.RawUrl.Substring(1)}\n\n");

                    if (Request != null)
                    {
                        Ready = false;

                        AnswerRequest(Request.Request.RawUrl.Substring(1));

                        Request = null;
                    }
                }
                catch (System.Net.HttpListenerException)
                {
                    log.Log($"{DateTime.Now} - Servidor {IP}:{this.Port} encerrado\n\n");

                    if (Request == null) break;
                }
            }
        }

        public void Stop()
        {
            Server.Stop();

            Ready = true;
        }

        public void AnswerRequest(string Method)
        {
            HttpListenerResponse Response = Request.Response;

            string json;
            
            if (Request.Request.HttpMethod.Equals(HttpMethod.Get.ToString()) && Method.Equals("GETDATA", StringComparison.OrdinalIgnoreCase))
            {
                using (Stream body = Request.Request.InputStream)
                {
                    using (StreamReader reader = new StreamReader(body, Request.Request.ContentEncoding))
                    {
                        string requestBody = reader.ReadToEnd();

                        json = requestBody.Equals("") ? Request.Request.Headers["CSXKEY"] : requestBody;
                    }
                }

                JsonObject request;

                try
                {
                    request = JsonSerializer.Deserialize<JsonObject>(json);
                }
                catch
                {
                    Response.ContentType = "text/plain";

                    Response.StatusCode = (int)HttpStatusCode.UnsupportedMediaType;

                    byte[] buffer = Encoding.UTF8.GetBytes("Informação de corpo inválida");

                    Response.ContentLength64 = buffer.Length;

                    Response.OutputStream.Write(buffer, 0, buffer.Length);

                    Response.OutputStream.Close();

                    Response.Close();

                    return;
                }

                if (request.ContainsKey("CSXKEY"))
                {
                    if (Container.ContainsKey(request["CSXKEY"].GetValue<string>()))
                    {
                        JsonObject response = Get(request);

                        Response.StatusCode = (int)HttpStatusCode.OK;

                        Response.ContentType = "application/json";

                        byte[] buffer = Encoding.UTF8.GetBytes(response.ToJsonString());

                        Response.ContentLength64 = buffer.Length;

                        Response.OutputStream.Write(buffer, 0, buffer.Length);

                        Response.OutputStream.Close();

                        log.Log($"{DateTime.Now} - Requisição respondida: {response.ToJsonString()}\n\n");
                    }
                    else
                    {
                        Response.StatusCode = (int)HttpStatusCode.NotFound;

                        Response.ContentType = "text/plain";

                        byte[] buffer = Encoding.UTF8.GetBytes("Chave inexistente");

                        Response.ContentLength64 = buffer.Length;

                        Response.OutputStream.Write(buffer, 0, buffer.Length);

                        Response.OutputStream.Close();

                        log.Log($"{DateTime.Now} - {Response.StatusCode} - {Response.StatusDescription}\n\n");
                    }
                }
                else
                {
                    Response.StatusCode = (int)HttpStatusCode.Forbidden;

                    Response.ContentType = "text/plain";

                    byte[] buffer = Encoding.UTF8.GetBytes("O Json fornecido não contém o campo CSXKEY");

                    Response.ContentLength64 = buffer.Length;

                    Response.OutputStream.Write(buffer, 0, buffer.Length);

                    Response.OutputStream.Close();

                    log.Log($"{DateTime.Now} - {Response.StatusCode} - {Response.StatusDescription}\n\n");
                }

                Response.Close();
            }
            else if (Request.Request.HttpMethod.Equals(HttpMethod.Get.ToString()) && Method.Equals("GETDATABYUCID", StringComparison.OrdinalIgnoreCase))
            {
                using (Stream body = Request.Request.InputStream)
                {
                    using (StreamReader reader = new StreamReader(body, Request.Request.ContentEncoding))
                    {
                        string requestBody = reader.ReadToEnd();

                        json = requestBody.Equals("") ? Request.Request.Headers["UCID"] : requestBody;
                    }
                }

                JsonObject request;

                try
                {
                    request = JsonSerializer.Deserialize<JsonObject>(json);
                }
                catch
                {
                    Response.ContentType = "text/plain";

                    Response.StatusCode = (int)HttpStatusCode.UnsupportedMediaType;

                    byte[] buffer = Encoding.UTF8.GetBytes("Informação de corpo inválida");

                    Response.ContentLength64 = buffer.Length;

                    Response.OutputStream.Write(buffer, 0, buffer.Length);

                    Response.OutputStream.Close();

                    Response.Close();

                    return;
                }

                if (request.ContainsKey("UCID"))
                {
                    JsonObject response = GetByUCID(request);

                    if(response != null)
                    {
                        if (response.Equals(request))
                        {
                            Response.StatusCode = (int)HttpStatusCode.Ambiguous;

                            Response.ContentType = "text/plain";

                            byte[] buffer = Encoding.UTF8.GetBytes("O UCID fornecido não é único");

                            Response.ContentLength64 = buffer.Length;

                            Response.OutputStream.Write(buffer, 0, buffer.Length);

                            Response.OutputStream.Close();

                            log.Log($"{DateTime.Now} - {Response.StatusCode} - {Response.StatusDescription}\n\n");
                        }
                        else
                        {
                            Response.StatusCode = (int)HttpStatusCode.OK;

                            Response.ContentType = "application/json";

                            byte[] buffer = Encoding.UTF8.GetBytes(response.ToJsonString());

                            Response.ContentLength64 = buffer.Length;

                            Response.OutputStream.Write(buffer, 0, buffer.Length);

                            Response.OutputStream.Close();

                            log.Log($"{DateTime.Now} - Requisição respondida: {response.ToJsonString()}\n\n");
                        }
                    }
                    else
                    {
                        Response.StatusCode = (int)HttpStatusCode.NotFound;

                        Response.ContentType = "text/plain";

                        byte[] buffer = Encoding.UTF8.GetBytes("UCID inexistente");

                        Response.ContentLength64 = buffer.Length;

                        Response.OutputStream.Write(buffer, 0, buffer.Length);

                        Response.OutputStream.Close();

                        log.Log($"{DateTime.Now} - {Response.StatusCode} - {Response.StatusDescription}\n\n");
                    }
                }
                else
                {
                    Response.StatusCode = (int)HttpStatusCode.Forbidden;

                    Response.ContentType = "text/plain";

                    byte[] buffer = Encoding.UTF8.GetBytes("O Json fornecido não contém o campo UCID");

                    Response.ContentLength64 = buffer.Length;

                    Response.OutputStream.Write(buffer, 0, buffer.Length);

                    Response.OutputStream.Close();

                    log.Log($"{DateTime.Now} - {Response.StatusCode} - {Response.StatusDescription}\n\n");
                }

                Response.Close();
            }
            else if (Request.Request.HttpMethod.Equals(HttpMethod.Post.ToString()) && Method.Equals("INSERTDATA", StringComparison.OrdinalIgnoreCase))
            {
                using (Stream stream = Request.Request.InputStream)
                {
                    StreamReader reader = new StreamReader(stream);
                    {
                        json = reader.ReadToEnd();
                    }
                }

                JsonObject request;

                try
                {
                    request = JsonSerializer.Deserialize<JsonObject>(json);
                }
                catch
                {
                    Response.ContentType = "text/plain";

                    Response.StatusCode = (int)HttpStatusCode.UnsupportedMediaType;

                    byte[] buffer = Encoding.UTF8.GetBytes("Informação de corpo inválida");

                    Response.ContentLength64 = buffer.Length;

                    Response.OutputStream.Write(buffer, 0, buffer.Length);

                    Response.OutputStream.Close();

                    Response.Close();

                    return;
                }

                Response.ContentType = "application/json";

                string CSXKEY = Post(request);

                if (!CSXKEY.Equals(""))
                {
                    Response.StatusCode = (int)HttpStatusCode.OK;

                    JsonObject response = new JsonObject
                    {
                        { "CSXKEY", CSXKEY }
                    };

                    byte[] buffer = Encoding.UTF8.GetBytes(response.ToJsonString());

                    Response.ContentLength64 = buffer.Length;

                    Response.OutputStream.Write(buffer, 0, buffer.Length);

                    Response.OutputStream.Close();

                    log.Log($"{DateTime.Now} - Requisição respondida: {response.ToJsonString()}\n\n");
                }
                else
                {
                    Response.StatusCode = (int)HttpStatusCode.Conflict;

                    log.Log($"{DateTime.Now} - {Response.StatusCode} - {Response.StatusDescription}\n\n");
                }

                Response.Close();
            }
            else if (Request.Request.HttpMethod.Equals(HttpMethod.Put.ToString()) && Method.Equals("UPDATEDATA", StringComparison.OrdinalIgnoreCase))
            {
                using (Stream stream = Request.Request.InputStream)
                {
                    StreamReader reader = new StreamReader(stream);
                    {
                        json = reader.ReadToEnd();
                    }
                }

                JsonObject request;

                try
                {
                    request = JsonSerializer.Deserialize<JsonObject>(json);
                }
                catch
                {
                    Response.ContentType = "text/plain";

                    Response.StatusCode = (int)HttpStatusCode.UnsupportedMediaType;

                    byte[] buffer = Encoding.UTF8.GetBytes("Informação de corpo inválida");

                    Response.ContentLength64 = buffer.Length;

                    Response.OutputStream.Write(buffer, 0, buffer.Length);

                    Response.OutputStream.Close();

                    Response.Close();

                    return;
                }

                if (request.ContainsKey("CSXKEY"))
                {
                    if (Container.ContainsKey(request["CSXKEY"].GetValue<string>()))
                    {
                        Response.StatusCode = (int)HttpStatusCode.OK;

                        Put(request["CSXKEY"].GetValue<string>(), request);
                    }
                    else
                    {
                        Response.StatusCode = (int)HttpStatusCode.NotFound;
                    }
                }
                else
                {
                    Response.StatusCode = (int)HttpStatusCode.Forbidden;

                    Response.ContentType = "text/plain";

                    byte[] buffer = Encoding.UTF8.GetBytes("O Json fornecido não contém o campo CSXKEY");

                    Response.ContentLength64 = buffer.Length;

                    Response.OutputStream.Write(buffer, 0, buffer.Length);

                    Response.OutputStream.Close();
                }

                Response.Close();

                log.Log($"{DateTime.Now} - Requisição respondida: {Response.StatusCode} - {Response.StatusDescription}\n\n");
            }
            else
            {
                Response.ContentType = "text/plain";

                Response.StatusCode = (int)HttpStatusCode.NotImplemented;

                byte[] buffer = Encoding.UTF8.GetBytes("Request inválido");

                Response.ContentLength64 = buffer.Length;

                Response.OutputStream.Write(buffer, 0, buffer.Length);

                Response.OutputStream.Close();

                Response.Close();

                log.Log($"{DateTime.Now} - {Response.StatusCode} - {Response.StatusDescription}\n\n");
            }
        }

        public JsonObject Get(JsonObject json)
        {
            string CSXKEY = json["CSXKEY"].GetValue<string>();

            return Container[CSXKEY];
        }

        public JsonObject GetByUCID(JsonObject json)
        {
            string CSXKEY = "";

            int c = 0;

            string UCID = json["UCID"].GetValue<string>();

            foreach(var property in Container)
            {
                if (property.Value["UCID"].GetValue<string>().Equals(UCID))
                {
                    CSXKEY = property.Key;
                    c++;
                }
            }

            if (c > 1) return json;

            else if (c == 1) return Container[CSXKEY];

            else return null;
        }

        public string Post(JsonObject json)
        {
            string Date = $"{DateTime.Now.Year}-{DateTime.Now.Month.ToString("D2")}-{DateTime.Now.Day.ToString("D2")}";
            
            string Time = $"{DateTime.Now.Hour.ToString("D2")}:{DateTime.Now.Minute.ToString("D2")}:{DateTime.Now.Second.ToString("D2")}.{DateTime.Now.Millisecond.ToString("D3")}";
            
            string CSXKEY;

            if (json.ContainsKey("CSXKEY"))
            {
                foreach (var element in Container)
                {
                    if (element.Key.Equals(json["CSXKEY"]))
                    {
                        return "";
                    }
                }

                CSXKEY = json["CSXKEY"].GetValue<string>();

                if (!json.ContainsKey("CREATIONTIMESTAMP"))
                {
                    json.Add("CREATIONTIMESTAMP", $"{Date} {Time}");

                    json.Add("LASTUPDATETIMESTAMP", $"{Date} {Time}");
                }

                if (!json.ContainsKey("UCID"))
                {
                    json.Add("UCID", "****MISSING*UCID****");
                }

                Container.Add(CSXKEY, json);

                return CSXKEY;
            }
            else
            {
                JsonObject newKey = new JsonObject();

                if (!json.ContainsKey("CREATIONTIMESTAMP"))
                {
                    json.Add("CREATIONTIMESTAMP", $"{Date} {Time}");
                }

                if (!json.ContainsKey("LASTUPDATETIMESTAMP"))
                {
                    json.Add("LASTUPDATETIMESTAMP", $"{Date} {Time}");
                }

                newKey.Add("Date", Date);

                newKey.Add("Time", Time);

                if (!json.ContainsKey("UCID"))
                {
                    json.Add("UCID", "****MISSING*UCID****");

                    newKey.Add("UCID", json["UCID"].GetValue<string>());
                }
                else
                {
                    newKey.Add("UCID", json["UCID"].GetValue<string>());
                }

                newKey.Add("IP", this.IP);

                newKey.Add("Port", this.Port);

                if (Seq == 32767)
                {
                    newKey.Add("Sequential", Seq.ToString());

                    Seq = 0;
                }
                else
                {
                    newKey.Add("Sequential", Seq.ToString());

                    Seq++;
                }

                CSXKEY = GenerateCSXKEY(newKey);

                json.Add("CSXKEY", CSXKEY);

                Container.Add(CSXKEY, json);

                return CSXKEY;
            }
        }

        public void Put(string CSXKEY, JsonObject json)
        {
            JsonObject data = Container[CSXKEY];

            data["UPDATETIMESTAMP"] = $"{DateTime.Now.Year}-{DateTime.Now.Month.ToString("D2")}-{DateTime.Now.Day.ToString("D2")} " +
                $"{DateTime.Now.Hour.ToString("D2")}:{DateTime.Now.Minute.ToString("D2")}:{DateTime.Now.Second.ToString("D2")}.{DateTime.Now.Millisecond.ToString("D3")}";

            foreach (var property in json)
            {
                if(property.Key.Equals("CREATIONTIMESTAMP") || property.Key.Equals("UPDATETIMESTAMP"))
                {
                    continue;
                }
                if (data.ContainsKey(property.Key))
                {
                    data[property.Key] = property.Value.GetValue<string>();
                }
                else
                {
                    data.Add(property.Key, property.Value.GetValue<string>());
                }
            }
        }

        private void StartClear()
        {
            Timer = new ClearTimer(60000);

            Timer.Elapsed += ClearData;

            Timer.Start();
        }

        public void ClearData(object sender, System.Timers.ElapsedEventArgs e)
        {
            Time++;
            if ((Time % ClearTime) == 0)
            {
                if (Container.Count > 0 && Ready)
                {
                    bool Run = true;

                    do
                    {
                        foreach (var element in Container)
                        {
                            if (Index(element) == Container.Count - 1)
                            {
                                Run = false;
                            }

                            int comparator = (Int32.Parse(element.Value["CREATIONTIMESTAMP"].GetValue<string>().Substring(14, 2)) + ClearTime) % 60;

                            if (comparator == DateTime.Now.Minute)
                            {
                                log.Log($"{DateTime.Now} - Informação removida: {element.Value.ToJsonString()}\n\n");

                                Container.Remove(element.Key);

                                break;
                            }
                        }
                    } while (Run);
                }
            }
        }

        private int Index(KeyValuePair<string, JsonObject> Element, int i = 0)
        {
            foreach(var element in Container)
            {
                if (element.Equals(Element)) break;
                else i++;
            }

            return i;
        }

        private string GenerateCSXKEY(JsonObject json)
        {
            string CSXKEY = json["Date"].GetValue<string>().Replace("-", "");

            CSXKEY += json["Time"].GetValue<string>().Replace(":", "").Replace(".", "");

            string[] ipArray = json["IP"].GetValue<string>().Split('.');

            foreach (string number in ipArray)
            {
                CSXKEY += Int32.Parse(number).ToString("D3");
            }

            CSXKEY += Int32.Parse(json["Port"].GetValue<string>()).ToString("D5");

            CSXKEY += json["UCID"].GetValue<string>();

            CSXKEY += UInt32.Parse(json["Sequential"].GetValue<string>()).ToString("D5");

            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(CSXKEY));
        }
    }
}