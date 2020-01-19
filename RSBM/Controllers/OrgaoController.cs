using RSBM.Models;
using RSBM.Repository;
using System.Collections.Generic;
using System;
using RSBM.Util;

namespace RSBM.Controllers
{
    class OrgaoController
    {

        public static Dictionary<string,Orgao> GetNomeUfToOrgao()
        {
            OrgaoRepository repo = new OrgaoRepository();
            List<Orgao> orgaos = repo.FindAll();
            Dictionary<string, Orgao> nomeToOrgao = new Dictionary<string, Orgao>();
            foreach (Orgao org in orgaos)
            {
                if (!string.IsNullOrEmpty(org.Nome) && !string.IsNullOrEmpty(org.Estado)
                    && !nomeToOrgao.ContainsKey(StringHandle.RemoveAccent(org.Nome.Trim().ToUpper() + ":" + org.Estado.Trim().ToUpper())))

                    nomeToOrgao.Add(StringHandle.RemoveAccent(org.Nome.Trim().ToUpper() + ":" + org.Estado.Trim().ToUpper()), org);
            }
            return nomeToOrgao;
        }

        internal static Orgao FindById(int id)
        {
            OrgaoRepository repo = new OrgaoRepository();
            return repo.FindById(id);
        }

        public static Orgao GetOrgaoByNameAndUf(string nomeUf, Dictionary<string , Orgao> nameToOrgao)
        {
            OrgaoRepository repo = new OrgaoRepository();
            if (!nameToOrgao.ContainsKey(StringHandle.RemoveAccent(nomeUf)))
            {
                Orgao org = new Orgao();
                org.Estado = nomeUf.Split(':')[1];
                org.Nome = nomeUf.Split(':')[0];

                if (repo == null)
                {
                    repo = new OrgaoRepository();
                }
                repo.Insert(org);
                return org;
            }
            else
            {
                return nameToOrgao[StringHandle.RemoveAccent(nomeUf)];
            }
        }
    }
}
