﻿using Microsoft.Diagnostics.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assignment_3
{
    class Program
    {
    
        const string SOME_86_DUMP = @"C:\temp\dumps\Assignment_3.dmp";

        static void Main(string[] args)
        {
            ThreadStackHandler handler = new ThreadStackHandler();


            if (File.Exists(SOME_86_DUMP))
            {
                //Loading a crash dump
                using (DataTarget target = DataTarget.LoadCrashDump(SOME_86_DUMP))
                {
                    var runtime = target.ClrVersions[0].CreateRuntime();

                    foreach (ClrThread thread in runtime.Threads)
                    {

                        handler.Handle(target.DebuggerInterface, thread, runtime);
                        break;
                    }
                }

            }

            Console.ReadKey();
        }
    }

}
