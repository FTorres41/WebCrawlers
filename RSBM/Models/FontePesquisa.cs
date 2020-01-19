using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSBM.Models
{
    class FontePesquisa
    {
        public virtual int Id { get; set; }
        public virtual string Nome { get; set; }
        public virtual int? IdOrgao { get; set; }
        public virtual int? Portal { get; set; }
        public virtual string Link { get; set; }
        public virtual string Regex { get; set; }
        public virtual int? UsuarioRota { get; set; }
        public virtual string ConfRota { get; set; }
        public virtual string UltimoConteudo { get; set; }
        public virtual int? AtivarRobot { get; set; }
        public virtual string Uf { get; set; }
        public virtual int? IdCidade { get; set; }
        public virtual string Email { get; set; }
        public virtual int? Excluido { get; set; }
    }
}
