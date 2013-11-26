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
		public RecordFactory ByteReader { get; private set; }

		//public SocketRequest FCgiRequest { get; private set; }

		/*/// <summary>
		/// This will be null until a BeginRequestRecord arrives.
		/// </summary>*/
		public FosRequest FosRequest;

		public ByteReaderAndRequest(RecordFactory reader, Socket sock, Fos.Logging.IServerLogger logger)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");
			else if (sock == null)
				throw new ArgumentNullException("sock");

			ByteReader = reader;
			//FCgiRequest = req;
            FosRequest = new FosRequest(sock, logger);
		}
	}
}
