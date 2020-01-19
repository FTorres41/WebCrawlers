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
    class FontePesquisaRepository : Repository<int, FontePesquisa>
    {
        public List<FontePesquisa> FindByActiveRobot()
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                var result = (List<FontePesquisa>)session.CreateCriteria(typeof(FontePesquisa))
                    .Add(Restrictions.Eq("AtivarRobot", 1))
                    .List<FontePesquisa>();

                session.Close();

                return result;
            }
        }

        public List<FontePesquisa> FindByElement()
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                var result = (List<FontePesquisa>)session.CreateCriteria(typeof(FontePesquisa))
                    .Add(Expression.Or(
                        Restrictions.Like("Regex", "%html/body%"),
                        Restrictions.Like("Regex", "%//*%")))
                    .Add(Restrictions.Eq("AtivarRobot", 1))
                    .Add(Restrictions.Eq("Excluido", 0))
                    .List<FontePesquisa>();

                session.Close();

                return result;
            }
        }

        public List<FontePesquisa> FindByRegex()
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                var result = (List<FontePesquisa>)session.CreateCriteria(typeof(FontePesquisa))
                    .Add(Restrictions.Not(Restrictions.Like("Regex", "%//*%")))
                    .Add(Restrictions.Not(Restrictions.Like("Regex", "%/html/body%")))
                    .Add(Restrictions.Eq("AtivarRobot", 1))
                    .Add(Restrictions.Eq("Excluido", 0))
                    .List<FontePesquisa>();

                session.Close();

                return result;
            }

        }
    }
}
