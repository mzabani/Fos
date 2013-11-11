using System;
using FastCgiNet;
using System.Net.Sockets;

namespace Fos.Logging
{
	/// <summary>
	/// This class helps collect connection statistics. It could be entirely implemented outside Fos, but remains here
	/// for its usefulness.
	/// </summary>
	/// <remarks>THIS IS NOT IMPLEMENTED YET.</remarks>
	public class StatsLogger : IServerLogger
	{
		public TimeSpan AggregationInterval { get; private set; }
		public long TotalConnectionsReceived { get; private set; }

		public void ServerStart()
		{
			throw new NotImplementedException();
		}

		public void ServerStop()
		{
			throw new NotImplementedException();
		}

		public void LogConnectionReceived(Socket createdSocket)
		{
			throw new NotImplementedException();
		}

		public void LogConnectionClosedAbruptly(Socket s)
		{
			throw new NotImplementedException ();
		}

		public void LogConnectionEndedNormally(Socket s)
		{
			throw new NotImplementedException ();
		}

		public void LogApplicationError(Exception e)
		{
			throw new NotImplementedException ();
		}

		public void LogServerError(Exception e, string format, params object[] prms)
		{
			throw new NotImplementedException ();
		}

		public void LogSocketError(Socket s, Exception e, string format, params object[] prms)
		{
			throw new NotImplementedException ();
		}

		public void LogInvalidRecordReceived(RecordBase invalidRecord)
		{
			throw new NotImplementedException ();
		}

		public StatsLogger(TimeSpan aggregationInterval)
		{
			AggregationInterval = aggregationInterval;
		}
	}
}

