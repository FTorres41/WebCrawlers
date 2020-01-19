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
    class LoteController
    {

        /*Cria um novo lote na base de dados.*/
        public static Lote CreateLote(int idCadastrador, int idFonte)
        {
            try {
                Lote lote = new Lote();
                lote.Data = DateTime.Now;
                lote.DataCadastroLote = DateTime.Now;
                lote.IdCadastrador = idCadastrador;
                lote.IdDigitador = idCadastrador;
                lote.IdFonte = idFonte;
                lote.Status = 1;

                LoteRepository repo = new LoteRepository();
                repo.Insert(lote);

                return lote;
            }
            catch (Exception e)
            {
                RService.Log("RService Exception: (CreateLote) " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
            }
            return null;
        }

        internal static void Delete(Lote lote)
        {
            LoteRepository repo = new LoteRepository();
            repo.Delete(lote);
        }
    }
}
