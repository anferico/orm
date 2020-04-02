using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace ObjectRelationalMapper 
{
    public class Query<T> : IQuery<T> where T : new() 
    {
        private EntityManager<T> manager;
        private SQLiteCommand command;

        public Query(EntityManager<T> manager, SQLiteCommand command) 
        {
            this.manager = manager;
            this.command = command;
        }

        public List<T> getResultList() 
        {
            var resultList = new List<T>();
            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                resultList.Add(manager.find(reader[0]));
            }

            return resultList;
        }

        public void execute() 
        {
            command.ExecuteNonQuery();
        }
    }
}
