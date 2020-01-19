using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSBM.Models
{
    public class Preco
    {
        public virtual int Id { get; set; }
        public virtual int IdLicitacao { get; set; }
        public virtual string Item { get; set; }
        public virtual string Descricao { get; set; }
        public virtual double Quantidade { get; set; }
        public virtual double ValorEstimado { get; set; }
        public virtual double ValorHomologado { get; set; }
        public virtual DateTime DataHomologacao { get; set; }
        public virtual DateTime DataInsercao { get; set; }
    }
}
