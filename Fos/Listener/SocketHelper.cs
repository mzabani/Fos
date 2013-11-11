using System;
using System.Net.Sockets;

namespace Fos.Listener
{
	internal class SocketHelper
	{
		/*
		/// <summary>
		/// Closes the socket from this connection safely, i.e. if it has already been closed, no exceptions happen.
		/// </summary>
		/// <returns>True if the connection has been successfuly closed, false if it was already closed or in the process of being closed.</returns>
		/// <remarks>If the connection was open but couldn't be closed for some reason, an exception is thrown. The caller should check for all possibilities because remote connection ending is not uncommon at all.</remarks>
		public static bool CloseAndDispose(this Socket s)
		{
			try
			{
				s.Close();
				s.Dispose();
			}
			catch (ObjectDisposedException)
			{
			}
			catch (SocketException e)
			{
				if (e.SocketErrorCode != SocketError.Shutdown)
					throw;
			}
		}
		*/
	}
}
