using RSBM.Models;
using RSBM.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSBM.Controllers
{
    public class SegmentoController
    {
        internal static List<Segmento> GetSegmentos()
        {
            SegmentoRepository repo = new SegmentoRepository();
            return repo.GetSegmentos();
        }

        internal static List<Segmento> GetSegmentosLeilao()
        {
            SegmentoRepository repo = new SegmentoRepository();
            return repo.GetSegmentosLeilao();
        }

        internal static List<Segmento> GetSegmentosVeterinaria()
        {
            SegmentoRepository repo = new SegmentoRepository();
            return repo.GetSegmentosVeterinaria();
        }

        internal static List<Segmento> GetSegmentosConcessao()
        {
            SegmentoRepository repo = new SegmentoRepository();
            return repo.GetSegmentosConcessao();
        }

        internal static List<Segmento> GetSegmentosHumanos()
        {
            SegmentoRepository repo = new SegmentoRepository();
            return repo.GetSegmentosHumanos();
        }

        internal static List<Segmento> CreateListaSegmentos(Licitacao licitacao)
        {
            List<Segmento> segmentos = new List<Segmento>();

            if (licitacao.Modalidade.Modalidades == "Leilão")
            {
                segmentos = SegmentoController.GetSegmentosLeilao();
            }
            else if (licitacao.Objeto.ToUpper().Contains("VETERINÁR") || licitacao.Objeto.ToUpper().Contains("VETERINAR"))
            {
                segmentos = SegmentoController.GetSegmentosVeterinaria();
            }
            else if (licitacao.Objeto.ToUpper().Contains("CONCESSÃO") ||
                licitacao.Objeto.ToUpper().Contains("CONCESSAO") ||
                licitacao.Objeto.ToUpper().Contains("OUTORGA") ||
                licitacao.Objeto.ToUpper().Contains("PERMISSÃO DE USO") ||
                licitacao.Objeto.ToUpper().Contains("PERMISSAO DE USO") ||
                licitacao.Objeto.ToUpper().Contains("EXPLORAÇÃO") ||
                licitacao.Objeto.ToUpper().Contains("EXPLORACAO"))
            {
                segmentos = SegmentoController.GetSegmentosConcessao();
            }
            else
            {
                segmentos = SegmentoController.GetSegmentosHumanos();
            }

            return segmentos;
        }
    }
}
