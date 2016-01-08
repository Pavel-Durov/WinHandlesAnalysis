﻿using Microsoft.Diagnostics.Runtime;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.Runtime.Interop;
using Assignments.Core.msos;
using Assignments.Core.Handlers.WCT;
using Assignments.Core.Model.StackFrames;
using Assignments.Core.Model.Unified;
using System;
using Assignments.Core.Model.WCT;

namespace Assignments.Core.Handlers
{
    public class ThreadStackHandler
    {
        public void Handle(ClrThread thread)
        {

        }

        WctApiHandler _wctApi;


        public WctApiHandler WctApi
        {
            get
            {
                if (_wctApi == null)
                {
                    _wctApi = new WctApiHandler();
                }
                return _wctApi;
            }
            set { _wctApi = value; }
        }

        IDebugClient _debugClient;
        private ClrRuntime _runtime;

        

        public List<UnifiedResult> Handle(IDebugClient debugClient, ClrRuntime runtime)
        {
            _debugClient = debugClient;
            _runtime = runtime;

            uint _numThreads = 0;
            Util.VerifyHr(((IDebugSystemObjects)_debugClient).GetNumberThreads(out _numThreads));

            var threads = new List<ThreadInfo>();

            ThreadInfo specific_info = null;

            List<UnifiedResult> result = new List<UnifiedResult>();

            var clrThreads = runtime.Threads;

            for (uint threadIdx = 0; threadIdx < _numThreads; ++threadIdx)
            {
                specific_info = GetThreadInfo(threadIdx);
                threads.Add(specific_info);
                
                if (specific_info.IsManagedThread)
                {
                    result.Add(DealWithManagedThread(specific_info, runtime));
                }
                else
                {
                    result.Add(DealWithUnManagedThread(specific_info));
                }
            }
            
            return result;
        }
        
        private UnifiedResult DealWithUnManagedThread(ThreadInfo specific_info)
        {
            UnifiedResult result = null;
            var unmanagedStack = GetNativeStackTrace(specific_info.EngineThreadId);
            var blockingObjecets = GetWCTBlockingObject(specific_info.OSThreadId);
            result = new UnifiedResult(specific_info, unmanagedStack, blockingObjecets);
            return result;
        }

        private UnifiedResult DealWithManagedThread(ThreadInfo specific_info, ClrRuntime runtime)
        {
            UnifiedResult result = null;

            ClrThread clr_thread = runtime.Threads.Where(x => x.OSThreadId == specific_info.OSThreadId).FirstOrDefault();
            if (clr_thread != null)
            {
                var managedStack = GetManagedStackTrace(clr_thread);
                var unmanagedStack = GetNativeStackTrace(specific_info.EngineThreadId);

                var blockingObjs = GetBlockingObjects(clr_thread, runtime);
                result = new UnifiedResult(clr_thread, specific_info, managedStack, unmanagedStack, blockingObjs);
                
            }
            return result;
        }

        #region Blocking Objects Methods

        private List<UnifiedBlockingObject> GetBlockingObjects(ClrThread thread, ClrRuntime runtime)
        {
            List<UnifiedBlockingObject> result = new List<UnifiedBlockingObject>();

            //Clr Blocking Objects
            var clr_blockingObjects = GetClrBlockingObjects(thread, runtime);
            if (clr_blockingObjects != null)
            {
                result.AddRange(clr_blockingObjects);
            }
            //WCT API Blocking Objects
            var wct_blockingObjects = GetWCTBlockingObject(thread.OSThreadId);

            if (wct_blockingObjects != null)
            {
                result.AddRange(wct_blockingObjects);
            }

            return result;
        }
        private List<UnifiedBlockingObject> GetWCTBlockingObject(uint threadId)
        {
            List<UnifiedBlockingObject> result = null;

            ThreadWCTInfo wct_threadInfo = null;
            if (WctApi.GetBlockingObjects(threadId, out wct_threadInfo))
            {
                result = new List<UnifiedBlockingObject>();

                if (wct_threadInfo.WctBlockingObjects?.Count > 0)
                {
                    foreach (var blockingObj in wct_threadInfo.WctBlockingObjects)
                    {
                        result.Add(new UnifiedBlockingObject(blockingObj));
                    }
                }
            }

            return result;
        }

        private List<UnifiedBlockingObject> GetClrBlockingObjects(ClrThread thread, ClrRuntime runtime)
        {
            List<UnifiedBlockingObject> result = null;
            if (thread.BlockingObjects?.Count > 0)
            {
                // ClrHeap heap = runtime.GetHeap();
                result = new List<UnifiedBlockingObject>();

                foreach (var item in thread.BlockingObjects)
                {
                    //ClrType type = heap.GetObjectType(item.Object);
                    result.Add(new UnifiedBlockingObject(item));//, type.Name));
                }
            }
            return result;
        }

        #endregion

        private ThreadInfo GetThreadInfo(uint threadIndex)
        {
            uint[] engineThreadIds = new uint[1];
            uint[] osThreadIds = new uint[1];
            Util.VerifyHr(((IDebugSystemObjects)_debugClient).GetThreadIdsByIndex(threadIndex, 1, engineThreadIds, osThreadIds));
            ClrThread managedThread = _runtime.Threads.FirstOrDefault(thread => thread.OSThreadId == osThreadIds[0]);
            return new ThreadInfo
            {
                Index = threadIndex,
                EngineThreadId = engineThreadIds[0],
                OSThreadId = osThreadIds[0],
                ManagedThread = managedThread
            };
        }

        #region StackTrace

        public List<UnifiedStackFrame> GetStackTrace(uint threadIndex)
        {
            ThreadInfo threadInfo = GetThreadInfo(threadIndex);
            List<UnifiedStackFrame> unifiedStackTrace = new List<UnifiedStackFrame>();
            List<UnifiedStackFrame> nativeStackTrace = GetNativeStackTrace(threadInfo.EngineThreadId);
            if (threadInfo.IsManagedThread)
            {
                List<UnifiedStackFrame> managedStackTrace = GetManagedStackTrace(threadInfo.ManagedThread);
                int managedFrame = 0;
                for (int nativeFrame = 0; nativeFrame < nativeStackTrace.Count; ++nativeFrame)
                {
                    bool found = false;
                    for (int temp = managedFrame; temp < managedStackTrace.Count; ++temp)
                    {
                        if (nativeStackTrace[nativeFrame].InstructionPointer == managedStackTrace[temp].InstructionPointer)
                        {
                            managedStackTrace[temp].LinkedStackFrame = nativeStackTrace[nativeFrame];
                            unifiedStackTrace.Add(managedStackTrace[temp]);
                            managedFrame = temp + 1;
                            found = true;
                            break;
                        }
                        else if (managedFrame > 0)
                        {
                            // We have already seen at least one managed frame, and we're about
                            // to skip a managed frame because we didn't find a matching native
                            // frame. In this case, add the managed frame into the stack anyway.
                            unifiedStackTrace.Add(managedStackTrace[temp]);
                            managedFrame = temp + 1;
                            found = true;
                            break;
                        }
                    }
                    // We didn't find a matching managed frame, so add the native frame directly.
                    if (!found)
                        unifiedStackTrace.Add(nativeStackTrace[nativeFrame]);
                }
            }
            else
            {
                return nativeStackTrace;
            }
            return unifiedStackTrace;
        }

        private List<UnifiedStackFrame> GetManagedStackTrace(ClrThread thread)
        {
            return (from frame in thread.StackTrace
                    let sourceLocation = SymbolCache.GetFileAndLineNumberSafe(frame)
                    select new UnifiedStackFrame(frame, sourceLocation)
                    ).ToList();
        }

        private List<UnifiedStackFrame> GetNativeStackTrace(uint engineThreadId)
        {
            Util.VerifyHr(((IDebugSystemObjects)_debugClient).SetCurrentThreadId(engineThreadId));

            DEBUG_STACK_FRAME[] stackFrames = new DEBUG_STACK_FRAME[200];
            uint framesFilled;
            Util.VerifyHr(((IDebugControl)_debugClient).GetStackTrace(0, 0, 0, stackFrames, stackFrames.Length, out framesFilled));

            List<UnifiedStackFrame> stackTrace = new List<UnifiedStackFrame>();
            for (uint i = 0; i < framesFilled; ++i)
            {
                stackTrace.Add(new UnifiedStackFrame(stackFrames[i], (IDebugSymbols2)_debugClient));
            }
            return stackTrace;
        }

        #endregion

    }


}

