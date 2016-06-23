using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace DBBranchManager.Utils.Sql
{
    internal sealed class SqlCommandFactory : IDisposable
    {
        private static readonly object NullObject = new object();

        private readonly string mConnectionString;
        private readonly SqlMessageEventHandler mOnMessage;
        private readonly Dictionary<object, Tuple<SqlConnection, ConnectionMessageHandler>> mMap;

        public SqlCommandFactory(string connectionString, SqlMessageEventHandler onMessage)
        {
            mConnectionString = connectionString;
            mOnMessage = onMessage;
            mMap = new Dictionary<object, Tuple<SqlConnection, ConnectionMessageHandler>>();
        }

        public void Dispose()
        {
            foreach (var kvp in mMap)
            {
                kvp.Value.Item1.InfoMessage -= kvp.Value.Item2.OnInfoMessage;
                kvp.Value.Item1.Dispose();
            }
            mMap.Clear();
        }

        public SqlCommand CreateCommand(object context)
        {
            var key = context ?? NullObject;

            Tuple<SqlConnection, ConnectionMessageHandler> connection;
            if (!mMap.TryGetValue(key, out connection))
            {
                mMap[key] = connection = CreateConnection(context);
            }

            return CreateCommandFrom(connection.Item1);
        }

        private Tuple<SqlConnection, ConnectionMessageHandler> CreateConnection(object context)
        {
            var connection = new SqlConnection(mConnectionString)
            {
                FireInfoMessageEventOnUserErrors = true
            };
            var handler = new ConnectionMessageHandler(this, context);

            connection.InfoMessage += handler.OnInfoMessage;
            connection.Open();

            return Tuple.Create(connection, handler);
        }

        private SqlCommand CreateCommandFrom(SqlConnection connection)
        {
            var cmd = connection.CreateCommand();
            cmd.CommandTimeout = 0;
            return cmd;
        }

        private void OnMessage(SqlInfoMessageEventArgs e, object context)
        {
            if (mOnMessage != null)
                mOnMessage(this, new SqlMessageEventArgs(e.Errors, e.Message, e.Source, context));
        }

        private class ConnectionMessageHandler
        {
            private readonly SqlCommandFactory mFactory;
            private readonly object mContext;

            public ConnectionMessageHandler(SqlCommandFactory factory, object context)
            {
                mFactory = factory;
                mContext = context;
            }

            public void OnInfoMessage(object sender, SqlInfoMessageEventArgs e)
            {
                mFactory.OnMessage(e, mContext);
            }
        }
    }
}