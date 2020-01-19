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
    class LicitacaoHistoricoRepository : Repository<int, LicitacaoHistorico>
    {
        internal bool IfExist(LicitacaoHistorico historico)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                List<LicitacaoHistorico> historicos = (List<LicitacaoHistorico>)session.CreateCriteria(typeof(LicitacaoHistorico))
                    .Add(Restrictions.Eq("Historico", historico.Historico))
                    .Add(Restrictions.Eq("DataCadastro", historico.DataCadastro))
                    .List<LicitacaoHistorico>();

                session.Close();

                if (historicos.Count > 0)
                {
                    //RService.Log("(HistoricFiles) CNETHT: Item de histórico, já existente. at {0}", Path.GetTempPath() + "CNETHT.txt");
                    return true;
                }

                return false;
            }
        }
    }
}
