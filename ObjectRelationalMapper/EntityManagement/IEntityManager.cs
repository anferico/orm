using System;

namespace ObjectRelationalMapper 
{
    public interface IEntityManager<T> where T : new() 
    {
        void persist(T entity);
        void remove(T entity);
        T find(object primaryKey);
        Query<T> createQuery(string query);
    }
}
