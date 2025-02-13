using System.Collections.Generic;
using System.Data.Common;
using System.Net.Http.Headers;
using System.Text;
using System.Xml.Linq;
using Microsoft.IdentityModel.Tokens;
///C:\Users\kilyushev_nd\Desktop\PlanyVS\Планы

namespace InjectData
{

    internal class Program
    {
        private static Dictionary<string, (StringBuilder tableData, bool IsFirstRecord)> namesOfTablesData = new Dictionary<string, (StringBuilder, bool)> ();

        private static Dictionary<string, TableColumnInfo[]> tableColumnsCache = new Dictionary<string, TableColumnInfo[]>();
        private static void AddToDictionary(string tablesNames, ApiToUpload api)
        {
            foreach (var tableName in tablesNames.Split(' ').ToArray())
            {
                if (!namesOfTablesData.ContainsKey(tableName))
                {
                    var ntb = new StringBuilder();
                    ntb.Append($"insert into [dbo].[{tableName}] (");
                    var columns = api.GetTableColumns(tableName).Elements().First().Elements().Select(TableColumnInfo.Parse).ToArray();
                    ntb.Append("[id_education_plan]");
                    foreach (var column in columns)
                    {
                        ntb.Append($", [{column.Name}]");
                    }
                    ntb.Append(")\n");
                    ntb.Append("values\n\t");

                    namesOfTablesData[tableName] = (ntb, true);
                }
            }
            Console.WriteLine("Словарь сформирован");
        }

        private static void SaveDictionaryToFiles(Dictionary<string, StringBuilder> nameOfTablesData, string Directory)
        {
            foreach (var keyAndValue in nameOfTablesData)
            {
                var fileName = keyAndValue.Key;
                var fileValue = keyAndValue.Value;
                var filePath = Path.Combine(Directory, $"{fileName}.txt");

                try
                {
                    File.WriteAllText(filePath, fileValue.ToString(), Encoding.Unicode);
                    Console.WriteLine($"Успешная запись в файл: {fileName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка записи в файл: {fileName}");
                }
            }
        }
        //Сделать словарь
        public static string ParseFilePath(string filePath)
        {
            var parts = filePath.Split(Path.DirectorySeparatorChar);
            var plansIndex = Array.IndexOf(parts, "Планы");
            var filePathFromPlans = string.Join(Path.DirectorySeparatorChar, parts[plansIndex..]);
            return (filePathFromPlans);
        }
        private static List<string> GetXmlFiles(string folderPath)
        {
            List<string> xmlFiles = new List<string>();

            foreach (string file in Directory.GetFiles(folderPath, "*.plx"))
            {
                xmlFiles.Add(file);
            }

            foreach (string subfolder in Directory.GetDirectories(folderPath))
            {
                xmlFiles.AddRange(GetXmlFiles(subfolder));
            }

            return xmlFiles;
        }

        class TableColumnInfo
        {
            public string Name { get; set; }
            public string Namespace { get; set; }
            public string Datatype { get; set; }

            public static TableColumnInfo Parse(XElement x)
            {
                var res = new TableColumnInfo();
                res.Name = (string)x.Attribute("c");
                res.Namespace = (string)x.Attribute("ns");
                res.Datatype = (string)x.Attribute("data_type");
                return res;
            }
        }

        private static TableColumnInfo[] GetCachedTableColumns(string tableName, ApiToUpload api)
        {
            if (!tableColumnsCache.ContainsKey(tableName))
            {
                var columns = api.GetTableColumns(tableName).Elements().First().Elements().Select(TableColumnInfo.Parse).ToArray();
                tableColumnsCache[tableName] = columns;
            }
            return tableColumnsCache[tableName];
        }

        private static void GetDataForTables(string filePath, ApiToUpload api)
        {
            var file = XDocument.Load(filePath);
            var el = file.Elements().First().Elements().First().Elements().First();
            var PathForTable = ParseFilePath(filePath);
            var idFile = api.GetIdEduPlan(PathForTable);

            foreach (var element in el.Elements())
            {
                var elName = element.Name.LocalName;
                
                if (namesOfTablesData.TryGetValue(elName, out var tableInfo))
                {
                    var (tableRow, isFirstRecord) = tableInfo;

                    if (isFirstRecord)
                    {
                        tableRow.Append("(");
                        namesOfTablesData[elName] = (tableRow, false);
                    }
                    else
                    {
                        tableRow.Append(", (");
                    }
                    tableRow.Append($"{idFile}");

                    var columns = GetCachedTableColumns(elName, api);

                    foreach (var column in columns)
                    {
                        var attrValue = element.Attributes().FirstOrDefault(a => a.Name.LocalName == column.Name);

                        if (attrValue is not null && attrValue.Value != "" && column.Datatype is not null)
                        {
                            if (column.Datatype.Contains("varchar"))
                            {
                                tableRow.Append($", '{attrValue.Value}'");
                            }
                            else
                            {
                                tableRow.Append($", {attrValue.Value}");
                            }
                        }
                        else
                        {
                            tableRow.Append($", null");
                        }
                    }
                    tableRow.Append(")\n\t");
                }
            }
        }

        static void MainLoop(ApiToUpload api)
        {
            Console.WriteLine("Введите путь к папке 'Планы':");
            //var input = "C:\\Users\\kilyushev_nd\\Desktop\\PlanyVS\\Планы";
            var input = "D:\\khsu\\Планы";
            var tablesNames = api.GetTablesInDB();
            AddToDictionary(tablesNames, api);

            if (string.IsNullOrWhiteSpace(input) || !Directory.Exists(input))
            {
                Console.WriteLine("Указанный файл не существует.");
                return;
            }
            var files = GetXmlFiles(input);
            //try
            //{
            Parallel.ForEach(files, filePath =>
            {
                Console.WriteLine($"Обработка файла: {filePath}");
                lock (namesOfTablesData)
                {
                    GetDataForTables(filePath, api);
                }
                //try
                //{
                //    Console.WriteLine('\n');
                //}
                //catch (Exception ex)
                //{
                //    Console.WriteLine($"Ошибка при обработке файла {filePath}: {ex.Message}");
                //}

            });
            SaveDictionaryToFiles(namesOfTablesData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.tableData), "D:\\khsu\\E\\ForSQL\\"); // Папка для сохранения
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"Произошла ошибка: {ex.Message}");
            //}

        }

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.Unicode;
            Console.InputEncoding = Encoding.Unicode;
            using (var api = new ApiToUpload())
            {
                MainLoop(api);
            }
        }
    }
}

