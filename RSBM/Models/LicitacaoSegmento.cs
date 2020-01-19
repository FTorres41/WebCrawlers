using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSBM.Models
{
    public class LicitacaoSegmento
    {
        public virtual int IdLicitacaoSegmento { get; set; }
        public virtual int IdLicitacao { get; set; }
        public virtual int IdSegmento { get; set; }
    }
}
