﻿using FluentFTP;
using Microsoft.Extensions.Logging;

namespace FluentFTP.Logging {
	public sealed class FtpLogAdapter : IFtpLogger {
		private readonly ILogger adaptee;

		public FtpLogAdapter(ILogger adaptee) =>
			this.adaptee = adaptee;

		public void Log(FtpLogEntry entry) =>
			adaptee.Log(ToLevel(entry.Severity), 0, entry.Message, entry.Exception, (s, _) => s);

		private static LogLevel ToLevel(FtpTraceLevel s) => s switch {
			FtpTraceLevel.Verbose => LogLevel.Debug,
			FtpTraceLevel.Info => LogLevel.Information,
			FtpTraceLevel.Warn => LogLevel.Warning,
			FtpTraceLevel.Error => LogLevel.Error,
			_ => LogLevel.Information
		};
	}
}