using RSBM.Models;
using RSBM.Repository;
using RSBM.Util;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RSBM.Controllers
{
    class CidadeController
    {
        public static Dictionary<string, int?> GetNameToCidade(string uf)
        {
            CidadeRepository repository = new CidadeRepository();
            Dictionary<string, int?> NameToCidade = new Dictionary<string, int?>();
            foreach (Cidade cidade in repository.FindByUf(uf))
            {
                if (NameToCidade.ContainsKey(StringHandle.RemoveAccent(cidade.Nome.ToUpper())) || NameToCidade.ContainsValue(cidade.Id))
                    continue;
                else
                    NameToCidade.Add(StringHandle.RemoveAccent(cidade.Nome.ToUpper()), cidade.Id);
            }
            return NameToCidade;
        }

        public static Dictionary<string, int?> GetNameToCidadeWithDiacritics(string uf)
        {
            CidadeRepository repository = new CidadeRepository();
            Dictionary<string, int?> NameToCidade = new Dictionary<string, int?>();
            foreach (Cidade cidade in repository.FindByUf(uf))
            {
                if (NameToCidade.ContainsKey(cidade.Nome.ToUpper()) || NameToCidade.ContainsValue(cidade.Id))
                    continue;
                else
                    NameToCidade.Add(cidade.Nome.ToUpper(), cidade.Id);
            }
            return NameToCidade;
        }

        public static Dictionary<string, Dictionary<string, int?>> GetUfToNameCidadeToIdCidade()
        {
            CidadeRepository repository = new CidadeRepository();
            Dictionary<string, Dictionary<string, int?>> UfToNameCidadeToIdCidade = new Dictionary<string, Dictionary<string, int?>>();
            foreach (Cidade cidade in repository.FindAllCities())
            {
                if (!UfToNameCidadeToIdCidade.ContainsKey(cidade.IdUf))
                    UfToNameCidadeToIdCidade.Add(cidade.IdUf, new Dictionary<string, int?>());
                if (!UfToNameCidadeToIdCidade[cidade.IdUf].ContainsKey(StringHandle.RemoveAccent(cidade.Nome.ToUpper())))
                    UfToNameCidadeToIdCidade[cidade.IdUf].Add(StringHandle.RemoveAccent(cidade.Nome.ToUpper()), cidade.Id);
            }
            return UfToNameCidadeToIdCidade;
        }

        /*Coloca em um dicionario o Estado e a Cidade*/
        public static Dictionary<string, string> GetUFCidade(string word)
        {
            Dictionary<string, string> UFCidade = new Dictionary<string, string>();
            UFCidade.Add(Regex.Match(word, @"([A-Z]{2})$").Value,
                         Regex.Replace(StringHandle.RemoveAccent(word).ToUpper(), @"(\-( *)[A-Z]{2})$", "").Trim());

            return UFCidade;
        }

        public static int GetIdCidade(string cidade, string uf)
        {
            CidadeRepository repository = new CidadeRepository();

            int IdCidade = repository.FindIdByCity(cidade, uf);

            return IdCidade;
        }
    }
}
