using RSBM.Models;
using RSBM.Repository;
using System;
using System.Collections.Generic;
using System.Text;

namespace RSBM.Controllers
{
    class LicitacaoController
    {
        public static HashSet<long> GetAlreadyInserted(string site)
        {
            HashSet<long> numeros = new HashSet<long>();

            foreach (Licitacao licitacao in new LicitacaoRepository().FindBySite(site))
            {
                numeros.Add(licitacao.IdLicitacaoFonte);
            }
            return numeros;
        }

        public static HashSet<long> GetAlreadyInserted(int idFonte, DateTime data)
        {
            return new LicitacaoRepository().FindBySite(idFonte, data);
        }

        internal static List<Licitacao> GetAberturaGratherThan(DateTime now, string site)
        {
            LicitacaoRepository repo = new LicitacaoRepository();
            return repo.FindBySiteAndAberturaGratherThan(now, site);
        }

        internal static List<Licitacao> FindBySiteHistoric(string site, DateTime dataLimite)
        {
            LicitacaoRepository repo = new LicitacaoRepository();
            return repo.FindBySiteHistoric(site, dataLimite);
        }

        internal static List<Licitacao> FindByRangeHistoric(string site, DateTime dataLimite)
        {
            LicitacaoRepository repo = new LicitacaoRepository();
            return repo.FindByRangeHistoric(site, dataLimite);
        }

        internal static bool IsValid(Licitacao licitacao, out string mensagemErro)
        {
            StringBuilder sb = new StringBuilder();
            mensagemErro = sb.ToString();

            if (licitacao != null)
            {
                if (licitacao.AberturaData != null)
                {
                    if (licitacao.AberturaData < DateTime.Today)
                       sb.Append("Licitação vencida. ");
                } 
                else
                {
                    sb.Append("Data de abertura inválida/nula. ");
                }

                if (licitacao.IdFonte == null)
                    sb.Append("Fonte inválida. ");

                if (licitacao.Orgao == null)
                    sb.Append("Orgão Inválido. ");

                if (licitacao.Num == null)
                    sb.Append("Número inválido. ");

                if (licitacao.Modalidade == null)
                    sb.Append("Modalidade inválida. ");

                if (licitacao.Objeto == null)
                    sb.Append("Descrição do Objeto inválida. ");

                if (licitacao.EstadoFonte == null)
                    sb.Append("Estado fonte inválido. ");

                if (licitacao.CidadeFonte == null)
                    sb.Append("Cidade fonte inválida. ");

                if (sb.Length > 0)
                {
                    mensagemErro = sb.ToString();
                    return false;
                }
                else
                    return true;
            }
            else
            {
                mensagemErro = "Licitação nula. ";
                return false;
            }
        }

        internal static Licitacao GetByIdLicitacaoFonte(string idLicitacaoFonte)
        {
            LicitacaoRepository repo = new LicitacaoRepository();
            return repo.GetByIdLicitacaoFonte(idLicitacaoFonte);
        }

        internal static int GetIdByObservacoes(string observacoes)
        {
            LicitacaoRepository repo = new LicitacaoRepository();
            return repo.GetIdByObservacoes(observacoes);
        }

        internal static Licitacao GetByObservacoes(string observacoes)
        {
            LicitacaoRepository repo = new LicitacaoRepository();
            return repo.GetByObservacoes(observacoes);
        }

        internal static bool Exists(string idLicitacaoFonte)
        {
            LicitacaoRepository repo = new LicitacaoRepository();
            return repo.Exists(idLicitacaoFonte);
        }

        internal static bool ExistsCNET(string uasg, string pregao, int modalidade)
        {
            LicitacaoRepository repo = new LicitacaoRepository();
            return repo.ExistsCNET(uasg, pregao, modalidade);
        }

        internal static bool ExistsCNET(long idFonte)
        {
            LicitacaoRepository repo = new LicitacaoRepository();
            return repo.ExistsCNET(idFonte);
        }

        internal static bool ExistsBEC(string observacoes)
        {
            LicitacaoRepository repo = new LicitacaoRepository();
            return repo.ExistsBEC(observacoes);
        }

        internal static bool SituacaoAlterada(string idLicitacaoFonte, string situacao)
        {
            LicitacaoRepository repo = new LicitacaoRepository();
            return repo.SituacaoAlterada(idLicitacaoFonte, situacao);
        }

        internal static bool SituacaoAlteradaBEC(string observacoes, string situacao)
        {
            LicitacaoRepository repo = new LicitacaoRepository();
            return repo.SituacaoAlteradaBEC(observacoes, situacao);
        }

        /*Busca a licitação item por item*/
        internal static bool Exists(string objeto, string valorMax, string observacaoes, string num, string processo, string linkSite)
        {
            LicitacaoRepository repo = new LicitacaoRepository();
            return repo.Exists(objeto, valorMax, observacaoes, num, processo, linkSite);
        }

        internal static List<Licitacao> FindBySituationBB()
        {
            LicitacaoRepository repo = new LicitacaoRepository();
            return repo.FindBySituationBB();
        }

        internal static bool ExistsTCERS(long idLicitacaoFonte, int idFonte)
        {
            LicitacaoRepository repo = new LicitacaoRepository();
            return repo.ExistsTCERS(idLicitacaoFonte, idFonte);
        }

        internal static void UpdateSituacaoByIdLicitacao(int id, string situacao)
        {
            LicitacaoRepository repo = new LicitacaoRepository();
            repo.UpdateSituacaoByIdLicitacao(id, situacao);
        }
    }
}
