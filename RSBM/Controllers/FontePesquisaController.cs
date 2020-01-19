using RSBM.Models;
using RSBM.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSBM.Controllers
{
    class FontePesquisaController
    {
        public static void Atualizar(FontePesquisa fp)
        {
            FontePesquisaRepository fprepo = new FontePesquisaRepository();
            fprepo.Update(fp);
        }
    }
}
