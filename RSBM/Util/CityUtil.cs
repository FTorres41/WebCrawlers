using RSBM.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSBM.Util
{
    class CityUtil
    {

        public static Dictionary<string, string> GetUfToCapital()
        {
            Dictionary<string, string> capitais = new Dictionary<string, string>();
            capitais.Add("AC", "Rio Branco"); capitais.Add("AL", "Maceió"); capitais.Add("AP", "Macapá");
            capitais.Add("AM", "Manaus"); capitais.Add("BA", "Salvador"); capitais.Add("CE", "Fortaleza");
            capitais.Add("DF", "Brasília"); capitais.Add("ES", "Vitória"); capitais.Add("GO", "Goiânia");
            capitais.Add("MA", "São Luís"); capitais.Add("MT", "Cuiabá"); capitais.Add("MS", "Campo Grande");
            capitais.Add("MG", "Belo Horizonte"); capitais.Add("PA", "Belém"); capitais.Add("PB", "João Pessoa");
            capitais.Add("PR", "Curitiba"); capitais.Add("PE", "Recife"); capitais.Add("PI", "Teresina");
            capitais.Add("RJ", "Rio de Janeiro"); capitais.Add("RN", "Natal"); capitais.Add("RS", "Porto Alegre");
            capitais.Add("RO", "Porto Velho"); capitais.Add("RR", "Boa Vista"); capitais.Add("SC", "Florianópolis");
            capitais.Add("SP", "São Paulo"); capitais.Add("SE", "Aracaju"); capitais.Add("TO", "Palmas");
            return capitais;
        }

        public static string FindCity(Licitacao licitacao)
        {
            string cidadeEstado = string.Empty;
            string nomeDpto = licitacao.Orgao.Nome;
            string estado = licitacao.Orgao.Estado;

            try
            {
                if (nomeDpto.Contains("- AC") || nomeDpto.Contains("/AC") || nomeDpto.Contains("ACRE") || nomeDpto.Contains("RIO BRANCO") || estado.Contains("AC"))
                {
                    cidadeEstado = "Rio Branco/AC/16";
                }
                else if (nomeDpto.Contains("- AL") || nomeDpto.Contains("/AL") || nomeDpto.Contains("ALAGOAS") || nomeDpto.Contains("MACEIÓ") || nomeDpto.Contains("MACEIO") || estado.Contains("AL"))
                {
                    cidadeEstado = "Maceió/AL/109";
                }
                else if (nomeDpto.Contains("- AM") || nomeDpto.Contains("/AM") || nomeDpto.Contains("AMAZONAS") || nomeDpto.Contains("MANAUS") || estado.Contains("AM"))
                {
                    cidadeEstado = "Manaus/AM/AM";
                }
                else if (nomeDpto.Contains("- AP") || nomeDpto.Contains("/AP") || nomeDpto.Contains("AMAPÁ") || nomeDpto.Contains("MACAPÁ") || nomeDpto.Contains("MACAPA") || estado.Contains("AP"))
                {
                    cidadeEstado = "Macapá/AP/307";
                }
                else if (nomeDpto.Contains("- BA") || nomeDpto.Contains("/BA") || nomeDpto.Contains("BAHIA") || nomeDpto.Contains("SALVADOR") || estado.Contains("BA"))
                {
                    cidadeEstado = "Salvador/BA/988";
                }
                else if (nomeDpto.Contains("- CE") || nomeDpto.Contains("/CE") || nomeDpto.Contains("CEARÁ") || nomeDpto.Contains("FORTALEZA") || estado.Contains("CE"))
                {
                    cidadeEstado = "Fortaleza/CE/1347";
                }
                else if (nomeDpto.Contains("- DF") || nomeDpto.Contains("/DF") || nomeDpto.Contains("DISTRITO FEDERAL") || nomeDpto.Contains("BRASÍLIA") || nomeDpto.Contains("BRASILIA") || estado.Contains("DF"))
                {
                    cidadeEstado = "Brasília/DF/1778";
                }
                else if (nomeDpto.Contains("- ES") || nomeDpto.Contains("/ES") || nomeDpto.Contains("ESPÍRITO SANTO") || nomeDpto.Contains("VITÓRIA") || nomeDpto.Contains("VITORIA") || estado.Contains("ES"))
                {
                    cidadeEstado = "Vitória/ES/2048";
                }
                else if (nomeDpto.Contains("- GO") || nomeDpto.Contains("/GO") || nomeDpto.Contains("GOIÁS") || nomeDpto.Contains("GOIÂNIA") || nomeDpto.Contains("GOIAS") || nomeDpto.Contains("GOIANIA") || estado.Contains("GO"))
                {
                    cidadeEstado = "Goiânia/GO/2174";
                }
                else if (nomeDpto.Contains("- MA") || nomeDpto.Contains("/MA") || nomeDpto.Contains("MARANHÃO") || nomeDpto.Contains("SÃO LUÍS") || nomeDpto.Contains("SAO LUIS") || estado.Contains("MA"))
                {
                    cidadeEstado = "São Luís/MA/2587";
                }
                else if (nomeDpto.Contains("- MG") || nomeDpto.Contains("/MG") || nomeDpto.Contains("MINAS GERAIS") || nomeDpto.Contains("BELO HORIZONTE") || nomeDpto.Contains("UFMG") || estado.Contains("MG"))
                {
                    cidadeEstado = "Belo Horizonte/MG/2754";
                }
                else if (nomeDpto.Contains("- MS") || nomeDpto.Contains("/MS") || nomeDpto.Contains("MATO GROSSO DO SUL") || nomeDpto.Contains("CAMPO GRANDE") || estado.Contains("MS"))
                {
                    cidadeEstado = "Campo Grande/MS/4141";
                }
                else if (nomeDpto.Contains("- MT") || nomeDpto.Contains("/MT") || nomeDpto.Contains("MATO GROSSO") || nomeDpto.Contains("CUIABÁ") || nomeDpto.Contains("CUIABA") || estado.Contains("MT"))
                {
                    cidadeEstado = "Cuiabá/MT/4347";
                }
                else if (nomeDpto.Contains("- PA") || nomeDpto.Contains("/PA") || nomeDpto.Contains("PARÁ") || nomeDpto.Contains("BELÉM") || nomeDpto.Contains("BELEM") || estado.Contains("PA"))
                {
                    cidadeEstado = "Belém/PA/4565";
                }
                else if (nomeDpto.Contains("- PB") || nomeDpto.Contains("/PB") || nomeDpto.Contains("PARAÍBA") || nomeDpto.Contains("JOÃO PESSOA") || nomeDpto.Contains("JOAO PESSOA") || estado.Contains("PB"))
                {
                    cidadeEstado = "João Pessoa/PB/4964";
                }
                else if (nomeDpto.Contains("- PE") || nomeDpto.Contains("/PE") || nomeDpto.Contains("PERNAMBUCO") || nomeDpto.Contains("RECIFE") || estado.Contains("PE"))
                {
                    cidadeEstado = "Recife/PE/5406";
                }
                else if (nomeDpto.Contains("- PI") || nomeDpto.Contains("/PI") || nomeDpto.Contains("PIAUÍ") || nomeDpto.Contains("TERESINA") || estado.Contains("PI"))
                {
                    cidadeEstado = "Teresina/PI/5721";
                }
                else if (nomeDpto.Contains("- PR") || nomeDpto.Contains("/PR") || nomeDpto.Contains("PARANÁ") || nomeDpto.Contains("CURITIBA") || estado.Contains("PR"))
                {
                    cidadeEstado = "Curitiba/PR/6015";
                }
                else if (nomeDpto.Contains("- RJ") || nomeDpto.Contains("/RJ") || nomeDpto.Contains("RJ") || nomeDpto.Contains("RIO DE JANEIRO") || estado.Contains("RJ"))
                {
                    cidadeEstado = "Rio de Janeiro/RJ";
                }
                else if (nomeDpto.Contains("- RN") || nomeDpto.Contains("/RN") || nomeDpto.Contains("RIO GRANDE DO NORTE") || nomeDpto.Contains("NATAL") || estado.Contains("RN"))
                {
                    cidadeEstado = "Natal/RN/7221";
                }
                else if (nomeDpto.Contains("- RO") || nomeDpto.Contains("/RO") || nomeDpto.Contains("RONDÔNIA") || nomeDpto.Contains("PORTO VELHO") || estado.Contains("RO"))
                {
                    cidadeEstado = "Porto Velho/RO/7352";
                }
                else if (nomeDpto.Contains("- RR") || nomeDpto.Contains("/RR") || nomeDpto.Contains("RORAIMA") || nomeDpto.Contains("BOA VISTA") || estado.Contains("RR"))
                {
                    cidadeEstado = "Boa Vista/RR/7375";
                }
                else if (nomeDpto.Contains("- RS") || nomeDpto.Contains("/RS") || nomeDpto.Contains("RIO GRANDE DO SUL") || nomeDpto.Contains("PORTO ALEGRE") || estado.Contains("RS"))
                {
                    cidadeEstado = "Porto Alegre/RS/7994";
                }
                else if (nomeDpto.Contains("- SC") || nomeDpto.Contains("/SC") || nomeDpto.Contains("SANTA CATARINA") || nomeDpto.Contains("FLORIANÓPOLIS") || nomeDpto.Contains("FLORIANOPOLIS") || estado.Contains("SC"))
                {
                    cidadeEstado = "Florianópolis/SC/8452";
                }
                else if (nomeDpto.Contains("- SE") || nomeDpto.Contains("/SE") || nomeDpto.Contains("SERGIPE") || nomeDpto.Contains("ARACAJU") || estado.Contains("SE"))
                {
                    cidadeEstado = "Aracaju/SE/8770";
                }
                else if (nomeDpto.Contains("- SP") || nomeDpto.Contains("/SP") || nomeDpto.Contains("SÃO PAULO") || nomeDpto.Contains("PAULISTA") || nomeDpto.Contains("SAO PAULO") || estado.Contains("SP"))
                {
                    cidadeEstado = "São Paulo/SP/9668";
                }
                else if (nomeDpto.Contains("- TO") || nomeDpto.Contains("/TO") || nomeDpto.Contains("TOCANTINS") || nomeDpto.Contains("PALMAS") || estado.Contains("TO"))
                {
                    cidadeEstado = "Palmas/TO/9899";
                }
                else
                {
                    RService.Log("Exception (FindCity): Não foi possível identificar a localidade do órgão " + nomeDpto + " at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (FindCity): Não foi possível identificar a localidade do órgão " + nomeDpto + " at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
                return null;
            }

            return cidadeEstado;
        }

        public static int GetCidadeFonte(string cidade, Dictionary<string, int?> ufToCidade)
        {
            foreach (var ufCity in ufToCidade)
            {
                var cid = ufCity.Key.Replace(" ", "").ToCharArray();
                var licCid = cidade.Replace(" ", "").ToUpper().ToCharArray();
                int matchCount = 0, limit = 0;

                if (cid.Count() > licCid.Count())
                    limit = licCid.Count();
                else if (cid.Count() < licCid.Count())
                    limit = cid.Count();
                else
                    limit = cid.Count();

                for (int i = 0; i < limit; i++)
                {
                    if (cid[i] == licCid[i])
                    {
                        matchCount++;
                    }
                }

                double result = (double)matchCount / (double)cidade.Length;

                if (result >= 0.7)
                {
                    return Convert.ToInt32(ufCity.Value);
                }
            }

            return 0;
        }
    }
}