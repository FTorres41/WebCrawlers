using System;

namespace RSBM.Models
{
    class Lote
    {
        public virtual int Id { get; set; }
        public virtual int IdFonte { get; set; }
        public virtual DateTime Data { get; set; }
        public virtual string Licitacao { get; set; }
        public virtual int Status { get; set; }
        public virtual int? IdDigitador { get; set; }
        public virtual int? IdCadastrador { get; set; }
        public virtual DateTime DataCadastroLote { get; set; }
        public virtual int? IdRotaLote { get; set; }
        public virtual DateTime? DataFinalizacaoLote { get; set; }
    }
}
