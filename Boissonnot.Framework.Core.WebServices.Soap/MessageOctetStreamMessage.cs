using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Boissonnot.Framework.Core.WebServices.Soap.SoapWithAttachments
{
    public class MessageOctetStreamMessage : Message
    {
        #region Fields
        private Message _internalMessage = null;
        #endregion

        #region Constructors
        public MessageOctetStreamMessage()
        {
            this._internalMessage = Message.CreateMessage(MessageVersion.Default, "");
        }
        #endregion

        #region Internal methods
        protected override void OnWriteBodyContents(XmlDictionaryWriter writer)
        {
        }
        #endregion

        #region Properties
        public override MessageHeaders Headers => this._internalMessage.Headers;

        public override MessageProperties Properties => this._internalMessage.Properties;

        public override MessageVersion Version => MessageVersion.Default;

        /// <summary>
        /// Stream reçu en réponse
        /// </summary>
        public System.IO.Stream OctetStream { get; set; }
        #endregion

    }
}
