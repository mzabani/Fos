using System;
using FastCgiNet;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Fos.Owin
{
	public class OwinContext : IDictionary<string, object>
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

		void SetOwinParameter(string key, object obj) {
			if (parametersDictionary.ContainsKey(key))
				parametersDictionary[key] = obj;
			else
				parametersDictionary.Add (key, obj);
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
			if (nameValuePair.Name == "SERVER_PROTOCOL")
				SetOwinParameter("owin.RequestProtocol", nameValuePair.Value);
			else if (nameValuePair.Name == "REQUEST_METHOD")
				SetOwinParameter("owin.RequestMethod", nameValuePair.Value);
			else if (nameValuePair.Name == "QUERY_STRING")
				SetOwinParameter("owin.RequestQueryString", nameValuePair.Value);
			else if (nameValuePair.Name == "HTTPS")
				SetOwinParameter("owin.RequestScheme", "https");
			else if (nameValuePair.Name == "DOCUMENT_URI")
			{
				SetOwinParameter("owin.RequestPathBase", string.Empty);
				SetOwinParameter("owin.RequestPath", nameValuePair.Value);
			}
			
			// HTTP_* parameters (these represent the http request header), such as:
			// HTTP_CONNECTION: keep-alive
			// HTTP_ACCEPT: text/html... etc.
			// HTTP_USER_AGENT: Mozilla/5.0
			// HTTP_ACCEPT_ENCODING
			// HTTP_ACCEPT_LANGUAGE
			// HTTP_COOKIE
			// many others..
			else if (nameValuePair.Name == "HTTP_HOST")
			{
				SetRequestHeader("Host", nameValuePair.Value);
			}
			else if (nameValuePair.Name == "HTTP_ACCEPT")
				SetRequestHeader("Accept", nameValuePair.Value);
			else if (nameValuePair.Name == "HTTP_USER_AGENT")
				SetRequestHeader("User-Agent", nameValuePair.Value);
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

		public Stream RequestBody
		{
			get
			{
				return (Stream)parametersDictionary["owin.RequestBody"];
			}
			internal set
			{
				SetOwinParameter("owin.RequestBody", value);
			}
		}

		public Stream ResponseBody
		{
			get
			{
				return (Stream)parametersDictionary["owin.ResponseBody"];
			}
			internal set
			{
				SetOwinParameter("owin.ResponseBody", value);
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
			SetOwinParameter("owin.RequestHeaders", requestHeaders);
			SetOwinParameter("owin.ResponseHeaders", responseHeaders);

			SetOwinParameter("owin.Version", owinVersion);

			CancellationToken = token;
			SetOwinParameter("owin.CallCancelled", token);

			// Empty bodies
			RequestBody = Stream.Null;
			ResponseBody = Stream.Null;

			// It is http (not https) until proven otherwise
			SetOwinParameter("owin.RequestScheme", "http");
		}
	}
}
