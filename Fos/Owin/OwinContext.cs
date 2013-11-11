using System;
using FastCgiNet;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;

namespace Fos.Owin
{
	internal class OwinContext : IDictionary<string, object>
	{
		/// <summary>
		/// The parameters dictionary of the owin pipeline, built through this class's methods.
		/// </summary>
		Dictionary<string, object> parametersDictionary;

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

		public object this [string key] {
			get {
				return parametersDictionary[key];
			}
			set {
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

		Dictionary<string, string[]> requestHeaders;
		Dictionary<string, string[]> responseHeaders;

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
			if (requestHeaders.ContainsKey(headerName))
				requestHeaders[headerName][0] = headerValue;
			else
				requestHeaders.Add(headerName, new string[1] { headerValue });
		}

		public void SetResponseHeader(string headerName, string headerValue)
		{
			if (responseHeaders.ContainsKey(headerName))
				responseHeaders[headerName][0] = headerValue;
			else
				responseHeaders.Add(headerName, new string[1] { headerValue });
		}

		internal void SetOwinParametersFromFastCgiNvp(NameValuePair nameValuePair)
		{
			//Console.WriteLine("{0}: {1}", nameValuePair.Name, nameValuePair.Value);

			if (nameValuePair.Name == "SERVER_PROTOCOL")
				Set("owin.RequestProtocol", nameValuePair.Value);
			else if (nameValuePair.Name == "REQUEST_METHOD")
				Set("owin.RequestMethod", nameValuePair.Value);
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
			// many others.. see http://www.w3.org/Protocols/rfc2616/rfc2616-sec14.html and CGI Environment Variables
			//TODO: Check if replacing _ by - and camel casing single words would do it for all headers
			else if (nameValuePair.Name == "HTTP_HOST")
				SetRequestHeader("Host", nameValuePair.Value);
			else if (nameValuePair.Name == "HTTP_ACCEPT")
				SetRequestHeader("Accept", nameValuePair.Value);
			else if (nameValuePair.Name == "HTTP_ACCEPT_ENCODING")
				SetRequestHeader("Accept-Encoding", nameValuePair.Value);
			else if (nameValuePair.Name == "HTTP_ACCEPT_LANGUAGE")
				SetRequestHeader("Accept-Language", nameValuePair.Value);
			else if (nameValuePair.Name == "HTTP_CONNECTION")
				SetRequestHeader("Connection", nameValuePair.Value);
			else if (nameValuePair.Name == "HTTP_CONTENT_LENGTH")
				SetRequestHeader("Content-Length", nameValuePair.Value);
			else if (nameValuePair.Name == "HTTP_ORIGIN")
				SetRequestHeader("Origin", nameValuePair.Value);
			else if (nameValuePair.Name == "HTTP_X_REQUESTED_WITH")
				SetRequestHeader("X-Requested-With", nameValuePair.Value);
			else if (nameValuePair.Name == "HTTP_USER_AGENT")
				SetRequestHeader("User-Agent", nameValuePair.Value);
			else if (nameValuePair.Name == "HTTP_CONTENT_TYPE")
				SetRequestHeader("Content-Type", nameValuePair.Value);
			else if (nameValuePair.Name == "HTTP_REFERER")
				SetRequestHeader("Referer", nameValuePair.Value);
			else if (nameValuePair.Name == "HTTP_AUTHORIZATION")
				SetRequestHeader("Authorization", nameValuePair.Value);
			else if (nameValuePair.Name == "HTTP_COOKIE")
				SetRequestHeader("Cookie", nameValuePair.Value);
		}

		/// <summary>
		/// Sets Owin parameters according to the received FastCgi Params record <paramref name="rec"/>.
		/// </summary>
		internal void AddParamsRecord(ParamsRecord rec) {
			if (rec == null)
				throw new ArgumentNullException("rec");

			foreach (var nameValuePair in rec.Parameters)
			{
				SetOwinParametersFromFastCgiNvp(nameValuePair);
			}
		}

		public FragmentedRequestStream<RecordContentsStream> RequestBody
		{
			get
			{
				return (FragmentedRequestStream<RecordContentsStream>)parametersDictionary["owin.RequestBody"];
			}
			internal set
			{
				Set("owin.RequestBody", value);
			}
		}

		public FragmentedResponseStream<RecordContentsStream> ResponseBody
		{
			get
			{
				return (FragmentedResponseStream<RecordContentsStream>)parametersDictionary["owin.ResponseBody"];
			}
			internal set
			{
				Set("owin.ResponseBody", value);
			}
		}

		/// <summary>
		/// The response's status code. This could be "200 OK", for example.
		/// </summary>
		/// <value>The response's status code.</value>
		public string ResponseStatusCode
		{
			get
			{
				int num;
				if (this.ContainsKey("owin.ResponseStatusCode"))
					num = Get<int>("owin.ResponseStatusCode");
				else
					num = 200;

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
					Set("owin.ResponseStatusCode", int.Parse(value));
				else
				{
					int num = int.Parse(value.Substring(0, firstSpaceIdx + 1));
					string reason = value.Substring(firstSpaceIdx + 1);

					Set("owin.ResponseStatusCode", num);
					Set("owin.ResponseReasonPhrase", reason);
				}
			}
		}

		/// <summary>
		/// If the application set either a response status code, a response reason phrase, some header or some response body, this is true. It is false otherwise.
		/// </summary>
		public bool SomeResponseExists {
			get
			{
				return this.ContainsKey("owin.ResponseStatusCode") || this.ContainsKey("owin.ResponseReasonPhrase") || responseHeaders.Any() || ResponseBody.Length > 0;
			}
		}

		public string CompleteUri
		{
			get
			{
				//TODO: Although the standard does not mention this, we should percent-decode this uri's components
				return (string)parametersDictionary["owin.RequestScheme"] + "://" + (string)requestHeaders["Host"][0]
				+ (string)parametersDictionary["owin.RequestPathBase"] + (string)parametersDictionary["owin.RequestPath"]
				+ ((parametersDictionary.ContainsKey("owin.RequestQueryString") && !string.IsNullOrEmpty((string)parametersDictionary["owin.RequestQueryString"])) ? "?" + (string)parametersDictionary["owin.RequestQueryString"] : null);
			}
		}

		public OwinContext(string owinVersion, CancellationToken token)
		{
			// For now, only version 1.0 is allowed
			if (owinVersion != "1.0")
				throw new ArgumentException("Owin Version must be equal to '1.0'");

			parametersDictionary = new Dictionary<string, object>();
			requestHeaders = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
			responseHeaders = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
			Set("owin.RequestHeaders", requestHeaders);
			Set("owin.ResponseHeaders", responseHeaders);

			Set("owin.Version", owinVersion);

			CancellationToken = token;
			Set("owin.CallCancelled", token);

			// Empty bodies
			RequestBody = new FragmentedRequestStream<RecordContentsStream>();
			ResponseBody = new FragmentedResponseStream<RecordContentsStream>();

			// It is http (not https) until proven otherwise
			Set("owin.RequestScheme", "http");
		}
	}
}
