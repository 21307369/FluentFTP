﻿using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;
using System.Security.Authentication;
using System.Net;
using FluentFTP.Helpers;
using FluentFTP.Proxy;
#if !CORE
using System.Web;
#endif
#if (CORE || NETFX)
using System.Threading;
using FluentFTP.Helpers.Hashing;
using HashAlgos = FluentFTP.Helpers.Hashing.HashAlgorithms;

#endif
#if ASYNC
using System.Threading.Tasks;

#endif

namespace FluentFTP {
	public partial class FtpClient : IDisposable {

		#region Get z/OS Realm

		/// <summary>
		/// If an FTP Server has "different realms", in which realm is the
		/// current working directory. 
		/// </summary>
		/// <returns>The realm</returns>
		public FtpZOSListRealm GetZOSListRealm() {

			LogFunc(nameof(GetZOSListRealm));

			// this case occurs immediately after connection and after the working dir has changed
			if (_LastWorkingDir == null) {
				ReadCurrentWorkingDirectory();
			}

			if (ServerType != FtpServer.IBMzOSFTP) {
				return FtpZOSListRealm.Invalid;
			}

			// It is a unix like path (starts with /)
			if (_LastWorkingDir[0] != '\'') {
				return FtpZOSListRealm.Unix;
			}

			// Ok, the CWD starts with a single quoute. Classic z/OS dataset realm
			FtpReply reply;

#if !CORE14
			lock (m_lock) {
#endif
				// Go to where we are. The reply will tell us what it is we we are...
				if (!(reply = Execute("CWD " + _LastWorkingDir)).Success) {
					throw new FtpCommandException(reply);
				}
#if !CORE14
			}
#endif
			// 250-The working directory may be a load library                          
			// 250 The working directory "GEEK.PRODUCT.LOADLIB" is a partitioned data set

			if (reply.InfoMessages != null &&
				reply.InfoMessages.Contains("may be a load library")) {
				return FtpZOSListRealm.MemberU;
			}

			if (reply.Message.Contains("is a partitioned data set")) {
				return FtpZOSListRealm.Member;
			}

			return FtpZOSListRealm.Dataset;
		}

#if ASYNC
		/// <summary>
		/// If an FTP Server has "different realms", in which realm is the
		/// current working directory. 
		/// </summary>
		/// <returns>The realm</returns>
		public async Task<FtpZOSListRealm> GetZOSListRealmAsync(CancellationToken token = default(CancellationToken)) {
			LogFunc(nameof(GetZOSListRealmAsync));

			// this case occurs immediately after connection and after the working dir has changed
			if (_LastWorkingDir == null) {
				await ReadCurrentWorkingDirectoryAsync(token);
			}

			if (ServerType != FtpServer.IBMzOSFTP) {
				return FtpZOSListRealm.Invalid;
			}

			// It is a unix like path (starts with /)
			if (_LastWorkingDir[0] != '\'') {
				return FtpZOSListRealm.Unix;
			}

			// Ok, the CWD starts with a single quoute. Classic z/OS dataset realm
			FtpReply reply;

			// Go to where we are. The reply will tell us what it is we we are...
			if (!(reply = await ExecuteAsync("CWD " + _LastWorkingDir, token)).Success) {
				throw new FtpCommandException(reply);
			}

			// 250-The working directory may be a load library                          
			// 250 The working directory "GEEK.PRODUCTS.LOADLIB" is a partitioned data set

			if (reply.InfoMessages != null &&
				reply.InfoMessages.Contains("may be a load library")) {
				return FtpZOSListRealm.MemberU;
			}

			if (reply.Message.Contains("is a partitioned data set")) {
				return FtpZOSListRealm.Member;
			}

			return FtpZOSListRealm.Dataset;
		}
#endif
		#endregion

		#region Get z/OS File Size

		/// <summary>
		/// Get z/OS file size
		/// </summary>
		/// <param name="path">The full path of th file whose size you want to retrieve</param>
		/// <remarks>
		/// Make sure you are in the right realm (z/OS or HFS) before doing this
		/// </remarks>
		/// <returns>The size of the file</returns>
		public long GetZOSFileSize(string path) {

			// Verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			LogFunc(nameof(GetZOSFileSize), new object[] { path });

			// prevent automatic parser detection switching to unix on HFS paths
			ListingParser = FtpParser.IBMzOS;

			FtpListItem[] entries = GetListing(path);

			// no entries or more than one: path is NOT for a single dataset or file
			if (entries.Length != 1) { return -1; }

			// if the path is for a SINGLE dataset or file, there will be only one entry
			FtpListItem entry = entries[0];

			// z/OS list parser will have determined that size
			return entry.Size;
		}

#if ASYNC
		/// <summary>
		/// Get z/OS file size
		/// </summary>
		/// <param name="path">The full path of th file whose size you want to retrieve</param>
		/// <remarks>
		/// Make sure you are in the right realm (z/OS or HFS) before doing this
		/// </remarks>
		/// <returns>The size of the file</returns>
		public async Task<long> GetZOSFileSizeAsync(string path, CancellationToken token = default(CancellationToken)) {// verify args

			// Verify args
			if (path.IsBlank()) {
				throw new ArgumentException("Required parameter is null or blank.", "path");
			}

			LogFunc(nameof(GetZOSFileSizeAsync), new object[] { path });

			// prevent automatic parser detection switching to unix on HFS paths
			ListingParser = FtpParser.IBMzOS;

			FtpListItem[] entries = await GetListingAsync(path, token);
			// no entries or more than one: path is NOT for a single dataset or file

			if (entries.Length != 1) return -1;
			// if the path is for a SINGLE dataset or file, there will be only one entry

			FtpListItem entry = entries[0];
			// z/OS list parser will have determined that size

			return entry.Size;
		}
#endif
		#endregion
	}
}