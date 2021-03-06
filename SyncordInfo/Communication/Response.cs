﻿using ProtoBuf;

namespace SyncordInfo.Communication
{
    [ProtoContract]
    public sealed class Response : DataBase
    {
        [ProtoMember(4)]
        public Query Query { get; set; }
        [ProtoMember(5)]
        public string Content { get; set; }
    }
}
