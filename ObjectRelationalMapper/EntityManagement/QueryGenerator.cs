using System;
using System.Collections.Generic;
using System.Text;

namespace ObjectRelationalMapper 
{
    public class QueryGenerator 
    {
        public string CreateInsertQuery(string tableName, List<object> values) 
        {
            var strBuilder = new StringBuilder(
                $"INSERT OR IGNORE INTO {tableName} VALUES ("
            );
            int i;

            for (i = 0; i < values.Count - 1; i++)
            {
                strBuilder.Append($"'{values[i]}', ");
            }

            strBuilder.Append($"'{values[i]}');");

            return strBuilder.ToString();
        }

        public string CreateDeleteQuery(string tableName, string pkeyName, object pkeyValue) 
        {
            return $"DELETE FROM {tableName} WHERE {pkeyName} = '{pkeyValue}';";
        }

        public string CreateSelectQuery(string tableName, string attrName, object attrValue) 
        {
            return $"SELECT * FROM {tableName} WHERE {attrName} = '{attrValue}';";
        }
    }
}
