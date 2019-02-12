using Chat.Web.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static Chat.Web.Models.Enums;
using HttpMethod = Chat.Web.Models.Enums.HttpMethod;

namespace Chat.Web.Helpers
{
    public class Apis
    {
        public static Response GestStock()
        {
            Response output = new Response();

            try
            {
                string _url = "https://stooq.com/q/l/?s=aapl.us&f=sd2t2ohlcv&h&e=csv";

                output = ApisRequest(_url, HttpMethod.GET);
            }
            catch (Exception ex)
            {
                output.Message = ex.Message;
                output.Trace = ex.StackTrace;
                output.Data = ex.InnerException;
            }

            return output;
        }

        public static Response ApisRequest(string _url, HttpMethod _method, object _data = null)
        {
            Response response = new Response();

            string json = string.Empty;

            try
            {
                WebRequest request = WebRequest.Create(_url);

                request.Method = _method.ToString();
                request.ContentType = "application/json";

                if (_data != null)
                {
                    Stream dataStream = request.GetRequestStream();

                    using (StreamWriter requestWriter = new StreamWriter(dataStream))
                    {
                        json = JsonConvert.SerializeObject(_data);

                        requestWriter.Write(json);
                    }
                }

                using (WebResponse wr = request.GetResponse())
                {
                    using (Stream receiveStream = wr.GetResponseStream())
                    {
                        using (StreamReader reader = new StreamReader(receiveStream, Encoding.UTF8))
                        {
                            response.Message = reader.ReadToEnd();

                            response.Status = true;
                            response.Type = ResponseType.success;

                            return response;
                        }
                    }
                }


            }
            catch (WebException we)
            {
                response.Message = $"{we.Message}. Json: {json}";
                response.Trace = we.StackTrace;

            }
            catch (Exception e)
            {
                response.Message = e.Message;
                response.Trace = e.StackTrace;
            }

            return response;
        }

    }
}