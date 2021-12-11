﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace ChangeDB
{
    public class PluginRegister
    {
        public static void LoadPlugins()
        {
            var rootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            foreach (var dllFile in Directory.GetFiles(rootPath, "*.dll", SearchOption.TopDirectoryOnly))
            {
                LoadAssembly(dllFile);
            }
        }

        private static void LoadAssembly(string dll)
        {
            try
            {
                var assembly = Assembly.LoadFrom(dll);
                Debug.WriteLine("Load plugin assembly \"{dll}\"");
            }
#pragma warning disable CA1031 // 不捕获常规异常类型
            catch (Exception)
#pragma warning restore CA1031 // 不捕获常规异常类型
            {
                //Debug.Fail($"Load plugin assembly \"{dll}\" error.", ex.Message);
            }
        }
    }
}
