﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeDB.Dump
{
    public interface IDatabaseSqlDumper
    {
        Task DumpSql(DumpContext dumpContext);
    }
}