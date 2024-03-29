﻿using System;

namespace ChangeDB.Migration
{
    public record AgentSetting
    {
        public bool SupportSchema { get; init; }
        public string DefaultSchema { get; init; }
        public int ObjectNameMaxLength { get; init; }

        public Func<string, string, string> IdentityName { get; init; }
    }
}
