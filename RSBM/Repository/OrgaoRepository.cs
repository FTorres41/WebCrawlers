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
    class OrgaoRepository : Repository<int, Orgao>
    {
        public static Orgao CreateOrgao(string nomeDpto, string observacoes)
        {
            Orgao o = new Orgao();

            try
            {
                if (nomeDpto.Contains("- AC") || nomeDpto.Contains("-AC") || nomeDpto.Contains("/AC") || nomeDpto.Contains("ACRE") || nomeDpto.Contains("RIO BRANCO") ||
                    observacoes.Contains("Rio Branco") || observacoes.Contains("Acre") || observacoes.Contains("AC"))
                {
                    o.Nome = nomeDpto;
                    o.Estado = "AC";
                }
                else if (nomeDpto.Contains("- AL") || nomeDpto.Contains("-AL") || nomeDpto.Contains("/AL") || nomeDpto.Contains("ALAGOAS") || nomeDpto.Contains("MACEIÓ") ||
                    observacoes.Contains("Maceió") || observacoes.Contains("Alagoas") || observacoes.Contains("AL"))
                {
                    o.Nome = nomeDpto;
                    o.Estado = "AL";
                }
                else if (nomeDpto.Contains("- AM") || nomeDpto.Contains("-AM") || nomeDpto.Contains("/AM") || nomeDpto.Contains("AMAZONAS") || nomeDpto.Contains("MANAUS") ||
                    observacoes.Contains("Manaus") || observacoes.Contains("Amazonas") || observacoes.Contains("AM"))
                {
                    o.Nome = nomeDpto;
                    o.Estado = "AM";
                }
                else if (nomeDpto.Contains("- AP") || nomeDpto.Contains("-AP") || nomeDpto.Contains("/AP") || nomeDpto.Contains("AMAPÁ") || nomeDpto.Contains("MACAPÁ") ||
                    observacoes.Contains("Macapá") || observacoes.Contains("Amapá") || observacoes.Contains("AP"))
                {
                    o.Nome = nomeDpto;
                    o.Estado = "AP";
                }
                else if (nomeDpto.Contains("- BA") || nomeDpto.Contains("-BA") || nomeDpto.Contains("/BA") || nomeDpto.Contains("BAHIA") || nomeDpto.Contains("SALVADOR") ||
                    observacoes.Contains("Salvador") || observacoes.Contains("Bahia") || observacoes.Contains("BA"))
                {
                    o.Nome = nomeDpto;
                    o.Estado = "BA";
                }
                else if (nomeDpto.Contains("- CE") || nomeDpto.Contains("-CE") || nomeDpto.Contains("/CE") || nomeDpto.Contains("CEARÁ") || nomeDpto.Contains("FORTALEZA") ||
                    observacoes.Contains("Fortaleza") || observacoes.Contains("Ceará") || observacoes.Contains("CE"))
                {
                    o.Nome = nomeDpto;
                    o.Estado = "CE";
                }
                else if (nomeDpto.Contains("DF") || nomeDpto.Contains("DISTRITO FEDERAL") || nomeDpto.Contains("BRASÍLIA") || nomeDpto.Contains("FEDERAL") ||
                    observacoes.Contains("Brasília") || observacoes.Contains("Distrito Federal") || observacoes.Contains("Federal") || observacoes.Contains("DF"))
                {
                    o.Nome = nomeDpto;
                    o.Estado = "DF";
                }
                else if (nomeDpto.Contains("- ES") || nomeDpto.Contains("-ES") || nomeDpto.Contains("/ES") || nomeDpto.Contains("ESPÍRITO SANTO") || nomeDpto.Contains("VITÓRIA") ||
                    observacoes.Contains("Vitória") || observacoes.Contains("Espírito Santo") || observacoes.Contains("ES"))
                {
                    o.Nome = nomeDpto;
                    o.Estado = "ES";
                }
                else if (nomeDpto.Contains("- GO") || nomeDpto.Contains("-GO") || nomeDpto.Contains("/GO") || nomeDpto.Contains("GOIÁS") || nomeDpto.Contains("GOIÂNIA") ||
                    observacoes.Contains("Goiânia") || observacoes.Contains("Goiás") || observacoes.Contains("GO"))
                {
                    o.Nome = nomeDpto;
                    o.Estado = "GO";
                }
                else if (nomeDpto.Contains("- MA") || nomeDpto.Contains("-MA") || nomeDpto.Contains("/MA") || nomeDpto.Contains("MARANHÃO") || nomeDpto.Contains("SÃO LUÍS") ||
                    observacoes.Contains("São Luís") || observacoes.Contains("Maranhão") || observacoes.Contains("MA"))
                {
                    o.Nome = nomeDpto;
                    o.Estado = "MA";
                }
                else if (nomeDpto.Contains("MG") || nomeDpto.Contains("MINAS GERAIS") || nomeDpto.Contains("BELO HORIZONTE") || nomeDpto.Contains("UFMG") ||
                    observacoes.Contains("Belo Horizonte") || observacoes.Contains("Minas Gerais") || observacoes.Contains("MG"))
                {
                    o.Nome = nomeDpto;
                    o.Estado = "MG";
                }
                else if (nomeDpto.Contains("- MS") || nomeDpto.Contains("/MS") || nomeDpto.Contains("-MS") || nomeDpto.Contains("MATO GROSSO DO SUL") || nomeDpto.Contains("CAMPO GRANDE") ||
                    observacoes.Contains("Campo Grande") || observacoes.Contains("Mato Grosso do Sul") || observacoes.Contains("MS"))
                {
                    o.Nome = nomeDpto;
                    o.Estado = "MS";
                }
                else if (nomeDpto.Contains("MT") || nomeDpto.Contains("MATO GROSSO") || nomeDpto.Contains("CUIABÁ") ||
                    observacoes.Contains("Cuiabá") || observacoes.Contains("Mato Grosso") || observacoes.Contains("MT"))
                {
                    o.Nome = nomeDpto;
                    o.Estado = "MT";
                }
                else if (nomeDpto.Contains("- PA") || nomeDpto.Contains("-PA") || nomeDpto.Contains("/PA") || nomeDpto.Contains("PARÁ") || nomeDpto.Contains("BELÉM") ||
                    observacoes.Contains("Belém") || observacoes.Contains("Pará") || observacoes.Contains("PA"))
                {
                    o.Nome = nomeDpto;
                    o.Estado = "PA";
                }
                else if (nomeDpto.Contains("PB") || nomeDpto.Contains("-PB") || nomeDpto.Contains("/PB") || nomeDpto.Contains("PARAÍBA") || nomeDpto.Contains("JOÃO PESSOA") ||
                    observacoes.Contains("João Pessoa") || observacoes.Contains("Paraíba") || observacoes.Contains("PB"))
                {
                    o.Nome = nomeDpto;
                    o.Estado = "PB";
                }
                else if (nomeDpto.Contains("- PE") || nomeDpto.Contains("-PE") || nomeDpto.Contains("/PE") || nomeDpto.Contains("PERNAMBUCO") || nomeDpto.Contains("RECIFE") ||
                    observacoes.Contains("Recife") || observacoes.Contains("Pernambuco") || observacoes.Contains("PE"))
                {
                    o.Nome = nomeDpto;
                    o.Estado = "PB";
                }
                else if (nomeDpto.Contains("- PI") || nomeDpto.Contains("-PI") || nomeDpto.Contains("/PI") || nomeDpto.Contains("PIAUÍ") || nomeDpto.Contains("TERESINA") ||
                    observacoes.Contains("Teresina") || observacoes.Contains("Piauí") || observacoes.Contains("PI"))
                {
                    o.Nome = nomeDpto;
                    o.Estado = "PI";
                }
                else if (nomeDpto.Contains("- PR") || nomeDpto.Contains("-PR") || nomeDpto.Contains("/PR") || nomeDpto.Contains("PARANÁ") || nomeDpto.Contains("CURITIBA") ||
                    observacoes.Contains("Curitiba") || observacoes.Contains("Paraná") || observacoes.Contains("PR"))
                {
                    o.Nome = nomeDpto;
                    o.Estado = "PR";
                }
                else if (nomeDpto.Contains("RJ") || nomeDpto.Contains("RJ") || nomeDpto.Contains("RIO DE JANEIRO") ||
                    observacoes.Contains("Rio de Janeiro") || observacoes.Contains("RJ"))
                {
                    o.Nome = nomeDpto;
                    o.Estado = "RJ";
                }
                else if (nomeDpto.Contains("RN") || nomeDpto.Contains("RIO GRANDE DO NORTE") || nomeDpto.Contains("NATAL") ||
                    observacoes.Contains("Natal") || observacoes.Contains("Rio Grande do Norte") || observacoes.Contains("RN"))
                {
                    o.Nome = nomeDpto;
                    o.Estado = "RN";
                }
                else if (nomeDpto.Contains("- RO") || nomeDpto.Contains("-RO") || nomeDpto.Contains("/RO") || nomeDpto.Contains("RONDÔNIA") || nomeDpto.Contains("PORTO VELHO") ||
                    observacoes.Contains("Porto Velho") || observacoes.Contains("Rondônia") || observacoes.Contains("RO"))
                {
                    o.Nome = nomeDpto;
                    o.Estado = "RO";
                }
                else if (nomeDpto.Contains("- RR") || nomeDpto.Contains("-RR") || nomeDpto.Contains("/RR") || nomeDpto.Contains("RORAIMA") || nomeDpto.Contains("BOA VISTA") ||
                    observacoes.Contains("Boa Vista") || observacoes.Contains("Roraima") || observacoes.Contains("RR"))
                {
                    o.Nome = nomeDpto;
                    o.Estado = "RR";
                }
                else if (nomeDpto.Contains("RS") || nomeDpto.Contains("RIO GRANDE DO SUL") || nomeDpto.Contains("PORTO ALEGRE") ||
                    observacoes.Contains("Porto Alegre") || observacoes.Contains("Rio Grande do Sul") || observacoes.Contains("RS"))
                {
                    o.Nome = nomeDpto;
                    o.Estado = "RS";
                }
                else if (nomeDpto.Contains("- SC") || nomeDpto.Contains("-SC") || nomeDpto.Contains("/SC") || nomeDpto.Contains("SANTA CATARINA") || nomeDpto.Contains("FLORIANÓPOLIS") ||
                    observacoes.Contains("Florianópolis") || observacoes.Contains("Santa Catarina") || observacoes.Contains("SC"))
                {
                    o.Nome = nomeDpto;
                    o.Estado = "SC";
                }
                else if (nomeDpto.Contains("- SE") || nomeDpto.Contains("-SE") || nomeDpto.Contains("/SE") || nomeDpto.Contains("SERGIPE") || nomeDpto.Contains("ARACAJU") ||
                    observacoes.Contains("Aracaju") || observacoes.Contains("Sergipe") || observacoes.Contains("SE"))
                {
                    o.Nome = nomeDpto;
                    o.Estado = "SE";
                }
                else if (nomeDpto.Contains("- SP") || nomeDpto.Contains("-SP") || nomeDpto.Contains("/SP") || nomeDpto.Contains("SÃO PAULO") || nomeDpto.Contains("PAULISTA") ||
                    observacoes.Contains("São Paulo") || observacoes.Contains("SP"))
                {
                    o.Nome = nomeDpto;
                    o.Estado = "SP";
                }
                else if (nomeDpto.Contains("- TO") || nomeDpto.Contains("-TO") || nomeDpto.Contains("/TO") || nomeDpto.Contains("TOCANTINS") || nomeDpto.Contains("PALMAS") ||
                    observacoes.Contains("Palmas") || observacoes.Contains("Tocantins") || observacoes.Contains("TO"))
                {
                    o.Nome = nomeDpto;
                    o.Estado = "TO";
                }
                else
                {
                    RService.Log("Exception (CreateOrgao): Não foi possível identificar a localidade do órgão " + nomeDpto + " at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (CreateOrgao): Não foi possível identificar a localidade do órgão " + nomeDpto + " at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
                return null;
            }

            OrgaoRepository repo = new OrgaoRepository();
            repo.Insert(o);

            return o;
        }

        public static Orgao FindOrgao(string nomeDpto)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                try
                {
                    List<Orgao> orgaos = (List<Orgao>)session.CreateCriteria(typeof(Orgao))
                        .AddOrder(Order.Asc("Id"))
                        .Add(Restrictions.Eq("Nome", nomeDpto))
                        .List<Orgao>();

                    session.Close();

                    return orgaos[0];
                }
                catch (Exception e)
                {
                    session.Close();

                    return null;
                }
            }
        }

        public static List<Orgao> FindByUF(string UF)
        {
            using (ISession session = NHibernateHelper.OpenSession())
            {
                List<Orgao> orgaos = (List<Orgao>)session.CreateCriteria(typeof(Orgao))
                    .Add(Restrictions.Eq("Estado", UF))
                    .List<Orgao>();

                session.Close();

                return orgaos;
            }
        }
    }
}
