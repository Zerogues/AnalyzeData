using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Data.SqlClient;

namespace InjectData
{
    internal class ApiToUpload : IDisposable
    {
        Database db;

        public ApiToUpload()
        {
            db = new Database();
        }

        // MS SQL MERGE (UPSERT)

        public void AddXml(int InstitutionID, int EduFormID, string XmlData, string FilePath)
        {
            db.ExecuteNonQueryProc(
                "add_xml_data",
                ("@Institution", InstitutionID),
                ("@EducationForm", EduFormID),
                ("@DataAll", XmlData),
                ("@FilePath", FilePath)
            );
        }

        public XDocument GetTableColumns(string element)
        {
            var result = (string)db.ExecuteScalarProc(
                "get_table_columns",
                ("@table", element)
            );

            return XDocument.Parse(result);
        }

        public int GetIdEduPlan(string FilePath)
        {
            var result = (int)db.ExecuteScalarProc(
                "get_id_education_plan",
                ("@identifier", FilePath)
            );
            return result;
        }

        public string GetTablesInDB()
        {
            var result = (string)db.ExecuteScalarProc(
                "get_tables_names");
            return result;
        }
        public void Dispose()
        {
            db.Dispose();
        }
    }
}
