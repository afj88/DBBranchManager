using System;
using System.Data.SqlClient;

namespace DBBranchManager.Utils.Sql
{
    internal class SqlMessageEventArgs : EventArgs
    {
        private readonly SqlErrorCollection mErrors;
        private readonly string mMessage;
        private readonly string mSource;
        private readonly object mContext;

        internal SqlMessageEventArgs(SqlErrorCollection errors, string message, string source, object context)
        {
            mErrors = errors;
            mMessage = message;
            mSource = source;
            mContext = context;
        }

        public SqlErrorCollection Errors
        {
            get { return mErrors; }
        }

        public string Message
        {
            get { return mMessage; }
        }

        public string Source
        {
            get { return mSource; }
        }

        public object Context

        {
            get { return mContext; }
        }
    }
}