using System;
using System.Collections.Generic;

namespace RSBM.Models
{
    class Licitacao
    {
        public virtual int Id { get; set; }
        public virtual int? IdFonte { get; set; }
        //public virtual ICollection<LicitacaoArquivo> LicitacoesArquivo { get; set; }
        public virtual ICollection<ItemLicitacao> ItensLicitacao { get; set; }
        public virtual string Departamento { get; set; }
        public virtual string EstadoFonte { get; set; }
        public virtual int? CidadeFonte { get; set; }
        public virtual string Num { get; set; }
        public virtual string Processo { get; set; }
        public virtual string Pag { get; set; }
        public virtual DateTime? AberturaData { get; set; }
        public virtual DateTime? EntregaData { get; set; }
        public virtual Modalidade Modalidade { get; set; }
        public virtual string Uasg { get; set; }
        public virtual string NumPregao { get; set; }
        public virtual string Objeto { get; set; }
        public virtual string ValorEdital { get; set; }
        public virtual string ValorMax { get; set; }
        public virtual string LinkEdital { get; set; }
        public virtual int SemEdital { get; set; }
        public virtual int EditalIndisponivel { get; set; }
        //public virtual int DataHoraPrazoEdital { get; set; }
        public virtual string SemEditalJustificativa { get; set; }
        public virtual string LinkSite { get; set; }
        public virtual string Email { get; set; }
        public virtual string Observacoes { get; set; }
        public virtual string Cep { get; set; }
        public virtual string Endereco { get; set; }
        public virtual string NumEndereco { get; set; }
        public virtual string Complemento { get; set; }
        public virtual string Bairro { get; set; }
        public virtual string Cidade { get; set; }
        public virtual string Estado { get; set; }
        //public virtual int AberturaTelaData { get; set; }
        //public virtual int? DigitacaoData { get; set; }
        public virtual int? DigitacaoUsuario { get; set; }
        //public virtual int? ProcessamentoData { get; set; }
        public virtual int? ProcessamentoUsuario { get; set; }
        public virtual int Excluido { get; set; }
        public virtual long IdLicitacaoFonte { get; set; }
        public virtual int SegmentoAguardandoEdital { get; set; }
        //public virtual DateTime AcessoData { get; set; }
        public virtual Orgao Orgao { get; set; }
        public virtual Lote Lote { get; set; }
        public virtual string Situacao { get; set; }
    }
}
