using System;
using System.Collections.Generic;

namespace ObjectRelationalMapper 
{
    public interface IQuery<T> 
    {
        List<T> getResultList();
        void execute();
    }
}
