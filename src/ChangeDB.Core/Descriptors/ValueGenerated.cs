﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChangeDB
{
    public enum ValueGenerated
    {
        //
        // Summary:
        //     A value is never generated by the database.
        Never = 0,
        //
        // Summary:
        //     A value is generated by the database when an entity is first added to the database.
        //     The most common scenario for this is generated primary key values.
        OnAdd = 1,
        //
        // Summary:
        //     No value is generated when the entity is first added to the database, but a value
        //     will be read from the database whenever the entity is subsequently updated.
        OnUpdate = 2,
        //
        // Summary:
        //     A value is read from the database when the entity is first added and whenever
        //     the entity is subsequently updated. This is typically used for computed columns
        //     and scenarios such as rowversions, timestamps, etc.
        OnAddOrUpdate = 3,
        //
        // Summary:
        //     No value is generated when the entity is first added to the database, but a value
        //     will be read from the database under certain conditions when the entity is subsequently
        //     updated.
        OnUpdateSometimes = 4
    }
}