using RSBM.Models;
using RSBM.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSBM.Controllers
{
    class FontePesquisaRobotController
    {
        public static void Criar(FontePesquisaRobot fpr)
        {
            FontePesquisaRobotRepository fprr = new FontePesquisaRobotRepository();
            fprr.Insert(fpr);
        }
    }
}
