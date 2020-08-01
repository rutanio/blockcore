﻿using Blockcore.P2P.Protocol.Payloads;
using NBitcoin;
using NBitcoin.Protocol;

namespace Blockcore.Features.Storage.Payloads
{
    /// <summary>
    /// The StoragePayload is the type that initiates queries for data and information around storage. The StorageInvPayload is the response to these messages.
    /// </summary>
    [Payload("storage")]
    public class StoragePayload : Payload
    {
        private VarString[] collections;

        private ulong action;

        private ushort version = 1;

        /// <summary>
        /// Name of collections that a node want to retrieve from other nodes.
        /// </summary>
        public VarString[] Collections { get { return this.collections; } set { this.collections = value; } }

        public StoragePayloadAction Action
        {
            get
            {
                return (StoragePayloadAction)this.action;
            }
            set
            {
                this.action = (ulong)value;
            }
        }

        public ushort Version { get { return this.version; } set { this.version = value; } }

        public StoragePayload(VarString[] collections)
        {
            this.collections = collections;
        }

        public StoragePayload()
        {

        }

        public override void ReadWriteCore(BitcoinStream stream)
        {
            stream.ReadWrite(ref this.version);
            stream.ReadWrite(ref this.action);
            stream.ReadWrite(ref this.collections);
        }

        public override string ToString()
        {
            return base.ToString() + " : " + this.Collections;
        }
    }
}