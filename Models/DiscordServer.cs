using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiscNite.Models
{
    [Serializable]
    [Table("DiscordServer")]
    public class DiscordServer
    {

        [Key]
        public Guid IdDiscordServer { get; set; }
        public string Nome { get; set; }
        public string Descricao { get; set; }
        public ulong IdDiscord { get; set; }
        public ulong IdTextChannel { get; set; }

        public virtual ICollection<FortnitePlayer> FortnitePlayers { get; set; }
        public virtual ICollection<PUBGPlayer> PUBGPlayers { get; set; }

    }
}
