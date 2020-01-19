using NHibernate;
using NHibernate.Cfg;
using RSBM.Models;
using System;
using System.IO;

namespace RSBM.Repository
{
    class NHibernateHelper
    {
        private static string ConnectionString { get; } = "Server=127.0.0.1;Port=3306;Database=*******;Uid=*******;Pwd=***********;Convert Zero Datetime=True;Allow Zero Datetime=True;";

        private static ISessionFactory _sessionFactory;
        private static ISessionFactory SessionFactory
        {
            get
            {
                try
                {
                    if (_sessionFactory == null)
                        CreateSessionFactory();

                    return _sessionFactory;
                }
                catch (Exception e)
                {
                    RService.Log("Exception : " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
                    return null;
                }
            }
        }

        public static ISession OpenSession()
        {
            return SessionFactory.OpenSession();
        }

        public static IStatelessSession OpenStatelessSession()
        {
            return SessionFactory.OpenStatelessSession();
        }

        private static void CreateSessionFactory()
        {
            var configuration = new Configuration();
            configuration.SetProperty(NHibernate.Cfg.Environment.ConnectionString, ConnectionString);
            configuration.Configure();

            configuration.AddAssembly(typeof(ConfigRobot).Assembly);

            _sessionFactory = configuration.BuildSessionFactory();
        }

    }
}
