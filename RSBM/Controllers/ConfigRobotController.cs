using RSBM.Models;
using RSBM.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSBM.Controllers
{
    class ConfigRobotController
    {
        public static string Interval { get { return "INTERVAL"; } }
        public static string Daily { get { return "DAILY"; } }

        public static ConfigRobot FindByName(string name)
        {
            ConfigRobotRepository config = new ConfigRobotRepository();
            return config.FindByName(name);
        }

        public static void Update(ConfigRobot config)
        {
            ConfigRobotRepository repoc = new ConfigRobotRepository();
            repoc.Update(config);
        }
    }
}
