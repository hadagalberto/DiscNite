using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiscNite.Models
{
    [Serializable]
    [Table("FortnitePlayer")]
    public class FortnitePlayer
    {

        [Key]
        public Guid IdFortnitePlayer { get; set; }
        public string Nome { get; set; }
        public string IdDiscord { get; set; }
        public Guid IdDiscordServer { get; set; }
        public DateTime DateUpdated { get; set; }
        public long Vitorias { get; set; }
        public string PlayerStatsJSON { get; set; }

        public virtual DiscordServer DiscordServer { get; set; }
    }
}
