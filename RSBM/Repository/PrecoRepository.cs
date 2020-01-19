using NHibernate;
using NHibernate.Criterion;
using RSBM.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSBM.Repository
{
    class PrecoRepository : Repository<int, Preco>
    {
        internal bool Exists(Preco preco)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                session.Clear();
                List<Preco> precos = (List<Preco>)session.CreateCriteria(typeof(Preco))
                    .Add(Restrictions.Eq("Item", preco.Item))
                    .Add(Restrictions.Eq("IdLicitacao", preco.IdLicitacao))
                    .Add(Restrictions.Eq("ValorHomologado", preco.ValorHomologado))
                    .Add(Restrictions.Eq("Descricao", preco.Descricao))
                    .List<Preco>();

                session.Close();

                return precos.Count > 0;
            }
        }
    }
}
