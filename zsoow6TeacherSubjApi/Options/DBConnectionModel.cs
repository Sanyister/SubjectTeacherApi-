﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace zsoow6TeacherSubjApi.Options
{
    public class DBConnectionModel
    {
        private readonly DBConnectionOption _connectionOptions;

        public DBConnectionModel(IOptions<DBConnectionOption> connectionOptions)
        {
            _connectionOptions = connectionOptions.Value;
        }

        public DBConnectionOption GetConnection()
        {
            return _connectionOptions;
        }
    }
}
