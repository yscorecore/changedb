﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeDB
{
    public interface IConnectionProvider
    {
        DbConnection CreateConnection(string connectionString);
        [Obsolete]
        string ChangeDatabase(string connectionString, string databaseName);

    }
}
