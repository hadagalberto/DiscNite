using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DiscNite.Models
{

    [Serializable]
    [Table("PUBGPlayer")]
    public class PUBGPlayer
    {
        [Key]
        public Guid IdPUBGPlayer { get; set; }
        public string Nome { get; set; }
        public string IdDiscord { get; set; }
        public Guid IdDiscordServer { get; set; }
        public DateTime DateUpdated { get; set; }
        public long VitoriasQuad { get; set; }
        public long VitoriasDuo { get; set; }
        public long VitoriasSolo { get; set; }
        public string PlayerStatsJSON { get; set; }
        
        public virtual DiscordServer DiscordServer { get; set; }
    }
}