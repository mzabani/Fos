using System;
using FastCgiNet;
using System.Net.Sockets;

namespace Fos.Logging
{
	/// <summary>
	/// Implement this to be able to collect your own statistics of the server's operation.
	/// </summary>
	public interface IServerLogger
	{
		void ServerStart();
		void ServerStop();
		void LogConnectionReceived(Socket createdSocket);
		void LogConnectionClosedAbruptly(Socket s);
		void LogConnectionEndedNormally(Socket s);
		void LogApplicationError(Exception e);
		void LogServerError(Exception e, string format, params object[] prms);
		void LogSocketError(Socket s, Exception e, string format, params object[] prms);
		void LogInvalidRecordReceived(RecordBase invalidRecord);
	}
}
