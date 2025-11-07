using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyORMLibrary
{
    public interface IConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
