using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Boissonnot.Framework.Core.WebServices.Soap.SoapWithAttachments
{
    public static class SwaEncoderConstants
    {
        /// <summary>
        /// Constante pour avoir le contenu stream sérialisé
        /// </summary>
        public const string ATTACHMENT_PROPERTY = "swa_encoder_soap_attachment";

        /// <summary>
        /// Constante pour avoir le contenu en string (le xml du soap)
        /// </summary>
        public const string CONTENTASSTRING_PROPERTY = "sw_encoder_soap_asstring";
    }
}
