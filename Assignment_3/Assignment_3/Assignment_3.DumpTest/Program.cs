﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HANDLE = System.IntPtr;

namespace Assignment_3.DumpTest
{
    class Program
    {
        private static AutoResetEvent AUTO_EVENT  =new AutoResetEvent(false);
        //Run this project in orer to take a dump
        //Wait functions -> https://msdn.microsoft.com/en-us/library/windows/desktop/ms687069(v=vs.85).aspx
        static void Main(string[] args)
        {

            //Console.WriteLine("Calling native methods... :)");

            
            ////WaitHandle wh = new System.Threading.EventWaitHandle(true, EventResetMode.AutoReset);

            var autoEvent = new AutoResetEvent(false);

            ////WaitForSingleObject
            //Console.WriteLine("Calling WaitForSingleObject");
            //int result = WaitForMultipleObjects(autoEvent.Handle, WAIT_TIMEOUT);
            //int result2 = WaitForSingleObject(autoEvent.Handle, WAIT_TIMEOUT);

            ////WaitForMultipleObjects

            ////Console.WriteLine("Calling WaitForMultipleObjects");

            HANDLE[] arr = new HANDLE[3];
            for (int i = 0; i < 3; i++)
            {
                var loopAutoEvent = new AutoResetEvent(false);
                arr[i] = loopAutoEvent.Handle;
            }
            var mulRes = WaitForMultipleObjects(3, arr, true, 9999999);

            var mulRes2 = WaitForMultipleObjects(1, new HANDLE[] { AUTO_EVENT.Handle }, true, 9999999);


            //Console.ReadKey();
        }

        private static void TestResetEvent()
        {
            var autoEvent = new AutoResetEvent(false);

            var task = new Thread(new ThreadStart(() =>
            {
                Thread.Sleep(3000);
                autoEvent.Set();
            }));

            task.Start();
            autoEvent.WaitOne();
        }

        [DllImport("kernel32.dll")]
        static extern uint WaitForMultipleObjects(uint nCount, IntPtr[] lpHandles, bool bWaitAll, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto)]
        public static extern HANDLE CreateEvent(HANDLE lpEventAttributes, [In, MarshalAs(UnmanagedType.Bool)] bool bManualReset, [In, MarshalAs(UnmanagedType.Bool)] bool bIntialState, [In, MarshalAs(UnmanagedType.BStr)] string lpName);

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(HANDLE hObject);

        //Sigle WAit////////////////////////////////////////////////////////////
        //http://www.pinvoke.net/default.aspx/coredll/WaitForSingleObject.html
        ////////////////////////////////////////////////////////////////////////
        //[DllImport("coredll.dll", SetLastError = true)]
        //public static extern Int32 WaitForSingleObject(IntPtr Handle, Int32 Wait);


        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern Int32 WaitForSingleObject(IntPtr Handle, Int32 Wait);

        public const Int32 INFINITE = -1;
        public const Int32 WAIT_ABANDONED = 0x80;
        public const Int32 WAIT_OBJECT_0 = 0x00;
        public const Int32 WAIT_TIMEOUT = 0x102;
        public const Int32 WAIT_FAILED = -1;

    }
}