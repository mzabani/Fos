using System;
using FastCgiNet.Logging;

namespace Fos.Logging
{
	/// <summary>
	/// This logger prints Fatal and Error messages to Stderr. Everything else is simply ignored.
	/// </summary>
	public class StderrLogger : ILogger
	{
		public void Info (string msg, params object[] prms)
		{
		}
		public void Debug (string msg, params object[] prms)
		{
		}
		public void Debug (Exception e)
		{
		}
		public void Error (Exception e)
		{
			Console.Error.WriteLine("[{0}] ERROR", DateTime.Now);
			Console.Error.WriteLine("THREW {0}", e);
		}
		public void Error (Exception e, string msg, params object[] prms)
		{
			Console.Error.WriteLine("[{0}] ERROR " + string.Format(msg, prms), DateTime.Now);
			Console.Error.WriteLine("THREW {0}", e);
		}
		public void Fatal (Exception e)
		{
			Console.Error.WriteLine("[{0}] FATAL", DateTime.Now);
			Console.Error.WriteLine("THREW {0}", e);
		}
		public void Fatal (Exception e, string msg, params object[] prms)
		{
			Console.Error.WriteLine("[{0}] FATAL " + string.Format(msg, prms), DateTime.Now);
			Console.Error.WriteLine("THREW {0}", e);
		}
	}
}

