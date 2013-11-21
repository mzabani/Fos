using System;
using FastCgiNet;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;

namespace Fos.Logging
{
	/// <summary>
	/// Class that holds minimum, maximum and average times of a request to a specific endpoint, for a specific Http method.
	/// </summary>
	internal class RequestTimes
	{
		public string HttpMethod { get; private set; }
		public string RelativePath { get; private set; }
		
		public TimeSpan MinimumTime { get; internal set; }
		public TimeSpan AverageTime { get; internal set; }
		public TimeSpan MaximumTime { get; internal set; }
		public long NumRequests { get; internal set; }

		internal RequestTimes(string verb, string relativePath, TimeSpan onlyTime)
		{
			HttpMethod = verb;
			RelativePath = relativePath;
			MinimumTime = onlyTime;
			AverageTime = onlyTime;
			MaximumTime = onlyTime;
			NumRequests = 1;
		}
	}

	/// <summary>
	/// This class helps collect connection statistics. All DateTimes are record in UTC time.
	/// </summary>
	internal class StatsLogger : IServerLogger
	{
		//TODO: Case sensitive or insensitive path comparison (and super option to supply IComparer<string>)
		//TODO: Light weight locks?

		private readonly object connectionReceivedLock = new object();

		public TimeSpan AggregationInterval { get; private set; }
		public ulong TotalConnectionsReceived { get; private set; }

		/// <summary>
		/// The maximum number of concurrent established connections at any given time during the server's operation.
		/// </summary>
		public int MaxConcurrentConnections { get; private set; }
		
		private int ConcurrentConnectionsNow = 0;

		/// <summary>
		/// The total number of connections closed abruptly by the other side.
		/// </summary>
		public ulong AbruptConnectionCloses { get; private set; }

		private DateTime LastAggregationPeriodStart;
		private ConcurrentDictionary<Socket, Stopwatch> RequestWatches = new ConcurrentDictionary<Socket, Stopwatch>();
		/// <summary>
		/// Number of connections received per time interval. An interval begins at a DateTime specified by the dictionary's
		/// key (inclusive) and ends at this same DateTime + <see cref="AggregationInterval"/> (exclusive).
		/// </summary>
		/// <value>The connections received per interval of time.</value>
		public ConcurrentDictionary<DateTime, int> ConnectionsReceivedAggregated { get; private set; }

		/// <summary>
		/// The minimum, average and maximum request processing times per requested relative path. Only requests that return
        /// 200 and 301 are logged.
		/// </summary>
		/// <remarks>Abruptly closed connections do not interfere with this data.</remarks>
		private ConcurrentDictionary<string, LinkedList<RequestTimes>> TimesPerEndpoint = new ConcurrentDictionary<string, LinkedList<RequestTimes>>();
		
		/// <summary>
		/// Enumerates all requests time info with response status 200 and 301 ordered by average response time, descending.
		/// </summary>
        /// <remarks>Abruptly closed connections do not interfere with this data.</remarks>
		public IEnumerable<RequestTimes> GetAllRequestTimes()
		{
			return TimesPerEndpoint.Values.SelectMany(list => list).OrderByDescending(req => req.AverageTime);
		}

        private readonly object applicationErrorsLock = new object();
        /// <summary>
        /// Application errors per request path.
        /// </summary>
        private LinkedList<ApplicationError> ApplicationErrors = new LinkedList<ApplicationError>();
        
        /// <summary>
        /// Enumerates all application errors in descending order of when they happened.
        /// </summary>
        public IEnumerable<ApplicationError> GetAllApplicationErrors()
        {
            return ApplicationErrors.Reverse();
        }
		
		public IList<DateTime> ServerStarted { get; private set; }
		public IList<DateTime> ServerStopped { get; private set; }

		/// <summary>
		/// A DateTime that represents now in UTC time.
		/// </summary>
		private DateTime Now
		{
			get
			{
				return DateTime.UtcNow;
			}
		}

		public void ServerStart()
		{
			ServerStarted.Add(Now);
		}

		public void ServerStop()
		{
			ServerStopped.Add(Now);
		}

		/// <summary>
		/// Non thread safe method that stops a timer for a closing socket and returns the elapsed time since
        /// the timer started.
		/// </summary>
		private TimeSpan StopConnectionTimer(Socket s)
		{
			var stopWatch = RequestWatches[s];
			stopWatch.Stop();
			return stopWatch.Elapsed;
		}
		
        private readonly object lockPerPathObj = new object();
        /// <summary>
        /// If a lock to access the LinkedList&lt;RequestTime&gt; for path <paramref name="relativePath"/> already exists, it returns that lock. Otherwise, it creates one
        /// and returns it. This method avoids using a global lock as much as possible.
        /// </summary>
        private object CreateAndReturnRelativePathLock(string relativePath, out LinkedList<RequestTimes> requestTimes)
        {
            if (!TimesPerEndpoint.TryGetValue(relativePath, out requestTimes))
            {
                lock (lockPerPathObj)
                {
                    // We need to check again
                    if (!TimesPerEndpoint.TryGetValue(relativePath, out requestTimes))
                    {
                        requestTimes = new LinkedList<RequestTimes>();
                        TimesPerEndpoint[relativePath] = requestTimes;
                    }
                }
            }

            // Return the list itself as the locking object
            return requestTimes;
        }

		public void LogConnectionReceived(Socket createdSocket)
		{
			var now = Now;
			
			DateTime lastAggrPeriod;
			lock (connectionReceivedLock)
			{
				TotalConnectionsReceived++;
				ConcurrentConnectionsNow++;

				// Updates maximum concurrent connections if we have to
				if (MaxConcurrentConnections < ConcurrentConnectionsNow)
					MaxConcurrentConnections = ConcurrentConnectionsNow;

				lastAggrPeriod = LastAggregationPeriodStart;
				if (LastAggregationPeriodStart + AggregationInterval <= now)
				{
					LastAggregationPeriodStart += AggregationInterval;
					lastAggrPeriod = LastAggregationPeriodStart;
				}
			}

            // Start the Watch
            var stopWatch = new Stopwatch();
            RequestWatches[createdSocket] = stopWatch;
            stopWatch.Start();
			
			// Add it to the aggregated connections
			ConnectionsReceivedAggregated[lastAggrPeriod] = ConnectionsReceivedAggregated[lastAggrPeriod] + 1;
		}
		
		public void LogConnectionClosedAbruptly(Socket s, RequestInfo req)
		{
			//TODO: Lock
			StopConnectionTimer(s);
			AbruptConnectionCloses++;
		}

		public void LogConnectionEndedNormally(Socket s, RequestInfo req)
		{
			var now = Now;
			
			TimeSpan requestTime;
			lock (connectionReceivedLock)
			{
				ConcurrentConnectionsNow--;
				
				// Stop the watch
				requestTime = StopConnectionTimer(s);
			}
			
			if (req.ResponseStatusCode != 200 && req.ResponseStatusCode != 301)
				return;
			
			// Look for the times with our method
            // We could do with a global lock here, but that just sounds so bad.
            // Instead, we try to avoid a global lock as much as possible, and count on another method
            // to give us a finer grained lock.
            LinkedList<RequestTimes> timesForEndpoint;
            lock (CreateAndReturnRelativePathLock(req.RelativePath, out timesForEndpoint))
            {
                var verbTimes = timesForEndpoint.FirstOrDefault(t => t.HttpMethod == req.HttpMethod);
                if (verbTimes == null)
                {
                    // First request to this endpoint with this method. Add it.
                    verbTimes = new RequestTimes(req.HttpMethod, req.RelativePath, requestTime);
                    timesForEndpoint.AddLast(verbTimes);
                } else
                {
                    // Just update the times
                    if (verbTimes.MinimumTime > requestTime)
                        verbTimes.MinimumTime = requestTime;
                    if (verbTimes.MaximumTime < requestTime)
                        verbTimes.MaximumTime = requestTime;
				
                    //TODO: Precision issues with the line below
                    verbTimes.AverageTime = new TimeSpan((verbTimes.NumRequests * verbTimes.AverageTime.Ticks + requestTime.Ticks) / (verbTimes.NumRequests + 1));
                    verbTimes.NumRequests++;
                }
            }
		}

		public void LogApplicationError(Exception e, RequestInfo req)
		{
            lock (applicationErrorsLock)
            {
                ApplicationErrors.AddLast(new ApplicationError(req.HttpMethod, req.RelativePath, e));
            }
		}

		public void LogServerError(Exception e, string format, params object[] prms)
		{
			//throw new NotImplementedException ();
		}

		public void LogSocketError(Socket s, Exception e, string format, params object[] prms)
		{
			//throw new NotImplementedException ();
		}

		public void LogInvalidRecordReceived(RecordBase invalidRecord)
		{
			//throw new NotImplementedException ();
		}

		/// <summary>
		/// The smaller the <paramref name="aggregationInterval"/>, the more work the logger will have to do.
		/// </summary>
		public StatsLogger(TimeSpan aggregationInterval)
		{
			AggregationInterval = aggregationInterval;
			LastAggregationPeriodStart = Now;
			ServerStarted = new List<DateTime>();
			ServerStopped = new List<DateTime>();
			ConnectionsReceivedAggregated = new ConcurrentDictionary<DateTime, int>();
		}
	}
}
