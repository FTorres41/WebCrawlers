using RSBM.Models;
using RSBM.Repository;
using RSBM.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace RSBM.Controllers
{
    class ModalidadeController
    {
        public static Modalidade FindById(int id)
        {
            ModalidadeRepository repo = new ModalidadeRepository();
            return repo.FindById(id);
        }

        public static Dictionary<string, Modalidade> GetNameToModalidade()
        {
            ModalidadeRepository repo = new ModalidadeRepository();
            Dictionary<string, Modalidade> nameToModalidade = new Dictionary<string, Modalidade>();

            try
            {

                foreach (Modalidade m in repo.FindAll())
                {
                    if (!nameToModalidade.ContainsKey(StringHandle.RemoveAccent(m.Modalidades.ToUpper())))
                    {
                        nameToModalidade.Add(StringHandle.RemoveAccent(m.Modalidades).ToUpper(), m);

                        if (m.Modalidades.Contains("RDC"))
                        {
                            nameToModalidade.Add("RDC ELETRONICO SRP", m);
                            nameToModalidade.Add("RDC ELETRONICO", m);
                            nameToModalidade.Add("RDC PRESENCIAL SRP", m);
                            nameToModalidade.Add("RDC PRESENCIAL", m);
                            nameToModalidade.Add("REGIME DIFERENCIADO DE CONTRATACOES", m);
                        }

                        if (m.Modalidades.Contains("Pregão Presencial") && !m.Modalidades.Contains("Pregão Presencial Internacional") && !nameToModalidade.ContainsKey("PREGAO PRESENCIAL - SRP"))
                        {
                            nameToModalidade.Add("PREGAO PRESENCIAL - SRP", m);
                        }

                        if (m.Modalidades.Contains("Carta Convite") && !m.Modalidades.Contains("Carta Convite Internacional") && !nameToModalidade.ContainsKey("CONVITE"))
                        {
                            nameToModalidade.Add("CONVITE", m);
                        }

                        if (m.Modalidades.Contains("Tomada de Preço") && !nameToModalidade.ContainsKey("TOMADA DE PRECOS"))
                        {
                            nameToModalidade.Add("TOMADA DE PRECOS", m);
                        }

                        if (m.Modalidades.Contains("Dispensa de Licitação") && !nameToModalidade.ContainsKey("PROCESSO DISPENSA"))
                        {
                            nameToModalidade.Add("PROCESSO DISPENSA", m);
                        }

                        if (!m.Modalidades.Contains("Concorrência - SRP") &&
                            !m.Modalidades.Contains("Concorrência Internacional") &&
                            m.Modalidades.Contains("Concorrência") && !nameToModalidade.ContainsKey("REGISTRO DE PRECOS (CONCORRENCIA)"))
                        {
                            nameToModalidade.Add("REGISTRO DE PRECOS (CONCORRENCIA)", m);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (GetModalidades) " + e.Message + " / " + e.StackTrace + " at {0}", Path.GetTempPath() + "RSERVICE.txt");
            }

            return nameToModalidade;
        }
    }
}
