using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Minimizer
{
    class Program
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hWnd);


        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr i);
        public delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool EnumThreadWindows(uint dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetWindowTextLength(IntPtr hWnd);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        private const int SW_SHOWNORMAL = 1;
        private const int SW_SHOWMINIMIZED = 2;

        private const int SW_SHOWMAXIMIZED = 3;

        public delegate bool EnumWindowProc(IntPtr hWnd, IntPtr parameter);

        static void Main(string[] args)
        {
            Minimize();
            Console.ReadKey();
            Minimize();
            Console.ReadKey();
        }

        static void Minimize()
        {
            string SkypeTitulo = "";

            IntPtr hWnd = default(IntPtr);
            foreach (Process p in Process.GetProcessesByName("skype"))
            {

                @hWnd = p.MainWindowHandle;
                if (!hWnd.Equals(IntPtr.Zero))
                {
                    //ShowWindowAsync(hWnd, SW_SHOWMINIMIZED);
                    //GetChildWindows(hWnd);
                    EnumerateProcessWindows(p);
                }
            }
            if (_minimize && _main != IntPtr.Zero)
                ShowWindowAsync(_main, SW_SHOWMINIMIZED);
            else
                ShowWindowAsync(_main, SW_SHOWNORMAL);
        }

        static void EnumerateProcessWindows(Process proc)
        {
            foreach (ProcessThread pt in proc.Threads)
            {
                EnumThreadWindows((uint)pt.Id, new EnumThreadDelegate(EnumThreadCallback), IntPtr.Zero);
            }
        }

        static bool EnumThreadCallback(IntPtr hWnd, IntPtr lParam)
        {
            WINDOWPLACEMENT wp = GetPlacement(hWnd);
            string name = GetText(hWnd);
            string className = GetClassName(hWnd);
            System.Diagnostics.Debug.WriteLine(string.Format("{0};{1};{2};{3};{4};{5};{6}", name
                , className
                , wp.showCmd
                , wp.flags
                , wp.ptMaxPosition
                , wp.ptMinPosition
                , wp.rcNormalPosition));


            // This will minimize the Skype window
            if (className.Equals("tSkMainForm", StringComparison.CurrentCultureIgnoreCase))
            {
                _main = hWnd;
                //ShowWindowAsync(hWnd, SW_SHOWMINIMIZED);
            }
            GetChildWindows(hWnd);
            return true;
        }

        static IntPtr _main;
        static bool _minimize;
        public static List<IntPtr> GetChildWindows(IntPtr parent)
        {
            List<IntPtr> result = new List<IntPtr>();
            GCHandle listHandle = GCHandle.Alloc(result);
            try
            {
                WINDOWPLACEMENT wp = GetPlacement(parent);
                string name = GetText(parent);
                string className = GetClassName(parent);
                System.Diagnostics.Debug.WriteLine(string.Format("{0};{1};{2};{3};{4};{5};{6}", parent
                    , className
                    , wp.showCmd
                    , wp.flags
                    , wp.ptMaxPosition
                    , wp.ptMinPosition
                    , wp.rcNormalPosition));

                if (className.StartsWith("TLiveConversationPanel", StringComparison.CurrentCultureIgnoreCase))
                {
                    EnumWindowProc childProc = new EnumWindowProc(EnumWindow);
                    EnumChildWindows(parent, childProc, GCHandle.ToIntPtr(listHandle));
                    if (result.Count > 0)
                        _minimize = true;
                    else
                        _minimize = false;
                    //foreach(IntPtr child in result)
                    //{
                    //    GetChildWindows(child);
                    //    //ShowWindowAsync(child, SW_SHOWMINIMIZED);
                    //}
                }
                EnumWindowProc proc = new EnumWindowProc(EnumWindow);
                EnumChildWindows(parent, proc, GCHandle.ToIntPtr(listHandle));
                foreach (IntPtr child in result)
                {
                    GetChildWindows(child);
                    //ShowWindowAsync(child, SW_SHOWMINIMIZED);
                }
            }
            finally
            {
                if (listHandle.IsAllocated)
                    listHandle.Free();
            }
            return result;
        }

        public static string GetText(IntPtr hWnd)
        {
            // Allocate correct string length first
            int length = GetWindowTextLength(hWnd);
            StringBuilder sb = new StringBuilder(length + 1);
            GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }


        private static WINDOWPLACEMENT GetPlacement(IntPtr hwnd)
        {
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(placement);
            GetWindowPlacement(hwnd, ref placement);
            return placement;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowPlacement(
            IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        internal struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public ShowWindowCommands showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Rectangle rcNormalPosition;
        }
        
        private static string GetClassName(IntPtr hWnd)
        {
            int nRet;
            // Pre-allocate 256 characters, since this is the maximum class name length.
            StringBuilder ClassName = new StringBuilder(256);
            //Get the window class name
            nRet = GetClassName(hWnd, ClassName, ClassName.Capacity);
            if (nRet != 0)
            {
                return ClassName.ToString();
            }
            else
            {
                return string.Empty;
            }
        }

        internal enum ShowWindowCommands : int
        {
            Hide = 0,
            Normal = 1,
            Minimized = 2,
            Maximized = 3,
        }
        private static bool EnumWindow(IntPtr handle, IntPtr pointer)
        {
            GCHandle gch = GCHandle.FromIntPtr(pointer);
            List<IntPtr> list = gch.Target as List<IntPtr>;
            if (list == null)
                throw new InvalidCastException("GCHandle Target could not be cast as List<IntPtr>");

            list.Add(handle);
            return true;
        }
    }
}
