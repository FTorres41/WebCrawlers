using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSBM.Models
{
    class Segmento
    {
        public virtual int IdSegmento { get; set; }
        public virtual string Nome { get; set; }
        public virtual int Filiacao { get; set; }
        public virtual double GastosGovernoUltimoAno { get; set; }
        public virtual int CodigoInterno { get; set; }
        public virtual string Imagem { get; set; }
        public virtual int Status { get; set; }
        public virtual string PalavrasChave { get; set; }
    }
}
