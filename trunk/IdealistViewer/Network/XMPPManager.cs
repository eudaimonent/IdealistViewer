using System;
using System.Collections.Generic;
using System.Text;
using bedrock.util;
using jabber;
using jabber.client;
using jabber.protocol.client;
using jabber.protocol.iq;
using jabber.connection;

namespace IdealistViewer.Network
{
    /// <summary>
    /// XMPPManager handles group and private chat using XMPP services.
    /// </summary>
    public class XMPPManager
    {
        string m_jid;
        bool m_connected;
        private JabberClient m_server;
        private frmCommunications CommWindow;
        private bool m_untrustedOK = true;
        private bool register = false;
        private string certificateFile = null;

        public XMPPManager(string server, string jid, string password)
        {
            m_server = new JabberClient();
            m_server.OnError +=new bedrock.ExceptionHandler(jc_OnError);
            m_server.OnStreamError += new jabber.protocol.ProtocolHandler(jc_OnStreamError);
            m_server.OnMessage += new MessageHandler(OnMessage);
            m_server.OnIQ +=new IQHandler(OnIQ);

            m_server.AutoReconnect = 3f;

            if (m_untrustedOK)
                m_server.OnInvalidCertificate +=
                    new System.Net.Security.RemoteCertificateValidationCallback(jc_OnInvalidCertificate);

            JID j = new JID(jid);
            m_server.User = j.User;
            m_server.Server = j.Server;
            m_server.NetworkHost = server;
            m_server.Port = 5222;
            m_server.Resource = "Idealist";
            m_server.Password = password;
            m_server.AutoStartTLS = true;
            m_server.AutoPresence = true;
/*
            if (certificateFile != null)
            {
                m_server.SetCertificateFile(certificateFile, certificatePass);
                Console.WriteLine(m_server.LocalCertificate.ToString(true));
            }

            if (boshURL != null)
            {
                jc[Options.POLL_URL] = boshURL;
                jc[Options.CONNECTION_TYPE] = ConnectionType.HTTP_Binding;
            }
            
            if (register)
            {
                m_server.AutoLogin = false;
                m_server.OnLoginRequired +=
                    new bedrock.ObjectHandler(jc_OnLoginRequired);
                m_server.OnRegisterInfo += new RegisterInfoHandler(this.jc_OnRegisterInfo);
                m_server.OnRegistered += new IQHandler(jc_OnRegistered);
            }

            CapsManager cm = new CapsManager();
            cm.Stream = m_server;
            cm.Node = "http://cursive.net/clients/ConsoleClient";
 */
        }

        public void Connect( frmCommunications window )
        {
            CommWindow = window;
            System.Console.WriteLine("XMPP Connecting to "+ m_server.NetworkHost);
            m_server.Connect();
            System.Console.WriteLine("XMPP Connected");
       }

        /// <summary>
        /// Process response to an Information Query
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="iq"></param>
        public void OnIQ(object sender, IQ iq)
        {
            switch (iq.Type)
            {
                case IQType.result: // Other end has an answer for us
                    break;
                case IQType.get:    // Other end wants something
                    break;
                case IQType.set:    // Other end is setting something
                    break;
                case IQType.error:  // Other end did not like that.
                    break;
            }

        }

        bool jc_OnInvalidCertificate(object sender,
            System.Security.Cryptography.X509Certificates.X509Certificate certificate,
            System.Security.Cryptography.X509Certificates.X509Chain chain,
            System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            System.Console.WriteLine("Invalid certificate ({0}):\n{1}", sslPolicyErrors.ToString(), certificate.ToString(true));
            return true;
        }

        /// <summary>
        /// Process received XMPP messages.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="msg"></param>
        private void OnMessage(object sender, Message msg)
        {
            CommWindow.XMPP_OnInstantMessage(msg);
        }

        private void jc_OnWriteText(object sender, string txt)
        {
            if (txt != " ")
                System.Console.WriteLine("XMPP SENT: " + txt);
        }

        private void jc_OnError(object sender, Exception ex)
        {
            System.Console.WriteLine("XMPP ERROR: " + ex.ToString());
            Environment.Exit(1);
        }

        private void jc_OnStreamError(object sender, System.Xml.XmlElement rp)
        {
            System.Console.WriteLine("Stream ERROR: " + rp.OuterXml);
            Environment.Exit(1);
        }

        private void jc_OnLoginRequired(object sender)
        {
            System.Console.WriteLine("Registering");
            JabberClient jc = (JabberClient) sender;
            m_server.Register(new JID(m_server.User, m_server.Server, null));
        }

        private void jc_OnRegistered(object sender, IQ iq)
        {
            JabberClient jc = (JabberClient) sender;
            if (iq.Type == IQType.result)
                m_server.Login();
        }

        private bool jc_OnRegisterInfo(object sender, Register r)
        {
            return true;
        }

        private int IDCounter = 0;
        private string NextID()
        {
            IDCounter++;
            return "QID" + IDCounter.ToString();
        }
    }

}
