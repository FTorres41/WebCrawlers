using NHibernate;
using NHibernate.Criterion;
using RSBM.Models;
using System.Collections.Generic;

namespace RSBM.Repository
{
    class LicitacaoArquivoRepository : Repository<int, LicitacaoArquivo>
    {
        internal List<LicitacaoArquivo> FindByLicitacao(int IdLicitacao)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                List<LicitacaoArquivo> arquivos = (List<LicitacaoArquivo>)session.CreateCriteria(typeof(LicitacaoArquivo))
                    .Add(Restrictions.Eq("IdLicitacao", IdLicitacao))
                    .List<LicitacaoArquivo>();

                session.Close();

                return arquivos;
            }
        }
    }
}
