using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSBM.Repository
{
    abstract class Repository<ID,T>
    {
        private const string Id = "Id";

        public void Insert(T obj)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            using (ITransaction tx = session.BeginTransaction())
            {
                session.Save(obj);
                tx.Commit();
                tx.Dispose();
                session.Close();
            }
        }

        public void Insert(List<T> listObj)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            using (ITransaction tx = session.BeginTransaction())
            {
                int count = 0;
                foreach (T obj in listObj)
                {
                    session.Save(obj);
                    if (count == 100)
                    {
                        session.Flush();
                        session.Clear();
                        count = 0;
                    }
                    count++;
                }
                tx.Commit();
                tx.Dispose();
                session.Close();
            }
        }

        public void Update(T obj)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            using (ITransaction tx = session.BeginTransaction())
            {
                session.Update(obj);
                tx.Commit();
                tx.Dispose();
                session.Close();
            }
        }

        public void Update(List<T> listObj)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            using (ITransaction tx = session.BeginTransaction())
            {
                int count = 0;
                foreach (T obj in listObj)
                {
                    session.Update(obj);
                    if (count == 100)
                    {
                        session.Flush();
                        session.Clear();
                        count = 0;
                    }
                    count++;
                }
                tx.Commit();
                tx.Dispose();
                session.Close();
            }
        }

        public void Delete(T obj)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            using (ITransaction tx = session.BeginTransaction())
            {
                session.Delete(obj);
                tx.Commit();
                tx.Dispose();
                session.Close();
            }
        }

        public List<T> FindAll()
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                return (List<T>)session.CreateCriteria(typeof(T)).List<T>();
            }
        }

        public T FindById(ID id)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                return session.CreateCriteria(typeof(T)).Add(Restrictions.Eq(Id, id))
                    .UniqueResult<T>();
            }
        }

    }
}
