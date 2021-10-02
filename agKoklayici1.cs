using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using System.Threading;
using PcapDotNet.Base;
using PcapDotNet.Packets.Arp;
using PcapDotNet.Packets.Dns;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.Gre;
using PcapDotNet.Packets.Http;
using PcapDotNet.Packets.Icmp;
using PcapDotNet.Packets.Igmp;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.IpV6;
using PcapDotNet.Packets.Transport;
using PcapDotNet.Core.Extensions;
using System.Net;
using System.Net.NetworkInformation;


namespace tezKoklayici
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            cihazGoster.Click += new EventHandler(cihazGoster_Click_1);
            kapat.Click += new EventHandler(kapat_Click);
        }

        private void cihazGoster_Click_1(object sender, EventArgs e)
        {
            IList<LivePacketDevice> allDevices = LivePacketDevice.AllLocalMachine;
            List<Label> chzlar = new List<Label>();
            List<Label> chzOzllk = new List<Label>();
            if (allDevices.Count == 0)
            {
                cihazHata.Text = "No interfaces found! Make sure WinPcap is installed.";
            }
            else
            {
                for (int i = 0; i != allDevices.Count; i++)
                {
                    LivePacketDevice device = allDevices[i];
                    LinkLabel gecici = new LinkLabel();
                    LinkLabel chzOzel = new LinkLabel();
                    gecici.Location = new Point(180, 20 * (i + 1));
                    gecici.AutoSize = true;
                    gecici.LinkColor = System.Drawing.Color.DeepPink;
                    gecici.LinkBehavior = System.Windows.Forms.LinkBehavior.NeverUnderline;
                    gecici.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
                    gecici.Font = new Font("Noto Sans", 11);
                    gecici.Text = (i + 1) + ". " + " (" + device.Description + ")";
                    gecici.TextAlign = ContentAlignment.MiddleLeft;
                    gecici.Click += (s, EventArgs) => { arayuzSec(s, EventArgs, device); };
                    this.Controls.Add(gecici);
                    gecici.Show();
                    chzlar.Add(gecici);

                    SizeF uzunluk = gecici.CreateGraphics().MeasureString(gecici.Text, gecici.Font);

                    chzOzel.Location = new Point((int)Math.Ceiling(uzunluk.Width) + 180, 20 * (i + 1));
                    chzOzel.AutoSize = true;
                    chzOzel.LinkColor = System.Drawing.Color.DarkSalmon;
                    chzOzel.LinkBehavior = System.Windows.Forms.LinkBehavior.NeverUnderline;
                    chzOzel.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
                    chzOzel.Font = new Font("Noto Sans", 11, FontStyle.Bold);
                    chzOzel.Text = "Özellikleri";
                    chzOzel.TextAlign = ContentAlignment.MiddleLeft;
                    chzOzel.Click += (s, EventArgs) => { chzO(s, EventArgs, device); };
                    this.Controls.Add(chzOzel);
                    chzOzel.Show();
                    chzlar.Add(chzOzel);
                }
            }

        }
        private void arayuzSec(object sender, EventArgs e, LivePacketDevice device)
        {
            using(paketleriOkuma pktGoster = new paketleriOkuma(device))
            {
                this.Visible = false;
                pktGoster.ShowDialog(this);
                this.Visible = true;
            }
        }
        private void chzO(object sender, EventArgs e, LivePacketDevice device)
        {
            String mesaj = "";
            foreach (DeviceAddress add in device.Addresses)
            {
                mesaj += ("\nAdres Ailesi: " + add.Address.Family);
                if (add.Address != null)
                    mesaj += ("\nAdres: " + add.Address);
                if (add.Netmask != null)
                    mesaj += ("\nNetmask: " + add.Netmask);
                if (add.Broadcast != null)
                    mesaj += ("\nGenel Yayın: " + add.Broadcast);
                if (add.Destination != null)
                    mesaj += ("\nVarış: " + add.Destination);
            }
            MessageBox.Show(mesaj);
        }
        private void kapat_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
