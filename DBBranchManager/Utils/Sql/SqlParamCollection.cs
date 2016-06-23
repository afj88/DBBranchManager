using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace DBBranchManager.Utils.Sql
{
    public class SqlParamCollection : IEnumerable<SqlParameter>
    {
        private readonly List<SqlParameter> mParameters;

        public SqlParamCollection()
        {
            mParameters = new List<SqlParameter>();
        }

        public IEnumerator<SqlParameter> GetEnumerator()
        {
            return mParameters.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public SqlParameter Add(string name, SqlDbType type)
        {
            var parameter = new SqlParameter(name, type);
            mParameters.Add(parameter);
            return parameter;
        }
    }
}