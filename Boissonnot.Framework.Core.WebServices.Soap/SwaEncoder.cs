using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Channels;
using System.Diagnostics;
using System.ServiceModel;
using System.IO;
using System.Xml;
using System.ServiceModel.Web;
using Boissonnot.Framework.Core.WebServices.Soap.Mime;

namespace Boissonnot.Framework.Core.WebServices.Soap.SoapWithAttachments
{
    public class SwaEncoder : MessageEncoder
    {
        #region Fields
        private string _contentType;
        private string _mediaType;

        protected MimeContent _myContent;
        protected MimePart _soapMimeContent;
        protected MimePart _attachmentMimeContent;

        protected readonly MimeParser _mimeParser;
        protected readonly SwaEncoderFactory _factory;
        protected readonly MessageEncoder _innerEncoder;
        #endregion

        #region Constructors
        public SwaEncoder(MessageEncoder innerEncoder, SwaEncoderFactory factory)
        {
            //
            // Initialize general fields
            //
            _contentType = "multipart/related";
            _mediaType = _contentType;

            //
            // Create owned objects
            //
            _factory = factory;
            _innerEncoder = innerEncoder;
            _mimeParser = new MimeParser();

            //
            // Create object for the mime content message
            // 
            _soapMimeContent = new MimePart()
            {
                ContentType = "text/xml",
                ContentId = "<EFD659EE6BD5F31EA7BC0D59403AF049>",   // TODO: make content id dynamic or configurable?
                CharSet = "UTF-8",                                  // TODO: make charset configurable?
                TransferEncoding = "binary"                         // TODO: make transfer-encoding configurable?
            };
            _attachmentMimeContent = new MimePart()
            {
                ContentType = "application/zip",                    // TODO: AttachmentMimeContent.ContentType configurable?
                ContentId = "<UZE_26123_>",                         // TODO: AttachmentMimeContent.ContentId configurable/dynamic?
                TransferEncoding = "binary"                         // TODO: AttachmentMimeContent.TransferEncoding dynamic/configurable?
            };
            _myContent = new MimeContent()
            {
                Boundary = "------=_Part_0_21714745.1249640163820"  // TODO: MimeContent.Boundary configurable/dynamic?
            };
            _myContent.Parts.Add(_soapMimeContent);
            _myContent.Parts.Add(_attachmentMimeContent);
            _myContent.SetAsStartPart(_soapMimeContent);
        }
        #endregion

        #region Properties
        public override string ContentType
        {
            get
            {
                VerifyOperationContext();

                if (OperationContext.Current.OutgoingMessageProperties.ContainsKey(SwaEncoderConstants.ATTACHMENT_PROPERTY))
                    return _myContent.ContentType;
                else
                    return _innerEncoder.ContentType;
            }
        }

        public override string MediaType
        {
            get { return _mediaType; }
        }

        public override MessageVersion MessageVersion
        {
            get { return MessageVersion.Soap11; }
        }
        #endregion

        #region Public methods
        public override bool IsContentTypeSupported(string contentType)
        {
            if (contentType.ToLower().StartsWith("multipart/related"))
                return true;
            else if (contentType.ToLower().StartsWith("text/xml"))
                return true;
            else
                return false;
        }

        public override Message ReadMessage(ArraySegment<byte> buffer, BufferManager bufferManager, string contentType)
        {
            VerifyOperationContext();

            //
            // Verify the content type
            //
            byte[] msgContents = new byte[buffer.Count];
            Array.Copy(buffer.Array, buffer.Offset, msgContents, 0, msgContents.Length);
            bufferManager.ReturnBuffer(buffer.Array);

            string contents = Encoding.UTF8.GetString(msgContents);

            // Debug code
#if DEBUG
            Debug.WriteLine("-------------------");
            Debug.WriteLine(contents);
            Debug.WriteLine("-------------------");
#endif

            MemoryStream ms = new MemoryStream(msgContents);
            Message message = ReadMessage(ms, int.MaxValue, contentType);

            message.Properties.Add(SwaEncoderConstants.CONTENTASSTRING_PROPERTY, contents);

            return message;
        }

        public override Message ReadMessage(System.IO.Stream stream, int maxSizeOfHeaders, string contentType)
        {
            Message message = Message.CreateMessage(MessageVersion, string.Empty);

            VerifyOperationContext();

            if (contentType.ToLower().StartsWith("multipart/related"))
            {
                byte[] contentBytes = new byte[stream.Length];
                stream.Read(contentBytes, 0, contentBytes.Length);
                MimeContent content = _mimeParser.DeserializeMimeContent(contentType, contentBytes);

                if (content.Parts.Count >= 1)
                {
                    MemoryStream ms = new MemoryStream(content.Parts[0].Content);
                    Message msg = ReadMessage(ms, int.MaxValue, content.Parts[0].ContentType);
                    msg.Properties.Add(SwaEncoderConstants.ATTACHMENT_PROPERTY, content.Parts[0].Content);
                    message = msg;
                }
            }
            else if (contentType.ToLower().StartsWith("text/xml"))
            {
                XmlReader reader = XmlReader.Create(stream);
                message = Message.CreateMessage(reader, maxSizeOfHeaders, MessageVersion);
            }
            else if (contentType.ToLower().StartsWith("application/octet-stream"))
            {
                message = new MessageOctetStreamMessage()
                {
                    OctetStream = stream
                };
            }
            else
            {
                throw new ApplicationException(
                    string.Format(
                        "Invalid content type for reading message: {0}! Supported content types are multipart/related and text/xml!",
                        contentType));
            }

            return message;
        }

        public override void WriteMessage(Message message, System.IO.Stream stream)
        {
            VerifyOperationContext();

            message.Properties.Encoder = this._innerEncoder;

            byte[] Attachment = null;
            if (OperationContext.Current.OutgoingMessageProperties.ContainsKey(SwaEncoderConstants.ATTACHMENT_PROPERTY))
                Attachment = (byte[])OperationContext.Current.OutgoingMessageProperties[SwaEncoderConstants.ATTACHMENT_PROPERTY];

            if (Attachment == null)
            {
                _innerEncoder.WriteMessage(message, stream);
            }
            else
            {
                // Associate the contents to the mime-part
                _soapMimeContent.Content = Encoding.UTF8.GetBytes(message.GetBody<string>());
                _attachmentMimeContent.Content = (byte[])OperationContext.Current.OutgoingMessageProperties[SwaEncoderConstants.ATTACHMENT_PROPERTY];

                // Now create the message content for the stream
                _mimeParser.SerializeMimeContent(_myContent, stream);
            }
        }

        public override ArraySegment<byte> WriteMessage(Message message, int maxMessageSize, BufferManager bufferManager, int messageOffset)
        {
            VerifyOperationContext();

            message.Properties.Encoder = this._innerEncoder;

            byte[] Attachment = null;
            if (OperationContext.Current.OutgoingMessageProperties.ContainsKey(SwaEncoderConstants.ATTACHMENT_PROPERTY))
                Attachment = (byte[])OperationContext.Current.OutgoingMessageProperties[SwaEncoderConstants.ATTACHMENT_PROPERTY];

            if (Attachment == null)
            {
                return _innerEncoder.WriteMessage(message, maxMessageSize, bufferManager, messageOffset);
            }
            else
            {
                // Associate the contents to the mime-part
                _soapMimeContent.Content = Encoding.UTF8.GetBytes(message.ToString());
                _attachmentMimeContent.Content = (byte[])OperationContext.Current.OutgoingMessageProperties[SwaEncoderConstants.ATTACHMENT_PROPERTY];

                // Now create the message content for the stream
                byte[] MimeContentBytes = _mimeParser.SerializeMimeContent(_myContent);
                int MimeContentLength = MimeContentBytes.Length;

                // Write the mime content into the section of the buffer passed into the method
                byte[] TargetBuffer = bufferManager.TakeBuffer(MimeContentLength + messageOffset);
                Array.Copy(MimeContentBytes, 0, TargetBuffer, messageOffset, MimeContentLength);

                // Return the segment of the buffer to the framework
                return new ArraySegment<byte>(TargetBuffer, messageOffset, MimeContentLength);
            }
        }

        private void VerifyOperationContext()
        {
            if (OperationContext.Current == null)
            {
                throw new ApplicationException
                (
                    "No current OperationContext available! On clients please use OperationScope as follows to establish " +
                    "an operation context: " + Environment.NewLine + Environment.NewLine +
                    "using(OperationScope Scope = new OperationScope(YourProxy.InnerChannel) { YouProxy.MethodCall(...); }"
                );
            }
            else if (OperationContext.Current.OutgoingMessageProperties.ContainsKey(SwaEncoderConstants.ATTACHMENT_PROPERTY))
            {
                if (OperationContext.Current.OutgoingMessageProperties[SwaEncoderConstants.ATTACHMENT_PROPERTY] != null)
                {
                    if (!(OperationContext.Current.OutgoingMessageProperties[SwaEncoderConstants.ATTACHMENT_PROPERTY] is byte[]))
                    {
                        throw new ArgumentException(string.Format(
                            "OperationContext.Current.OutgoingMessageProperties[\"{0}\"] needs to be a byte[] array with the attachment content!",
                                SwaEncoderConstants.ATTACHMENT_PROPERTY));
                    }
                }
            }
        }
        #endregion
    }
}
