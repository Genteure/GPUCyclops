using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace SharedMemory
{
    public enum MemAccess
    {
        FILE_MAP_ALL_ACCESS = 0x1f,
        FILE_MAP_COPY = 1,
        FILE_MAP_EXECUTE = 0x20,
        FILE_MAP_MASK = -1,
        FILE_MAP_READ = 4,
        FILE_MAP_WRITE = 2
    }

    public enum MemProtection
    {
        PAGE_EXECUTE_READ = 0x20,
        PAGE_EXECUTE_READWRITE = 0x40,
        PAGE_MASK = -1,
        PAGE_READONLY = 2,
        PAGE_READWRITE = 4,
        PAGE_WRITECOPY = 8
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode), ComVisible(false)]
    public class SECURITY_ATTRIBUTES
    {
        public int nLength;
        public SECURITY_DESCRIPTOR lpSecurityDescriptor;
        public bool bInheritHandle;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode), ComVisible(false)]
    public class SECURITY_DESCRIPTOR
    {
        public byte Revision;
        public byte Sbz1;
        public ushort Control;
        public IntPtr Owner;
        public IntPtr Group;
        public IntPtr Sacl;
        public IntPtr Dacl;
    }

    public class GlobalMemClass
    {
        // Fields
        private static IntPtr hMap;
        private static IntPtr pMem;
        public const int VideoFrameSize = 0x151800;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern bool CloseHandle(IntPtr hFileMapping);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern IntPtr CreateFileMappingW(IntPtr hFile, SECURITY_ATTRIBUTES lpAttributes, [In, MarshalAs(UnmanagedType.U4)] MemProtection flProtect, int dwMaximumSizeHi, int dwMaximumSizeLo, string lpName);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern bool InitializeSecurityDescriptor([Out, MarshalAs(UnmanagedType.LPStruct)] SECURITY_DESCRIPTOR pSecurityDescriptor, uint dwRevision);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern IntPtr MapViewOfFile(IntPtr hFileMapping, [In, MarshalAs(UnmanagedType.U4)] MemAccess dwDesiredAccess, int dwFileOffsetHi, int dwFileOffsetLo, int dwNumberOfBytesToMap);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern IntPtr OpenFileMappingW([In, MarshalAs(UnmanagedType.U4)] MemAccess dwDesiredAccess, [In, MarshalAs(UnmanagedType.U1)] bool bInheritHandle, string lpName);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern bool SetSecurityDescriptorDacl([Out, MarshalAs(UnmanagedType.LPStruct)] SECURITY_DESCRIPTOR pSecurityDescriptor, bool bDaclPresent, IntPtr pDacl, bool pDaclDefaulted);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern bool UnmapViewOfFile(IntPtr pMem);


        // Methods
        public static bool Cleanup()
        {
            return (UnmapViewOfFile(pMem) && CloseHandle(hMap));
        }

        public static IntPtr CreateSharedMem(string name)
        {
            IntPtr pMem = IntPtr.Zero;
            hMap = OpenFileMappingW(MemAccess.FILE_MAP_WRITE, false, @"Global\" + name);
            if (hMap == IntPtr.Zero)
            {
                throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            pMem = MapViewOfFile(hMap, MemAccess.FILE_MAP_WRITE, 0, 0, 0);
            if (pMem == IntPtr.Zero)
            {
                throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            return pMem;
        }


        public static IntPtr OpenSharedMem(string name)
        {
            hMap = OpenFileMappingW(MemAccess.FILE_MAP_READ, false, @"Global\" + name);
            if (hMap == IntPtr.Zero)
            {
                throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            pMem = MapViewOfFile(hMap, MemAccess.FILE_MAP_READ, 0, 0, 0);
            if (pMem == IntPtr.Zero)
            {
                throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            return pMem;
        }
    }
}

