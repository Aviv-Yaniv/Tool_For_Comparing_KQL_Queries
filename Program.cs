using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompareTables
{
    class Program
    {
        private const string OpeninngFileMessage                                = "Opening file: ";

        private const string SameContentMessageFormat                           = "strcat(\"Same Content, \", {0} ,\" rows compared :)\")";
        private const string SameNumberOfRowsDiffrentContentsErrorMessageFormat = "\"ERROR! Same number of rows, diffrent contents, use {0} to view diffrences\"";
        private const string DiffrentNumberOfRowsErrorMessageFormat             = "strcat(\"ERROR! Diffrent number of rows: gold=\", {0}, \" refactored=\", {1})";
        
        private const string diffrenceColumnName                                = "DiffrenceColsCounter";

        private const string goldName                                           = "gold";
        private const string refactoredName                                     = "refactored";
        
        private const string HeaderFilePath                                     = "header.txt";
        private const string ColumnsComparatorsFilePath                         = "columns_comparators.txt";
        
        private const string GoldColumnsFilePath                                = "gold_columns.txt";
        private const string RefactoredColumnsFilePath                          = "refactored_columns.txt";
        
        private const string GoldQueryFilePath                                  = "gold.kql";
        private const string RefactoredQueryFilePath                            = "refactored.kql";
        private const string CompareQueryFilePath                               = "compare.kql";

        private static string[] ParseCommaSeperated(string raw_columns)
        {
            return raw_columns.Split(',').Select(x => { var token = x.Split('='); return token[0].Trim(); } ).ToArray();
        }

        public static string GetComperatorFunctionName(string type)
        {
            return $"are_{type}_equal";
        }

        public static string CompareColumns(string column1, string column2, string type)
        {
            column2 = column1 == column2 ? $"{column1}1" : column2;
            return type == "regulars" ? $"{column1} == {column2}" : $"{GetComperatorFunctionName(type)}({column1}, {column2})";
        }

        private static string QueryToFunction(string queryFunctionName, string queryContent)
        {
            return $"let {queryFunctionName} = () \n" +
                    "{\n" +
                    $"{queryContent}"+
                    "\n};\n";
        }

        private static string QueryToFunctionName(string queryName)
        {
            return $"calc_{queryName}";
        }

        private static string QueryToCountName(string queryName)
        {
            return $"count_{queryName}";
        }

        private static string QueryFunctionResultTableName(string queryName)
        {
            return $"table_{queryName}";
        }

        private static List<(string comparisioColumnName, string comparisionContent)> CreateComparisionColumns(string[] columnsTypes, string[] goldColumns, string[] refactoredColumns)
        {
            int columnsNumber = goldColumns.Length;
            var comparisionColumns = new List<(string comparisioColumnName, string comparisionContent)>(columnsNumber);

            for (int i = 0; i < columnsNumber; i++)
            {
                comparisionColumns.Add(($"IsSame_{goldColumns[i]}", CompareColumns(goldColumns[i], refactoredColumns[i], columnsTypes[i])));
            }

            return comparisionColumns;
        }

        public static string GenerateDiffrenceColumnsCounter(List<(string comparisioColumnName, string comparisionContent)> columnsComparisions)
        {
            return String.Join(" + ", columnsComparisions.Select(x => $"toint({x.comparisioColumnName} == FALSE)"));
        }

        private static string BuildInnerJoin(string table1Name, string table2Name, string[] columnsTypes, string[] table1Columns, string[] table2Columns)
        {
            var joinHeader =    $"{table1Name}" +
                                $"\n| join kind = inner {table2Name} on $left.{table1Columns[0]} == $right.{table2Columns[0]}" +
                                $"\n| extend ";
            var columnsComparisions = CreateComparisionColumns(columnsTypes, table1Columns, table2Columns);
            var comparisionBody = String.Join(", ", columnsComparisions.Select(x => $"\n{x.comparisioColumnName} = {x.comparisionContent}"));
            var diffrenceResults = new StringBuilder();
            diffrenceResults.Append($"\n| project");
            diffrenceResults.Append($"\n{table1Columns[0]}, ");
            diffrenceResults.Append($"\n{diffrenceColumnName} = {GenerateDiffrenceColumnsCounter(columnsComparisions)}, ");
            diffrenceResults.Append(String.Join(", ", columnsComparisions.Select(x => $"\n{x.comparisioColumnName}")));
            diffrenceResults.Append($"\n| where {diffrenceColumnName} != 0;");
            return joinHeader + comparisionBody.ToString() + diffrenceResults.ToString();
        }

        private static string BuildCompareQuery(string headerFunctions, string goldQuery, string refactoredQuery, string columnsTypesRaw, string goldColumnsRaw, string refactoredColumnsRaw)
        {
            string goldQueryCountName                     = QueryToCountName(goldName);
            string refactoredQueryCountName               = QueryToCountName(refactoredName);

            string goldQueryFunctionName                  = QueryToFunctionName(goldName);
            string refactoredQueryFunctionName            = QueryToFunctionName(refactoredName);

            string goldQueryFunctionResultTableName       = QueryFunctionResultTableName(goldName);
            string refactoredQueryFunctionResultTableName = QueryFunctionResultTableName(refactoredName);

            string[] columnsTypes                         = ParseCommaSeperated(columnsTypesRaw);

            string[] goldColumns                          = ParseCommaSeperated(goldColumnsRaw);
            string[] refactoredColumns                    = ParseCommaSeperated(refactoredColumnsRaw);

            var comarisionQueryStringBuilder              = new StringBuilder();

            comarisionQueryStringBuilder.Append($"{headerFunctions.Trim('\n')}");
            comarisionQueryStringBuilder.Append($"{KqlComparisionUtils.ComparisionUtilsFunctions}\n");

            comarisionQueryStringBuilder.Append($"{QueryToFunction(goldQueryFunctionName, goldQuery)}");
            comarisionQueryStringBuilder.Append($"{QueryToFunction(refactoredQueryFunctionName, refactoredQuery)}");
            
            comarisionQueryStringBuilder.Append($"let {goldQueryFunctionResultTableName} = materialize({goldQueryFunctionName}());\n");
            comarisionQueryStringBuilder.Append($"let {refactoredQueryFunctionResultTableName} = materialize({refactoredQueryFunctionName}());\n");

            comarisionQueryStringBuilder.Append($"let compared_rows_table = {BuildInnerJoin(goldQueryFunctionResultTableName, refactoredQueryFunctionResultTableName, columnsTypes, goldColumns, refactoredColumns)}\n");

            comarisionQueryStringBuilder.Append($"let count_diffrent = toscalar(compared_rows_table | count);\n");
            comarisionQueryStringBuilder.Append($"let isDiffrentRowsContent = count_diffrent != 0;\n");

            comarisionQueryStringBuilder.Append($"let {goldQueryCountName} = toscalar({goldQueryFunctionResultTableName} | count);\n");
            comarisionQueryStringBuilder.Append($"let {refactoredQueryCountName} = toscalar({refactoredQueryFunctionResultTableName} | count);\n");
            comarisionQueryStringBuilder.Append($"let isSameCounts = {goldQueryCountName} == {refactoredQueryCountName};\n");

            comarisionQueryStringBuilder.Append($"print(iff(isSameCounts, iff(isDiffrentRowsContent, {string.Format(SameNumberOfRowsDiffrentContentsErrorMessageFormat, "compared_rows_table")}, {string.Format(SameContentMessageFormat, goldQueryCountName)}), {string.Format(DiffrentNumberOfRowsErrorMessageFormat, goldQueryCountName, refactoredQueryCountName)}));\n");

            return comarisionQueryStringBuilder.ToString();
        }

        static void Main(string[] args)
        {
            Console.WriteLine($"{OpeninngFileMessage} {HeaderFilePath}");
            string headerFunctions = File.ReadAllText(HeaderFilePath);
            Console.WriteLine($"{OpeninngFileMessage} {GoldQueryFilePath}");
            string goldQuery = File.ReadAllText(GoldQueryFilePath);
            Console.WriteLine($"{OpeninngFileMessage} {RefactoredQueryFilePath}");
            string refactoredQuery = File.ReadAllText(RefactoredQueryFilePath);
            Console.WriteLine($"{OpeninngFileMessage} {ColumnsComparatorsFilePath}");
            string columnsTypesRaw = File.ReadAllText(ColumnsComparatorsFilePath); 
            Console.WriteLine("NOTE: First column name in each column file is used for inner-join: ");
            Console.WriteLine($"{OpeninngFileMessage} {GoldColumnsFilePath}");
            string goldColumnsRaw = File.ReadAllText(GoldColumnsFilePath);
            Console.WriteLine($"{OpeninngFileMessage} {RefactoredColumnsFilePath}");
            string refactoredColumnsRaw = File.ReadAllText(RefactoredColumnsFilePath);
            string compareQuery = BuildCompareQuery(headerFunctions, goldQuery, refactoredQuery, columnsTypesRaw, goldColumnsRaw, refactoredColumnsRaw);
            File.WriteAllText(CompareQueryFilePath, compareQuery);
        }
    }
}
