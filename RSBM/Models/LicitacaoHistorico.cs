using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSBM.Models
{
    class LicitacaoHistorico
    {
        public virtual int Id { get; set; }
        public virtual int IdLicitacao { get; set; }
        public virtual string Historico { get; set; }
        public virtual DateTime DataCadastro { get; set; }
        public virtual string Mensagem { get; set; }
        public virtual string Resposta { get; set; }
    }
}
