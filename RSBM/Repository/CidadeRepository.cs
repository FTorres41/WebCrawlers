using NHibernate;
using NHibernate.Criterion;
using RSBM.Models;
using System.Collections.Generic;
using System.Linq;

namespace RSBM.Repository
{
    class CidadeRepository : Repository<int, Cidade>
    {
        public List<Cidade> FindAllCities()
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                List<Cidade> cidades = (List<Cidade>)session.CreateCriteria(typeof(Cidade))
                    .Add(Restrictions.Eq("SubCidade", 0))
                    .List<Cidade>();

                session.Close();

                return cidades;
            }
        }

        public List<Cidade> FindByUf(string uf)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                List<Cidade> cidades = (List<Cidade>)session.CreateCriteria(typeof(Cidade))
                    .Add(Restrictions.Eq("IdUf", uf))
                    .Add(Restrictions.Eq("SubCidade", 0))
                    .List<Cidade>();

                session.Close();

                return cidades;
            }
        }

        public int FindIdByCity(string cidade, string uf)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                var cidades = (List<Cidade>)session.CreateCriteria(typeof(Cidade))
                    .Add(Restrictions.Eq("Nome", cidade))
                    .Add(Restrictions.Eq("IdUf", uf))
                    .Add(Restrictions.Eq("SubCidade", 0))
                    .List<Cidade>();

                session.Close();

                return cidade.Count() > 0 ? cidades[0].Id : 0;
            }
        }
    }
}
