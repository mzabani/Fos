using System;
using FastCgiNet;
using Fos;
using System.Net.Sockets;

namespace Fos.Listener
{
	/// <summary>
	/// Holds together a Request and a ByteReader. Just for <see cref="Listener"/>'s convention. 
	/// </summary>
	internal class ByteReaderAndRequest
	{
		public ByteReader ByteReader { get; private set; }

		public Request FCgiRequest { get; private set; }

		/// <summary>
		/// This will be null until a BeginRequestRecord arrives.
		/// </summary>
		public FosRequest FosRequest;

		public ByteReaderAndRequest(ByteReader reader, Request req)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");
			else if (req == null)
				throw new ArgumentNullException("req");

			ByteReader = reader;
			FCgiRequest = req;
			FosRequest = null;
		}
	}
}
