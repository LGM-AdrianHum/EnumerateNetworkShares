//  _______          __                       __    
//  \      \   _____/  |___  _  _____________|  | __
//  /   |   \_/ __ \   __\ \/ \/ /  _ \_  __ \  |/ /
// /    |    \  ___/|  |  \     (  <_> )  | \/    < 
// \____|__  /\___  >__|   \/\_/ \____/|__|  |__|_ \
//         \/     \/                              \/
//   _________.__                                   
//  /   _____/|  |__ _____ _______   ____   ______  
//  \_____  \ |  |  \\__  \\_  __ \_/ __ \ /  ___/  
//  /        \|   Y  \/ __ \|  | \/\  ___/ \___ \   
// /_______  /|___|  (____  /__|    \___  >____  >  
//         \/      \/     \/            \/     \/   
// File: EnumShares/EnumShares/Shares.cs
// User: Adrian Hum/
// 
// Created:  2017-10-22 11:51 PM
// Modified: 2017-10-23 12:17 AM

using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;

namespace EnumShares
{
    #region Share Type

    /// <summary>
    ///     Type of share
    /// </summary>
    [Flags]
    public enum ShareType
    {
        /// <summary>Disk share</summary>
        Disk = 0,

        /// <summary>Printer share</summary>
        Printer = 1,

        /// <summary>Device share</summary>
        Device = 2,

        /// <summary>IPC share</summary>
        Ipc = 3,

        /// <summary>Special share</summary>
        Special = -2147483648 // 0x80000000,
    }

    #endregion

    #region Share

    /// <summary>
    ///     Information about a local share
    /// </summary>
    public class Share
    {
        #region Constructor

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="server"></param>
        /// <param name="netName"></param>
        /// <param name="path"></param>
        /// <param name="shareType"></param>
        /// <param name="remark"></param>
        public Share(string server, string netName, string path, ShareType shareType, string remark)
        {
            if (ShareType.Special == shareType && "IPC$" == netName)
                shareType |= ShareType.Ipc;

            Server = server;
            NetName = netName;
            Path = path;
            ShareType = shareType;
            Remark = remark;
        }

        #endregion

        /// <summary>
        ///     Returns the path to this share
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (string.IsNullOrEmpty(Server))
                return $@"\\{Environment.MachineName}\{NetName}";
            return $@"\\{Server}\{NetName}";
        }

        /// <summary>
        ///     Returns true if this share matches the local path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool MatchesPath(string path)
        {
            if (!IsFileSystem) return false;
            if (string.IsNullOrEmpty(path)) return true;

            return path.ToLower().StartsWith(Path.ToLower());
        }

        #region Private data

        #endregion

        #region Properties

        /// <summary>
        ///     The name of the computer that this share belongs to
        /// </summary>
        public string Server { get; }

        /// <summary>
        ///     Share name
        /// </summary>
        public string NetName { get; }

        /// <summary>
        ///     Local path
        /// </summary>
        public string Path { get; }

        /// <summary>
        ///     Share type
        /// </summary>
        public ShareType ShareType { get; }

        /// <summary>
        ///     Comment
        /// </summary>
        public string Remark { get; }

        /// <summary>
        ///     Returns true if this is a file system share
        /// </summary>
        public bool IsFileSystem
        {
            get
            {
                // Shared device
                if (0 != (ShareType & ShareType.Device)) return false;
                // IPC share
                if (0 != (ShareType & ShareType.Ipc)) return false;
                // Shared printer
                if (0 != (ShareType & ShareType.Printer)) return false;

                // Standard disk share
                if (0 == (ShareType & ShareType.Special)) return true;

                // Special disk share (e.g. C$)
                if (ShareType.Special == ShareType && !string.IsNullOrEmpty(NetName))
                    return true;
                return false;
            }
        }

        /// <summary>
        ///     Get the root of a disk-based share
        /// </summary>
        public DirectoryInfo Root
        {
            get
            {
                if (!IsFileSystem) return null;
                if (string.IsNullOrEmpty(Server))
                    if (string.IsNullOrEmpty(Path))
                        return new DirectoryInfo(ToString());
                    else
                        return new DirectoryInfo(Path);
                return new DirectoryInfo(ToString());
            }
        }

        #endregion
    }

    #endregion

    #region Share Collection

    /// <summary>
    ///     A collection of shares
    /// </summary>
    public class ShareCollection : ReadOnlyCollectionBase
    {
        #region Private Data

        /// <summary>The name of the server this collection represents</summary>
        private readonly string _server;

        #endregion

        #region Implementation of ICollection

        /// <summary>
        ///     Copy this collection to an array
        /// </summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        public void CopyTo(Share[] array, int index)
        {
            InnerList.CopyTo(array, index);
        }

        #endregion

        #region Platform

        /// <summary>
        ///     Is this an NT platform?
        /// </summary>
        protected static bool IsNt => PlatformID.Win32NT == Environment.OSVersion.Platform;

        /// <summary>
        ///     Returns true if this is Windows 2000 or higher
        /// </summary>
        protected static bool IsW2KUp
        {
            get
            {
                var os = Environment.OSVersion;
                if (PlatformID.Win32NT == os.Platform && os.Version.Major >= 5)
                    return true;
                return false;
            }
        }

        #endregion

        #region Interop

        #region Constants

        /// <summary>Maximum path length</summary>
        protected const int MaxPath = 260;

        /// <summary>No error</summary>
        protected const int NoError = 0;

        /// <summary>Access denied</summary>
        protected const int ErrorAccessDenied = 5;

        /// <summary>Access denied</summary>
        protected const int ErrorWrongLevel = 124;

        /// <summary>More data available</summary>
        protected const int ErrorMoreData = 234;

        /// <summary>Not connected</summary>
        protected const int ErrorNotConnected = 2250;

        /// <summary>Level 1</summary>
        protected const int UniversalNameInfoLevel = 1;

        /// <summary>Max extries (9x)</summary>
        protected const int MaxSi50Entries = 20;

        #endregion

        #region Structures

        /// <summary>Unc name</summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        protected struct UniversalNameInfo
        {
            [MarshalAs(UnmanagedType.LPTStr)] public string lpUniversalName;
        }

        /// <summary>Share information, NT, level 2</summary>
        /// <remarks>
        ///     Requires admin rights to work.
        /// </remarks>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        protected struct ShareInfo2
        {
            [MarshalAs(UnmanagedType.LPWStr)] public string NetName;
            public ShareType ShareType;
            [MarshalAs(UnmanagedType.LPWStr)] public string Remark;
            public int Permissions;
            public int MaxUsers;
            public int CurrentUsers;
            [MarshalAs(UnmanagedType.LPWStr)] public string Path;
            [MarshalAs(UnmanagedType.LPWStr)] public string Password;
        }

        /// <summary>Share information, NT, level 1</summary>
        /// <remarks>
        ///     Fallback when no admin rights.
        /// </remarks>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        protected struct ShareInfo1
        {
            [MarshalAs(UnmanagedType.LPWStr)] public string NetName;
            public ShareType ShareType;
            [MarshalAs(UnmanagedType.LPWStr)] public string Remark;
        }

        /// <summary>Share information, Win9x</summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        protected struct ShareInfo50
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 13)] public string NetName;

            public byte bShareType;
            public ushort Flags;

            [MarshalAs(UnmanagedType.LPTStr)] public string Remark;
            [MarshalAs(UnmanagedType.LPTStr)] public string Path;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 9)] public string PasswordRW;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 9)] public string PasswordRO;

            public ShareType ShareType => (ShareType) (bShareType & 0x7F);
        }

        /// <summary>Share information level 1, Win9x</summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        protected struct ShareInfo1_9X
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 13)] public string NetName;
            public byte Padding;

            public ushort bShareType;

            [MarshalAs(UnmanagedType.LPTStr)] public string Remark;

            public ShareType ShareType => (ShareType) (bShareType & 0x7FFF);
        }

        #endregion

        #region Functions

        /// <summary>Get a UNC name</summary>
        [DllImport("mpr", CharSet = CharSet.Auto)]
        protected static extern int WNetGetUniversalName(string lpLocalPath,
            int dwInfoLevel, ref UniversalNameInfo lpBuffer, ref int lpBufferSize);

        /// <summary>Get a UNC name</summary>
        [DllImport("mpr", CharSet = CharSet.Auto)]
        protected static extern int WNetGetUniversalName(string lpLocalPath,
            int dwInfoLevel, IntPtr lpBuffer, ref int lpBufferSize);

        /// <summary>Enumerate shares (NT)</summary>
        [DllImport("netapi32", CharSet = CharSet.Unicode)]
        protected static extern int NetShareEnum(string lpServerName, int dwLevel,
            out IntPtr lpBuffer, int dwPrefMaxLen, out int entriesRead,
            out int totalEntries, ref int hResume);

        /// <summary>Enumerate shares (9x)</summary>
        [DllImport("svrapi", CharSet = CharSet.Ansi)]
        protected static extern int NetShareEnum(
            [MarshalAs(UnmanagedType.LPTStr)] string lpServerName, int dwLevel,
            IntPtr lpBuffer, ushort cbBuffer, out ushort entriesRead,
            out ushort totalEntries);

        /// <summary>Free the buffer (NT)</summary>
        [DllImport("netapi32")]
        protected static extern int NetApiBufferFree(IntPtr lpBuffer);

        #endregion

        #region Enumerate shares

        /// <summary>
        ///     Enumerates the shares on Windows NT
        /// </summary>
        /// <param name="server">The server name</param>
        /// <param name="shares">The ShareCollection</param>
        protected static void EnumerateSharesNt(string server, ShareCollection shares)
        {
            var level = 2;
            var hResume = 0;
            var pBuffer = IntPtr.Zero;

            try
            {
                int entriesRead;
                int totalEntries;
                var nRet = NetShareEnum(server, level, out pBuffer, -1,
                    out entriesRead, out totalEntries, ref hResume);

                if (ErrorAccessDenied == nRet)
                {
                    //Need admin for level 2, drop to level 1
                    level = 1;
                    nRet = NetShareEnum(server, level, out pBuffer, -1,
                        out entriesRead, out totalEntries, ref hResume);
                }

                if (NoError == nRet && entriesRead > 0)
                {
                    var t = 2 == level ? typeof(ShareInfo2) : typeof(ShareInfo1);
                    var offset = Marshal.SizeOf(t);

                    for (int i = 0, lpItem = pBuffer.ToInt32(); i < entriesRead; i++, lpItem += offset)
                    {
                        var pItem = new IntPtr(lpItem);
                        if (1 == level)
                        {
                            var si = (ShareInfo1) Marshal.PtrToStructure(pItem, t);
                            shares.Add(si.NetName, string.Empty, si.ShareType, si.Remark);
                        }
                        else
                        {
                            var si = (ShareInfo2) Marshal.PtrToStructure(pItem, t);
                            shares.Add(si.NetName, si.Path, si.ShareType, si.Remark);
                        }
                    }
                }
            }
            finally
            {
                // Clean up buffer allocated by system
                if (IntPtr.Zero != pBuffer)
                    NetApiBufferFree(pBuffer);
            }
        }

        /// <summary>
        ///     Enumerates the shares on Windows 9x
        /// </summary>
        /// <param name="server">The server name</param>
        /// <param name="shares">The ShareCollection</param>
        protected static void EnumerateShares9X(string server, ShareCollection shares)
        {
            var level = 50;

            var t = typeof(ShareInfo50);
            var size = Marshal.SizeOf(t);
            var cbBuffer = (ushort) (MaxSi50Entries * size);
            //On Win9x, must allocate buffer before calling API
            var pBuffer = Marshal.AllocHGlobal(cbBuffer);

            try
            {
                ushort entriesRead;
                ushort totalEntries;
                var nRet = NetShareEnum(server, level, pBuffer, cbBuffer,
                    out entriesRead, out totalEntries);

                if (ErrorWrongLevel == nRet)
                {
                    level = 1;
                    t = typeof(ShareInfo1_9X);
                    size = Marshal.SizeOf(t);

                    nRet = NetShareEnum(server, level, pBuffer, cbBuffer,
                        out entriesRead, out totalEntries);
                }

                if (NoError == nRet || ErrorMoreData == nRet)
                    for (int i = 0, lpItem = pBuffer.ToInt32(); i < entriesRead; i++, lpItem += size)
                    {
                        var pItem = new IntPtr(lpItem);

                        if (1 == level)
                        {
                            var si = (ShareInfo1_9X) Marshal.PtrToStructure(pItem, t);
                            shares.Add(si.NetName, string.Empty, si.ShareType, si.Remark);
                        }
                        else
                        {
                            var si = (ShareInfo50) Marshal.PtrToStructure(pItem, t);
                            shares.Add(si.NetName, si.Path, si.ShareType, si.Remark);
                        }
                    }
                else
                    Console.WriteLine(nRet);
            }
            finally
            {
                //Clean up buffer
                Marshal.FreeHGlobal(pBuffer);
            }
        }

        /// <summary>
        ///     Enumerates the shares
        /// </summary>
        /// <param name="server">The server name</param>
        /// <param name="shares">The ShareCollection</param>
        protected static void EnumerateShares(string server, ShareCollection shares)
        {
            if (!string.IsNullOrEmpty(server) && !IsW2KUp)
            {
                server = server.ToUpper();

                // On NT4, 9x and Me, server has to start with "\\"
                if (!('\\' == server[0] && '\\' == server[1]))
                    server = @"\\" + server;
            }

            if (IsNt)
                EnumerateSharesNt(server, shares);
            else
                EnumerateShares9X(server, shares);
        }

        #endregion

        #endregion

        #region Static methods

        /// <summary>
        ///     Returns true if fileName is a valid local file-name of the form:
        ///     X:\, where X is a drive letter from A-Z
        /// </summary>
        /// <param name="fileName">The filename to check</param>
        /// <returns></returns>
        public static bool IsValidFilePath(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return false;

            var drive = char.ToUpper(fileName[0]);
            if ('A' > drive || drive > 'Z')
                return false;

            if (Path.VolumeSeparatorChar != fileName[1])
                return false;
            if (Path.DirectorySeparatorChar != fileName[2])
                return false;
            return true;
        }

        /// <summary>
        ///     Returns the UNC path for a mapped drive or local share.
        /// </summary>
        /// <param name="fileName">The path to map</param>
        /// <returns>The UNC path (if available)</returns>
        public static string PathToUnc(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return string.Empty;

            fileName = Path.GetFullPath(fileName);
            if (!IsValidFilePath(fileName)) return fileName;

            var rni = new UniversalNameInfo();
            var bufferSize = Marshal.SizeOf(rni);

            var nRet = WNetGetUniversalName(
                fileName, UniversalNameInfoLevel,
                ref rni, ref bufferSize);

            if (ErrorMoreData == nRet)
            {
                var pBuffer = Marshal.AllocHGlobal(bufferSize);

                try
                {
                    nRet = WNetGetUniversalName(
                        fileName, UniversalNameInfoLevel,
                        pBuffer, ref bufferSize);

                    if (NoError == nRet)
                        rni = (UniversalNameInfo) Marshal.PtrToStructure(pBuffer,
                            typeof(UniversalNameInfo));
                }
                finally
                {
                    Marshal.FreeHGlobal(pBuffer);
                }
            }

            switch (nRet)
            {
                case NoError:
                    return rni.lpUniversalName;

                case ErrorNotConnected:
                    //Local file-name
                    var shi = LocalShares;
                    var share = shi?[fileName];
                    if (share == null) return fileName;
                    var path = share.Path;
                    if (string.IsNullOrEmpty(path)) return fileName;
                    var index = path.Length;
                    if (Path.DirectorySeparatorChar != path[path.Length - 1])
                        index++;

                    fileName = index < fileName.Length ? fileName.Substring(index) : string.Empty;

                    fileName = Path.Combine(share.ToString(), fileName);

                    return fileName;

                default:
                    Console.WriteLine("Unknown return value: {0}", nRet);
                    return string.Empty;
            }
        }

        /// <summary>
        ///     Returns the local <see cref="Share" /> object with the best match
        ///     to the specified path.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static Share PathToShare(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;

            fileName = Path.GetFullPath(fileName);
            if (!IsValidFilePath(fileName)) return null;

            var shi = LocalShares;
            return shi?[fileName];
        }

        #endregion

        #region Local shares

        /// <summary>The local shares</summary>
        private static ShareCollection _local;

        /// <summary>
        ///     Return the local shares
        /// </summary>
        public static ShareCollection LocalShares => _local ?? (_local = new ShareCollection());

        /// <summary>
        ///     Return the shares for a specified machine
        /// </summary>
        /// <param name="server"></param>
        /// <returns></returns>
        public static ShareCollection GetShares(string server)
        {
            return new ShareCollection(server);
        }

        #endregion

        #region Constructor

        /// <summary>
        ///     Default constructor - local machine
        /// </summary>
        public ShareCollection()
        {
            _server = string.Empty;
            EnumerateShares(_server, this);
        }

        /// <inheritdoc />
        /// <summary>
        ///     Constructor
        /// </summary>
        public ShareCollection(string server)
        {
            _server = server;
            EnumerateShares(_server, this);
        }

        #endregion

        #region Add

        protected void Add(Share share)
        {
            InnerList.Add(share);
        }

        protected void Add(string netName, string path, ShareType shareType, string remark)
        {
            InnerList.Add(new Share(_server, netName, path, shareType, remark));
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Returns the name of the server this collection represents
        /// </summary>
        public string Server => _server;

        /// <summary>
        ///     Returns the <see cref="Share" /> at the specified index.
        /// </summary>
        public Share this[int index] => (Share) InnerList[index];

        /// <summary>
        ///     Returns the <see cref="Share" /> which matches a given local path
        /// </summary>
        /// <param name="path">The path to match</param>
        public Share this[string path]
        {
            get
            {
                if (string.IsNullOrEmpty(path)) return null;

                path = Path.GetFullPath(path);
                if (!IsValidFilePath(path)) return null;

                Share match = null;

                foreach (var t in InnerList)
                {
                    var s = (Share) t;

                    if (!s.IsFileSystem || !s.MatchesPath(path)) continue;
                    if (null == match)
                        match = s;

                    // If this has a longer path,
                    // and this is a disk share or match is a special share, 
                    // then this is a better match
                    else if (match.Path.Length < s.Path.Length)
                        if (ShareType.Disk == s.ShareType || ShareType.Disk != match.ShareType)
                            match = s;
                }

                return match;
            }
        }

        #endregion
    }

    #endregion
}