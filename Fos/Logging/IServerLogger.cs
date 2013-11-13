using System;
using FastCgiNet;
using System.Net.Sockets;

namespace Fos.Logging
{
	/// <summary>
	/// Implement this to be able to collect your own statistics of the server's operation. All methods of any implementations
	/// must be completely thread safe, except when noted.
	/// </summary>
	public interface IServerLogger
	{
		/// <summary>
		/// Does not need to be thread safe.
		/// </summary>
		void ServerStart();

		/// <summary>
		/// Does not need to be thread safe.
		/// </summary>
		void ServerStop();

		void LogConnectionReceived(Socket createdSocket);
		
		/// <summary>
		/// Some times the connection is closed abruptly. For those cases, this method is called to log this occurrence.
		/// Do note that <paramref name="req"/> can be null if the connection was closed before the server received any request data. Beware that
		/// sometimes <paramref name="req"/> is not null, yet its members can be null, depending on the amount of data the server received before 
		/// the connection was closed by the other side.
		/// </summary>
		/// <param name="s">The socket that was closed abruptly.</param>
		/// <param name="req">The request info we had obtained so far. You should null check this object and every member in it.</param>
		void LogConnectionClosedAbruptly(Socket s, RequestInfo req);
		
		void LogConnectionEndedNormally(Socket s, RequestInfo req);
		void LogApplicationError(Exception e);
		void LogServerError(Exception e, string format, params object[] prms);
		void LogSocketError(Socket s, Exception e, string format, params object[] prms);
		void LogInvalidRecordReceived(RecordBase invalidRecord);
	}
}
