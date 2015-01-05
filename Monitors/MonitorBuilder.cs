﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using HYMonitors.MonitoredObjs;
using System.Xml.Linq;

namespace HYMonitors
{
    static class MonitorBuilder
    {
        private static readonly string xmlPath = "Monitors.xml";

        internal static Dictionary<string, Monitor> Build()
        {
            // todo
            var monitors = new Dictionary<string, Monitor>();
            var configs = LoadConfig(xmlPath);
            foreach (var monitorConfig in configs)
            {
                var monitor = CreateMonitor(monitorConfig);
                monitors.Add(monitor.Name, monitor);
            }
            return monitors;
        }

        internal static List<MonitorConfig> LoadConfig(string xmlpath)
        {
            var doc = XDocument.Load(xmlpath);
            var monitors = doc.Element("Monitors").Elements("Monitor");
            var monitorsConf = from monitor in doc.Element("Monitors").Elements("Monitor")
                               select new MonitorConfig()
                               {
                                   Name = monitor.Element("Name").Value,
                                   MonitoredObjs = (from monitoredObj in monitor.Element("MonitoredObjs").Elements()
                                                    select new MonitoredObjConfig()
                                                    {
                                                        Name = monitoredObj.Element("Name").Value,
                                                        Desc = monitoredObj.Element("Desc").Value,
                                                        Type = monitoredObj.Element("Type").Value,
                                                        Property1 = monitoredObj.Element("Property1") == null ? null : monitoredObj.Element("Property1").Value,
                                                        Property2 = monitoredObj.Element("Property2") == null ? null : monitoredObj.Element("Property2").Value
                                                    }).ToList()
                               };
            return monitorsConf.ToList();
        }

        private static Monitor CreateMonitor(MonitorConfig config)
        {
            var monitor = new Monitor() { Name = config.Name };

            foreach (var mo in config.MonitoredObjs)
            {
                var monitoredObj = MonitoredObjFactory.CreateMonitoredObj(mo);
                monitor.MonitoredObjs.Add(monitoredObj.Name, monitoredObj);
            }

            return monitor;
        }
    }

    static class MonitoredObjFactory
    {
        internal static BaseMonitoredObj CreateMonitoredObj(MonitoredObjConfig config)
        {
            switch (config.Type)
            {
                case "MonitoredWebSite":
                    return CreateMonitoredIISApp(config);
                case "MonitoredService":
                    return CreateMonitoredService(config);
                case "MonitoredSchTask":
                    return CreateMonitoredSchTask(config);
                default:
                    break;
            }
            //扩展的自定义类
            if (config.Type.StartsWith("CustomMonitoredObj"))
            {
                return CreateCustomMonitoredObj(config);
            }
            else
            {
                throw new Exception(string.Format("未知的监视类型: {0}", config.Type));
            }
        }

        private static BaseMonitoredObj CreateMonitoredIISApp(MonitoredObjConfig config)
        {
            var monitoredObj = new MonitoredIISApp(new MonitoredIISPool(new MonitoredIIS())
            {
                Name = config.Property1
            })
            {
                Name = config.Name,
                Desc = config.Desc,
                HasLog = false,
                Url = config.Property2
            };

            return monitoredObj;
        }

        private static BaseMonitoredObj CreateMonitoredService(MonitoredObjConfig config)
        {
            var monitoredObj = new MonitoredService
            {
                Name = config.Name,
                Desc = config.Desc,
                HasLog = false
            };
            return monitoredObj;
        }

        private static BaseMonitoredObj CreateMonitoredSchTask(MonitoredObjConfig config)
        {
            var monitoredObj = new MonitoredSchTask
            {
                Name = config.Name,
                Desc = config.Desc,
                HasLog = false,
                ProcessFile = config.Property1
            };
            return monitoredObj;
        }

        private static BaseMonitoredObj CreateCustomMonitoredObj(MonitoredObjConfig config)
        {
            var typeName = string.Format("{0}.{1}", "HYMonitors", config.Type);
            var type = Assembly.GetExecutingAssembly().GetType(typeName);
            var monitoredObj = Activator.CreateInstance(type);

            #region set Name
            var nameProp = type.GetProperty("Name");
            nameProp.SetValue(monitoredObj, config.Name, null);
            #endregion

            #region set Desc
            var descProp = type.GetProperty("Desc");
            descProp.SetValue(monitoredObj, config.Desc, null);
            #endregion

            return monitoredObj as BaseMonitoredObj;
        }
    }
}
