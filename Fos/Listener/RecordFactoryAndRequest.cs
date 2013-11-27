using System;
using FastCgiNet;
using Fos;
using System.Net.Sockets;

namespace Fos.Listener
{
	/// <summary>
	/// Holds together a Request and a RecordFactory. Just for <see cref="Listener"/>'s convention. 
	/// </summary>
	internal class RecordFactoryAndRequest
	{
		public RecordFactory RecordFactory { get; private set; }
        public FosRequest FosRequest { get; private set; }

		public RecordFactoryAndRequest(RecordFactory reader, Socket sock, Fos.Logging.IServerLogger logger)
		{
			if (reader == null)
				throw new ArgumentNullException("reader");
			else if (sock == null)
				throw new ArgumentNullException("sock");

			RecordFactory = reader;
            FosRequest = new FosRequest(sock, logger);
		}
	}
}
