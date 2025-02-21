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
        private static Dictionary<string, List<string>> namesOfTablesData = new();

        private static Dictionary<string, TableColumnInfo[]> tableColumnsCache = new();
        private static void AddToDictionary(XElement tablesNames, ApiToUpload api)
        {
            var names = tablesNames.Elements().Select(e => (string)e.Attribute("t"));
            foreach (var tableName in names)
            {
                if (!namesOfTablesData.ContainsKey(tableName))
                {
                    namesOfTablesData[tableName] = new();
                }
            }
            Console.WriteLine("Словарь сформирован");
        }

        private static void SaveDictionaryToFiles(Dictionary<string, List<string>> nameOfTablesData, string Directory)
        {
            foreach (var keyAndValue in nameOfTablesData)
            {
                var fileName = keyAndValue.Key;
                var fileValue = keyAndValue.Value;
                var filePath = Path.Combine(Directory, $"{fileName}.txt");
                
                var insert = 
                    $"insert into [dbo].[{fileName}] ([id_education_plan], [" 
                    + string.Join("], [", tableColumnsCache[fileName].Select(ci => ci.Name)) 
                    + "]) values";

                //var columns = (fileName).Elements().First().Elements().Select(TableColumnInfo.Parse).ToArray();
                //insert.Append("[id_education_plan]");
                //foreach (var column in columns)
                //{

                //}
                //insert.Append(")\n");
                //insert.Append("values\n\t");

                var sb = new StringBuilder();

                var valuesChunks = fileValue.Chunk(1000).Select(c => string.Join("," + Environment.NewLine, c));
                foreach (var chunk in valuesChunks)
                {
                    sb.AppendLine(insert);
                    sb.AppendLine(chunk);
                    sb.AppendLine("GO");
                }

                var content =  sb.ToString();

                try
                {
                    File.WriteAllText(filePath, content, Encoding.Unicode);
                    Console.WriteLine($"Успешная запись в файл: {fileName}");
                }
                catch (IOException ex)
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
        private static string[] GetXmlFiles(string folderPath)
        {
            var xmlFiles = Directory.GetFiles(folderPath, "*.plx", SearchOption.AllDirectories).ToArray();
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

            foreach (var element in el.Elements().SelectMany(el => el.Elements().Prepend(el)))
            {
                var elName = element.Name.LocalName;

                if (elName == "ООП" && element.Attribute("КодРодительскогоООП") is not null)
                {
                    elName = "ООП2";
                }


                if (namesOfTablesData.TryGetValue(elName, out var lst))
                {
                    var tableRow = new StringBuilder();

                    tableRow.Append("(");

                    tableRow.Append($"{idFile}");

                    var columns = GetCachedTableColumns(elName, api);

                    foreach (var column in columns)
                    {
                        var attrValue = element.Attributes().FirstOrDefault(a => a.Name.LocalName == column.Name);

                        if (attrValue is not null && attrValue.Value != "" && column.Datatype is not null)
                        {
                            if (column.Datatype.StartsWith("varchar") || column.Datatype.StartsWith("datetime2"))
                            {
                                    tableRow.Append($", '{attrValue.Value.Replace("'", "''")}'");
                            }
                            else if (column.Datatype == "bit")
                            {
                                tableRow.Append(attrValue.Value == "true" ? ", 1" : ", 0");
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
                    tableRow.Append(")");
                    lst.Add(tableRow.ToString());
                }
            }
        }

        static void MainLoop(ApiToUpload api)
        {
            
            var input = "D:\\khsu\\Планы";
            Console.WriteLine($"Путь к папке 'Планы': {input}");
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
            foreach(var filePath in files)
            {
                Console.WriteLine($"Обработка файла: {filePath}");
                GetDataForTables(filePath, api);

                //try
                //{
                //    Console.WriteLine('\n');
                //}
                //catch (Exception ex)
                //{
                //    Console.WriteLine($"Ошибка при обработке файла {filePath}: {ex.Message}");
                //}

            });
            SaveDictionaryToFiles(namesOfTablesData, "D:\\khsu\\E\\ForSQL"); // Папка для сохранения "C:\\Users\\kilyushev_nd\\Desktop\\PlanyVS\\Final"
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

