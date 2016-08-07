﻿using WinHandlesQuerier.Core.Infra;
using WinHandlesQuerier.Core.Model;
using Microsoft.Diagnostics.Runtime;
using System;
using System.Threading.Tasks;
using WinHandlesQuerier.Core.Handlers;

namespace WinHandlesQuerier.ProcessStrategies
{
    public class LiveProcessStrategy : ProcessStrategy
    {
        public LiveProcessStrategy(uint pid) : base(pid)
        {

        }

        public uint PID => _pid;

        public override async Task<ProcessAnalysisResult> Run()
        {
            ProcessAnalysisResult result = null;

            try
            {
                using (DataTarget target = DataTarget.AttachToProcess((int)_pid, Constants.MAX_ATTACH_TO_PPROCESS_TIMEOUT))
                {
                    if (IsSuitableBitness(target))
                    {
                        Console.WriteLine("Attached To Process Successfully");

                        var clrVer = target.ClrVersions[0];

                        var runtime = clrVer.CreateRuntime();

                        using (ProcessAnalyzer handler = new ProcessAnalyzer(target, runtime, _pid))
                        {
                            result = await handler.Handle();
                        }
                    }
                }
            }
            catch (ClrDiagnosticsException ex)
            {
                SetError(ex.Message);
            }

            return result;
        }
    }
}