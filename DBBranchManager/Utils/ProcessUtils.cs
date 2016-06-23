using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DBBranchManager.Utils
{
    public class ProcessOutputLine
    {
        public enum OutputTypeEnum
        {
            StandardOutput,
            StandardError
        }

        public ProcessOutputLine(OutputTypeEnum outputType, string line)
        {
            OutputType = outputType;
            Line = line;
        }

        public OutputTypeEnum OutputType { get; private set; }
        public string Line { get; private set; }
    }

    public interface IProcessExecutionResult : IDisposable
    {
        IEnumerable<ProcessOutputLine> GetOutput();

        int ExitCode { get; }
    }

    internal static class ProcessUtils
    {
        public static IProcessExecutionResult Exec(string file, string args, Stream input)
        {
            return new ProcessExecutionResult(file, args, input);
        }


        private class ProcessExecutionResult : IProcessExecutionResult
        {
            private readonly Process mProcess;
            private readonly Stream mInputStream;
            private readonly BlockingCollection<ProcessOutputLine> mOutput;
            private bool mOutputFinished;
            private bool mErrorFinished;
            private bool mDisposed;


            public ProcessExecutionResult(string file, string args, Stream input)
            {
                mProcess = new Process
                {
                    StartInfo = new ProcessStartInfo(file, args)
                    {
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };
                mInputStream = input;

                mOutput = new BlockingCollection<ProcessOutputLine>();

                mProcess.OutputDataReceived += OnOutputReceived;
                mProcess.ErrorDataReceived += OnErrorReceived;

                mProcess.Start();

                mProcess.BeginOutputReadLine();
                mProcess.BeginErrorReadLine();
            }

            #region IProcessExecutionResult

            public IEnumerable<ProcessOutputLine> GetOutput()
            {
                var writeTask = mInputStream.CopyToAsync(mProcess.StandardInput.BaseStream).ContinueWith(_ =>
                    mProcess.StandardInput.BaseStream.FlushAsync().ContinueWith(__ =>
                        mProcess.StandardInput.BaseStream.Close()));

                foreach (var outputLine in mOutput.GetConsumingEnumerable())
                {
                    yield return outputLine;
                }

                writeTask.Unwrap().Wait();
            }

            public int ExitCode
            {
                get { return mProcess.ExitCode; }
            }

            #endregion

            #region IDisposable

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (mDisposed)
                    return;

                if (disposing)
                {
                    mProcess.Dispose();
                    mInputStream.Dispose();
                    mOutput.Dispose();
                }

                mDisposed = true;
            }

            #endregion

            private void OnOutputReceived(object sender, DataReceivedEventArgs e)
            {
                if (e.Data == null)
                {
                    lock (this)
                    {
                        mOutputFinished = true;
                        mProcess.OutputDataReceived -= OnOutputReceived;
                    }
                    MaybeComplete();
                }
                else
                {
                    mOutput.Add(new ProcessOutputLine(ProcessOutputLine.OutputTypeEnum.StandardOutput, e.Data));
                }
            }

            private void OnErrorReceived(object sender, DataReceivedEventArgs e)
            {
                if (e.Data == null)
                {
                    lock (this)
                    {
                        mErrorFinished = true;
                        mProcess.ErrorDataReceived -= OnErrorReceived;
                    }
                    MaybeComplete();
                }
                else
                {
                    mOutput.Add(new ProcessOutputLine(ProcessOutputLine.OutputTypeEnum.StandardError, e.Data));
                }
            }

            private void MaybeComplete()
            {
                lock (this)
                {
                    if (mOutputFinished && mErrorFinished)
                    {
                        if (!mProcess.HasExited)
                        {
                            mProcess.WaitForExit();
                        }
                        mOutput.CompleteAdding();
                    }
                }
            }
        }
    }
}