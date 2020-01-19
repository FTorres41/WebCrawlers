using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSBM.Models
{
    public class ItemLicitacao
    {
        public virtual int Id { get; set; }
        public virtual string Descricao { get; set; }
        public virtual int? Numero { get; set; }
        public virtual string Tipo { get; set; }
        public virtual string Codigo { get; set; }
        public virtual int? Quantidade { get; set; }
        public virtual string Unidade { get; set; }
        public virtual string DescricaoDetalhada { get; set; }
        public virtual string TratamentoDiferenciado { get; set; }
        public virtual string Decreto7174 { get; set; }
        public virtual string MargemPreferencia { get; set; }
    }
}
