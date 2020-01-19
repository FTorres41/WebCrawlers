using RSBM.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace RSBM
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {

            if (System.Diagnostics.Debugger.IsAttached)
            {
#if DEBUG
                RService service1 = new RService();
                service1.StartDebug(new string[2]);
                System.Threading.Thread.Sleep(1000);
#endif
            }
            else
            {

                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                new RService()
                };
                ServiceBase.Run(ServicesToRun);

            }
        }
    }
}
