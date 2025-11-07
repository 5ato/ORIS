using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyORMLibrary
{
    public interface IORMContext
    {
        void Create<T>(T entity, string tableName) where T : class;
        T? ReadById<T>(int id, string tableName) where T : class;
        List<T> ReadByAll<T>(string tableName) where T : class;
    }
}
