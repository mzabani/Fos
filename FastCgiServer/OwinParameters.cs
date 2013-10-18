using System;
using FastCgiNet;
using System.Collections.Generic;
using System.IO;

namespace FastCgiServer
{
	class OwinParameters : IDictionary<string, object>
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

		/// <summary>
		/// Sets Owin parameters according to the received FastCgi Params record <paramref name="rec"/>.
		/// </summary>
		public void AddParamsRecord(Record rec) {
			if (rec == null)
				throw new ArgumentNullException("rec");
			else if (rec.RecordType != RecordType.FCGIParams)
				throw new ArgumentException("The record supplied must be of type Params");

			foreach (var nameValuePair in rec.NamesAndValues.Content)
			{
				if (nameValuePair.Name == "SERVER_PROTOCOL")
					SetOwinParameter("owin.RequestProtocol", nameValuePair.Value);
				else if (nameValuePair.Name == "REQUEST_METHOD")
					SetOwinParameter("owin.RequestMethod", nameValuePair.Value);
				else if (nameValuePair.Name == "QUERY_STRING")
					SetOwinParameter("owin.RequestQueryString", nameValuePair.Value);
				else if (nameValuePair.Name == "DOCUMENT_URI")
				{
					int lastSlashIdx = nameValuePair.Value.LastIndexOf('/');
					if (lastSlashIdx == 0)
					{
						SetOwinParameter("owin.RequestPathBase", string.Empty);
						SetOwinParameter("owin.RequestPath", nameValuePair.Value);
					}
					else
					{
						SetOwinParameter("owin.RequestPathBase", nameValuePair.Value.Substring(0, lastSlashIdx));
						SetOwinParameter("owin.RequestPath", nameValuePair.Value.Substring(lastSlashIdx));
					}
				}
				else if (nameValuePair.Name == "HTTP_HOST")
				{
					SetRequestHeader("Host", nameValuePair.Value);
				}
			}
		}

		public Stream RequestBody
		{
			get
			{
				return (Stream)parametersDictionary["owin.RequestBody"];
			}
			set
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
			set
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

		public OwinParameters()
		{
			parametersDictionary = new Dictionary<string, object>();
			requestHeaders = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
			responseHeaders = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
			SetOwinParameter("owin.RequestHeaders", requestHeaders);
			SetOwinParameter("owin.ResponseHeaders", responseHeaders);

			// Empty bodies
			RequestBody = Stream.Null;
			ResponseBody = Stream.Null;

			//TODO: Do this right later on.. what about https?
			SetOwinParameter("owin.RequestScheme", "http");
		}
	}
}

