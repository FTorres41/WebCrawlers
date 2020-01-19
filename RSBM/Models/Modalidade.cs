namespace RSBM.Models
{
    public class Modalidade
    {
        public Modalidade()
        {

        }

        public Modalidade(int id, string modalidade)
        {
            Id = id;
            Modalidades = modalidade;
        }

        public virtual int Id { get; set; }
        public virtual string Modalidades { get; set; }
    }
}
