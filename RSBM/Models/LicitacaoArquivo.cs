namespace RSBM.Models
{
    public class LicitacaoArquivo
    {
        public virtual int Id { get; set; }
        public virtual string NomeArquivo { get; set; }
        public virtual string NomeArquivoOriginal { get; set; }
        public virtual string NomeArquivoFonte { get; set; }
        public virtual string Conteudo { get; set; }
        public virtual int Status { get; set; }
        public virtual int IdLicitacao { get; set; }
    }
}
