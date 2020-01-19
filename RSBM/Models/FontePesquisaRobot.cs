using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSBM.Models
{
    class FontePesquisaRobot
    {
        public virtual int Id { get; set; }
        public virtual DateTime DataHoraPesquisa { get; set; }
        public virtual DateTime DataHoraProcessado { get; set; }
        public virtual string Conteudo { get; set; }
        public virtual byte Status { get; set; }
        public virtual int? IdUsuario { get; set; }
        public virtual int IdFontePesquisa { get; set; }

        public FontePesquisaRobot() { }

        public FontePesquisaRobot(int idFontePesquisa)
        {
            this.IdFontePesquisa = idFontePesquisa;
        }
    }
}
