using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;

namespace xmltest
{
    public partial class Model4 : DbContext
    {
        DbSet<Movie> Movies { get; set; }
    }

    public partial class Movie
    {
        [Key]
        public Int32 Id { get; set; }

        [Required]
        public String Name { get; set; }

        [Required]
        public String Actor { get; set; }
    }
}
