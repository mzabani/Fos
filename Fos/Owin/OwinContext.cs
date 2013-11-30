using System;
using FastCgiNet;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using FastCgiNet.Streams;

namespace Fos.Owin
{
	internal class OwinContext : IDictionary<string, object>
	{
		/// <summary>
		/// The parameters dictionary of the owin pipeline, built through this class's methods.
		/// </summary>
		private Dictionary<string, object> parametersDictionary;

		#region IDictionary implementation
		public void Add (string key, object value)
		{
			parametersDictionary.Add(key, value);
		}

		public bool ContainsKey (string key)
		{
			return parametersDictionary.ContainsKey(key);
		}

		public bool Remove (string key)
		{
			return parametersDictionary.Remove(key);
		}

		public bool TryGetValue (string key, out object value)
		{
			return parametersDictionary.TryGetValue(key, out value);
		}

		public object this[string key]
        {
			get
            {
				return parametersDictionary[key];
			}
			set
            {
				parametersDictionary[key] = value;
			}
		}

		public ICollection<string> Keys {
			get {
				return parametersDictionary.Keys;
			}
		}

		public ICollection<object> Values {
			get {
				return parametersDictionary.Values;
			}
		}
		#endregion

		#region ICollection implementation
		public void Add (KeyValuePair<string, object> item)
		{
			parametersDictionary.Add(item.Key, item.Value);
		}

		public void Clear ()
		{
			parametersDictionary.Clear();
		}

		public bool Contains (KeyValuePair<string, object> item)
		{
			if (parametersDictionary.ContainsKey(item.Key) == false)
				return false;

			return parametersDictionary[item.Key] == item.Value;
		}

		public void CopyTo (KeyValuePair<string, object>[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}

		public bool Remove (KeyValuePair<string, object> item)
		{
			return parametersDictionary.Remove(item.Key);
		}

		public int Count {
			get {
				return parametersDictionary.Count;
			}
		}

		public bool IsReadOnly {
			get {
				throw new NotImplementedException ();
			}
		}
		#endregion

		#region IEnumerable implementation
		public IEnumerator<KeyValuePair<string, object>> GetEnumerator ()
		{
			return parametersDictionary.GetEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
		{
			return parametersDictionary.GetEnumerator();
		}
		#endregion

        public HeaderDictionary RequestHeaders { get; private set; }
        public HeaderDictionary ResponseHeaders { get; private set; }

		public CancellationToken CancellationToken { get; private set; }

		public void Set(string key, object obj) {
			if (parametersDictionary.ContainsKey(key))
				parametersDictionary[key] = obj;
			else
				parametersDictionary.Add (key, obj);
		}

		public T Get<T>(string key)
		{
			return (T)this[key];
		}

		public void SetRequestHeader(string headerName, string headerValue)
		{
			if (RequestHeaders.ContainsKey(headerName))
				RequestHeaders[headerName][0] = headerValue;
			else
				RequestHeaders.Add(headerName, new string[1] { headerValue });
		}

		public void SetResponseHeader(string headerName, string headerValue)
		{
			if (ResponseHeaders.ContainsKey(headerName))
				ResponseHeaders[headerName][0] = headerValue;
			else
				ResponseHeaders.Add(headerName, new string[1] { headerValue });
		}

        //private readonly static System.Text.RegularExpressions.Regex HttpHeaderRegex = new System.Text.RegularExpressions.Regex(@"HTTP_(([^ _])+(_?))+");
		public void SetOwinParametersFromFastCgiNvp(NameValuePair nameValuePair)
		{
			if (nameValuePair.Name == "SERVER_PROTOCOL")
                Set("owin.RequestProtocol", nameValuePair.Value);
            else if (nameValuePair.Name == "REQUEST_METHOD")
                Set("owin.RequestMethod", nameValuePair.Value.ToUpperInvariant());
            else if (nameValuePair.Name == "QUERY_STRING")
                Set("owin.RequestQueryString", nameValuePair.Value);
            else if (nameValuePair.Name == "HTTPS" && nameValuePair.Value == "on")
                Set("owin.RequestScheme", "https");
            else if (nameValuePair.Name == "DOCUMENT_URI")
            {
                Set("owin.RequestPathBase", string.Empty);
                Set("owin.RequestPath", nameValuePair.Value);
            }
			
			// HTTP_* parameters (these represent the http request header), such as:
			// HTTP_CONNECTION: keep-alive
			// HTTP_ACCEPT: text/html... etc.
			// HTTP_USER_AGENT: Mozilla/5.0
			// HTTP_ACCEPT_ENCODING
			// HTTP_ACCEPT_LANGUAGE
			// HTTP_COOKIE
			// many others..
            else if (nameValuePair.Name.StartsWith("HTTP_"))
            {
                //TODO: Avoid creating strings and create a decent algorithm for this conversion
                // Replace _ by - and pascal case single words to create pretty http headers
                string[] headerNameParts = nameValuePair.Name.Split('_');
                var builder = new System.Text.StringBuilder(nameValuePair.NameLength - 5);

                int i = 1;
                foreach (string part in headerNameParts.Skip(1))
                {
                    builder.Append(part[0]);
                    builder.Append(part.Substring(1, part.Length - 1).ToLowerInvariant());

                    if (i < headerNameParts.Length - 1)
                        builder.Append('-');

                    ++i;
                }

                SetRequestHeader(builder.ToString(), nameValuePair.Value);
            }

//			else if (nameValuePair.Name == "HTTP_HOST")
//				SetRequestHeader("Host", nameValuePair.Value);
//			else if (nameValuePair.Name == "HTTP_ACCEPT")
//				SetRequestHeader("Accept", nameValuePair.Value);
//			else if (nameValuePair.Name == "HTTP_ACCEPT_ENCODING")
//				SetRequestHeader("Accept-Encoding", nameValuePair.Value);
//			else if (nameValuePair.Name == "HTTP_ACCEPT_LANGUAGE")
//				SetRequestHeader("Accept-Language", nameValuePair.Value);
//			else if (nameValuePair.Name == "HTTP_CONNECTION")
//				SetRequestHeader("Connection", nameValuePair.Value);
//			else if (nameValuePair.Name == "HTTP_CONTENT_LENGTH")
//				SetRequestHeader("Content-Length", nameValuePair.Value);
//			else if (nameValuePair.Name == "HTTP_ORIGIN")
//				SetRequestHeader("Origin", nameValuePair.Value);
//			else if (nameValuePair.Name == "HTTP_X_REQUESTED_WITH")
//				SetRequestHeader("X-Requested-With", nameValuePair.Value);
//			else if (nameValuePair.Name == "HTTP_USER_AGENT")
//				SetRequestHeader("User-Agent", nameValuePair.Value);
//			else if (nameValuePair.Name == "HTTP_CONTENT_TYPE")
//				SetRequestHeader("Content-Type", nameValuePair.Value);
//			else if (nameValuePair.Name == "HTTP_REFERER")
//				SetRequestHeader("Referer", nameValuePair.Value);
//			else if (nameValuePair.Name == "HTTP_AUTHORIZATION")
//				SetRequestHeader("Authorization", nameValuePair.Value);
//			else if (nameValuePair.Name == "HTTP_COOKIE")
//				SetRequestHeader("Cookie", nameValuePair.Value);
		}
        /// <summary>
        /// Sets Owin parameters according to the received FastCgi Params in <paramref name="stream"/>.
        /// </summary>
        public void AddParams(FastCgiStream stream) {
            if (stream == null)
                throw new ArgumentNullException("stream");

            using (var reader = new FastCgiNet.Streams.NvpReader(stream))
            {
                NameValuePair nvp;
                while ((nvp = reader.Read()) != null)
                    SetOwinParametersFromFastCgiNvp(nvp);
            }
        }

		public FastCgiStream RequestBody
		{
			get
			{
				return (FastCgiStream)parametersDictionary["owin.RequestBody"];
			}
			internal set
			{
				Set("owin.RequestBody", value);
			}
		}

		public FastCgiStream ResponseBody
		{
			get
			{
                return (FastCgiStream)parametersDictionary["owin.ResponseBody"];
			}
			internal set
			{
				Set("owin.ResponseBody", value);
			}
		}

        /// <summary>
        /// The response's status code. If no response status code has been set by the application, this returns 200.
        /// </summary>
        public int ResponseStatusCode
        {
            get
            {
                if (this.ContainsKey("owin.ResponseStatusCode"))
                    return Get<int>("owin.ResponseStatusCode");
                else
                    return 200;
            }
            set
            {
                Set("owin.ResponseStatusCode", value);
            }
        }

		/// <summary>
		/// The response's status code and reason. This could be "200 OK", for example. It could also be just the status code, in case
        /// the application didn't set a reason.
		/// </summary>
		/// <value>The response's status code and reason.</value>
		public string ResponseStatusCodeAndReason
		{
			get
			{
                int num = ResponseStatusCode;

				string reason;
				if (this.ContainsKey("owin.ResponseReasonPhrase"))
				{
					reason = Get<string>("owin.ResponseReasonPhrase");
					return string.Format("{0} {1}", num, reason);
				}
				else
				{
					return num.ToString();
				}
			}
			set
			{
				int firstSpaceIdx = value.IndexOf(" ");
				if (firstSpaceIdx == -1)
                    this.ResponseStatusCode = int.Parse(value);
				else
				{
					int num = int.Parse(value.Substring(0, firstSpaceIdx + 1));
					string reason = value.Substring(firstSpaceIdx + 1);

					this.ResponseStatusCode = num;
					Set("owin.ResponseReasonPhrase", reason);
				}
			}
		}

		/// <summary>
		/// If the application set either a response status code, a response reason phrase, some header or some response body, this is <c>true</c>. It is <c>false</c> otherwise.
		/// </summary>
		public bool SomeResponseExists
        {
			get
			{
				return this.ContainsKey("owin.ResponseStatusCode") || this.ContainsKey("owin.ResponseReasonPhrase") || ResponseHeaders.Any() || (this.ContainsKey("owin.ResponseBody") && ResponseBody.Length > 0);
			}
		}

		public string CompleteUri
		{
			get
			{
                string queryString = this.QueryString;
				return Get<string>("owin.RequestScheme") + "://" + (string)RequestHeaders["Host"][0]
				+ this.RelativePath
				+ (!string.IsNullOrEmpty(queryString) ? "?" + queryString : null);
			}
		}

        /// <summary>
        /// Before reading the <see cref="RelativePath"/> property, make sure this is true, otherwise there is not enough
        /// information to find the relative path and an exception could be thrown.
        /// </summary>
        /// <remarks>One only needs to check for this before all ParamsRecords are received and added to this context.</remarks>
        public bool RelativePathDefined
        {
            get
            {
                return this.ContainsKey("owin.RequestPathBase") && this.ContainsKey("owin.RequestPath");
            }
        }
        public string RelativePath
        {
            get
            {
                return Get<string>("owin.RequestPathBase") + Get<string>("owin.RequestPath");
            }
        }

        /// <summary>
        /// Before reading the <see cref="HttpMethod"/> property, make sure this is true, otherwise there is not enough
        /// information to find the http method and an exception could be thrown.
        /// </summary>
        /// <remarks>One only needs to check for this before all ParamsRecords are received and added to this context.</remarks>
        public bool HttpMethodDefined
        {
            get
            {
                return this.ContainsKey("owin.RequestMethod");
            }
        }

        /// <summary>
        /// The Http Method. If not set manually, this will always be all in upper case letters.
        /// </summary>
        public string HttpMethod
        {
            get
            {
                return Get<string>("owin.RequestMethod");
            }
        }

        /// <summary>
        /// The query string. This can be null if no query string parameter has been defined.
        /// </summary>
        /// <remarks>This property is safe even in incomplete contexts.</remarks>
        public string QueryString
        {
            get
            {
                //TODO: We should percent-decode this uri's components

                if (this.ContainsKey("owin.RequestQueryString"))
                    return Get<string>("owin.RequestQueryString");
                else
                    return null;
            }
        }

		public OwinContext(string owinVersion, CancellationToken token)
		{
			// For now, only version 1.0 is allowed
			if (owinVersion != "1.0")
				throw new ArgumentException("Owin Version must be equal to '1.0'");

			parametersDictionary = new Dictionary<string, object>();
            RequestHeaders = new HeaderDictionary();
            ResponseHeaders = new HeaderDictionary();
			Set("owin.RequestHeaders", RequestHeaders);
			Set("owin.ResponseHeaders", ResponseHeaders);

			Set("owin.Version", owinVersion);

			CancellationToken = token;
			Set("owin.CallCancelled", token);

			// It is http (not https) until proven otherwise
			Set("owin.RequestScheme", "http");
		}
	}
}
