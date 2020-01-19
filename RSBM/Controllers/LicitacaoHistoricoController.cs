using RSBM.Models;
using RSBM.Repository;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSBM.Controllers
{
    class LicitacaoHistoricoController
    {
        internal static bool Insert(LicitacaoHistorico historico)
        {
            LicitacaoHistoricoRepository repoH = new LicitacaoHistoricoRepository();

            if (!repoH.IfExist(historico))
            {
                try
                {
                    repoH.Insert(historico);
                    return true;
                }
                catch(Exception ex)
                {
                    RService.Log("Exception (Insert) CNETHT: " + ex.Message + " / " + ex.StackTrace + " / " + ex.InnerException + " at {0}", Path.GetTempPath() + "CNETHT.txt");
                    return false;
                }
            }
            return false;
        }
    }
}
