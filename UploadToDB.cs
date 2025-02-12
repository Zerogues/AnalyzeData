using Microsoft.Data.SqlClient;
using System;
using System.IO;
using System.Xml;

namespace InjectData
{
    internal class Database : IDisposable
    {
        SqlConnection conToDB;

        public Database()
        {
            //Data Source = localhost/236_4
            conToDB = new SqlConnection("Data Source=localhost;Initial Catalog=EDU2025;Integrated Security=True;Trust Server Certificate=True");
            conToDB.Open();
        }

        public object ExecuteScalarProc(string name, params (string name, object value)[] parameters)
        {
            using (var cmd = conToDB.CreateCommand())
            {
                cmd.CommandText = name;
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                foreach (var p in parameters)
                    cmd.Parameters.AddWithValue(p.name, p.value);

                return cmd.ExecuteScalar();
            }
        }

        public void ExecuteNonQueryProc(string name, params(string name, object value)[] parameters)
        {
            using (var cmd = conToDB.CreateCommand())
            {
                cmd.CommandText = name;
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                foreach (var p in parameters)
                    cmd.Parameters.AddWithValue(p.name, p.value);

                cmd.ExecuteNonQuery();
            }
        }

        public void Dispose()
        {
            conToDB.Dispose();
        }
    }
}
