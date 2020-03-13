using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace Zeroconf
{
    /// <summary>
    /// Object representation of an incoming DNS packet
    /// </summary>
    public class DNSIncoming
    {
        private Byte[] Data;
        private BigEndianReader ber;

        public List<DNSQuestion> Questions;
        public List<DNSRecord> Answers;
        public ushort ID;
        public QueryFlags Flags;
        public ushort NumQuestions;
        public ushort NumAnswers;
        public ushort NumAuthorities;
        public ushort NumAdditionals;
        public bool Valid;

        public bool IsQuery
        {
            get => ((this.Flags & QueryFlags.Mask) == QueryFlags.Query);
        }

        public bool IsResponse
        {
            get => ((this.Flags & QueryFlags.Mask) == QueryFlags.Response);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Zeroconf.DNSIncoming"/> class.
        /// Constructs from byte array
        /// </summary>
        /// <param name="data">Data.</param>
        public DNSIncoming(Byte[] data)
        {
            this.Data = data;
            this.ber = new BigEndianReader(this.Data);
            this.Questions = new List<DNSQuestion>();
            this.Answers = new List<DNSRecord>();
            this.ID = 0;
            this.Flags = 0;
            this.NumQuestions = 0;
            this.NumAnswers = 0;
            this.NumAuthorities = 0;
            this.NumAdditionals = 0;
            this.Valid = false;

            this.ReadHeader();
            this.ReadQuestions();
            this.ReadOthers();
            this.Valid = true;
        }

        /// <summary>
        /// Reads header portion of packet
        /// </summary>
        private void ReadHeader()
        {
            this.ID = this.ber.ReadUInt16();
            this.Flags = (QueryFlags)this.ber.ReadUInt16();
            this.NumQuestions = this.ber.ReadUInt16();
            this.NumAnswers = this.ber.ReadUInt16();
            this.NumAuthorities = this.ber.ReadUInt16();
            this.NumAdditionals = this.ber.ReadUInt16();
        }

        /// <summary>
        /// Reads questions section of packet
        /// </summary>
        private void ReadQuestions()
        {
            for (int i = 0; i < this.NumQuestions; i++)
            {
                String name = this.ber.ReadName();
                DNSType type = (DNSType)this.ber.ReadUInt16();
                DNSClass cls = (DNSClass)this.ber.ReadUInt16();

                this.Questions.Add(new DNSQuestion(name, type, cls));
            }
        }

        /// <summary>
        /// Reads the answers, authorities and additionals section of packet
        /// </summary>
        private void ReadOthers()
        {
            int count = this.NumAnswers + this.NumAuthorities + this.NumAdditionals;
            for (int i = 0; i < count; i++)
            {
                String domain = this.ber.ReadName();
                DNSType type = (DNSType)this.ber.ReadUInt16();
                DNSClass cls = (DNSClass)this.ber.ReadUInt16();
                uint ttl = this.ber.ReadUInt32();
                ushort length = this.ber.ReadUInt16();

                switch(type)
                {
                    case DNSType.A:
                    case DNSType.AAAA:
                        Byte[] addr_bytes;
                        if (type == DNSType.A)
                            addr_bytes = this.ber.ReadBytes(4);
                        else
                            addr_bytes = this.ber.ReadBytes(16);
                        
                        this.Answers.Add(new DNSAddress(domain, type, cls, ttl, addr_bytes));
                        break;
                    case DNSType.CNAME:
                    case DNSType.PTR:
                        String name = this.ber.ReadName();
                        this.Answers.Add(new DNSPointer(domain, type, cls, ttl, name));
                        break;
                    case DNSType.TXT:
                        byte[] text = this.ber.ReadBytes(length);
                        this.Answers.Add(new DNSText(domain, type, cls, ttl, text));
                        break;
                    case DNSType.SRV:
                        ushort priority = this.ber.ReadUInt16();
                        ushort weight = this.ber.ReadUInt16();
                        ushort port = this.ber.ReadUInt16();
                        string _addr = this.ber.ReadName();
                        this.Answers.Add(new DNSService(domain, type, cls, ttl,
                                                        priority, weight, port, _addr));
                        break;
                    case DNSType.HINFO:
                        String cpu = this.ber.ReadPrefixedString();
                        String os = this.ber.ReadPrefixedString();
                        this.Answers.Add(new DNSHinfo(domain, type, cls, ttl, cpu, os));
                        break;
                    default:
                        this.ber.BaseStream.Position += length;
                        break;
                }
            }
        }
    }
}
