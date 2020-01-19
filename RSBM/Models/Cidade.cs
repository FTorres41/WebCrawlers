namespace RSBM.Models
{
    class Cidade
    {
        public virtual int Id { get; set; }
        public virtual string IdUf { get; set; }
        public virtual string Nome { get; set; }
        public virtual string Cep { get; set; }
        public virtual int SubCidade { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            Cidade id;
            id = (Cidade)obj;
            if (id == null)
                return false;
            if (Id == id.Id && IdUf == id.IdUf)
                return true;
            return false;
        }

        public override int GetHashCode()
        {
            return (Id + "|" + IdUf).GetHashCode();
        }


    }
}
