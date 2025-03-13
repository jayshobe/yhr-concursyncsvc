using log4net.Core;
using log4net.Layout;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;


namespace ConcurSyncLib.Extensions
{
    public class Log4NetLayout : PatternLayout
    {
        static readonly string ProcessSessionId = Guid.NewGuid().ToString();
        static readonly int ProcessId = Process.GetCurrentProcess().Id;
        static readonly string MachineName = Environment.MachineName;
        private readonly string? _productName;
        public Log4NetLayout() : base()
        {
            var configuration = new ConfigurationBuilder();

            _productName = "UpwardFeedbackSvc";

        }

        public override void ActivateOptions()
        {
            base.ActivateOptions();
        }

        public override void Format(TextWriter writer, LoggingEvent e)
        {
            StringBuilder sb = new StringBuilder();
            yardi objYardi = new yardi();
            url objUrl = new url();

            objYardi.AddContextInfo(ref objYardi);
            if (!String.IsNullOrEmpty(Convert.ToString(e.RenderedMessage)))
            {
                objYardi.log.message = Convert.ToString(e.RenderedMessage);
            }
            if (e.Level == Level.Error)
            {
                WriteExceptionDetails(e.ExceptionObject, ref sb, 3);
                objYardi.log.exception = Convert.ToString(sb);
            }

            objYardi.log.timestamp = e.TimeStamp.ToUniversalTime();
            objYardi.log.level = e.Level.DisplayName;

            objYardi.log.source = _productName;

            logentry lg = new logentry("");
            lg.yardi = objYardi;
            lg.url = objUrl;
            writer.Write(JsonConvert.SerializeObject(lg, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }) + Environment.NewLine);
        }

        public void WriteExceptionDetails(Exception exception, ref StringBuilder builderToFill, int level)
        {
            if (exception == null) return;
            var tempbuilderToFill = builderToFill;
            string indent = new string(' ', level);
            if (level > 0)
            {
                tempbuilderToFill.AppendLine(indent + "=== INNER EXCEPTION ===");
            }

            Action<string> append = prop =>
            {
                var propInfo = exception?.GetType().GetProperty(prop);
                var val = propInfo?.GetValue(exception);
                if (val is object)
                {
                    tempbuilderToFill.AppendFormat("{0}{1}: {2}{3}", indent, prop, val.ToString(), Environment.NewLine);
                }
            };
            append("Message");
            append("HResult");
            append("HelpLink");
            append("Source");
            append("StackTrace");
            append("TargetSite");
            foreach (DictionaryEntry de in exception?.Data)
                tempbuilderToFill.AppendFormat("{0} {1} = {2}{3}", indent, de.Key, de.Value, Environment.NewLine);
            if (exception?.InnerException is object)
            {
                WriteExceptionDetails(exception?.InnerException, ref tempbuilderToFill, +(+level));
            }
            else
            {
                builderToFill = tempbuilderToFill;
            }
        }

    }

    public partial class logentry
    {
        public yardi yardi { get; set; }
        public url url { get; set; }

        public http http { get; set; }

        public cloudflare cloudflare { get; set; }
        public logentry(string type) : base()
        {
            this.yardi = new yardi(type);
            this.http = new http();
            this.cloudflare = new cloudflare();
        }
    }

    public partial class cloudflare
    {
        public string ray_id { get; set; }
        public cloudflare() : base()
        {
            ray_id = "";
        }
    }

    public partial class url
    {
        public string username { get; set; }
        public string original { get; set; }

        public url() : base()
        {
            username = "";
            original = "";
        }

    }
    public partial class http
    {
        public request request { get; set; }
        public http() : base()
        {
            request = new request();
        }
    }
    public partial class request
    {
        public string xrequestid { get; set; }
        public request() : base()
        {
            xrequestid = "";
        }
    }

    public partial class yardi
    {
        public yardi(string type) : base()
        {
            log = new log();
            client = new client();
        }

        public yardi(string type, string Xrequestid) : this(type)
        {
            //xrequestid = Xrequestid;
            log = new log();
            client = new client();

        }

        public yardi() : base()
        {
            log = new log();
            log.timestamp = DateTime.UtcNow;
            client = new client();
            client.database.name = "";
            client.role.name = "";
        }


        public log log { get; set; }

        public client client { get; set; }

        //[JsonProperty(PropertyName = "http.request.xrequestid")]

        //public string xrequestid { get; set; }


        public void AddContextInfo(ref yardi le)
        {
            le.log.timestamp = DateTime.UtcNow;

            //if (String.IsNullOrEmpty(le.xrequestid))
            //{
            //    le.xrequestid = Utils.RequestUtil.GetXRequestId();
            //}
        }

    }

    public partial class LogContent
    {
        public string message { get; set; }

        public LogContent(string _message) : base()
        {
            message = _message;
        }
    }

    public partial class LogResponseContent : LogRequestContent
    {
        public int? ResponseStatusCode { get; set; }

        public LogResponseContent(string message, int statuscode, string URI, string Content, string Headers, string RequestId) : base(message, URI, Content, Headers, RequestId)
        {
            ResponseStatusCode = statuscode;
        }
    }

    public partial class LogRequestContent : LogContent
    {
        public string uri { get; set; }
        public string content { get; set; }
        public string headers { get; set; }
        public string requestid { get; set; }

        public LogRequestContent(string message, string _uri, string _content, string _headers, string _requestId) : base(message)
        {
            uri = _uri;
            content = _content;
            headers = _headers;
            requestid = _requestId;
        }
    }

    public partial class client
    {
        public client() : base()
        {
            database = new database();
            role = new role();
        }

        public string pin { get; set; }
        public string name { get; set; }
        public string username { get; set; }
        public database database { get; set; }
        public role role { get; set; }
    }

    public partial class database
    {
        public string name { get; set; }
        public string server { get; set; }
    }

    public partial class role
    {
        public string name { get; set; }
    }



    public partial class log
    {
        public log() : base()
        {
        }
        public DateTime timestamp { get; set; }
        public string level { get; set; }
        public string message { get; set; }
        public string exception { get; set; }

        public string source { get; set; }

    }
}
