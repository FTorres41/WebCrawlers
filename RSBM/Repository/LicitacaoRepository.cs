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
    class LicitacaoRepository : Repository<int, Licitacao>
    {
        internal List<Licitacao> FindBySite(string site)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                List<Licitacao> licitacoes = session.QueryOver<Licitacao>().Where(x => x.LinkSite == site).List<Licitacao>().ToList();

                //List<Licitacao> licitacoes = (List<Licitacao>)session.CreateCriteria(typeof(Licitacao))
                //    .Add(Restrictions.Eq("LinkSite", site))
                //    .Add(Restrictions.Eq("Excluido", 0))
                //    .List<Licitacao>();

                //session.Close();
                session.Disconnect();

                return licitacoes;
            }
        }

        internal HashSet<long> FindBySite(int idFonte, DateTime data)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                try
                {
                    var query = session.CreateSQLQuery(string.Format("SELECT idlicitacao_fonte FROM licitacoes WHERE id_fonte = {0} AND abertura_data > '{1}'", idFonte, data.ToString("yyyy-MM-dd"))).List();

                    var result = new HashSet<long>();

                    for (int i = 0; i < query.Count; i++)
                    {
                        if (query[i] != null)
                        {
                            result.Add(Int64.Parse(query[i].ToString()));
                        }
                    }

                    //List<Licitacao> licitacoes = (List<Licitacao>)session.CreateCriteria(typeof(Licitacao))
                    //    .Add(Restrictions.Eq("IdFonte", idFonte))
                    //    .Add(Restrictions.Ge("AberturaData", data))
                    //    .List<Licitacao>();

                    //session.Close();
                    session.Disconnect();

                    return result;
                }
                catch (Exception e)
                {
                    return null;
                }
            }
        }

        internal List<Licitacao> FindBySiteAndAberturaGratherThan(DateTime now, string site)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                List<Licitacao> licitacoes = (List<Licitacao>)session.CreateCriteria(typeof(Licitacao))
                    .Add(Restrictions.Eq("LinkSite", site))
                    .Add(Restrictions.Gt("AberturaData", now))
                    .Add(Restrictions.Eq("Excluido", 0))
                    .List<Licitacao>();

                //session.Close();
                session.Disconnect();

                return licitacoes;
            }
        }

        internal List<Licitacao> FindBySiteHistoric(string site, DateTime dataLimite)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                var licitacoes = session.QueryOver<Licitacao>().Where(x => x.LinkSite == site && x.Excluido == 0).List<Licitacao>();

                //List<Licitacao> licitacoes = (List<Licitacao>)session.CreateCriteria(typeof(Licitacao))
                //    .Add(Restrictions.Eq("LinkSite", site))
                //    .Add(Restrictions.Eq("Excluido", 0))
                //    .Add(Restrictions.Ge("AberturaData", dataLimite))
                //    .List<Licitacao>();

                session.Disconnect();

                return licitacoes.ToList();
            }
        }

        internal List<Licitacao> FindByRangeHistoric(string site, DateTime dataLimite)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                var licitacoes = session.QueryOver<Licitacao>().Where(x => x.LinkSite == site && x.Excluido == 0).List<Licitacao>();

                //List<Licitacao> licitacoes = (List<Licitacao>)session.CreateCriteria(typeof(Licitacao))
                //    .Add(Restrictions.Eq("LinkSite", site))
                //    .Add(Restrictions.Eq("Excluido", 0))
                //    .Add(Restrictions.Between("AberturaData", dataLimite, DateTime.Now.AddDays(30)))
                //    .List<Licitacao>();

                session.Disconnect();

                return licitacoes.ToList();
            }
        }

        internal int GetIdByObservacoes(string observacoes)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                var query = session.CreateSQLQuery(string.Format("SELECT idlicitacoes FROM licitacoes WHERE MATCH observacoes AGAINST ('{0}')", observacoes));
                var id = query.UniqueResult();

                return Int32.Parse(id.ToString());
            }
        }

        public new void Insert(Licitacao obj)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            using (ITransaction tx = session.BeginTransaction())
            {
                //ISQLQuery query = session.CreateSQLQuery("SELECT CURRENT_TIMESTAMP FROM DUAL").AddScalar("CURRENT_TIMESTAMP", NHibernateUtil.DateTime);
                //obj.AcessoData = query.UniqueResult<DateTime>();
                session.SaveOrUpdate(obj);
                tx.Commit();
                tx.Dispose();
                session.Close();
            }
        }

        public new void Insert(List<Licitacao> listObj)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            using (ITransaction tx = session.BeginTransaction())
            {
                int count = 0;
                foreach (Licitacao obj in listObj)
                {
                    //ISQLQuery query = session.CreateSQLQuery("SELECT CURRENT_TIMESTAMP FROM DUAL").AddScalar("CURRENT_TIMESTAMP", NHibernateUtil.DateTime);
                    //obj.AcessoData = query.UniqueResult<DateTime>();
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

        internal Licitacao GetByIdLicitacaoFonte(string idLicitacaoFonte)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                var result = session.QueryOver<Licitacao>().Where(x => x.IdLicitacaoFonte == Int64.Parse(idLicitacaoFonte)).List<Licitacao>();

                //List<Licitacao> licitacoes = (List<Licitacao>)session.CreateCriteria(typeof(Licitacao))
                //    .Add(Restrictions.Eq("IdLicitacaoFonte", Int64.Parse(idLicitacaoFonte)))
                //    .List<Licitacao>();

                session.Close();

                return result.FirstOrDefault();
            }
        }

        internal void UpdateSituacaoByIdLicitacao(int id, string situacao)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                ISQLQuery query = session.CreateSQLQuery(string.Format("UPDATE licitacoes SET situacao = '{0}' WHERE idlicitacoes = {1}", situacao, id));
                int result = query.ExecuteUpdate();

                session.Close();
            }
        }

        internal Licitacao GetByObservacoes(string observacoes)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                ISQLQuery query = session.CreateSQLQuery(string.Format("SELECT * FROM licitacoes WHERE MATCH observacoes AGAINST ('{0}')", observacoes)).AddEntity(typeof(Licitacao));


                //var result = session.QueryOver<Licitacao>().Where(x => x.Observacoes == observacoes).List<Licitacao>();
                //List<Licitacao> licitacoes = (List<Licitacao>)session.CreateCriteria(typeof(Licitacao))
                //    .Add(Restrictions.Eq("Observacoes", observacoes))
                //    .List<Licitacao>();

                //session.Close();
                session.Disconnect();

                return new Licitacao();
            }
        }

        internal List<Licitacao> FindBySituationBB()
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                session.Clear();

                var result = session.QueryOver<Licitacao>().Where(x => x.IdFonte == 509
                                                                    && x.Situacao != "Encerrada"
                                                                     && x.Situacao != "Homologada"
                                                                      && x.Situacao != "Suspensa"
                                                                       && x.AberturaData > DateTime.Today.AddMonths(-6))
                                                                        .List<Licitacao>();

                //var result = (List<Licitacao>)session.CreateCriteria(typeof(Licitacao))
                //    .Add(Restrictions.Eq("IdFonte", 509))
                //    .Add(Restrictions.Gt("AberturaData", DateTime.Today.AddMonths(-3)))
                //    .Add(!Restrictions.Eq("Situacao", "Encerrada"))
                //    .Add(!Restrictions.Eq("Situacao", "Homologada"))
                //    .Add(!Restrictions.Eq("Situacao", "Suspensa"))
                //    .List<Licitacao>();

                //session.Close();
                session.Disconnect();

                return result.ToList();
            }
        }

        internal bool ExistsTCERS(long idLicitacaoFonte, int idFonte)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                session.Clear();

                var result = session.QueryOver<Licitacao>().Select(y => y.Id).Where(x => x.IdLicitacaoFonte == idLicitacaoFonte && x.IdFonte == idFonte).List<int>();

                //List<Licitacao> licitacoes = (List<Licitacao>)session.CreateCriteria(typeof(Licitacao))
                //    .Add(Restrictions.Eq("IdLicitacaoFonte", idLicitacaoFonte))
                //    .Add(Restrictions.Eq("IdFonte", idFonte))
                //    .List<Licitacao>();

                //session.Close();
                session.Disconnect();

                return result.Count == 0 ? false : true;
            }
        }

        internal bool Exists(string idLicitacaoFonte)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                session.Clear();

                var result = session.QueryOver<Licitacao>().Select(y => y.IdLicitacaoFonte).Where(l => l.IdLicitacaoFonte == Int64.Parse(idLicitacaoFonte)).List<long>();

                //List<Licitacao> licitacoes = (List<Licitacao>)session.CreateCriteria(typeof(Licitacao))
                //    .Add(Restrictions.Eq("IdLicitacaoFonte", Int64.Parse(idLicitacaoFonte)))
                //    .List<Licitacao>();

                //session.Close();
                session.Disconnect();

                return result.Count == 0 ? false : true;
            }
        }

        internal bool ExistsCNET(string uasg, string pregao, int modalidade)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                var result = session.QueryOver<Licitacao>().Select(y => y.IdLicitacaoFonte)
                                                           .Where(l => l.Uasg == uasg && l.Num == pregao && l.Modalidade.Id == modalidade)
                                                           .List<long>();

                session.Disconnect();

                return result.Count == 0 ? false : true;
            }
        }

        internal bool ExistsCNET(long idFonte)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                var result = session.QueryOver<Licitacao>().Select(y => y.IdLicitacaoFonte)
                                                           .Where(l => l.IdLicitacaoFonte == idFonte)
                                                           .List<long>();

                session.Disconnect();

                return result.Count == 0 ? false : true;
            }
        }

        internal bool ExistsBEC(string observacoes)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                ISQLQuery query = session.CreateSQLQuery(string.Format("SELECT observacoes FROM licitacoes WHERE MATCH observacoes AGAINST ('{0}')", observacoes));
                var result = query.UniqueResult<string>();

                //var result = session.QueryOver<Licitacao>().Select(y => y.Id).Where(x => x.Observacoes == observacoes).List<int>();

                //List<Licitacao> licitacoes = (List<Licitacao>)session.CreateCriteria(typeof(Licitacao))
                //    .Add(Restrictions.Eq("Observacoes", observacoes))
                //    .List<Licitacao>();

                //session.Close();
                session.Disconnect();

                return result == null ? false : true;
            }
        }

        internal bool SituacaoAlterada(string idLicitacaoFonte, string situacao)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                var result = session.QueryOver<Licitacao>().Select(x => x.Id).Where(y => y.IdLicitacaoFonte == Int64.Parse(idLicitacaoFonte) && y.Situacao == situacao).List<int>();

                //List<Licitacao> licitacoes = (List<Licitacao>)session.CreateCriteria(typeof(Licitacao))
                //    .Add(Restrictions.Eq("IdLicitacaoFonte", Int64.Parse(idLicitacaoFonte)))
                //    .Add(Restrictions.Eq("Situacao", situacao))
                //    .List<Licitacao>();

                //session.Close();
                session.Disconnect();

                return result.Count == 0 ? true : false;
            }
        }

        internal bool SituacaoAlteradaBEC(string observacoes, string situacao)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                var result = session.QueryOver<Licitacao>().Select(x => x.Id).Where(y => y.Observacoes == observacoes && y.Situacao == situacao).List<int>();

                //List<Licitacao> licitacoes = (List<Licitacao>)session.CreateCriteria(typeof(Licitacao))
                //    .Add(Restrictions.Eq("Observacoes", observacoes))
                //    .Add(Restrictions.Eq("Situacao", situacao))
                //    .List<Licitacao>();

                //session.Close();
                session.Disconnect();

                return result.Count == 0 ? true : false;
            }
        }

        internal bool Exists(string objeto, string valorMax, string observacaoes, string num, string processo, string linkSite)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                List<Licitacao> licitacoes = new List<Licitacao>();
                try
                {
                    licitacoes = (List<Licitacao>)session.CreateCriteria(typeof(Licitacao))
                        .Add(Restrictions.Eq("Objeto", objeto))
                        .Add(Restrictions.Eq("ValorMax", valorMax))
                        .Add(Restrictions.Eq("Observacoes", observacaoes))
                        .Add(Restrictions.Eq("Num", num))
                        .Add(Restrictions.Eq("Processo", processo))
                        .Add(Restrictions.Eq("LinkSite", linkSite))
                        .AddOrder(Order.Desc("DigitacaoData"))
                        .List<Licitacao>();

                }
                catch (Exception e)
                {

                }

                //session.Close();
                session.Disconnect();

                return licitacoes.Count == 0 ? false : true;
            }
        }

        public static Licitacao FindByUASG(string uasg)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                try
                {
                    List<Licitacao> licitacoes = (List<Licitacao>)session.CreateCriteria(typeof(Licitacao))
                    .Add(Restrictions.Eq("Uasg", uasg))
                    .List<Licitacao>();

                    //session.Close();
                    session.Disconnect();

                    return licitacoes[0];
                }
                catch (Exception)
                {
                    //session.Close();
                    session.Disconnect();

                    return null;
                }

            }
        }

        public static Licitacao FindByOrgao(string orgao)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                List<Licitacao> licitacoes = (List<Licitacao>)session.CreateCriteria(typeof(Licitacao))
                    .Add(Restrictions.Eq("Departamento", orgao))
                    .List<Licitacao>();

                //session.Close();
                session.Disconnect();

                return licitacoes.Count() > 0 ? licitacoes[0] : null;
            }
        }
    }
}
