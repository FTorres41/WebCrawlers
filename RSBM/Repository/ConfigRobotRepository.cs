using NHibernate;
using NHibernate.Criterion;
using RSBM.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSBM.Repository
{
    class ConfigRobotRepository : Repository<int, ConfigRobot>
    {
        public ConfigRobot FindByName(string name)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                ConfigRobot config = session.CreateCriteria(typeof(ConfigRobot))
                    .Add(Restrictions.Eq("Name", name))
                    .UniqueResult<ConfigRobot>();

                session.Close();

                return config;
            }
        }
    }
}
