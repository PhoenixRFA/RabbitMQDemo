using ProtoBuf;

namespace ExternalLogic.Models
{
    [ProtoContract]
    public class EmailModel
    {
        [ProtoMember(1)]
        public string? Email { get; set; }
        [ProtoMember(2)]
        public string? Subject { get; set; }
        [ProtoMember(3)]
        public string? Body { get; set; }
    }
}
