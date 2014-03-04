using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

namespace Fos.Logging
{
    using OwinHandler = Func<IDictionary<string, object>, Task>;

    internal class StatsPageMiddleware
    {
        private StatsLogger Logger;

        private string HtmlEncode(string text)
        {
            //TODO: THIS MUST BE VERY SECURE! IS IT SECURE ENOUGH?
            if (text == null)
                return string.Empty;

            return text.Replace("<", "&lt;").Replace(">", "&gt;");
        }

        private string Head()
        {
            return "<style type=\"text/css\">tr.even_row { background-color: #E3E3E3; } tr.row_data { display: none; } td.see_details { cursor: pointer; }</style>";
        }

        private string EndOfBody()
        {
            return @"<script type=""text/javascript"">
                        function showDetailsClick(e) {
                            var evt = e || window.event;
                            var el = evt.target || evt.srcElement;
                            if (el.nodeName == 'TD' && el.getAttribute('class') == 'see_details') {
                                var nextRow = el.parentElement.nextElementSibling;
                                if (!nextRow.style.display || nextRow.style.display == 'none')
                                    nextRow.style.display = 'table-row';
                                else
                                    nextRow.style.display = 'none';
                            }
                        }

                        document.getElementById('request_times').onclick = showDetailsClick;
                        document.getElementById('application_errors').onclick = showDetailsClick;
                        document.getElementById('server_errors').onclick = showDetailsClick;
                    </script>";
        }

        private string Body()
        {
            var builder = new System.Text.StringBuilder();
            builder.AppendLine("<h2>Overall</h2>");
            builder.AppendFormat("Server last started {0}<br/>", Logger.ServerStarted.Last());
            builder.AppendFormat("Total connections received: {0}<br />", Logger.TotalConnectionsReceived);

            builder.AppendLine("<h2>Request Times</h2>");
            builder.AppendLine("<table id=\"request_times\"><tr><th>Path</th><th>Method</th><th>Number of requests</th><th></th></tr>");
            int i = 0;
            foreach (var time in Logger.GetAllRequestTimes())
            {
                string row_class = (i % 2 == 0) ? "even_row" : "odd_row";
                builder.AppendFormat("<tr class=\"{0}\"><td>{1}</td><td>{2}</td><td>{3}</td><td class=\"see_details\">details</td></tr>\n", row_class, HtmlEncode(time.RelativePath), HtmlEncode(time.HttpMethod), time.NumRequests);

                // Now the line with more data
                builder.AppendFormat("<tr class=\"row_data\"><td colspan=\"4\"><div style=\"padding-left: 10px;\">Minimum time: {0}ms<br />Maximum time: {1}ms<br/>Average time: {2}ms</div></td></tr>\n", time.MinimumTime.TotalMilliseconds, time.MaximumTime.TotalMilliseconds, time.AverageTime.TotalMilliseconds);
                
                i++;
            }
            builder.Append("</table>\n");

            var applicationErrors = Logger.GetAllApplicationErrors();
            builder.Append("<h2>Application Errors</h2>");
            if (!applicationErrors.Any())
            {
                builder.AppendLine("<p>No application errors<p>");
            }
            else
            {
                builder.AppendLine("<table id=\"application_errors\"><tr><th>Path</th><th>Method</th><th>Error</th><th></th></tr>");
                i = 0;
                foreach (var error in applicationErrors)
                {
                    string row_class = (i % 2 == 0) ? "even_row" : "odd_row";
                    builder.AppendFormat("<tr class=\"{0}\"><td>{1}</td><td>{2}</td><td>{3}</td><td class=\"see_details\">details</td></tr>\n", row_class, HtmlEncode(error.RelativePath), HtmlEncode(error.HttpMethod), HtmlEncode(error.Error.Message));
                    
                    // Now the line with more data
                    builder.AppendFormat("<tr class=\"row_data\"><td colspan=\"4\"><div style=\"padding-left: 10px;\">{0}</div></td></tr>\n", error.Error.ToString());
                    
                    i++;
                }
                builder.Append("</table>\n");
            }

            var serverErrors = Logger.GetAllServerErrors();
            builder.Append("<h2>Server Errors</h2>");
            if (!serverErrors.Any())
            {
                builder.AppendLine("<p>No server errors</p>");
            }
            else
            {
                builder.AppendLine("<table id=\"server_errors\"><tr><th>Type</th><th></th></tr>");
                i = 0;
                foreach (var error in serverErrors)
                {
                    string row_class = (i % 2 == 0) ? "even_row" : "odd_row";
                    builder.AppendFormat("<tr class=\"{0}\"><td>{1}</td><td class=\"see_details\">details</td></tr>\n", row_class, HtmlEncode(error.GetType().ToString()));
                    
                    // Now the line with more data
                    builder.AppendFormat("<tr class=\"row_data\"><td colspan=\"4\"><div style=\"padding-left: 10px;\">{0}</div></td></tr>\n", HtmlEncode(error.ToString()));
                    
                    i++;
                }
                builder.Append("</table>\n");
            }

            return builder.ToString();
        }

        public Task Invoke(IDictionary<string, object> owinPrms)
        {
            return Task.Factory.StartNew(() => {
                var responseBody = (Stream) owinPrms["owin.ResponseBody"];

                // Sets the headers
                var headers = (IDictionary<string, string[]>) owinPrms["owin.ResponseHeaders"];
                headers.Add("Content-Type", new[] { "text/html" });

                // Sends the bodies
                using (var writer = new StreamWriter(responseBody))
                {
                    writer.Write("<html><head><title>Access Statistics</title>");
                    writer.Write(Head());
                    writer.Write("</head><body>");
                    //writer.Flush();

                    writer.Write(Body());
                    //writer.Flush();

                    writer.Write(EndOfBody());
                    writer.Write("</body></html>");
                }
            });
        }

        public StatsPageMiddleware(OwinHandler next, StatsLogger logger)
        {
            if (next != null)
                throw new ArgumentNullException("This middleware must be the last in the pipeline");
            else if (logger == null)
                throw new ArgumentNullException("You must provide an IServerLogger");

            Logger = logger;
        }
    }
}

