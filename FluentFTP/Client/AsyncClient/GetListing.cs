﻿using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using FluentFTP.Exceptions;
using FluentFTP.Helpers;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using FluentFTP.Client.Modules;

namespace FluentFTP {
	public partial class AsyncFtpClient {

#if NET50_OR_LATER
		/// <summary>
		/// Gets a file listing from the server asynchronously. Each <see cref="FtpListItem"/> object returned
		/// contains information about the file that was able to be retrieved. 
		/// </summary>
		/// <remarks>
		/// If a <see cref="DateTime"/> property is equal to <see cref="DateTime.MinValue"/> then it means the 
		/// date in question was not able to be retrieved. If the <see cref="FtpListItem.Size"/> property
		/// is equal to 0, then it means the size of the object could also not
		/// be retrieved.
		/// </remarks>
		/// <param name="path">The path to list</param>
		/// <param name="options">Options that dictate how the list operation is performed</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <param name="enumToken">The token that can be used to cancel the enumerator</param>
		/// <returns>An array of items retrieved in the listing</returns>
		public async IAsyncEnumerable<FtpListItem> GetListingAsyncEnumerable(string path, FtpListOption options, CancellationToken token = default(CancellationToken), [EnumeratorCancellation] CancellationToken enumToken = default(CancellationToken)) {

			// start recursive process if needed and unsupported by the server
			if (options.HasFlag(FtpListOption.Recursive) && !IsServerSideRecursionSupported(options)) {
				await foreach (FtpListItem i in GetListingRecursiveAsyncEnumerable(await GetAbsolutePathAsync(path, token), options, token, enumToken)) {
					yield return i;
				}

				yield break;
			}

			// FIX : #768 NullOrEmpty is valid, means "use working directory".
			if (!string.IsNullOrEmpty(path)) {
				path = path.GetFtpPath();
			}

			LogFunc(nameof(GetListingAsync), new object[] { path, options });

			var lst = new List<FtpListItem>();
			var rawlisting = new List<string>();
			string listcmd = null;

			// read flags
			var isIncludeSelf = options.HasFlag(FtpListOption.IncludeSelfAndParent);
			var isNameList = options.HasFlag(FtpListOption.NameList);
			var isRecursive = options.HasFlag(FtpListOption.Recursive) && RecursiveList;
			var isGetModified = options.HasFlag(FtpListOption.Modify);
			var isGetSize = options.HasFlag(FtpListOption.Size);

			path = await GetAbsolutePathAsync(path, token);

			// MLSD provides a machine readable format with 100% accurate information
			// so always prefer MLSD over LIST unless the caller of this method overrides it with the ForceList option
			bool machineList;
			CalculateGetListingCommand(path, options, out listcmd, out machineList);

			// read in raw file listing
			rawlisting = await GetListingInternalAsync(listcmd, options, true, token);

			FtpListItem item = null;

			for (var i = 0; i < rawlisting.Count; i++) {
				string rawEntry = rawlisting[i];

				// break if task is cancelled
				token.ThrowIfCancellationRequested();

				if (!isNameList) {

					// load basic information available within the file listing
					if (!LoadBasicListingInfo(ref path, ref item, lst, rawlisting, ref i, listcmd, rawEntry, isRecursive, isIncludeSelf, machineList)) {

						// skip unwanted listings
						continue;
					}

				}

				item = await GetListingProcessItemAsync(item, lst, rawEntry, listcmd, token,
					isIncludeSelf, isNameList, isRecursive, isGetModified, isGetSize
				);
				if (item != null) {
					yield return item;
				}
			}
		}

		/// <summary>
		/// Gets a file listing from the server asynchronously. Each <see cref="FtpListItem"/> object returned
		/// contains information about the file that was able to be retrieved. 
		/// </summary>
		/// <remarks>
		/// If a <see cref="DateTime"/> property is equal to <see cref="DateTime.MinValue"/> then it means the 
		/// date in question was not able to be retrieved. If the <see cref="FtpListItem.Size"/> property
		/// is equal to 0, then it means the size of the object could also not
		/// be retrieved.
		/// </remarks>
		/// <param name="path">The path to list</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <param name="enumToken">The token that can be used to cancel the enumerator</param>
		/// <returns>An array of items retrieved in the listing</returns>
		public IAsyncEnumerable<FtpListItem> GetListingAsyncEnumerable(string path, CancellationToken token = default(CancellationToken), CancellationToken enumToken = default(CancellationToken)) {
			return GetListingAsyncEnumerable(path, 0, token, enumToken);
		}

		/// <summary>
		/// Gets a file listing from the server asynchronously. Each <see cref="FtpListItem"/> object returned
		/// contains information about the file that was able to be retrieved. 
		/// </summary>
		/// <remarks>
		/// If a <see cref="DateTime"/> property is equal to <see cref="DateTime.MinValue"/> then it means the 
		/// date in question was not able to be retrieved. If the <see cref="FtpListItem.Size"/> property
		/// is equal to 0, then it means the size of the object could also not
		/// be retrieved.
		/// </remarks>
		/// <returns>An array of items retrieved in the listing</returns>
		public IAsyncEnumerable<FtpListItem> GetListingAsyncEnumerable(CancellationToken token = default(CancellationToken), CancellationToken enumToken = default(CancellationToken)) {
			return GetListingAsyncEnumerable(null, token, enumToken);
		}

#endif


#if ASYNC
		/// <summary>
		/// Gets a file listing from the server asynchronously. Each <see cref="FtpListItem"/> object returned
		/// contains information about the file that was able to be retrieved. 
		/// </summary>
		/// <remarks>
		/// If a <see cref="DateTime"/> property is equal to <see cref="DateTime.MinValue"/> then it means the 
		/// date in question was not able to be retrieved. If the <see cref="FtpListItem.Size"/> property
		/// is equal to 0, then it means the size of the object could also not
		/// be retrieved.
		/// </remarks>
		/// <param name="path">The path to list</param>
		/// <param name="options">Options that dictate how the list operation is performed</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <returns>An array of items retrieved in the listing</returns>
		public async Task<FtpListItem[]> GetListingAsync(string path, FtpListOption options, CancellationToken token = default(CancellationToken)) {

			// start recursive process if needed and unsupported by the server
			if (options.HasFlag(FtpListOption.Recursive) && !IsServerSideRecursionSupported(options)) {
				return await GetListingRecursiveAsync(await GetAbsolutePathAsync(path, token), options, token);
			}

			// FIX : #768 NullOrEmpty is valid, means "use working directory".
			if (!string.IsNullOrEmpty(path)) {
				path = path.GetFtpPath();
			}

			LogFunc(nameof(GetListingAsync), new object[] { path, options });

			var lst = new List<FtpListItem>();
			var rawlisting = new List<string>();
			string listcmd = null;

			// read flags
			var isIncludeSelf = options.HasFlag(FtpListOption.IncludeSelfAndParent);
			var isNameList = options.HasFlag(FtpListOption.NameList);
			var isRecursive = options.HasFlag(FtpListOption.Recursive) && RecursiveList;
			var isGetModified = options.HasFlag(FtpListOption.Modify);
			var isGetSize = options.HasFlag(FtpListOption.Size);

			path = await GetAbsolutePathAsync(path, token);

			// MLSD provides a machine readable format with 100% accurate information
			// so always prefer MLSD over LIST unless the caller of this method overrides it with the ForceList option
			bool machineList;
			CalculateGetListingCommand(path, options, out listcmd, out machineList);

			// read in raw file listing
			rawlisting = await GetListingInternalAsync(listcmd, options, true, token);

			FtpListItem item = null;

			for (var i = 0; i < rawlisting.Count; i++) {
				string rawEntry = rawlisting[i];

				// break if task is cancelled
				token.ThrowIfCancellationRequested();

				if (!isNameList) {

					// load basic information available within the file listing
					if (!LoadBasicListingInfo(ref path, ref item, lst, rawlisting, ref i, listcmd, rawEntry, isRecursive, isIncludeSelf, machineList)) {

						// skip unwanted listings
						continue;
					}

				}

				item = await GetListingProcessItemAsync(item, lst, rawEntry, listcmd, token,
					isIncludeSelf, isNameList, isRecursive, isGetModified, isGetSize
				);

			}
			return lst.ToArray();
		}

		protected async Task<FtpListItem> GetListingProcessItemAsync(FtpListItem item, List<FtpListItem> lst, string rawEntry, string listcmd, CancellationToken token, bool isIncludeSelf, bool isNameList, bool isRecursive, bool isGetModified, bool isGetSize) {

			if (isNameList) {
				// if NLST was used we only have a file name so
				// there is nothing to parse.
				item = new FtpListItem() {
					FullName = rawEntry
				};

				if (await DirectoryExistsAsync(item.FullName, token)) {
					item.Type = FtpObjectType.Directory;
				}
				else {
					item.Type = FtpObjectType.File;
				}
				lst.Add(item);
			}

			// load extended information that wasn't available if the list options flags say to do so.
			if (item != null) {

				// if need to get file modified date
				if (isGetModified && HasFeature(FtpCapability.MDTM)) {
					// if the modified date was not loaded or the modified date is more than a day in the future 
					// and the server supports the MDTM command, load the modified date.
					// most servers do not support retrieving the modified date
					// of a directory but we try any way.
					if (item.Modified == DateTime.MinValue || listcmd.StartsWith("LIST")) {
						DateTime modify;

						if (item.Type == FtpObjectType.Directory) {
							LogStatus(FtpTraceLevel.Verbose, "Trying to retrieve modification time of a directory, some servers don't like this...");
						}

						if ((modify = await GetModifiedTimeAsync(item.FullName, token: token)) != DateTime.MinValue) {
							item.Modified = modify;
						}
					}
				}

				// if need to get file size
				if (isGetSize && HasFeature(FtpCapability.SIZE)) {
					// if no size was parsed, the object is a file and the server
					// supports the SIZE command, then load the file size
					if (item.Size == -1) {
						if (item.Type != FtpObjectType.Directory) {
							item.Size = await GetFileSizeAsync(item.FullName, -1, token);
						}
						else {
							item.Size = 0;
						}
					}
				}
			}

			return item;
		}

		/// <summary>
		/// Get the records of a file listing and retry if temporary failure.
		/// </summary>
		protected async Task<List<string>> GetListingInternalAsync(string listcmd, FtpListOption options, bool retry, CancellationToken token) {
			var rawlisting = new List<string>();
			var isUseStat = options.HasFlag(FtpListOption.UseStat);

			// always get the file listing in binary to avoid character translation issues with ASCII.
			await SetDataTypeNoLockAsync(ListingDataType, token);

			try {

				// read in raw file listing from control stream
				if (isUseStat) {
					var reply = await ExecuteAsync(listcmd, token);
					if (reply.Success) {

						LogLine(FtpTraceLevel.Verbose, "+---------------------------------------+");

						foreach (var line in reply.InfoMessages.Split('\n')) {
							if (!Strings.IsNullOrWhiteSpace(line)) {
								rawlisting.Add(line);
								LogLine(FtpTraceLevel.Verbose, "Listing:  " + line);
							}
						}

						LogLine(FtpTraceLevel.Verbose, "-----------------------------------------");
					}
				}
				else {

					// read in raw file listing from data stream
					using (FtpDataStream stream = await OpenDataStreamAsync(listcmd, 0, token)) {
						try {
							LogLine(FtpTraceLevel.Verbose, "+---------------------------------------+");

							if (BulkListing) {
								// increases performance of GetListing by reading multiple lines of the file listing at once
								foreach (var line in await stream.ReadAllLinesAsync(Encoding, BulkListingLength, token)) {
									if (!Strings.IsNullOrWhiteSpace(line)) {
										rawlisting.Add(line);
										LogLine(FtpTraceLevel.Verbose, "Listing:  " + line);
									}
								}
							}
							else {
								// GetListing will read file listings line-by-line (actually byte-by-byte)
								string buf;
								while ((buf = await stream.ReadLineAsync(Encoding, token)) != null) {
									if (buf.Length > 0) {
										rawlisting.Add(buf);
										LogLine(FtpTraceLevel.Verbose, "Listing:  " + buf);
									}
								}
							}

							LogLine(FtpTraceLevel.Verbose, "-----------------------------------------");
						}
						finally {
							stream.Close();
						}
					}
				}
			}
			catch (FtpMissingSocketException) {
				// Some FTP server does not send any response when listing an empty directory
				// and the connection fails because no communication socket is provided by the server
			}
			catch (FtpCommandException ftpEx) {
				// Fix for #589 - CompletionCode is null
				if (ftpEx.CompletionCode == null) {
					throw new FtpException(ftpEx.Message + " - Try using FtpListOption.UseStat which might fix this.", ftpEx);
				}
				// Some FTP servers throw 550 for empty folders. Absorb these.
				if (!ftpEx.CompletionCode.StartsWith("550")) {
					throw ftpEx;
				}
			}
			catch (IOException ioEx) {
				// Some FTP servers forcibly close the connection, we absorb these errors

				// Fix #410: Retry if its a temporary failure ("Received an unexpected EOF or 0 bytes from the transport stream")
				if (retry && ioEx.Message.IsKnownError(ServerStringModule.unexpectedEOF)) {
					// retry once more, but do not go into a infinite recursion loop here
					LogLine(FtpTraceLevel.Verbose, "Warning:  Retry GetListing once more due to unexpected EOF");
					return await GetListingInternalAsync(listcmd, options, false, token);
				}
				else {
					// suppress all other types of exceptions
				}
			}

			return rawlisting;
		}

		/// <summary>
		/// Gets a file listing from the server asynchronously. Each <see cref="FtpListItem"/> object returned
		/// contains information about the file that was able to be retrieved. 
		/// </summary>
		/// <remarks>
		/// If a <see cref="DateTime"/> property is equal to <see cref="DateTime.MinValue"/> then it means the 
		/// date in question was not able to be retrieved. If the <see cref="FtpListItem.Size"/> property
		/// is equal to 0, then it means the size of the object could also not
		/// be retrieved.
		/// </remarks>
		/// <param name="path">The path to list</param>
		/// <param name="token">The token that can be used to cancel the entire process</param>
		/// <param name="enumToken">The token that can be used to cancel the enumerator</param>
		/// <returns>An array of items retrieved in the listing</returns>
		public Task<FtpListItem[]> GetListingAsync(string path, CancellationToken token = default(CancellationToken)) {
			return GetListingAsync(path, 0, token);
		}


		/// <summary>
		/// Gets a file listing from the server asynchronously. Each <see cref="FtpListItem"/> object returned
		/// contains information about the file that was able to be retrieved. 
		/// </summary>
		/// <remarks>
		/// If a <see cref="DateTime"/> property is equal to <see cref="DateTime.MinValue"/> then it means the 
		/// date in question was not able to be retrieved. If the <see cref="FtpListItem.Size"/> property
		/// is equal to 0, then it means the size of the object could also not
		/// be retrieved.
		/// </remarks>
		/// <returns>An array of items retrieved in the listing</returns>
		public Task<FtpListItem[]> GetListingAsync(CancellationToken token = default(CancellationToken)) {
			return GetListingAsync(null, token);
		}

#endif

#if NET50_OR_LATER
		/// <summary>
		/// Recursive method of GetListingAsync, to recurse through directories on servers that do not natively support recursion.
		/// Automatically called by GetListingAsync where required.
		/// Uses flat recursion instead of head recursion.
		/// </summary>
		/// <param name="path">The path of the directory to list</param>
		/// <param name="options">Options that dictate how a list is performed and what information is gathered.</param>
		/// <param name="token"></param>
		/// <param name="enumToken"></param>
		/// <returns>An array of FtpListItem objects</returns>

		protected async IAsyncEnumerable<FtpListItem> GetListingRecursiveAsyncEnumerable(string path, FtpListOption options, CancellationToken token, [EnumeratorCancellation] CancellationToken enumToken = default) {
			// remove the recursive flag
			options &= ~FtpListOption.Recursive;

			// add initial path to list of folders to explore
			var stack = new Stack<string>();
			stack.Push(path);
			var allFiles = new List<FtpListItem>();

			// explore folders
			while (stack.Count > 0) {
				// get path of folder to list
				var currentPath = stack.Pop();
				if (!currentPath.EndsWith("/")) {
					currentPath += "/";
				}

				// extract the directories
				await foreach (var item in GetListingAsyncEnumerable(currentPath, options, token)) {
					// break if task is cancelled
					token.ThrowIfCancellationRequested();

					if (item.Type == FtpObjectType.Directory && item.Name != "." && item.Name != "..") {
						stack.Push(item.FullName);
					}

					yield return item;
				}

				// recurse
			}
		}
#endif


#if ASYNC
		/// <summary>
		/// Recursive method of GetListingAsync, to recurse through directories on servers that do not natively support recursion.
		/// Automatically called by GetListingAsync where required.
		/// Uses flat recursion instead of head recursion.
		/// </summary>
		/// <param name="path">The path of the directory to list</param>
		/// <param name="options">Options that dictate how a list is performed and what information is gathered.</param>
		/// <param name="token"></param>
		/// <param name="enumToken"></param>
		/// <returns>An array of FtpListItem objects</returns>
		protected async Task<FtpListItem[]> GetListingRecursiveAsync(string path, FtpListOption options, CancellationToken token) {

			// remove the recursive flag
			options &= ~FtpListOption.Recursive;

			// add initial path to list of folders to explore
			var stack = new Stack<string>();
			stack.Push(path);
			var allFiles = new List<FtpListItem>();

			// explore folders
			while (stack.Count > 0) {
				// get path of folder to list
				var currentPath = stack.Pop();
				if (!currentPath.EndsWith("/")) {
					currentPath += "/";
				}

				// list it
				FtpListItem[] items = await GetListingAsync(currentPath, options, token);

				// break if task is cancelled
				token.ThrowIfCancellationRequested();

				// add it to the final listing
				allFiles.AddRange(items);

				// extract the directories
				foreach (var item in items) {
					if (item.Type == FtpObjectType.Directory && item.Name != "." && item.Name != "..") {
						stack.Push(item.FullName);
					}
				}

				items = null;

				// recurse
			}

			// final list of all files and dirs
			return allFiles.ToArray();
		}
#endif

	}
}