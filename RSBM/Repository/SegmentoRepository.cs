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
    class SegmentoRepository : Repository<int, Segmento>
    {
        internal List<Segmento> GetSegmentos()
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                List<Segmento> segmentos = (List<Segmento>)session.CreateCriteria(typeof(Segmento))
                    .Add(Restrictions.Not(Restrictions.Eq("PalavrasChave", "")))
                    .List<Segmento>();
                
                session.Close();

                return segmentos;
            }
        }

        internal List<Segmento> GetSegmentosLeilao()
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                List<Segmento> segmentos = (List<Segmento>)session.CreateCriteria(typeof(Segmento))
                    .Add(Restrictions.Eq("Filiacao", 156))
                    .Add(Restrictions.Not(Restrictions.Eq("PalavrasChave", "")))
                    .List<Segmento>();

                session.Close();

                return segmentos;
            }
        }

        internal List<Segmento> GetSegmentosHumanos()
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                List<Segmento> segmentos = (List<Segmento>)session.CreateCriteria(typeof(Segmento))
                    .Add(Restrictions.Not(Restrictions.Eq("Filiacao", 1)))
                    .Add(Restrictions.Not(Restrictions.Eq("PalavrasChave", "")))
                    .List<Segmento>();

                session.Close();

                return segmentos;
            }
        }

        internal List<Segmento> GetSegmentosConcessao()
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                List<Segmento> segmentos = (List<Segmento>)session.CreateCriteria(typeof(Segmento))
                    .Add(Restrictions.Eq("Filiacao", 169))
                    .Add(Restrictions.Not(Restrictions.Eq("PalavrasChave", "")))
                    .List<Segmento>();

                session.Close();

                return segmentos;
            }
        }

        internal List<Segmento> GetSegmentosVeterinaria()
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                List<Segmento> segmentos = (List<Segmento>)session.CreateCriteria(typeof(Segmento))
                    .Add(Restrictions.Not(Restrictions.Eq("Filiacao", 17)))
                    .Add(Restrictions.Not(Restrictions.Eq("PalavrasChave", "")))
                    .List<Segmento>();

                session.Close();

                return segmentos;
            }
        }

        internal static List<int> ObterSegmentos(string descricao, int id)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                IList<int> query = session.CreateSQLQuery(
                                            string.Format("SELECT id_segmento_lm FROM segmentos_fonte WHERE descricao_item = '{0}'", descricao))
                                            .List<int>();

                return (List<int>)query;
            }
        }

        internal static void InserirSegmentacao(List<int> segmentos, int id)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                for (int i = 0; i < segmentos.Count; i++)
                {
                    session.CreateSQLQuery(string.Format("INSERT INTO licitacao_segmento(idlicitacao, idsegmento) VALUES ({0}, {1})", id, segmentos[i]))
                            .ExecuteUpdate();
                }
            }
        }
    }
}
