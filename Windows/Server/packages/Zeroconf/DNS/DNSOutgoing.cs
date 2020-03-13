using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Zeroconf
{
    /// <summary>
    /// Object representation of an outgoing packet
    /// </summary>
    public class DNSOutgoing
    {
        private MemoryStream ms;
        private BigEndianWriter bew;

        public ushort ID;
        private bool Multicast;
        private ushort Flags;
        private State State;

        public List<DNSQuestion> Questions;
        public List<Tuple<DNSRecord, long>> Answers;
        public List<DNSRecord> Authorities;
        public List<DNSRecord> Additionals;

        public DNSOutgoing(ushort flags, bool multicast=false)
        {
            this.ms = new MemoryStream();
            this.bew = new BigEndianWriter(ms);

            this.ID = 0;
            this.Multicast = multicast;
            this.Flags = flags;
            this.State = State.Init;

            this.Questions = new List<DNSQuestion>();
            this.Answers = new List<Tuple<DNSRecord, long>>();
            this.Authorities = new List<DNSRecord>();
            this.Additionals = new List<DNSRecord>();
        }

        public void AddQuestion(DNSQuestion record)
        {
            this.Questions.Add(record);
        }

        public void AddAnswer(DNSIncoming incoming, DNSRecord record)
        {
            if (!record.SuppressedBy(incoming))
                AddAnswerAtTime(record, 0);
        }

        public void AddAnswerAtTime(DNSRecord record, long now)
        {
            if (now == 0 || !record.IsExpired(now))
                this.Answers.Add(new Tuple<DNSRecord, long>(record, now));
        }

        public void AddAuthorativeAnswer(DNSRecord record)
        {
            this.Authorities.Add(record);
        }

        /// <summary>
        /// Adds the additional answer.
        /// 
        ///         From: RFC 6763, DNS-Based Service Discovery, February 2013
        ///
        /// 12.  DNS Additional Record Generation
        ///
        ///   DNS has an efficiency feature whereby a DNS server may place
        ///   additional records in the additional section of the DNS message.
        ///   These additional records are records that the client did not
        ///   explicitly request, but the server has reasonable grounds to expect
        ///   that the client might request them shortly, so including them can
        ///   save the client from having to issue additional queries.
        ///
        ///   This section recommends which additional records SHOULD be generated
        ///   to improve network efficiency, for both Unicast and Multicast DNS-SD
        ///   responses.
        ///
        /// 12.1.  PTR Records
        ///
        ///   When including a DNS-SD Service Instance Enumeration or Selective
        ///   Instance Enumeration (subtype) PTR record in a response packet, the
        ///   server/responder SHOULD include the following additional records:
        ///
        ///   o The SRV record(s) named in the PTR rdata.
        ///   o The TXT record(s) named in the PTR rdata.
        ///   o All address records (type "A" and "AAAA") named in the SRV rdata.
        ///
        /// 12.2.  SRV Records
        ///
        ///   When including an SRV record in a response packet, the
        ///   server/responder SHOULD include the following additional records:
        ///
        ///   o All address records(type "A" and "AAAA") named in the SRV rdata.
        /// </summary>
        /// <param name="record">Record.</param>
        public void AddAdditionalAnswer(DNSRecord record)
        {
            this.Additionals.Add(record);
        }

        /// <summary>
        /// Writes a question to the packet
        /// </summary>
        /// <param name="question">Question.</param>
        public void WriteQuestion(DNSQuestion question)
        {
            this.bew.WriteName(question.Name);
            this.bew.Write((ushort)question.Type);
            this.bew.Write((ushort)question.Class);
        }

        /// <summary>
        /// Writes a record (answer, authoritative answer, additional) to
        /// the packet
        /// </summary>
        /// <param name="record">Record.</param>
        /// <param name="now">Now.</param>
        public int WriteRecord(DNSRecord record, long now)
        {
            if (this.State == State.Finished)
                return 1;

            long StartSize = this.bew.BaseStream.Length;

            this.bew.WriteName(record.Name);
            this.bew.Write((ushort)record.Type);

            if (record.Unique && this.Multicast)
                this.bew.Write((ushort)((ushort)record.Class | (ushort)DNSClass.UNIQUE));
            else
                this.bew.Write((ushort)record.Class);

            if (now == 0)
                this.bew.Write(record.TTL);
            else
                this.bew.Write((uint)record.GetRemainingTTL(now));
            
            // Reserve space for payload size
            this.bew.Write((UInt16) 0);

            // Save old position
            long index = this.bew.BaseStream.Position;

            // Write payload
            record.Write(this.bew);

            long payloadSize = this.bew.BaseStream.Position - index;

            // Rewind to payload size position
            this.bew.BaseStream.Position -= (payloadSize + sizeof(UInt16));

            this.bew.Write((UInt16)payloadSize);

            // Forward to end of stream
            this.bew.BaseStream.Position += payloadSize;

            // Need to add headerlength (12) for this check
            if ((this.bew.BaseStream.Length + 12) > Constants.MAX_MSG_ABSOLUTE)
            {
                byte[] newData = new byte[StartSize];
                Array.Copy(this.ms.ToArray(), newData, StartSize);
                this.ms = new MemoryStream(newData);
                this.bew = new BigEndianWriter(this.ms);
                this.State = State.Finished;
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// Returns a bytearray containing the packet's bytes
        ///
        /// No further parts should be added to the packet once this
        /// is done.
        /// </summary>
        /// <returns>The packet.</returns>
        public byte[] Packet()
        {
            int OverrunAnswers = 0;
            int OverrunAuthorities = 0;
            int OverrunAdditionals = 0;

            if (this.State != State.Finished)
            {
                // Reserve space for header fields
                this.bew.Write(new byte[12]);

                foreach (DNSQuestion question in this.Questions)
                    WriteQuestion(question);
                foreach (Tuple<DNSRecord, long> entry in this.Answers)
                {
                    DNSRecord answer = entry.Item1;
                    long time = entry.Item2;
                    OverrunAnswers += WriteRecord(answer, time);
                }
                foreach (DNSRecord authority in this.Authorities)
                    OverrunAuthorities += WriteRecord(authority, 0);
                foreach (DNSRecord additional in this.Additionals)
                    OverrunAdditionals += WriteRecord(additional, 0);

                this.State = State.Finished;

                // Write header fields
                this.bew.BaseStream.Position = 0;

                if (this.Multicast)
                    this.bew.Write((UInt16)0);
                else
                    this.bew.Write(this.ID);

                this.bew.Write(this.Flags);
                this.bew.Write((UInt16)this.Questions.Count);
                this.bew.Write((UInt16)(this.Answers.Count - OverrunAnswers));
                this.bew.Write((UInt16)(this.Authorities.Count - OverrunAuthorities));
                this.bew.Write((UInt16)(this.Additionals.Count - OverrunAdditionals));
            }
            return this.ms.ToArray();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("[DNSOutgoing] ");
            sb.AppendFormat("multicast={0}, ", this.Multicast);
            sb.AppendFormat("flags={0}, ", this.Flags);
            sb.AppendFormat("Questions={0}, ", this.Questions);
            sb.AppendFormat("Answers:D {0}, ", this.Answers);
            sb.AppendFormat("Authorities:D {0}, ", this.Authorities);
            sb.AppendFormat("Additionals: {0}", this.Additionals);
            return sb.ToString();
        }
    }
}
